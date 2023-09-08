namespace Ecng.Compilation.CodeDom
{
	using System;
	using System.CodeDom.Compiler;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Microsoft.CSharp;
	using Microsoft.VisualBasic;

	[Obsolete("Use RoslynCompiler.")]
	public class CodeDomCompiler : ICompiler
	{
		public CodeDomCompiler(CompilationLanguages language, string outputDir, string tempPath)
		{
			Language = language;
			OutputDir = outputDir;
			TempPath = tempPath;
		}

		public CompilationLanguages Language { get; }
		public string OutputDir { get; }
		public string TempPath { get; }

		Task<CompilationError[]> ICompiler.Analyse(object analyzer, IEnumerable<object> analyzerSettings, string name,IEnumerable<string> sources, IEnumerable<string> refs, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		CompilationResult ICompiler.Compile(string name, IEnumerable<string> sources, IEnumerable<string> refs, CancellationToken cancellationToken)
		{
			if (sources is null)
				throw new ArgumentNullException(nameof(sources));

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
			}, sources.ToArray());

			byte[] asm = null;

			if (result.Errors.HasErrors)
			{
				asm = File.ReadAllBytes(result.PathToAssembly);
				File.Delete(result.PathToAssembly);
			}

			return new()
			{
				Assembly = asm,
				Errors = result.Errors.Cast<CompilerError>().Select(e => new CompilationError
				{
					Message = e.ErrorText,
					Id = e.ErrorNumber,
					Type = e.IsWarning ? CompilationErrorTypes.Warning : CompilationErrorTypes.Error,
					Line = e.Line,
					Character = e.Column
				}).ToArray(),
			};
		}
	}
}