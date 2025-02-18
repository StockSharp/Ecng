namespace Ecng.Nuget;

using System.Reflection;

using Ecng.Reflection;

/// <summary>
/// NuGET extensions.
/// </summary>
public static class NugetExtensions
{
	/// <summary>
	/// Get the target frameworks for the package.
	/// </summary>
	/// <param name="reader"><see cref="PackageArchiveReader"/></param>
	/// <returns>Target frameworks.</returns>
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

		return [.. targetFrameworks];
	}

	/// <summary>
	/// Remove the platform version.
	/// </summary>
	/// <param name="fwk"><see cref="NuGetFramework"/></param>
	/// <returns>The framework without the platform version.</returns>
	public static NuGetFramework RemovePlatformVersion(this NuGetFramework fwk)
		=> new(fwk.Framework, fwk.Version, fwk.Platform, FrameworkConstants.EmptyVersion);

	/// <summary>
	/// Get all versions.
	/// </summary>
	/// <param name="repo"><see cref="SourceRepository"/></param>
	/// <param name="packageId">The package ID.</param>
	/// <param name="logger"><see cref="ILogger"/></param>
	/// <param name="cache"><see cref="SourceCacheContext"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>All versions ordered.</returns>
	public static async Task<NuGetVersion[]> GetAllVersionsOrderedAsync(this SourceRepository repo, string packageId, ILogger logger, SourceCacheContext cache, CancellationToken token)
	{
		var resource = await repo.GetResourceAsync<FindPackageByIdResource>(token);

		return [.. (await resource.GetAllVersionsAsync(packageId, cache, logger, token)).OrderBy(v => v)];
	}

	/// <summary>
	/// Get the last version.
	/// </summary>
	/// <param name="repo"><see cref="SourceRepository"/></param>
	/// <param name="packageId">The package ID.</param>
	/// <param name="allowPreview">Allow preview versions.</param>
	/// <param name="logger"><see cref="ILogger"/></param>
	/// <param name="cache"><see cref="SourceCacheContext"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>The last version.</returns>
	public static async Task<NuGetVersion> GetLastVersionAsync(this SourceRepository repo, string packageId, bool allowPreview, ILogger logger, SourceCacheContext cache, CancellationToken token)
	{
		var versions = await repo.GetAllVersionsOrderedAsync(packageId, logger, cache, token);
		Func<NuGetVersion, bool> cond = allowPreview ? _ => true : v => !v.IsPrerelease;

		return versions.LastOrDefault(cond);
	}

	/// <summary>
	/// Get the last version in the floating range.
	/// </summary>
	/// <param name="repo"><see cref="SourceRepository"/></param>
	/// <param name="packageId">The package ID.</param>
	/// <param name="floatingVer">The floating version.</param>
	/// <param name="logger"><see cref="ILogger"/></param>
	/// <param name="cache"><see cref="SourceCacheContext"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>The last version in the floating range.</returns>
	public static async Task<NuGetVersion> GetLastVersionInFloatingRangeAsync(this SourceRepository repo, string packageId, string floatingVer, ILogger logger, SourceCacheContext cache, CancellationToken token)
	{
		if (!FloatRange.TryParse(floatingVer, out var range))
			throw new ArgumentException($"invalid floating version '{floatingVer}'", nameof(floatingVer));

		var versions = await repo.GetAllVersionsOrderedAsync(packageId, logger, cache, token);

		return versions.LastOrDefault(range.Satisfies);
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

	/// <summary>
	/// Disable access to the nuget.config file.
	/// </summary>
	public static void DisableNugetConfig()
	{
		// disable access nuget.config file

		var proxy = new ProxyCache(new DummySettings(), EnvironmentVariableWrapper.Instance);

		var f = typeof(ProxyCache).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
		var lazy = (Lazy<ProxyCache>)f.GetValue(null);
		lazy.SetValue(proxy);
	}

	/// <summary>
	/// Increment the version.
	/// </summary>
	/// <param name="version"><see cref="NuGetVersion"/></param>
	/// <returns>The incremented version.</returns>
	public static NuGetVersion Increment(this NuGetVersion version)
	{
		if (version is null)
			throw new ArgumentNullException(nameof(version));

		return new(version.Major, version.Minor, version.Patch + 1);
	}

	/// <summary>
	/// Add a suffix to the version.
	/// </summary>
	/// <param name="version"><see cref="NuGetVersion"/></param>
	/// <param name="suffix">The suffix to add.</param>
	/// <returns>The version with the suffix.</returns>
	public static NuGetVersion WithSuffix(this NuGetVersion version, string suffix)
	{
		if (version is null)
			throw new ArgumentNullException(nameof(version));

		if (suffix.IsEmpty())
			throw new ArgumentNullException(nameof(suffix));

		return new(version.Major, version.Minor, version.Patch, suffix);
	}

	/// <summary>
	/// Get the base URL for the repository.
	/// </summary>
	/// <param name="repo"><see cref="SourceRepository"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The base URL for the repository.</returns>
	public static async Task<Uri> GetBaseUrl(this SourceRepository repo, CancellationToken cancellationToken)
	{
		if (repo is null)
			throw new ArgumentNullException(nameof(repo));

		var serviceIndex = await repo.GetResourceAsync<ServiceIndexResourceV3>(cancellationToken)
			?? throw new InvalidOperationException($"ServiceIndexResourceV3 for {repo.PackageSource.Name} is null.");

		var baseUrl = serviceIndex.GetServiceEntryUri(ServiceTypes.PackageBaseAddress)
			?? throw new InvalidOperationException($"No PackageBaseAddress endpoint for {repo.PackageSource.Name}.");

		var str = baseUrl.To<string>();

		if (!str.EndsWith('/'))
			baseUrl = (str + "/").To<Uri>();

		return baseUrl;
	}

	/// <summary>
	/// Create a private HTTP client.
	/// </summary>
	/// <param name="apiKey">The API key.</param>
	/// <returns>The private HTTP client.</returns>
	public static HttpClient CreatePrivateHttp(string apiKey)
	{
		var http = new HttpClient();
		http.DefaultRequestHeaders.Add(ProtocolConstants.ApiKeyHeader, apiKey);
		return http;
	}

	/// <summary>
	/// Get the nuspec file.
	/// </summary>
	/// <param name="http"><see cref="HttpClient"/></param>
	/// <param name="baseUrl">The base URL for the repository.</param>
	/// <param name="packageId">The package ID.</param>
	/// <param name="version">The package version.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The nuspec file.</returns>
	public static Task<Stream> GetNuspecAsync(this HttpClient http, Uri baseUrl, string packageId, NuGetVersion version, CancellationToken cancellationToken)
	{
		if (http is null)			throw new ArgumentNullException(nameof(http));
		if (baseUrl is null)		throw new ArgumentNullException(nameof(baseUrl));
		if (packageId.IsEmpty())	throw new ArgumentNullException(nameof(packageId));
		if (version is null)		throw new ArgumentNullException(nameof(version));

		return http.GetStreamAsync(new Uri(baseUrl, $"{packageId}/{version}/{packageId}{NuGetConstants.ManifestExtension}".ToLowerInvariant()), cancellationToken);
	}
}