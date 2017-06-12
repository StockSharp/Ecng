namespace Ecng.Common
{
	using System.Collections.Generic;

	public interface ICompiler
	{
		CompilationLanguages Language { get; }
		CompilationResult Compile(string name, string body, IEnumerable<string> refs);
	}
}