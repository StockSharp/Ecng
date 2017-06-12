namespace Ecng.Roslyn
{
	using Ecng.Collections;
	using Ecng.Common;

	public class RoslynCompilerService : ICompilerService
	{
		private readonly SynchronizedDictionary<CompilationLanguages, ICompiler> _compilers = new SynchronizedDictionary<CompilationLanguages, ICompiler>();

		public ICompiler GetCompiler(CompilationLanguages language)
		{
			return _compilers.SafeAdd(language, key => new RoslynCompiler(key));
		}
	}
}