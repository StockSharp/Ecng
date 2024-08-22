namespace Ecng.Interop;

using System;

static class Constants
{
	public const string Runtimes = "runtimes";
	public const string Native = "native";
	public const string Aot = "aot";
	public const string Lib = "lib";
	public const string NetCore50 = "netcore50";
	public const string NetStandard20 = "netstandard2.0";
	public const string Win = "win";
	public const string Unix = "unix";
	public const string X64 = "x64";
	public const string X86 = "x86";

	public static string GetOS() => Environment.OSVersion.Platform == PlatformID.Win32NT ? Win : Unix;
	public static string GetArchitecture() => Environment.Is64BitProcess ? X64 : X86;
}
