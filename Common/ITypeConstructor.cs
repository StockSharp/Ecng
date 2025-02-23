namespace Ecng.Common;

/// <summary>
/// Provides a mechanism for creating instances of a type using specified constructor arguments.
/// </summary>
public interface ITypeConstructor
{
	/// <summary>
	/// Creates an instance of a type using the provided arguments.
	/// </summary>
	/// <param name="args">An array of arguments to pass to the constructor.</param>
	/// <returns>An instance of the type created.</returns>
	object CreateInstance(object[] args);
}