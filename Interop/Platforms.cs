namespace Ecng.Interop
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
			switch (platform)
			{
				case Platforms.x86:
					return !Environment.Is64BitProcess;
				case Platforms.x64:
					return Environment.Is64BitProcess;
				case Platforms.AnyCPU:
					return true;
				default:
					throw new ArgumentOutOfRangeException(nameof(platform));
			}
		}
	}
}