namespace Ecng.Tests.Nuget;

using System.IO.Compression;
using System.Xml.Linq;

using Ecng.Nuget;

using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

[TestClass]
public class NugetExtensionsTests : BaseTestClass
{
	[TestMethod]
	public void Increment_ValidVersion_ReturnsIncrementedPatch()
	{
		// Arrange
		var version = new NuGetVersion("1.2.3");

		// Act
		var result = version.Increment();

		// Assert
		result.Major.AssertEqual(1);
		result.Minor.AssertEqual(2);
		result.Patch.AssertEqual(4);
		result.IsPrerelease.AssertFalse();
	}

	[TestMethod]
	public void Increment_NullVersion_ThrowsArgumentNullException()
	{
		// Arrange
		NuGetVersion version = null;

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() => version.Increment());
	}

	[TestMethod]
	public void WithSuffix_ValidVersionAndSuffix_ReturnsVersionWithSuffix()
	{
		// Arrange
		var version = new NuGetVersion("1.2.3");
		var suffix = "beta";

		// Act
		var result = version.WithSuffix(suffix);

		// Assert
		result.Major.AssertEqual(1);
		result.Minor.AssertEqual(2);
		result.Patch.AssertEqual(3);
		result.IsPrerelease.AssertTrue();
		result.Release.AssertEqual("beta");
	}

	[TestMethod]
	public void WithSuffix_NullVersion_ThrowsArgumentNullException()
	{
		// Arrange
		NuGetVersion version = null;

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() => version.WithSuffix("beta"));
	}

	[TestMethod]
	public void WithSuffix_EmptySuffix_ThrowsArgumentNullException()
	{
		// Arrange
		var version = new NuGetVersion("1.2.3");

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() => version.WithSuffix(""));
	}

	[TestMethod]
	public void WithSuffix_NullSuffix_ThrowsArgumentNullException()
	{
		// Arrange
		var version = new NuGetVersion("1.2.3");

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() => version.WithSuffix(null));
	}

	[TestMethod]
	public void RemovePlatformVersion_FrameworkWithPlatform_RemovesPlatformVersion()
	{
		// Arrange
		var framework = new NuGetFramework(".NETCoreApp", new Version(6, 0), "windows", new Version(10, 0));

		// Act
		var result = framework.RemovePlatformVersion();

		// Assert
		result.Framework.AssertEqual(".NETCoreApp");
		result.Version.Major.AssertEqual(6);
		result.Version.Minor.AssertEqual(0);
		result.Platform.AssertEqual("windows");
		result.PlatformVersion.AssertEqual(new Version(0, 0, 0, 0));
	}

	[TestMethod]
	public void RemovePlatformVersion_FrameworkWithoutPlatform_ReturnsSameFramework()
	{
		// Arrange
		var framework = new NuGetFramework(".NETCoreApp", new Version(6, 0));

		// Act
		var result = framework.RemovePlatformVersion();

		// Assert
		result.Framework.AssertEqual(".NETCoreApp");
		result.Version.Major.AssertEqual(6);
		result.Version.Minor.AssertEqual(0);
		result.HasPlatform.AssertFalse();
	}

	[TestMethod]
	public void CreatePrivateHttp_ValidApiKey_ReturnsHttpClientWithHeader()
	{
		// Arrange
		var apiKey = "test-api-key-123";

		// Act
		using var http = NugetExtensions.CreatePrivateHttp(apiKey);

		// Assert
		http.AssertNotNull();
		var headers = http.DefaultRequestHeaders;
		headers.Contains(ProtocolConstants.ApiKeyHeader).AssertTrue();
		var values = headers.GetValues(ProtocolConstants.ApiKeyHeader).ToArray();
		values.Length.AssertEqual(1);
		values[0].AssertEqual(apiKey);
	}

	[TestMethod]
	public void Increment_PrereleaseVersion_ReturnsIncrementedPatchWithoutPrerelease()
	{
		// Arrange
		var version = new NuGetVersion("1.2.3-beta");

		// Act
		var result = version.Increment();

		// Assert
		result.Major.AssertEqual(1);
		result.Minor.AssertEqual(2);
		result.Patch.AssertEqual(4);
		result.IsPrerelease.AssertFalse();
	}

	[TestMethod]
	public void WithSuffix_ComplexSuffix_HandlesCorrectly()
	{
		// Arrange
		var version = new NuGetVersion("2.0.0");
		var suffix = "rc.1";

		// Act
		var result = version.WithSuffix(suffix);

		// Assert
		result.Major.AssertEqual(2);
		result.Minor.AssertEqual(0);
		result.Patch.AssertEqual(0);
		result.Release.AssertEqual("rc.1");
		result.IsPrerelease.AssertTrue();
	}

	[TestMethod]
	public void GetNuspecAsync_NullHttp_ThrowsArgumentNullException()
	{
		// Arrange
		HttpClient http = null;
		var baseUrl = new Uri("https://example.com/");
		var packageId = "TestPackage";
		var version = new NuGetVersion("1.0.0");

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() => http.GetNuspecAsync(baseUrl, packageId, version, CancellationToken));
	}

	[TestMethod]
	public void GetNuspecAsync_NullBaseUrl_ThrowsArgumentNullException()
	{
		// Arrange
		using var http = new HttpClient();
		Uri baseUrl = null;
		var packageId = "TestPackage";
		var version = new NuGetVersion("1.0.0");

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() => http.GetNuspecAsync(baseUrl, packageId, version, CancellationToken));
	}

	[TestMethod]
	public void GetNuspecAsync_EmptyPackageId_ThrowsArgumentNullException()
	{
		// Arrange
		using var http = new HttpClient();
		var baseUrl = new Uri("https://example.com/");
		var packageId = "";
		var version = new NuGetVersion("1.0.0");

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() => http.GetNuspecAsync(baseUrl, packageId, version, CancellationToken));
	}

	[TestMethod]
	public void GetNuspecAsync_NullVersion_ThrowsArgumentNullException()
	{
		// Arrange
		using var http = new HttpClient();
		var baseUrl = new Uri("https://example.com/");
		var packageId = "TestPackage";
		NuGetVersion version = null;

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() => http.GetNuspecAsync(baseUrl, packageId, version, CancellationToken));
	}

	[TestMethod]
	[TestCategory("Integration")]
	public async Task GetNuspecAsync_RealPackage_ReturnsValidNuspec()
	{
		// Arrange
		const string packageId = "Ecng.Common";
		var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
		var cache = new SourceCacheContext();
		var logger = NullLogger.Instance;

		var baseUrl = await repo.GetBaseUrl(CancellationToken);
		var lastVersion = await repo.GetLastVersionAsync(packageId, allowPreview: false, logger, cache, CancellationToken);

		// Act
		using var http = new HttpClient();
		using var nuspecStream = await http.GetNuspecAsync(baseUrl, packageId, lastVersion, CancellationToken);

		// Assert
		nuspecStream.AssertNotNull();
		if (nuspecStream.CanSeek)
			nuspecStream.Position = 0;

		var doc = XDocument.Load(nuspecStream);
		var metadata = doc.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "metadata");
		metadata.AssertNotNull();

		var id = metadata.Elements().FirstOrDefault(e => e.Name.LocalName == "id")?.Value;
		id.AssertEqual(packageId);

		var versionStr = metadata.Elements().FirstOrDefault(e => e.Name.LocalName == "version")?.Value;
		versionStr.AssertEqual(lastVersion.ToNormalizedString());
	}

	[TestMethod]
	[TestCategory("Integration")]
	public async Task GetAllVersionsOrderedAsync_RealPackage_ReturnsOrderedVersions()
	{
		// Arrange
		var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
		var cache = new SourceCacheContext();
		var logger = NullLogger.Instance;

		// Act
		var versions = await repo.GetAllVersionsOrderedAsync("Ecng.Common", logger, cache, CancellationToken);

		// Assert
		versions.AssertNotNull();
		(versions.Length > 0).AssertTrue($"versions.Length={versions.Length} should be >0");

		// Verify ordering
		for (int i = 1; i < versions.Length; i++)
		{
			(versions[i] >= versions[i - 1]).AssertTrue();
		}
	}

	[TestMethod]
	[TestCategory("Integration")]
	public async Task GetLastVersionAsync_RealPackage_WithoutPrerelease_ReturnsStableVersion()
	{
		// Arrange
		var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
		var cache = new SourceCacheContext();
		var logger = NullLogger.Instance;

		// Act
		var version = await repo.GetLastVersionAsync("Ecng.Common", allowPreview: false, logger, cache, CancellationToken);

		// Assert
		version.AssertNotNull();
		version.IsPrerelease.AssertFalse();
		(version.Major >= 1).AssertTrue();
	}

	[TestMethod]
	[TestCategory("Integration")]
	public async Task GetLastVersionAsync_RealPackage_WithPrerelease_ReturnsAnyVersion()
	{
		// Arrange
		var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
		var cache = new SourceCacheContext();
		var logger = NullLogger.Instance;

		// Act
		var version = await repo.GetLastVersionAsync("Ecng.Common", allowPreview: true, logger, cache, CancellationToken);

		// Assert
		version.AssertNotNull();
		(version.Major >= 1).AssertTrue();
	}

	[TestMethod]
	[TestCategory("Integration")]
	public async Task GetLastVersionInFloatingRangeAsync_ValidRange_ReturnsVersionInRange()
	{
		// Arrange
		var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
		var cache = new SourceCacheContext();
		var logger = NullLogger.Instance;

		// Act - get latest 1.* version
		var version = await repo.GetLastVersionInFloatingRangeAsync("Ecng.Common", "1.*", logger, cache, CancellationToken);

		// Assert
		version.AssertNotNull();
		version.Major.AssertEqual(1);
	}

	[TestMethod]
	[TestCategory("Integration")]
	public Task GetLastVersionInFloatingRangeAsync_InvalidRange_ThrowsArgumentException()
	{
		// Arrange
		var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
		var cache = new SourceCacheContext();
		var logger = NullLogger.Instance;

		// Act & Assert
		return ThrowsExactlyAsync<ArgumentException>(() =>
			repo.GetLastVersionInFloatingRangeAsync("Ecng.Common", "invalid-range", logger, cache, CancellationToken));
	}

	[TestMethod]
	[TestCategory("Integration")]
	public async Task GetBaseUrl_ValidRepository_ReturnsUrlWithTrailingSlash()
	{
		// Arrange
		var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");

		// Act
		var baseUrl = await repo.GetBaseUrl(CancellationToken);

		// Assert
		baseUrl.AssertNotNull();
		baseUrl.ToString().AssertContains("nuget.org");
		baseUrl.ToString().EndsWith('/').AssertTrue();
	}

	[TestMethod]
	public Task GetBaseUrl_NullRepository_ThrowsArgumentNullException()
	{
		// Arrange
		SourceRepository repo = null;

		// Act & Assert
		return ThrowsExactlyAsync<ArgumentNullException>(() =>
			repo.GetBaseUrl(CancellationToken));
	}

	[TestMethod]
	[TestCategory("Integration")]
	public async Task GetTargetFrameworks_RealPackage_ReturnsFrameworks()
	{
		// Arrange
		var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
		var cache = new SourceCacheContext();
		var logger = NullLogger.Instance;

		var resource = await repo.GetResourceAsync<FindPackageByIdResource>(CancellationToken);
		using var packageStream = new MemoryStream();
		await resource.CopyNupkgToStreamAsync("Ecng.Common", new NuGetVersion("1.0.0"), packageStream, cache, logger, CancellationToken);
		packageStream.Position = 0;

		// Act
		using var reader = new PackageArchiveReader(packageStream);
		var frameworks = reader.GetTargetFrameworks();

		// Assert
		frameworks.AssertNotNull();
		(frameworks.Length > 0).AssertTrue($"frameworks.Length={frameworks.Length} should be >0");
	}

	[TestMethod]
	public void DisableNugetConfig_ReplacesProxyCacheInstance()
	{
		NugetExtensions.DisableNugetConfig();

		var instance = ProxyCache.Instance;
		instance.AssertNotNull();

		// replaced proxy should return null (no proxy configured in dummy settings)
		Assert.IsNull(instance.GetUserConfiguredProxy());
		Assert.IsNull(instance.GetProxy(new Uri("https://api.nuget.org/v3/index.json")));
	}

	[TestMethod]
	public void ParseFeedVersions_SkipsWhatItCannotRead()
	{
		var versions = NugetExtensions.ParseFeedVersions("""{"versions":["1.0.0","not-a-version","1.10.0","1.9.0"]}""");

		versions.Length.AssertEqual(3);
		versions[0].ToNormalizedString().AssertEqual("1.0.0");
		versions[1].ToNormalizedString().AssertEqual("1.9.0");
		versions[2].ToNormalizedString().AssertEqual("1.10.0");
	}

	[TestMethod]
	public void ParseFeedVersions_WithoutVersions_ReturnsEmpty()
	{
		NugetExtensions.ParseFeedVersions("{}").Length.AssertEqual(0);
		NugetExtensions.ParseFeedVersions("""{"versions":{}}""").Length.AssertEqual(0);
	}

	[TestMethod]
	[TestCategory("Integration")]
	public async Task GetFeedVersionsAsync_RealPackage_ReturnsOrderedVersions()
	{
		var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");

		using var http = new HttpClient();

		var versions = await repo.GetFeedVersionsAsync(http, "Ecng.Common", CancellationToken);

		versions.AssertNotNull();
		(versions.Length > 0).AssertTrue($"versions.Length={versions.Length} should be >0");

		for (var i = 1; i < versions.Length; i++)
			(versions[i] >= versions[i - 1]).AssertTrue();
	}

	[TestMethod]
	[TestCategory("Integration")]
	public async Task GetFeedVersionsAsync_UnknownPackage_ReturnsEmpty()
	{
		// A feed that does not carry the package answers 404, and the caller reads that as "not published
		// here" -- not as a failure.
		var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");

		using var http = new HttpClient();

		(await repo.GetFeedVersionsAsync(http, "Ecng.NoSuchPackage.Test", CancellationToken)).Length.AssertEqual(0);
	}

	[TestMethod]
	[TestCategory("Integration")]
	public async Task SearchAllAsync_RealPackage_CarriesTheDownloadCount()
	{
		var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");

		var found = await repo.SearchAllAsync("packageid:Ecng.Common", allowPreview: false, NullLogger.Instance, CancellationToken);

		found.AssertNotNull();
		(found.Length > 0).AssertTrue($"found.Length={found.Length} should be >0");

		var package = found.First(p => p.Identity.Id.EqualsIgnoreCase("Ecng.Common"));

		package.Identity.Version.AssertNotNull();
		(package.DownloadCount > 0).AssertTrue($"DownloadCount={package.DownloadCount} should be >0");
	}
	// The registration index of a package, in the shape nuget.org serves it: pages that carry their versions inline,
	// or -- for a package with many versions -- only the address to read them from.
	private const string _registrations = "https://feed.test/registration/";

	private static string Leaf(string version, string published, string framework)
		=> $$"""
		{
			"@id": "{{_registrations}}some.package/{{version}}.json",
			"catalogEntry": {
				"@id": "https://feed.test/catalog/some.package.{{version}}.json",
				"id": "Some.Package",
				"version": "{{version}}",
				"published": "{{published}}",
				"listed": true,
				"dependencyGroups": [ { "targetFramework": "{{framework}}" } ]
			},
			"packageContent": "https://feed.test/flat/some.package/{{version}}/some.package.{{version}}.nupkg"
		}
		""";

	private static string Page(string url, string lower, string upper, params string[] leaves)
		=> $$"""
		{
			"@id": "{{url}}",
			"count": {{leaves.Length}},
			"lower": "{{lower}}",
			"upper": "{{upper}}"{{(leaves.Length == 0 ? string.Empty : ", \"items\": [" + leaves.JoinComma() + "]")}}
		}
		""";

	private static string Index(params string[] pages)
		=> $$"""{ "count": {{pages.Length}}, "items": [ {{pages.JoinComma()}} ] }""";

	private sealed class FeedHandler(Dictionary<string, string> documents, bool gzip = false) : HttpMessageHandler
	{
		public List<string> Requested { get; } = [];

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var url = request.RequestUri.AbsoluteUri;
			Requested.Add(url);

			if (!documents.TryGetValue(url, out var body))
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

			if (!gzip)
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });

			using var packed = new MemoryStream();

			using (var zip = new GZipStream(packed, CompressionLevel.Optimal, true))
				zip.Write(Encoding.UTF8.GetBytes(body));

			var content = new ByteArrayContent(packed.ToArray());
			content.Headers.ContentEncoding.Add("gzip");

			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
		}
	}

	private static string IndexUrl => _registrations + "some.package/index.json";

	[TestMethod]
	public async Task GetFeedMetadataAsync_InlinePage_DescribesEveryVersion()
	{
		var handler = new FeedHandler(new()
		{
			[IndexUrl] = Index(Page(IndexUrl + "#page/1.0.0/1.1.0", "1.0.0", "1.1.0",
				Leaf("1.1.0", "2025-02-03T04:05:06+00:00", "net10.0"),
				Leaf("1.0.0", "2025-01-02T00:00:00+00:00", "net6.0"))),
		});

		using var http = new HttpClient(handler);

		var versions = await http.GetFeedMetadataAsync(new Uri(_registrations), "Some.Package", CancellationToken);

		versions.Select(v => v.Identity.Version.ToString()).ToArray().AssertEqual(["1.0.0", "1.1.0"]);
		versions[0].Published.Value.UtcDateTime.AssertEqual(new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc));
		versions[1].Published.Value.UtcDateTime.AssertEqual(new DateTime(2025, 2, 3, 4, 5, 6, DateTimeKind.Utc));
		versions[0].DependencySets.Single().TargetFramework.GetShortFolderName().AssertEqual("net6.0");
		versions[1].DependencySets.Single().TargetFramework.GetShortFolderName().AssertEqual("net10.0");
	}

	[TestMethod]
	public async Task GetFeedMetadataAsync_PageNotInline_IsReadFromItsAddress()
	{
		const string pageUrl = _registrations + "some.package/page/1.0.0/1.1.0.json";

		var handler = new FeedHandler(new()
		{
			[IndexUrl] = Index(Page(pageUrl, "1.0.0", "1.1.0")),
			[pageUrl] = Page(pageUrl, "1.0.0", "1.1.0",
				Leaf("1.0.0", "2025-01-02T00:00:00+00:00", "net6.0"),
				Leaf("1.1.0", "2025-02-03T00:00:00+00:00", "net10.0")),
		});

		using var http = new HttpClient(handler);

		var versions = await http.GetFeedMetadataAsync(new Uri(_registrations), "Some.Package", CancellationToken);

		versions.Length.AssertEqual(2);
		IsTrue(handler.Requested.Contains(pageUrl), handler.Requested.JoinComma());
	}

	[TestMethod]
	public async Task GetFeedMetadataAsync_GzipEncodedAnswer_IsRead()
	{
		// nuget.org serves the registrations that know SemVer 2.0.0 gzip-encoded, whether the client asked for it or not.
		var handler = new FeedHandler(new()
		{
			[IndexUrl] = Index(Page(IndexUrl + "#page/1.0.0/1.0.0", "1.0.0", "1.0.0",
				Leaf("1.0.0", "2025-01-02T00:00:00+00:00", "net6.0"))),
		}, gzip: true);

		using var http = new HttpClient(handler);

		var versions = await http.GetFeedMetadataAsync(new Uri(_registrations), "Some.Package", CancellationToken);

		versions.Single().Identity.Version.ToString().AssertEqual("1.0.0");
	}

	[TestMethod]
	public async Task GetFeedMetadataAsync_PackageTheFeedDoesNotCarry_IsEmpty()
	{
		using var http = new HttpClient(new FeedHandler([]));

		var versions = await http.GetFeedMetadataAsync(new Uri(_registrations), "Some.Package", CancellationToken);

		versions.Length.AssertEqual(0);
	}

	[TestMethod]
	[TestCategory("Integration")]
	public async Task GetFeedMetadataAsync_RealPackage_DescribesTheVersionsTheFeedLists()
	{
		const string packageId = "Ecng.Common";
		var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");

		using var http = new HttpClient();

		var listed = await repo.GetFeedVersionsAsync(http, packageId, CancellationToken);
		var described = await repo.GetFeedMetadataAsync(http, packageId, CancellationToken);

		described.Last().Identity.Version.AssertEqual(listed.Last());

		var newest = described.Last();
		(newest.Published > new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)).AssertTrue($"published {newest.Published}");
		newest.DependencySets.Any(g => !g.TargetFramework.IsAny).AssertTrue("no target framework");
	}
}
