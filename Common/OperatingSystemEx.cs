namespace Ecng.Common
{
	using System.Runtime.InteropServices;

	public static class OperatingSystemEx
	{
		public static bool IsWindows() =>
			RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

		public static bool IsMacOS() =>
			RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

		public static bool IsLinux() =>
			RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
	}
}