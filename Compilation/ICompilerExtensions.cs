namespace Ecng.Compilation;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

public static class ICompilerExtensions
{
	[Obsolete("Use Compile with specified context.")]
	public static CompilationResult Compile(this ICompiler compiler, string name, string body, IEnumerable<string> refs)
		=> compiler.CheckOnNull(nameof(compiler)).Compile(new(), name, body, refs);

	/// <summary>
	/// Are there any errors in the compilation.
	/// </summary>
	/// <param name="result">The result of the compilation.</param>
	/// <returns><see langword="true" /> - If there are errors, <see langword="true" /> - If the compilation is performed without errors.</returns>
	public static bool HasErrors(this CompilationResult result)
		=> result.CheckOnNull(nameof(result)).Errors.Any(e => e.Type == CompilationErrorTypes.Error);
}