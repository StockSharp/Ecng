namespace Ecng.Compilation
{
	using System;

	[Obsolete("Use ICompiler directly.")]
	public interface ICompilerService
	{
		ICompiler GetCompiler(CompilationLanguages language = CompilationLanguages.CSharp);
	}
}