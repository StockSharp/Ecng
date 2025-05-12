namespace Ecng.Compilation.Roslyn;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Compilation;
using Ecng.Localization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic;

/// <summary>
/// Represents a Roslyn compiler.
/// </summary>
/// <param name="extension">The file extension of the source files that the compiler supports.</param>
public abstract class RoslynCompiler(string extension) : ICompiler
{
	private static readonly Dictionary<string, string> _redirects = new()
	{
		{ "System.Collections.Immutable, Version=1.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Collections.Immutable.dll"},
		{ "System.IO.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.IO.FileSystem.dll"},
		//{ "System.IO.FileSystem, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.IO.FileSystem.dll"},
		//{ "System.Security.Cryptography.Primitives, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Security.Cryptography.Primitives.dll"},
	};

	static RoslynCompiler()
	{
		AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
		{
			if (_redirects.ContainsKey(args.Name))
			{
				var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _redirects[args.Name]);
				return Assembly.LoadFrom(path);
			}

			return null;
		};
	}

	bool ICompiler.IsAssemblyPersistable { get; } = true;
	string ICompiler.Extension { get; } = extension;

	/// <inheritdoc />
	public abstract bool IsTabsSupported { get; }
	/// <inheritdoc />
	public abstract bool IsCaseSensitive { get; }
	/// <inheritdoc />
	public abstract bool IsReferencesSupported { get; }

	private Compilation Create(string name, IEnumerable<string> sources, IEnumerable<(string name, byte[] body)> refs, CancellationToken cancellationToken)
	{
		if (sources is null)
			throw new ArgumentNullException(nameof(sources));

		if (refs is null)
			throw new ArgumentNullException(nameof(refs));

		var assemblyName = name + Path.GetRandomFileName();

		var references = refs.Select(r => MetadataReference.CreateFromImage(r.body)).ToArray();

		return Create(assemblyName, sources, references, cancellationToken);
	}

	/// <summary>
	/// Creates a new compilation instance.
	/// </summary>
	/// <param name="assemblyName">The name of the assembly.</param>
	/// <param name="sources">The source code files as strings.</param>
	/// <param name="references">A collection of references.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Compilation"/></returns>
	protected abstract Compilation Create(
		string assemblyName,
		IEnumerable<string> sources,
		PortableExecutableReference[] references,
		CancellationToken cancellationToken);

	async Task<CompilationError[]> ICompiler.Analyse(object analyzer, IEnumerable<object> analyzerSettings, string name, IEnumerable<string> sources, IEnumerable<(string name, byte[] body)> refs, CancellationToken cancellationToken)
	{
		if (analyzer is null)
			throw new ArgumentNullException(nameof(analyzer));

		if (analyzerSettings is null)
			throw new ArgumentNullException(nameof(analyzerSettings));

		var compilation = Create(name, sources, refs, cancellationToken);

		var compilationWithAnalyzers = compilation.WithAnalyzers(
			[(DiagnosticAnalyzer)analyzer],
			new AnalyzerOptions([.. analyzerSettings.Cast<AdditionalText>()]));

		static CompilationErrorTypes ToType(DiagnosticSeverity severity)
			=> severity switch
			{
				DiagnosticSeverity.Info => CompilationErrorTypes.Info,
				DiagnosticSeverity.Warning => CompilationErrorTypes.Warning,
				DiagnosticSeverity.Error => CompilationErrorTypes.Error,
				_ => throw new ArgumentOutOfRangeException(severity.To<string>()),
			};

		var analyzerDiagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync(cancellationToken).NoWait();
		return [.. analyzerDiagnostics.Select(e =>
		{
			var lineSpan = e.Location.GetLineSpan();

			return new CompilationError
			{
				Type = ToType(e.Severity),
				Message = e.GetMessage(),
				Line = lineSpan.StartLinePosition.Line,
				Character = lineSpan.StartLinePosition.Character,
				Id = e.Id,
			};
		}).Distinct()];
	}

	Task<CompilationResult> ICompiler.Compile(string name, IEnumerable<string> sources, IEnumerable<(string name, byte[] body)> refs, CancellationToken cancellationToken)
	{
		var compilation = Create(name, sources, refs, cancellationToken);

		using var ms = new MemoryStream();

		byte[] getBody()
		{
			ms.Seek(0, SeekOrigin.Begin);
			return ms.To<byte[]>();
		}

		var result = compilation.Emit(ms, cancellationToken: cancellationToken);

		var compilationResult = new AssemblyCompilationResult(result.Diagnostics.Select(diagnostic =>
		{
			var pos = diagnostic.Location.GetLineSpan().StartLinePosition;

			var error = new CompilationError
			{
				Id = diagnostic.Id,
				Line = pos.Line,
				Character = pos.Character,
				Message = diagnostic.GetMessage(),
				Type = diagnostic.Severity switch
				{
					DiagnosticSeverity.Hidden or DiagnosticSeverity.Info => CompilationErrorTypes.Info,
					DiagnosticSeverity.Warning => CompilationErrorTypes.Warning,
					DiagnosticSeverity.Error => CompilationErrorTypes.Error,
					_ => throw new ArgumentOutOfRangeException(nameof(diagnostic), diagnostic.Severity, "Invalid value.".Localize()),
				}
			};

			return error;
		}), result.Success ? getBody() : null);

		return ((CompilationResult)compilationResult).FromResult();
	}

	ICompilerContext ICompiler.CreateContext() => new AssemblyLoadContextTracker();
}

/// <summary>
/// Represents a C# compiler that supports compiling source files into an assembly.
/// </summary>
public class CSharpCompiler : RoslynCompiler
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CSharpCompiler"/> class.
	/// </summary>
	public CSharpCompiler()
		: base(FileExts.CSharp)
	{
	}

	/// <inheritdoc />
	public override bool IsTabsSupported => true;
	/// <inheritdoc />
	public override bool IsCaseSensitive => true;
	/// <inheritdoc />
	public override bool IsReferencesSupported => true;

	/// <inheritdoc />
	protected override Compilation Create(string assemblyName, IEnumerable<string> sources, PortableExecutableReference[] references, CancellationToken cancellationToken)
		=> CSharpCompilation.Create(
			assemblyName,
			sources.Select(source => CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken)),
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);
}

/// <summary>
/// Represents a Visual Basic compiler that supports compiling source files into an assembly.
/// </summary>
public class VisualBasicCompiler : RoslynCompiler
{
	/// <summary>
	/// Initializes a new instance of the <see cref="VisualBasicCompiler"/> class.
	/// </summary>
	public VisualBasicCompiler()
		: base(FileExts.VisualBasic)
	{
	}

	/// <inheritdoc />
	public override bool IsTabsSupported => true;
	/// <inheritdoc />
	public override bool IsCaseSensitive => false;
	/// <inheritdoc />
	public override bool IsReferencesSupported => true;

	/// <inheritdoc />
	protected override Compilation Create(string assemblyName, IEnumerable<string> sources, PortableExecutableReference[] references, CancellationToken cancellationToken)
		=> VisualBasicCompilation.Create(
			assemblyName,
			sources.Select(source => VisualBasicSyntaxTree.ParseText(source, cancellationToken: cancellationToken)),
			references,
			new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);
}