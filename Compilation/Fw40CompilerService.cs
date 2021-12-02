namespace Ecng.Compilation
{
	using System;
	using System.CodeDom.Compiler;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Microsoft.CSharp;
	using Microsoft.VisualBasic;

	public class Fw40Compiler : ICompiler
	{
		public Fw40Compiler(CompilationLanguages language, string outputDir, string tempPath)
		{
			Language = language;
			OutputDir = outputDir;
			TempPath = tempPath;
		}

		public CompilationLanguages Language { get; }
		public string OutputDir { get; }
		public string TempPath { get; }

		public CompilationResult Compile(string name, string body, IEnumerable<string> refs)
		{
			var providerOptions = new Dictionary<string, string> { { "CompilerVersion", "v4.0" } };

			CodeDomProvider provider = Language switch
			{
				CompilationLanguages.CSharp => new CSharpCodeProvider(providerOptions),
				CompilationLanguages.VisualBasic => new VBCodeProvider(providerOptions),
				_ => throw new InvalidOperationException(),
			};
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
				AssemblyLocation = result.Errors.HasErrors ? string.Empty : result.PathToAssembly,
				Errors = result.Errors.Cast<CompilerError>().Select(e => new CompilationError
				{
					Message = e.ErrorText,
					Id = e.ErrorNumber,
					Type = e.IsWarning ? CompilationErrorTypes.Warning : CompilationErrorTypes.Error,
					Line = e.Line,
					Character = e.Column
				}).ToArray(),
			};

			return compilationResult;
		}
	}

	public class Fw40CompilerService : ICompilerService
	{
		private readonly Dictionary<CompilationLanguages, ICompiler> _compilers = new();

		public Fw40CompilerService(string outputDir, string tempPath)
		{
			OutputDir = outputDir;
			TempPath = tempPath;
		}

		public string OutputDir { get; }
		public string TempPath { get; }

		public ICompiler GetCompiler(CompilationLanguages language)
		{
			if (!_compilers.ContainsKey(language))
				_compilers.Add(language, new Fw40Compiler(language, OutputDir, TempPath));

			return _compilers[language];
		}
	}
}