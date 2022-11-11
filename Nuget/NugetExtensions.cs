using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;

using NuGet.Common;
using NuGet.Versioning;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;
using NuGet.Configuration;
using NuGet.Frameworks;

namespace Ecng.Nuget;

public static class NugetExtensions
{
	public static string[] GetTargetFrameworks(this PackageArchiveReader reader)
	{
		var targetFrameworks = reader
			.GetSupportedFrameworks()
			.Select(f => f.GetShortFolderName())
			.ToList();

		// Default to the "any" framework if no frameworks were found.
		if (targetFrameworks.Count == 0)
		{
			targetFrameworks.Add("any");
		}

		return targetFrameworks.ToArray();
	}

	public static NuGetFramework RemovePlatformVersion(this NuGetFramework fwk) => new(fwk.Framework, fwk.Version, fwk.Platform, FrameworkConstants.EmptyVersion);

	public static async Task<NuGetVersion[]> GetAllVersionsOrderedAsync(this SourceRepository repo, string packageId, CancellationToken token = default, ILogger log = null, SourceCacheContext cacheCtx = null)
	{
		var cache = cacheCtx ?? new SourceCacheContext();
		var resource = await repo.GetResourceAsync<FindPackageByIdResource>(token);

		return (await resource.GetAllVersionsAsync(packageId, cache, log ?? NullLogger.Instance, token)).OrderBy(v => v).ToArray();
	}

	public static async Task<NuGetVersion> GetLastVersionAsync(this SourceRepository repo, string packageId, bool allowPreview, CancellationToken token = default, ILogger log = null, SourceCacheContext cacheCtx = null)
	{
		var versions = await repo.GetAllVersionsOrderedAsync(packageId, token, log, cacheCtx);
		Func<NuGetVersion, bool> cond = allowPreview ? _ => true : v => !v.IsPrerelease;

		return versions.LastOrDefault(cond);
	}

	public static async Task<NuGetVersion> GetLastVersionInFloatingRangeAsync(this SourceRepository repo, string packageId, string floatingVer, CancellationToken token = default, ILogger log = null, SourceCacheContext cacheCtx = null)
	{
		if (!FloatRange.TryParse(floatingVer, out var range))
			throw new ArgumentException($"invalid floating version '{floatingVer}'", nameof(floatingVer));

		var versions = await repo.GetAllVersionsOrderedAsync(packageId, token, log, cacheCtx);

		return versions.LastOrDefault(v => range.Satisfies(v));
	}

	private class DummySettings : ISettings
	{
		private class MockSettingSection : SettingSection
		{
			public MockSettingSection(string name, IReadOnlyDictionary<string, string> attributes, IEnumerable<SettingItem> children)
				: base(name, attributes, children)
			{
			}

			public MockSettingSection(string name, params SettingItem[] children)
				: base(name, attributes: null, children: new HashSet<SettingItem>(children))
			{
			}

			public override SettingBase Clone()
				=> throw new NotSupportedException();
		}

		event EventHandler ISettings.SettingsChanged
		{
			add { }
			remove { }
		}

		void ISettings.AddOrUpdate(string sectionName, SettingItem item) => throw new NotSupportedException();
		IList<string> ISettings.GetConfigFilePaths() => throw new NotSupportedException();
		IList<string> ISettings.GetConfigRoots() => throw new NotSupportedException();
		SettingSection ISettings.GetSection(string sectionName) => new MockSettingSection(sectionName);
		void ISettings.Remove(string sectionName, SettingItem item) => throw new NotSupportedException();
		void ISettings.SaveToDisk() => throw new NotSupportedException();
	}

	public static void DisableNugetConfig()
	{
		// disable access nuget.config file

		var proxy = new ProxyCache(new DummySettings(), EnvironmentVariableWrapper.Instance);

		var f = typeof(ProxyCache).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
		var lazy = (Lazy<ProxyCache>)f.GetValue(null);
		lazy.GetType().GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(lazy, null);
		lazy.GetType().GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(lazy, proxy);
	}
}
