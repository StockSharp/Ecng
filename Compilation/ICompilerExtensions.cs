namespace Ecng.Compilation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
#if NETCOREAPP
using System.Runtime.Loader;
#endif

using Ecng.Common;

public static class ICompilerExtensions
{
	public static string RuntimePath { get; } = Path.GetDirectoryName(typeof(object).Assembly.Location);

	public static string ToFullRuntimePath(this string assemblyName)
		=> Path.Combine(RuntimePath, assemblyName);

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

#if NETCOREAPP
	/// <summary>
	/// To compile the code.
	/// </summary>
	/// <param name="compiler">Compiler.</param>
	/// <param name="context"><see cref="AssemblyLoadContext"/></param>
	/// <param name="code">Code.</param>
	/// <param name="name">The reference name.</param>
	/// <param name="references">References.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns>The result of the compilation.</returns>
	public static CompilationResult CompileCode(this ICompiler compiler, AssemblyLoadContext context, string code, string name, IEnumerable<CodeReference> references, CancellationToken cancellationToken = default)
		=> compiler.Compile(new(context), name, code, references.Where(r => r.IsValid).Select(r => r.FullLocation).ToArray(), cancellationToken);
#endif
}