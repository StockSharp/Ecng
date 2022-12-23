namespace Ecng.Compilation.CodeDom
{
	using System;
	using System.CodeDom.Compiler;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading;

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

		CompilationResult ICompiler.Compile(AssemblyLoadContextVisitor context, string name, string body, IEnumerable<string> refs, CancellationToken cancellationToken)
		{
			if (context is null)
				throw new ArgumentNullException(nameof(context));

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
				Assembly = result.Errors.HasErrors ? null : context.LoadFromAssemblyPath(result.PathToAssembly),
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
}