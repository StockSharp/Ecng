namespace Ecng.Compilation;

using System;
using System.Reflection;

/// <summary>
/// Represents a context for compilation operations.
/// Provides functionality to load assemblies from binary data.
/// </summary>
public interface ICompilerContext : IDisposable
{
	/// <summary>
	/// Loads an assembly from the provided binary representation.
	/// </summary>
	/// <param name="body">The binary content of the assembly.</param>
	/// <returns>The loaded <see cref="Assembly"/> instance.</returns>
	Assembly LoadFromBinary(byte[] body);
}