namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class LifetimeServiceTests : BaseTestClass
{
	private class MockLifetimeService : ILifetimeService
	{
		private readonly CancellationTokenSource _cts = new();

		public CancellationToken Token => _cts.Token;

		public bool ShutdownCalled { get; private set; }
		public bool RestartCalled { get; private set; }

		public void Shutdown()
		{
			ShutdownCalled = true;
			_cts.Cancel();
		}

		public void Restart()
		{
			RestartCalled = true;
			_cts.Cancel();
		}
	}

	[TestMethod]
	public void Token_InitiallyNotCancelled()
	{
		var service = new MockLifetimeService();
		service.Token.IsCancellationRequested.AssertFalse();
	}

	[TestMethod]
	public void Shutdown_CancelsToken()
	{
		var service = new MockLifetimeService();

		service.Shutdown();

		service.ShutdownCalled.AssertTrue();
		service.Token.IsCancellationRequested.AssertTrue();
	}

	[TestMethod]
	public void Restart_CancelsToken()
	{
		var service = new MockLifetimeService();

		service.Restart();

		service.RestartCalled.AssertTrue();
		service.Token.IsCancellationRequested.AssertTrue();
	}

	[TestMethod]
	public void Token_CanBeUsedWithTaskDelay()
	{
		var service = new MockLifetimeService();

		var task = Task.Delay(10000, service.Token);

		service.Shutdown();

		ThrowsExactly<TaskCanceledException>(() => task.GetAwaiter().GetResult());
	}

	[TestMethod]
	public void MultipleShutdownCalls_DoNotThrow()
	{
		var service = new MockLifetimeService();

		service.Shutdown();
		// Second call should not throw
		service.Shutdown();

		service.Token.IsCancellationRequested.AssertTrue();
	}

	[TestMethod]
	public void AppTokenLifetimeService_TokenReturnsAppToken()
	{
		var service = new AppTokenLifetimeService(() => { });

		service.Token.AssertEqual(AppToken.Value);
	}

	[TestMethod]
	public void AppTokenLifetimeService_RestartCallsDelegate()
	{
		var restartCalled = false;
		var service = new AppTokenLifetimeService(() => restartCalled = true);

		service.Restart();

		restartCalled.AssertTrue();
	}

	[TestMethod]
	public void AppTokenLifetimeService_NullRestartThrows()
	{
		ThrowsExactly<ArgumentNullException>(() => new AppTokenLifetimeService(null));
	}
}
