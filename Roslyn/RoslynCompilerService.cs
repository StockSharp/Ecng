namespace Ecng.Roslyn
{
	using Ecng.Collections;
	using Ecng.Compilation;

	public class RoslynCompilerService : ICompilerService
	{
		private readonly SynchronizedDictionary<CompilationLanguages, ICompiler> _compilers = new();

		public ICompiler GetCompiler(CompilationLanguages language)
		{
			return _compilers.SafeAdd(language, key => new RoslynCompiler(key));
		}
	}
}