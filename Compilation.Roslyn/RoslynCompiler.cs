namespace Ecng.Compilation.Roslyn
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Threading;

	using Ecng.Compilation;

	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
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

		CompilationResult ICompiler.Compile(AssemblyLoadContextVisitor context, string name, IEnumerable<string> sources, IEnumerable<string> refs, CancellationToken cancellationToken)
		{
			if (context is null)
				throw new ArgumentNullException(nameof(context));

			var assemblyName = name + Path.GetRandomFileName();

			var references = refs.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

			Compilation compilation;

			switch (Language)
			{
				case CompilationLanguages.CSharp:
				{
					compilation = CSharpCompilation.Create(assemblyName,
						sources.Select(source => CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken)),
						references,
						new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

					break;
				}
				case CompilationLanguages.VisualBasic:
				{
					compilation = VisualBasicCompilation.Create(assemblyName,
						sources.Select(source => VisualBasicSyntaxTree.ParseText(source, cancellationToken: cancellationToken)),
						references,
						new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

					break;
				}
				default:
					throw new InvalidOperationException();
			}

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
				compilationResult.Assembly = context.LoadFromStream(ms);
			}

			return compilationResult;
		}
	}
}