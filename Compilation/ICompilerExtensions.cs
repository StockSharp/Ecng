namespace Ecng.Compilation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
		=> result.CheckOnNull(nameof(result)).Errors.HasErrors();

	/// <summary>
	/// Are there any errors in the compilation.
	/// </summary>
	/// <param name="errors">The result of the compilation.</param>
	/// <returns><see langword="true" /> - If there are errors, <see langword="true" /> - If the compilation is performed without errors.</returns>
	public static bool HasErrors(this IEnumerable<CompilationError> errors)
		=> errors.CheckOnNull(nameof(errors)).ErrorsOnly().Any();

	public static Task<CompilationResult> Compile(this ICompiler compiler, string name, string source, IEnumerable<string> refs, CancellationToken cancellationToken = default)
		=> Compile(compiler, name, [source], refs, cancellationToken);

	public static Task<CompilationResult> Compile(this ICompiler compiler, string name, IEnumerable<string> sources, IEnumerable<string> refs, CancellationToken cancellationToken = default)
		=> compiler.Compile(name, sources, refs.Select(ToRef), cancellationToken);

	public static (string name, byte[] body) ToRef(this string path)
		=> (Path.GetFileName(path), File.ReadAllBytes(path));

	public static async ValueTask<IEnumerable<(string name, byte[] body)>> ToValidRefImages<TRef>(this IEnumerable<TRef> references, CancellationToken cancellationToken)
		where TRef : ICodeReference
	{
		if (references is null)
			throw new ArgumentNullException(nameof(references));

		return (await references.Where(r => r.IsValid).Select(r => r.GetImages(cancellationToken)).WhenAll()).SelectMany(i => i).ToArray();
	}

	/// <summary>
	/// Throw if errors.
	/// </summary>
	/// <param name="res"><see cref="CompilationResult"/></param>
	/// <returns><see cref="CompilationResult"/></returns>
	public static CompilationResult ThrowIfErrors(this CompilationResult res)
	{
		res.Errors.ThrowIfErrors();
		return res;
	}

	public static IEnumerable<CompilationError> ThrowIfErrors(this IEnumerable<CompilationError> errors)
	{
		if (errors.HasErrors())
			throw new InvalidOperationException($"Compilation error: {errors.ErrorsOnly().Take(2).Select(e => e.ToString()).JoinN()}");

		return errors;
	}

	public static IEnumerable<CompilationError> ErrorsOnly(this IEnumerable<CompilationError> errors)
		=> errors.Where(e => e.Type == CompilationErrorTypes.Error);

	public static bool Is<T>(this IType type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		return type.Is(typeof(T));
	}

	public static T CreateInstance<T>(this IType type, params object[] args)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		return (T)type.CreateInstance(args);
	}

	public static IType TryFindType(this IEnumerable<IType> types, Func<IType, bool> isTypeCompatible, string typeName)
	{
		if (types is null)
			throw new ArgumentNullException(nameof(types));

		if (isTypeCompatible is null && typeName.IsEmpty())
			throw new ArgumentNullException(nameof(typeName));

		if (!typeName.IsEmpty())
			return types.FirstOrDefault(t => t.Name.EqualsIgnoreCase(typeName));
		else
			return types.FirstOrDefault(isTypeCompatible);
	}

	/// <summary>
	/// Is type compatible.
	/// </summary>
	/// <typeparam name="T">Required type.</typeparam>
	/// <param name="type">Type.</param>
	/// <returns>Check result.</returns>
	public static bool IsRequiredType<T>(this IType type)
		=> IsRequiredType(type, typeof(T));

	/// <summary>
	/// Is type compatible.
	/// </summary>
	/// <param name="type">Type.</param>
	/// <param name="required">Required type.</param>
	/// <returns>Check result.</returns>
	public static bool IsRequiredType(this IType type, Type required)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		if (required is null)
			throw new ArgumentNullException(nameof(required));

		return !type.IsAbstract &&
			type.IsPublic &&
			!type.IsGenericTypeDefinition &&
			type.Is(required) &&
			type.GetConstructor([]) is not null;
	}

	public static IAssembly ToIAssembly(this byte[] body)
		=> new AssemblyImpl(body);

	public static IType ToIType(this Type type)
		=> new TypeImpl(type);
}