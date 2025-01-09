namespace Ecng.Compilation.All;

using Ecng.Compilation.FSharp;
using Ecng.Compilation.Python;
using Ecng.Compilation.Roslyn;

public static class CompilerProviderExtensions
{
	public static CompilerProvider CreateCompilerProvider()
		=> new()
		{
			{ CompilationLanguages.CSharp, new CSharpCompiler() },
			{ CompilationLanguages.VisualBasic, new VisualBasicCompiler() },
			{ CompilationLanguages.FSharp, new FSharpCompiler() },
			{ CompilationLanguages.Python, new PythonCompiler() }
		};
}