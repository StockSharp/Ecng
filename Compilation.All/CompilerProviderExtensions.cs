namespace Ecng.Compilation;

using Ecng.Common;
using Ecng.Compilation.FSharp;
using Ecng.Compilation.Python;
using Ecng.Compilation.Roslyn;

public static class CompilerProviderExtensions
{
	public static CompilerProvider CreateCompilerProvider()
		=> new()
		{
			{ FileExts.CSharp, new CSharpCompiler() },
			{ FileExts.VisualBasic, new VisualBasicCompiler() },
			{ FileExts.FSharp, new FSharpCompiler() },
			{ FileExts.Python, new PythonCompiler() }
		};
}