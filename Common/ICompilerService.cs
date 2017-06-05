namespace Ecng.Common
{
	using System.Collections.Generic;

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
		CompilationResult Compile(CompilationLanguages language, string name, string body, IEnumerable<string> refs);
	}
}