namespace Ecng.Compilation
{
	using System.Collections.Generic;

	public interface ICompiler
	{
		CompilationLanguages Language { get; }
		CompilationResult Compile(AssemblyLoadContextVisitor context, string name, string body, IEnumerable<string> refs);
	}
}