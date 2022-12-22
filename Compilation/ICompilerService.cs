namespace Ecng.Compilation
{
	public interface ICompilerService
	{
		ICompiler GetCompiler(CompilationLanguages language = CompilationLanguages.CSharp);
	}
}