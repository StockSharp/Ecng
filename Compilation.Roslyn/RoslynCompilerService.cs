namespace Ecng.Compilation.Roslyn
{
	using Ecng.Collections;
	using Ecng.Compilation;

	public class RoslynCompilerService : ICompilerService
	{
		private readonly SynchronizedDictionary<CompilationLanguages, ICompiler> _compilers = new();

		ICompiler ICompilerService.GetCompiler(CompilationLanguages language)
			=> _compilers.SafeAdd(language, key => new RoslynCompiler(key));
	}
}