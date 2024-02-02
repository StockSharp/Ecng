namespace Ecng.Common
{
	using System.Linq;
	using System.Collections.Generic;
	using System.Runtime.InteropServices;
	using System.IO;
	using System;

	public static class OperatingSystemEx
	{
		public static bool IsWindows() => OSPlatform.Windows.IsOSPlatform();

		public static bool IsMacOS() =>	OSPlatform.OSX.IsOSPlatform();

		public static bool IsLinux() =>	OSPlatform.Linux.IsOSPlatform();

		public static bool IsOSPlatform(this OSPlatform platform)
			=> RuntimeInformation.IsOSPlatform(platform);

		public static IEnumerable<OSPlatform> Platforms =>
			typeof(OSPlatform)
				.GetProperties()
					.Where(p => p.PropertyType == typeof(OSPlatform))
					.Select(p => (OSPlatform)p.GetValue(null))
					.ToArray();

		public static bool IsFramework
			=> RuntimeInformation.FrameworkDescription.StartsWithIgnoreCase(".NET Framework");

		public static IDictionary<string, Version> GetRuntimePackages(Version fwVer)
		{
			if (fwVer is null)
				throw new ArgumentNullException(nameof(fwVer));

			var runtimePackages = new Dictionary<string, Version>(StringComparer.InvariantCultureIgnoreCase)
			{
				{ "NETStandard.Library", fwVer },
			};

			try
			{
				var fi = new DirectoryInfo(RuntimeEnvironment.GetRuntimeDirectory());

				if (fi.Exists && fi.Parent?.Parent is not null)
				{
					var dirs = fi.Parent.Parent.GetDirectories();

					void fillPackages(string name)
					{
						var dir = dirs.FirstOrDefault(d => d.Name.EqualsIgnoreCase(name));

						if (dir is null)
							return;

						var verDir = dir
							.GetDirectories()
							.Select(d => (dir: d, ver: Version.TryParse(d.Name, out var ver) ? ver : null))
						.Where(t => t.ver is not null && t.ver.Major == fwVer.Major && t.ver.Minor == fwVer.Minor)
						.OrderByDescending(t => t.ver)
						.FirstOrDefault().dir;

						if (verDir is null || !Version.TryParse(verDir.Name, out var ver))
							return;

						foreach (var packageName in verDir.GetFiles("*.dll").Select(f => Path.GetFileNameWithoutExtension(f.Name)))
						{
							if (runtimePackages.ContainsKey(packageName))
								continue;

							runtimePackages.Add(packageName, ver);
						}
					}

					fillPackages("Microsoft.NETCore.App");
					fillPackages("Microsoft.WindowsDesktop.App");
				}
			}
			catch
			{
			}

			return runtimePackages;
		}
	}
}