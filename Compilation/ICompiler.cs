namespace Ecng.Compilation
{
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;

	public interface ICompiler
	{
		string Extension { get; }
		Task<CompilationError[]> Analyse(object analyzer, IEnumerable<object> analyzerSettings, string name, IEnumerable<string> sources, IEnumerable<(string name, byte[] body)> refs, CancellationToken cancellationToken = default);
		Task<CompilationResult> Compile(string name, IEnumerable<string> sources, IEnumerable<(string name, byte[] body)> refs, CancellationToken cancellationToken = default);
	}
}