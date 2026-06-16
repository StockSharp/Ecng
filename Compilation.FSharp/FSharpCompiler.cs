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

using global::FSharp.Compiler.Text;
using global::FSharp.Compiler.CodeAnalysis;
using global::FSharp.Compiler.Diagnostics;
using global::FSharp.Compiler.IO;

/// <summary>
/// F# compiler.
/// </summary>
public class FSharpCompiler : ICompiler
{
	private sealed record FileSystemScope(Dictionary<string, byte[]> Input, Stream Output);

	// Per-execution-flow file set. The single global FCS file-system shim (installed once below)
	// routes reads/writes to the scope active on the current async flow, so concurrent compilations
	// stay isolated without a process-wide lock - each Compile/Analyse runs on its own flow, and the
	// shim falls back to the real disk when no scope is active.
	private static readonly AsyncLocal<FileSystemScope> _scope = new();

	private sealed class RoutingFileSystem : DefaultFileSystem
	{
		public override bool FileExistsShim(string fileName)
		{
			var scope = _scope.Value;

			if (scope is not null && (scope.Input.ContainsKey(fileName) || scope.Input.ContainsKey(Path.GetFileName(fileName))))
				return true;

			return base.FileExistsShim(fileName);
		}

		public override Stream OpenFileForReadShim(string filePath, FSharpOption<bool> useMemoryMappedFile, FSharpOption<bool> shouldShadowCopy)
		{
			var scope = _scope.Value;

			if (scope is not null && (scope.Input.TryGetValue(filePath, out var body) || scope.Input.TryGetValue(Path.GetFileName(filePath), out body)))
				return new MemoryStream(body);

			return base.OpenFileForReadShim(filePath, useMemoryMappedFile, shouldShadowCopy);
		}

		public override Stream OpenFileForWriteShim(string filePath,
			FSharpOption<FileMode> fileMode,
			FSharpOption<FileAccess> fileAccess,
			FSharpOption<FileShare> fileShare)
		{
			var scope = _scope.Value;

			if (scope?.Output is not null)
				return scope.Output;

			return base.OpenFileForWriteShim(filePath, fileMode, fileAccess, fileShare);
		}
	}

	private sealed class FileSystemContext : Disposable
	{
		public FileSystemContext(Dictionary<string, byte[]> input, Stream output)
			=> _scope.Value = new(input, output);

		protected override void DisposeManaged()
		{
			_scope.Value = null;
			base.DisposeManaged();
		}
	}

	static FSharpCompiler()
	{
		FileSystemAutoOpens.FileSystem = new RoutingFileSystem();
	}

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

	// Shared-framework runtime BCL assemblies for the running runtime, file name -> full path on disk.
	// They are referenced straight from disk (not staged into the in-memory file system) and under
	// their real assembly names, so the compiler follows netstandard 2.1's type forwards by name and
	// the async types (IAsyncDisposable / IAsyncEnumerable, used by the task { } builder) resolve.
	// Using the runtime framework (not the reference pack) keeps the BCL identity the same as caller
	// libraries, which are built against the runtime, so the task builder's awaiter constraints unify.
	// mscorlib is excluded: it is a forward-only facade the compiler would otherwise adopt as the
	// primary System provider, breaking System.Array/Object resolution.
	private static readonly Lazy<Dictionary<string, string>> _frameworkBcl = new(LoadFrameworkBcl);

	private static Dictionary<string, string> LoadFrameworkBcl()
	{
		var map = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

		try
		{
			var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);

			if (runtimeDir.IsEmpty())
				return map;

			foreach (var dll in Directory.GetFiles(runtimeDir, "*.dll"))
			{
				var fileName = Path.GetFileName(dll);

				if (fileName.EqualsIgnoreCase("mscorlib.dll"))
					continue;

				try
				{
					// Only managed assemblies are valid references — skip native images.
					_ = System.Reflection.AssemblyName.GetAssemblyName(dll);
					map[fileName] = dll;
				}
				catch
				{
				}
			}
		}
		catch
		{
			// Best effort — fall back to the caller's references only.
		}

		return map;
	}

	// Splits references into the --reference list and the in-memory bodies the file system must serve.
	// Framework BCL is referenced by its real on-disk path; the caller's own (non-framework) references
	// are kept in memory under a content-hash path (so distinct rebuilds get fresh metadata). Caller
	// references the framework already supplies are dropped to keep one consistent BCL surface.
	private static (Dictionary<string, byte[]> inMemory, List<string> referencePaths) BuildReferences(IEnumerable<(string name, byte[] body)> refs)
	{
		var inMemory = new Dictionary<string, byte[]>(StringComparer.InvariantCultureIgnoreCase);
		var referencePaths = new List<string>();
		var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

		var framework = _frameworkBcl.Value;

		foreach (var (fileName, diskPath) in framework)
		{
			if (seen.Add(fileName))
				referencePaths.Add(diskPath);
		}

		foreach (var reference in refs)
		{
			var fileName = Path.GetFileName(reference.name);

			if (framework.Count > 0 && framework.ContainsKey(fileName))
				continue;

			if (!seen.Add(fileName))
				continue;

			var path = GetReferencePath(reference);
			inMemory[path] = reference.body;
			referencePaths.Add(path);
		}

		return (inMemory, referencePaths);
	}

	private static string[] CreateOptions(string name, IEnumerable<string> sources, IEnumerable<string> refs)
	{
		if (sources == null)	throw new ArgumentNullException(nameof(sources));
		if (refs == null)		throw new ArgumentNullException(nameof(refs));

		var referencePaths = refs.Select(r => $"--reference:{r}").ToList();
		var profile = _frameworkBcl.Value.Count > 0 ? "--targetprofile:netcore" : null;

		return
		[
			"--target:library",
			$"--out:{name}.dll",
			"--nologo",
			"--noframework",
			// A library needs no win32 manifest; the default one ships with the SDK, not the runtime,
			// so skip it to avoid a "default.win32manifest not found" failure.
			"--nowin32manifest",
			.. profile is null ? Array.Empty<string>() : [profile],
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
		var (inMemoryRefs, referencePaths) = BuildReferences(refs);
		var projectDict = new Dictionary<string, byte[]>(inMemoryRefs, StringComparer.InvariantCultureIgnoreCase);
		projectDict.AddRange(sourcesDict);

		var options = CreateOptions(name, sourceNames, referencePaths);
		var projectOptions = _checker.GetProjectOptionsFromCommandLineArgs(name, options, default, default, default);

		var diagnostics = new List<CompilationError>();

		using var __ = new FileSystemContext(projectDict, new MemoryStream());

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

		var (inMemoryRefs, referencePaths) = BuildReferences(refs);
		var projectOptions = CreateOptions(name, sourcesDict.Keys, referencePaths);

		var projectDict = new Dictionary<string, byte[]>(StringComparer.InvariantCultureIgnoreCase);
		projectDict.AddRange(sourcesDict);
		projectDict.AddRange(inMemoryRefs);

		using var stream = new MemoryStream();
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
