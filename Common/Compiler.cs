namespace Ecng.Common
{
	using System;
	using System.CodeDom.Compiler;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;

	using Microsoft.CSharp;
	using Microsoft.VisualBasic;

	public enum CompilationLanguages
	{
		CSharp,
		VisualBasic,
		Java,
		JScript,
		Cpp,
	}

	public enum CompilationErrorTypes
	{
		Info,
		Warning,
		Error,
	}

	public class CompilationError
	{
		internal CompilationError(CompilerError nativeError)
		{
			NativeError = nativeError;

			if (nativeError.ErrorNumber.IsEmpty())
				Type = CompilationErrorTypes.Info;
			else
				Type = nativeError.IsWarning ? CompilationErrorTypes.Warning : CompilationErrorTypes.Error;
		}

		public CompilerError NativeError { get; private set; }
		public CompilationErrorTypes Type { get; private set; }
	}

	public class CompilationResult
	{
		public Assembly Assembly { get; set; }

		public string AssemblyLocation { get; set; }

		public IEnumerable<CompilationError> Errors { get; set; }
	}

	public sealed class Compiler
	{
		private Compiler(CompilationLanguages language, string outputDir, string tempPath)
		{
			Language = language;
			OutputDir = outputDir;
			TempPath = tempPath;
		}

		public CompilationLanguages Language { get; }
		public string OutputDir { get; }
		public string TempPath { get; }

		public static Compiler Create(CompilationLanguages language, string outputDir, string tempPath)
		{
			return new Compiler(language, outputDir, tempPath);
		}

		public CompilationResult Compile(string name, string body, IEnumerable<string> refs)
		{
			var providerOptions = new Dictionary<string, string> { { "CompilerVersion", "v4.0" } };

			CodeDomProvider provider;

			switch (Language)
			{
				case CompilationLanguages.CSharp:
					provider = new CSharpCodeProvider(providerOptions);
					break;
				case CompilationLanguages.VisualBasic:
					provider = new VBCodeProvider(providerOptions);
					break;
				case CompilationLanguages.Java:
					provider = CodeDomProvider.CreateProvider("VJSharp");
					break;
				case CompilationLanguages.JScript:
					provider = CodeDomProvider.CreateProvider("JScript");
					break;
				case CompilationLanguages.Cpp:
					provider = CodeDomProvider.CreateProvider("Cpp");
					break;
				default:
					throw new InvalidOperationException();
			}

			var result = provider.CompileAssemblyFromSource(new CompilerParameters(refs.ToArray())
			{
				OutputAssembly = Path.Combine(OutputDir, name + Guid.NewGuid() + ".dll"),
				CompilerOptions = "/t:library",
				//GenerateInMemory = true,
				GenerateExecutable = false,
				IncludeDebugInformation = true,
				TempFiles = new TempFileCollection(TempPath),
			}, body);

			var compilationResult = new CompilationResult
			{
				Assembly = result.Errors.HasErrors ? null : result.CompiledAssembly,
				AssemblyLocation = result.Errors.HasErrors ? String.Empty : result.PathToAssembly,
				Errors = result.Errors.Cast<CompilerError>().Select(e => new CompilationError(e)).ToArray(),
			};

			return compilationResult;
		}
	}
}