namespace Ecng.Compilation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Provides extension methods for the ICompiler interface and compilation results.
/// </summary>
public static class ICompilerExtensions
{
	/// <summary>
	/// Gets the runtime directory path where the System.Object assembly is located.
	/// </summary>
	public static string RuntimePath { get; } = Path.GetDirectoryName(typeof(object).Assembly.Location);

	/// <summary>
	/// Combines the runtime directory path with the given assembly name.
	/// </summary>
	/// <param name="assemblyName">The name of the assembly file.</param>
	/// <returns>The full path to the assembly in the runtime directory.</returns>
	public static string ToFullRuntimePath(this string assemblyName)
		=> Path.Combine(RuntimePath, assemblyName);

	/// <summary>
	/// Determines whether the given compilation result contains any errors.
	/// </summary>
	/// <param name="result">The compilation result to check.</param>
	/// <returns><c>true</c> if there are errors; otherwise, <c>false</c>.</returns>
	public static bool HasErrors(this CompilationResult result)
		=> result.CheckOnNull(nameof(result)).Errors.HasErrors();

	/// <summary>
	/// Determines whether the given collection of compilation errors contains any errors.
	/// </summary>
	/// <param name="errors">The collection of compilation errors to check.</param>
	/// <returns><c>true</c> if there are errors; otherwise, <c>false</c>.</returns>
	public static bool HasErrors(this IEnumerable<CompilationError> errors)
		=> errors.CheckOnNull(nameof(errors)).ErrorsOnly().Any();

	/// <summary>
	/// Compiles the provided source code using the specified compiler and references.
	/// </summary>
	/// <param name="compiler">The compiler instance.</param>
	/// <param name="name">The name of the compilation unit.</param>
	/// <param name="source">The source code as a string.</param>
	/// <param name="refs">A collection of reference paths.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous compilation, containing the compilation result.</returns>
	public static Task<CompilationResult> Compile(this ICompiler compiler, string name, string source, IEnumerable<string> refs, CancellationToken cancellationToken = default)
		=> Compile(compiler, name, [source], refs, cancellationToken);

	/// <summary>
	/// Compiles the provided source code using the specified compiler and references.
	/// </summary>
	/// <param name="compiler">The compiler instance.</param>
	/// <param name="name">The name of the compilation unit.</param>
	/// <param name="sources">The source code files as strings.</param>
	/// <param name="refs">A collection of reference paths.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous compilation, containing the compilation result.</returns>
	public static Task<CompilationResult> Compile(this ICompiler compiler, string name, IEnumerable<string> sources, IEnumerable<string> refs, CancellationToken cancellationToken = default)
		=> compiler.Compile(name, sources, refs.Select(ToRef), cancellationToken);

	/// <summary>
	/// Reads the file at the given path and returns its file name and binary content.
	/// </summary>
	/// <param name="path">The path to the reference file.</param>
	/// <returns>A tuple containing the file name and its binary content.</returns>
	public static (string name, byte[] body) ToRef(this string path)
		=> (Path.GetFileName(path), File.ReadAllBytes(path));

	/// <summary>
	/// Asynchronously extracts valid reference images from the provided references.
	/// </summary>
	/// <typeparam name="TRef">The type of the code reference.</typeparam>
	/// <param name="references">The collection of code references.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation that returns an array of reference image tuples.</returns>
	public static async ValueTask<IEnumerable<(string name, byte[] body)>> ToValidRefImages<TRef>(this IEnumerable<TRef> references, CancellationToken cancellationToken)
		where TRef : ICodeReference
	{
		if (references is null)
			throw new ArgumentNullException(nameof(references));

		return (await references.Where(r => r.IsValid).Select(r => r.GetImages(cancellationToken)).WhenAll()).SelectMany(i => i).ToArray();
	}

	/// <summary>
	/// Throws an exception if the compilation result contains errors.
	/// </summary>
	/// <param name="res">The compilation result.</param>
	/// <returns>The original compilation result if no errors are found.</returns>
	public static CompilationResult ThrowIfErrors(this CompilationResult res)
	{
		res.Errors.ThrowIfErrors();
		return res;
	}

	/// <summary>
	/// Throws an exception if the collection of compilation errors contains any errors.
	/// </summary>
	/// <param name="errors">The collection of compilation errors.</param>
	/// <returns>The original collection of errors if no errors are found.</returns>
	public static IEnumerable<CompilationError> ThrowIfErrors(this IEnumerable<CompilationError> errors)
	{
		if (errors.HasErrors())
			throw new InvalidOperationException($"Compilation error: {errors.ErrorsOnly().Take(2).Select(e => e.ToString()).JoinN()}");

		return errors;
	}

	/// <summary>
	/// Filters and returns only the errors (excluding warnings and info) from the collection of compilation errors.
	/// </summary>
	/// <param name="errors">The collection of compilation errors to filter.</param>
	/// <returns>A filtered collection containing only errors.</returns>
	public static IEnumerable<CompilationError> ErrorsOnly(this IEnumerable<CompilationError> errors)
		=> errors.Where(e => e.Type == CompilationErrorTypes.Error);

	/// <summary>
	/// Converts an exception to a compilation error.
	/// </summary>
	/// <param name="ex">The exception to convert.</param>
	/// <returns>A new CompilationError instance representing the exception.</returns>
	public static CompilationError ToError(this Exception ex)
		=> new()
		{
			Type = CompilationErrorTypes.Error,
			Message = ex.Message,
		};
}