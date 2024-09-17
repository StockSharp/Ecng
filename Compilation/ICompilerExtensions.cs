namespace Ecng.Compilation;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Ecng.Common;

public static class ICompilerExtensions
{
	public static string RuntimePath { get; } = Path.GetDirectoryName(typeof(object).Assembly.Location);

	public static string ToFullRuntimePath(this string assemblyName)
		=> Path.Combine(RuntimePath, assemblyName);

	/// <summary>
	/// Are there any errors in the compilation.
	/// </summary>
	/// <param name="result">The result of the compilation.</param>
	/// <returns><see langword="true" /> - If there are errors, <see langword="true" /> - If the compilation is performed without errors.</returns>
	public static bool HasErrors(this CompilationResult result)
		=> result.CheckOnNull(nameof(result)).Errors.Any(e => e.Type == CompilationErrorTypes.Error);

	public static CompilationResult Compile(this ICompiler compiler, string name, string body, IEnumerable<string> refs, CancellationToken cancellationToken = default)
		=> compiler.Compile(name, [body], refs, cancellationToken);

	public static IEnumerable<string> ToValidPaths(this IEnumerable<CodeReference> references)
		=> references.Where(r => r.IsValid).Select(r => r.FullLocation).ToArray();
}