namespace Ecng.Tests.Nuget;

using System.Net.Http;

using Ecng.Nuget;

using NuGet.Frameworks;
using NuGet.Protocol;
using NuGet.Versioning;

[TestClass]
public class NugetExtensionsTests
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
		Assert.ThrowsExactly<ArgumentNullException>(() => version.Increment());
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
		Assert.ThrowsExactly<ArgumentNullException>(() => version.WithSuffix("beta"));
	}

	[TestMethod]
	public void WithSuffix_EmptySuffix_ThrowsArgumentNullException()
	{
		// Arrange
		var version = new NuGetVersion("1.2.3");

		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => version.WithSuffix(""));
	}

	[TestMethod]
	public void WithSuffix_NullSuffix_ThrowsArgumentNullException()
	{
		// Arrange
		var version = new NuGetVersion("1.2.3");

		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => version.WithSuffix(null));
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
		Assert.ThrowsExactly<ArgumentNullException>(() => http.GetNuspecAsync(baseUrl, packageId, version, CancellationToken.None));
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
		Assert.ThrowsExactly<ArgumentNullException>(() => http.GetNuspecAsync(baseUrl, packageId, version, CancellationToken.None));
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
		Assert.ThrowsExactly<ArgumentNullException>(() => http.GetNuspecAsync(baseUrl, packageId, version, CancellationToken.None));
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
		Assert.ThrowsExactly<ArgumentNullException>(() => http.GetNuspecAsync(baseUrl, packageId, version, CancellationToken.None));
	}
}
