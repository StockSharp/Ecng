namespace Ecng.UnitTesting;

using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Base test class.
/// </summary>
public abstract class BaseTestClass
{
	/// <summary>
	/// <see cref="TestContext"/>
	/// </summary>
	public TestContext TestContext { get; set; }

	/// <summary>
	/// Cancellation token for the test.
	/// </summary>
	protected CancellationToken CancellationToken => TestContext.CancellationToken;
}
