namespace Ecng.Common
{
	using System.Linq;
	using System.Collections.Generic;
	using System.Runtime.InteropServices;
	using System.IO;
	using System;

	/// <summary>
	/// Provides operating system related helper methods and properties.
	/// </summary>
	public static class OperatingSystemEx
	{
		/// <summary>
		/// Determines whether the current operating system is Windows.
		/// </summary>
		/// <returns><c>true</c> if the operating system is Windows; otherwise, <c>false</c>.</returns>
		public static bool IsWindows() => OSPlatform.Windows.IsOSPlatform();

		/// <summary>
		/// Determines whether the current operating system is macOS.
		/// </summary>
		/// <returns><c>true</c> if the operating system is macOS; otherwise, <c>false</c>.</returns>
		public static bool IsMacOS() => OSPlatform.OSX.IsOSPlatform();

		/// <summary>
		/// Determines whether the current operating system is Linux.
		/// </summary>
		/// <returns><c>true</c> if the operating system is Linux; otherwise, <c>false</c>.</returns>
		public static bool IsLinux() => OSPlatform.Linux.IsOSPlatform();

		/// <summary>
		/// Determines whether the specified <see cref="OSPlatform"/> is the current platform.
		/// </summary>
		/// <param name="platform">The operating system platform to check.</param>
		/// <returns><c>true</c> if the specified platform is the current operating system; otherwise, <c>false</c>.</returns>
		public static bool IsOSPlatform(this OSPlatform platform)
			=> RuntimeInformation.IsOSPlatform(platform);

		/// <summary>
		/// Gets all available operating system platforms defined in <see cref="OSPlatform"/>.
		/// </summary>
		public static IEnumerable<OSPlatform> Platforms =>
			[.. typeof(OSPlatform)
				.GetProperties()
					.Where(p => p.PropertyType == typeof(OSPlatform))
					.Select(p => (OSPlatform)p.GetValue(null))];

		/// <summary>
		/// Gets a value indicating whether the current runtime framework is .NET Framework.
		/// </summary>
		public static bool IsFramework
			=> RuntimeInformation.FrameworkDescription.StartsWithIgnoreCase(".NET Framework");

		/// <summary>
		/// Retrieves runtime package versions based on the provided framework version.
		/// </summary>
		/// <param name="fwVer">The framework version to look up corresponding runtime packages.</param>
		/// <returns>
		/// A dictionary containing the package name as the key and its <see cref="Version"/> as the value.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="fwVer"/> is null.</exception>
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

					// Local function to fill the runtime packages from a specific folder.
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
				// Suppress any exceptions during package retrieval.
			}

			return runtimePackages;
		}
	}
}