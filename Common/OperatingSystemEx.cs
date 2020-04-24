namespace Ecng.Common
{
	using System.Runtime.InteropServices;

	public static class OperatingSystemEx
	{
		public static bool IsWindows() => OSPlatform.Windows.IsOSPlatform();

		public static bool IsMacOS() =>	OSPlatform.OSX.IsOSPlatform();

		public static bool IsLinux() =>	OSPlatform.Linux.IsOSPlatform();

		public static bool IsOSPlatform(this OSPlatform platform)
			=> RuntimeInformation.IsOSPlatform(platform);
	}
}