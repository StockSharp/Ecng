namespace Ecng.Compilation.FSharp;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Security;

using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

using Nito.AsyncEx;

using global::FSharp.Compiler.Text;
using global::FSharp.Compiler.CodeAnalysis;
using global::FSharp.Compiler.Diagnostics;
using global::FSharp.Compiler.IO;

/// <summary>
/// F# compiler.
/// </summary>
public class FSharpCompiler : ICompiler
{
	private class FileSystemContext : Disposable
	{
		private class InMemoryFileSystem(Dictionary<string, byte[]> input, Stream output) : DefaultFileSystem
		{
			private readonly Dictionary<string, byte[]> _input = input ?? throw new ArgumentNullException(nameof(input));
			private readonly Stream _output = output ?? throw new ArgumentNullException(nameof(output));

			public override bool FileExistsShim(string fileName)
			{
				if (_input.ContainsKey(fileName) || _input.ContainsKey(Path.GetFileName(fileName)))
					return true;

				return base.FileExistsShim(fileName);
			}

			public override Stream OpenFileForReadShim(string filePath, FSharpOption<bool> useMemoryMappedFile, FSharpOption<bool> shouldShadowCopy)
			{
				if (_input.TryGetValue(filePath, out var body) || _input.TryGetValue(Path.GetFileName(filePath), out body))
					return new MemoryStream(body);

				return base.OpenFileForReadShim(filePath, useMemoryMappedFile, shouldShadowCopy);
			}

			public override Stream OpenFileForWriteShim(string filePath,
				FSharpOption<FileMode> fileMode,
				FSharpOption<FileAccess> fileAccess,
				FSharpOption<FileShare> fileShare)
			{
				return _output;
			}
		}

		private readonly global::FSharp.Compiler.IO.IFileSystem _prev;

		public FileSystemContext(Dictionary<string, byte[]> input, MemoryStream output)
		{
			_prev = FileSystemAutoOpens.FileSystem;

			FileSystemAutoOpens.FileSystem = new InMemoryFileSystem(input, output);
		}

		protected override void DisposeManaged()
		{
			FileSystemAutoOpens.FileSystem = _prev;
			base.DisposeManaged();
		}
	}

	private static readonly AsyncLock _lock = new();

	bool ICompiler.IsAssemblyPersistable { get; } = true;
	string ICompiler.Extension { get; } = FileExts.FSharp;

	bool ICompiler.IsTabsSupported { get; } = false;
	bool ICompiler.IsCaseSensitive { get; } = true;
	bool ICompiler.IsReferencesSupported { get; } = true;

	private readonly FSharpChecker _checker;

	/// <summary>
	/// Initializes a new instance of the <see cref="FSharpCompiler"/> class.
	/// </summary>
	public FSharpCompiler()
	{
		_checker = FSharpChecker.Create(
			projectCacheSize: null,
			keepAssemblyContents: null,
			keepAllBackgroundResolutions: null,
			keepAllBackgroundSymbolUses: null,
			legacyReferenceResolver: null,
			tryGetMetadataSnapshot: null,
			suggestNamesForErrors: null,
			enableBackgroundItemKeyStoreAndSemanticClassification: null,
			enablePartialTypeChecking: null,
			parallelReferenceResolution: null,
			captureIdentifiersWhenParsing: null,
			documentSource: null,
			transparentCompilerCacheSizes: null,
			useTransparentCompiler: null
		);
	}

	private static string GetReferencePath((string name, byte[] body) reference)
		=> Path.Combine(Path.GetTempPath(), $"{reference.body.Sha512()}_{Path.GetFileName(reference.name)}");

	private static Dictionary<string, byte[]> CreateReferenceMap(IEnumerable<(string name, byte[] body)> refs)
	{
		var retVal = new Dictionary<string, byte[]>(StringComparer.InvariantCultureIgnoreCase);

		foreach (var reference in refs)
			retVal.TryAdd(GetReferencePath(reference), reference.body);

		return retVal;
	}

