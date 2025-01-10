namespace Ecng.Compilation;

using System;

using Ecng.Collections;

public class CompilerProvider : SynchronizedDictionary<string, ICompiler>
{
	public CompilerProvider()
		: base(StringComparer.InvariantCultureIgnoreCase)
	{
	}
}