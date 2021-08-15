namespace Ecng.Common
{
	using System;

	public enum Platforms
	{
		x86,
		x64,
		AnyCPU,
	}

	public static class PlatformHelper
	{
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
}