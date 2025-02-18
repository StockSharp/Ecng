namespace Ecng.Compilation;

using System;

using Ecng.Collections;

/// <summary>
/// Represents a provider for compilers.
/// </summary>
public class CompilerProvider : SynchronizedDictionary<string, ICompiler>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CompilerProvider"/> class.
	/// </summary>
	public CompilerProvider()
		: base(StringComparer.InvariantCultureIgnoreCase)
	{
	}
}