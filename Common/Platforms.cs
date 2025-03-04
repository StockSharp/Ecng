namespace Ecng.Common;

using System;

/// <summary>
/// Enumeration for representing supported platform types.
/// </summary>
public enum Platforms
{
	/// <summary>
	/// Represents the 32-bit platform.
	/// </summary>
	x86,

	/// <summary>
	/// Represents the 64-bit platform.
	/// </summary>
	x64,

	/// <summary>
	/// Represents any CPU architecture.
	/// </summary>
	AnyCPU,
}

/// <summary>
/// Provides helper methods for <see cref="Platforms"/>.
/// </summary>
public static class PlatformHelper
{
	/// <summary>
	/// Determines whether the current process is compatible with the specified platform.
	/// </summary>
	/// <param name="platform">The target platform to check compatibility for.</param>
	/// <returns>
	/// <c>true</c> if the current process is compatible with the specified platform; otherwise, <c>false</c>.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the platform is not recognized.</exception>
	public static bool IsCompatible(this Platforms platform)
	{
		return platform switch
		{
			Platforms.x86 => !Environment.Is64BitProcess,
			Platforms.x64 => Environment.Is64BitProcess,
			Platforms.AnyCPU => true,
			_ => throw new ArgumentOutOfRangeException(nameof(platform)),
		};
	}
}