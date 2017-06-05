namespace Ecng.Roslyn
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;

	using Ecng.Common;

	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.VisualBasic;

	public sealed class Compiler
	{
		private Compiler(CompilationLanguages language/*, string outputDir, string tempPath*/)
		{
			Language = language;
			//OutputDir = outputDir;
			//TempPath = tempPath;
		}

		public CompilationLanguages Language { get; }
		//public string OutputDir { get; }
		//public string TempPath { get; }

		public static Compiler Create(CompilationLanguages language)
		{
			return new Compiler(language);
		}

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

			using (var ms = new MemoryStream())
			{
				var result = compilation.Emit(ms);

				compilationResult.Errors = result.Diagnostics.Select(diagnostic =>
				{
					var pos = diagnostic.Location.GetLineSpan().StartLinePosition;

					var error = new CompilationError
					{
						Id = diagnostic.Id,
						Line = pos.Line,
						Character = pos.Character,
						Message = diagnostic.GetMessage()
					};

					switch (diagnostic.Severity)
					{
						case DiagnosticSeverity.Hidden:
						case DiagnosticSeverity.Info:
							error.Type = CompilationErrorTypes.Info;
							break;
						case DiagnosticSeverity.Warning:
							error.Type = CompilationErrorTypes.Warning;
							break;
						case DiagnosticSeverity.Error:
							error.Type = CompilationErrorTypes.Error;
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					return error;
				}).ToArray();

				if (result.Success)
				{
					ms.Seek(0, SeekOrigin.Begin);
					compilationResult.Assembly = Assembly.Load(ms.ToArray());
				}
			}

			return compilationResult;
		}
	}
}