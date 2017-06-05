namespace Ecng.Roslyn
{
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	public class RoslynCompilerService : ICompilerService
	{
		private readonly SynchronizedDictionary<CompilationLanguages, Compiler> _compilers = new SynchronizedDictionary<CompilationLanguages, Compiler>();

		public CompilationResult Compile(CompilationLanguages language, string name, string body, IEnumerable<string> refs)
		{
			return _compilers.SafeAdd(language, Compiler.Create).Compile(name, body, refs);
		}
	}
}