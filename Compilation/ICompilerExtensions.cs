namespace Ecng.Compilation;

using System;
using System.Collections.Generic;

public static class ICompilerExtensions
{
	[Obsolete("Use Compile with specified context.")]
	public static CompilationResult Compile(this ICompiler compiler, string name, string body, IEnumerable<string> refs)
		=> compiler.Compile(new(), name, body, refs);
}