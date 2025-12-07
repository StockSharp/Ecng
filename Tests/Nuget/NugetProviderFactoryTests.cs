namespace Ecng.Tests.Nuget;

using System.Security;

using Ecng.Logging;
using Ecng.Nuget;

[TestClass]
public class NugetProviderFactoryTests : BaseTestClass
{
	[TestMethod]
	public void Constructor_NullLog_ThrowsArgumentNullException()
	{
		// Arrange
		ILogReceiver log = null;
		var token = "t".Secure().ReadOnly();

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() => new NugetProviderFactory(log, token));
	}

	[TestMethod]
	public void Constructor_NullToken_ThrowsArgumentNullException()
	{
		// Arrange
		var log = new LogReceiver("test");
		SecureString token = null;

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() => new NugetProviderFactory(log, token));
	}

	[TestMethod]
	public void Constructor_ValidParameters_CreatesInstance()
	{
		// Arrange
		var log = new LogReceiver("test");
		var token = "test".Secure().ReadOnly();

		// Act
		var factory = new NugetProviderFactory(log, token);

		// Assert
		factory.AssertNotNull();
	}

	[TestMethod]
	public void GetCoreV3_ReturnsProviders()
	{
		// Arrange
		var log = new LogReceiver("test");
		var token = "t".Secure().ReadOnly();
		var factory = new NugetProviderFactory(log, token);

		// Act
		var providers = factory.GetCoreV3().ToArray();

		// Assert
		providers.AssertNotNull();
		(providers.Length > 0).AssertTrue($"providers.Length={providers.Length} should be >0");
	}
}