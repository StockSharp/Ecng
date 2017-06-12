namespace Ecng.Common
{
	public enum CompilationLanguages
	{
		CSharp,
		VisualBasic,
		//Java,
		//JScript,
		//Cpp,
	}

	public interface ICompilerService
	{
		ICompiler GetCompiler(CompilationLanguages language);
	}
}