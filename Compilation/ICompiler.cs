namespace Ecng.Compilation
{
	using System.Collections.Generic;
	using System.Threading;

	public interface ICompiler
	{
		CompilationLanguages Language { get; }
		CompilationResult Compile(string name, IEnumerable<string> sources, IEnumerable<string> refs, CancellationToken cancellationToken = default);
	}
}