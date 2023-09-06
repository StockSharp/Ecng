namespace Ecng.Compilation.Roslyn
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;
	using Ecng.Compilation;

	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.Diagnostics;
	using Microsoft.CodeAnalysis.VisualBasic;

	public class RoslynCompiler : ICompiler
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

		public RoslynCompiler(CompilationLanguages language = CompilationLanguages.CSharp)
		{
			Language = language;
		}

		public CompilationLanguages Language { get; }

		private Compilation Create(string name, IEnumerable<string> sources, IEnumerable<string> refs, CancellationToken cancellationToken)
		{
			if (sources is null)
				throw new ArgumentNullException(nameof(sources));

			var assemblyName = name + Path.GetRandomFileName();

			var references = refs.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

			switch (Language)
			{
				case CompilationLanguages.CSharp:
				{
					return CSharpCompilation.Create(assemblyName,
						sources.Select(source => CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken)),
						references,
						new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
				}
				case CompilationLanguages.VisualBasic:
				{
					return VisualBasicCompilation.Create(assemblyName,
						sources.Select(source => VisualBasicSyntaxTree.ParseText(source, cancellationToken: cancellationToken)),
						references,
						new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
				}
				default:
					throw new InvalidOperationException(Language.ToString());
			}
		}

		async Task<CompilationError[]> ICompiler.Analyse(object analyzer, IEnumerable<object> analyzerSettings, string name, IEnumerable<string> sources, IEnumerable<string> refs, CancellationToken cancellationToken)
		{
			if (analyzer is null)
				throw new ArgumentNullException(nameof(analyzer));

			if (analyzerSettings is null)
				throw new ArgumentNullException(nameof(analyzerSettings));

			var compilation = Create(name, sources, refs, cancellationToken);

			var compilationWithAnalyzers = compilation.WithAnalyzers(
				ImmutableArray.Create((DiagnosticAnalyzer)analyzer),
				new AnalyzerOptions(analyzerSettings.Cast<AdditionalText>().ToImmutableArray()),
				cancellationToken);

			static CompilationErrorTypes ToType(DiagnosticSeverity severity)
				=> severity switch
				{
					DiagnosticSeverity.Info => CompilationErrorTypes.Info,
					DiagnosticSeverity.Warning => CompilationErrorTypes.Warning,
					DiagnosticSeverity.Error => CompilationErrorTypes.Error,
					_ => throw new ArgumentOutOfRangeException(severity.To<string>()),
				};

			var analyzerDiagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync(cancellationToken);
			return analyzerDiagnostics.Select(e =>
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
			}).Distinct().ToArray();
		}

		CompilationResult ICompiler.Compile(string name, IEnumerable<string> sources, IEnumerable<string> refs, CancellationToken cancellationToken)
		{
			if (sources is null)
				throw new ArgumentNullException(nameof(sources));

			var compilation = Create(name, sources, refs, cancellationToken);

			var compilationResult = new CompilationResult();

			using var ms = new MemoryStream();

			var result = compilation.Emit(ms, cancellationToken: cancellationToken);

			compilationResult.Errors = result.Diagnostics.Select(diagnostic =>
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
						_ => throw new ArgumentOutOfRangeException(nameof(diagnostic), diagnostic.Severity, "Invalid value."),
					}
				};

				return error;
			}).ToArray();

			if (result.Success)
			{
				ms.Seek(0, SeekOrigin.Begin);
				compilationResult.Assembly = ms.To<byte[]>();
			}

			return compilationResult;
		}
	}
}