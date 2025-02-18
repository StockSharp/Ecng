namespace Ecng.Compilation
{
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// Defines the contract for a compiler that supports compiling source files into an assembly.
	/// </summary>
	public interface ICompiler
	{
		/// <summary>
		/// Gets a value indicating whether the compiled assembly can be persisted.
		/// </summary>
		bool IsAssemblyPersistable { get; }

		/// <summary>
		/// Gets the file extension used by the compiler.
		/// </summary>
		string Extension { get; }

		/// <summary>
		/// Gets a value indicating whether the compiler supports tabs in the source code.
		/// </summary>
		bool IsTabsSupported { get; }

		/// <summary>
		/// Gets a value indicating whether the language is case sensitive.
		/// </summary>
		bool IsCaseSensitive { get; }

		/// <summary>
		/// Gets a value indicating whether the compiler supports external references.
		/// </summary>
		bool IsReferencesSupported { get; }

		/// <summary>
		/// Creates a new compiler context.
		/// </summary>
		/// <returns>A new instance of <see cref="ICompilerContext"/>.</returns>
		ICompilerContext CreateContext();

		/// <summary>
		/// Analyzes the provided source code using the specified analyzer and settings.
		/// </summary>
		/// <param name="analyzer">The analyzer used to perform the analysis.</param>
		/// <param name="analyzerSettings">The settings applied to the analyzer.</param>
		/// <param name="name">The name of the compilation unit.</param>
		/// <param name="sources">The source code files as strings.</param>
		/// <param name="refs">A collection of references as tuples containing the name and binary content.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous analysis operation. The task result contains an array of <see cref="CompilationError"/> instances.</returns>
		Task<CompilationError[]> Analyse(object analyzer, IEnumerable<object> analyzerSettings, string name, IEnumerable<string> sources, IEnumerable<(string name, byte[] body)> refs, CancellationToken cancellationToken = default);

		/// <summary>
		/// Compiles the provided source code files with the specified references.
		/// </summary>
		/// <param name="name">The name of the compilation unit.</param>
		/// <param name="sources">The source code files as strings.</param>
		/// <param name="refs">A collection of references as tuples containing the name and binary content.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous compile operation. The task result contains a <see cref="CompilationResult"/>.</returns>
		Task<CompilationResult> Compile(string name, IEnumerable<string> sources, IEnumerable<(string name, byte[] body)> refs, CancellationToken cancellationToken = default);
	}
}