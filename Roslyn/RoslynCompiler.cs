namespace Ecng.Roslyn
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;

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

		public RoslynCompiler(CompilationLanguages language)
		{
			Language = language;
		}

		public CompilationLanguages Language { get; }

		public CompilationResult Compile(string name, string body, IEnumerable<string> refs)
		{
			var assemblyName = name + Path.GetRandomFileName();

			var references = refs.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

			//var providerOptions = new Dictionary<string, string>
			//{
			//	{ "CompilerVersion", "v4.0" }
			//};

			Compilation compilation;

			switch (Language)
			{
				case CompilationLanguages.CSharp:
				{
					compilation = CSharpCompilation.Create(assemblyName,
						new[] { CSharpSyntaxTree.ParseText(body) },
						references,
						new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

					break;
				}
				case CompilationLanguages.VisualBasic:
				{
					compilation = VisualBasicCompilation.Create(assemblyName,
						new[] { VisualBasicSyntaxTree.ParseText(body) },
						references,
						new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

					break;
				}
				//case CompilationLanguages.Java:
				//	provider = CodeDomProvider.CreateProvider("VJSharp");
				//	break;
				//case CompilationLanguages.JScript:
				//	provider = CodeDomProvider.CreateProvider("JScript");
				//	break;
				//case CompilationLanguages.Cpp:
				//	provider = CodeDomProvider.CreateProvider("Cpp");
				//	break;
				default:
					throw new InvalidOperationException();
			}

			var compilationResult = new CompilationResult();

			using var ms = new MemoryStream();

			var result = compilation.Emit(ms);

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
						_ => throw new ArgumentOutOfRangeException(),
					}
				};

				return error;
			}).ToArray();

			if (result.Success)
			{
				ms.Seek(0, SeekOrigin.Begin);
				compilationResult.Assembly = Assembly.Load(ms.ToArray());
			}

			return compilationResult;
		}
	}
}