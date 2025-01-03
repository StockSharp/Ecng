namespace Ecng.Compilation
{
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;

	public interface ICompiler
	{
		CompilationLanguages Language { get; }
		Task<CompilationError[]> Analyse(object analyzer, IEnumerable<object> analyzerSettings, string name, IEnumerable<string> sources, IEnumerable<(string name, byte[] body)> refs, CancellationToken cancellationToken = default);
		CompilationResult Compile(string name, IEnumerable<string> sources, IEnumerable<(string name, byte[] body)> refs, CancellationToken cancellationToken = default);
	}
}