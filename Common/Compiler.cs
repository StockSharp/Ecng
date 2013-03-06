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

	public enum Languages
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
		private Compiler(Languages language, string outputDir, string tempPath)
		{
			Language = language;
			OutputDir = outputDir;
			TempPath = tempPath;
		}

		public Languages Language { get; private set; }
		public string OutputDir { get; private set; }
		public string TempPath { get; private set; }

		public static Compiler Create(Languages language, string outputDir, string tempPath)
		{
			return new Compiler(language, outputDir, tempPath);
		}

		public CompilationResult Compile(string name, string body, IEnumerable<string> refs)
		{
			var providerOptions = new Dictionary<string, string> { { "CompilerVersion", "v4.0" } };

			CodeDomProvider provider;

			switch (Language)
			{
				case Languages.CSharp:
					provider = new CSharpCodeProvider(providerOptions);
					break;
				case Languages.VisualBasic:
					provider = new VBCodeProvider(providerOptions);
					break;
				case Languages.Java:
					provider = CodeDomProvider.CreateProvider("VJSharp");
					break;
				case Languages.JScript:
					provider = CodeDomProvider.CreateProvider("JScript");
					break;
				case Languages.Cpp:
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