	private static string[] CreateOptions(string name, IEnumerable<string> sources, IEnumerable<string> refs)
	{
		if (sources == null)	throw new ArgumentNullException(nameof(sources));
		if (refs == null)		throw new ArgumentNullException(nameof(refs));

		var referencePaths = refs.Select(r => $"--reference:{r}").ToList();

		return
		[
			"--target:library",
			$"--out:{name}.dll",
			"--nologo",
			"--target:library",
			"--noframework",
			.. sources,
			.. referencePaths,
		];
	}

	async Task<CompilationError[]> ICompiler.Analyse(object analyzer, IEnumerable<object> analyzerSettings, string name, IEnumerable<string> sources, IEnumerable<(string name, byte[] body)> refs, CancellationToken cancellationToken)
	{
		if (sources == null)
			throw new ArgumentNullException(nameof(sources));

		var index = 0;
		var sourcesArr = sources.ToArray();
		var sourceNames = sourcesArr.Select(_ => $"{name}_{index++}.fs").ToArray();
		var sourcesDict = sourceNames.Zip(sourcesArr).ToDictionary(p => p.First, p => p.Second.UTF8(), StringComparer.InvariantCultureIgnoreCase);
		var referenceDict = CreateReferenceMap(refs);
		var projectDict = new Dictionary<string, byte[]>(referenceDict, StringComparer.InvariantCultureIgnoreCase);
		projectDict.AddRange(sourcesDict);

		var options = CreateOptions(name, sourceNames, referenceDict.Keys);
		var projectOptions = _checker.GetProjectOptionsFromCommandLineArgs(name, options, default, default, default);

		var diagnostics = new List<CompilationError>();

		using var _ = await _lock.LockAsync(cancellationToken);
		using var __ = new FileSystemContext(projectDict, new());

		foreach (var (sourceName, source) in sourceNames.Zip(sourcesArr))
		{
			var sourceText = SourceText.ofString(source);
			var (results, answer) = await FSharpAsync.StartAsTask(_checker.ParseAndCheckFileInProject(sourceName, 0, sourceText, projectOptions, default), default, cancellationToken);

			diagnostics.AddRange(results.Diagnostics.Select(ToError));

			if (answer is FSharpCheckFileAnswer.Succeeded succeeded)
				diagnostics.AddRange(succeeded.Item.Diagnostics.Select(ToError));
		}

		return [.. diagnostics];
	}

	async Task<CompilationResult> ICompiler.Compile(string name, IEnumerable<string> sources, IEnumerable<(string name, byte[] body)> refs, CancellationToken cancellationToken)
	{
		if (sources == null)
			throw new ArgumentNullException(nameof(sources));

		var diagnostics = new List<CompilationError>();

		var index = 0;
		var sourcesDict = sources.ToDictionary(s => $"file{index++}.fs", s => s.UTF8());

		var referenceDict = CreateReferenceMap(refs);
		var projectOptions = CreateOptions(name, sourcesDict.Keys, referenceDict.Keys);

		var projectDict = new Dictionary<string, byte[]>(StringComparer.InvariantCultureIgnoreCase);
		projectDict.AddRange(sourcesDict);
		projectDict.AddRange(referenceDict);

		using var stream = new MemoryStream();
		using var _ = await _lock.LockAsync(cancellationToken);
		using var __ = new FileSystemContext(projectDict, stream);

		var (diagnostic, errorCode) = await FSharpAsync.StartAsTask(_checker.Compile(projectOptions, default), default, cancellationToken);

		foreach (var diag in diagnostic)
		{
			diagnostics.Add(ToError(diag));
		}

		return new AssemblyCompilationResult([.. diagnostics], errorCode == default ? stream.To<byte[]>() : null);
	}

	ICompilerContext ICompiler.CreateContext() => new AssemblyLoadContextTracker();

	private static CompilationError ToError(FSharpDiagnostic diag)
	{
		static CompilationErrorTypes toType(FSharpDiagnosticSeverity severity)
		{
			if (severity == FSharpDiagnosticSeverity.Error)
				return CompilationErrorTypes.Error;
			else if (severity == FSharpDiagnosticSeverity.Warning)
				return CompilationErrorTypes.Warning;
			else
				return CompilationErrorTypes.Info;
		}

		return new()
		{
			Line = Math.Max(0, diag.StartLine - 1),
			Character = diag.StartColumn,
			Message = diag.Message,
			Type = toType(diag.Severity),
		};
	}
}
