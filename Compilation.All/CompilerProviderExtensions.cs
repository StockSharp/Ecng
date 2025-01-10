namespace Ecng.Compilation;

using Ecng.Common;
using Ecng.Compilation.FSharp;
using Ecng.Compilation.Python;
using Ecng.Compilation.Roslyn;

public static class CompilerProviderExtensions
{
	public static CompilerProvider CreateCompilerProvider()
		=> CreateCompilerProvider(IronPython.Hosting.Python.CreateEngine());

	public static CompilerProvider CreateCompilerProvider(Microsoft.Scripting.Hosting.ScriptEngine pythonEngine)
		=> new()
		{
			{ FileExts.CSharp, new CSharpCompiler() },
			{ FileExts.VisualBasic, new VisualBasicCompiler() },
			{ FileExts.FSharp, new FSharpCompiler() },
			{ FileExts.Python, new PythonCompiler(pythonEngine) }
		};
}