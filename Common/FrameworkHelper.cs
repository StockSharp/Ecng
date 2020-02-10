namespace Ecng.Common
{
	using System;

	using Microsoft.Win32;

	public static class FrameworkHelper
	{
		public static Version Version => Get45Or451FromRegistry();

		private static Version Get45Or451FromRegistry()
		{
			using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
			{
				var releaseKey = ndpKey.GetValue("Release").To<int>();
				return CheckFor45DotVersion(releaseKey);
			}
		}

		private static Version CheckFor45DotVersion(int releaseKey)
		{
			// https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
			if (releaseKey >= 528040)
			{
				return "4.8".To<Version>();
			}
			else if (releaseKey >= 461808)
			{
				return "4.7.2".To<Version>();
			}
			else if (releaseKey >= 461308)
			{
				return "4.7.1".To<Version>();
			}
			else if (releaseKey >= 460798)
			{
				return "4.7".To<Version>();
			}
			else if (releaseKey >= 394802)
			{
				return "4.6.2".To<Version>();
			}
			else if (releaseKey >= 394254)
			{
				return "4.6.1".To<Version>();
			}
			else if (releaseKey >= 393295)
			{
				return "4.6".To<Version>();
			}
			else if (releaseKey >= 393273)
			{
				return "4.6".To<Version>();
			}
			else if (releaseKey >= 379893)
			{
				return "4.5.2".To<Version>();
			}
			else if (releaseKey >= 378675)
			{
				return "4.5.1".To<Version>();
			}
			else if (releaseKey >= 378389)
			{
				return "4.5".To<Version>();
			}

			throw new InvalidOperationException($"Unknown .NET FW version {releaseKey}.");
		}
	}
}