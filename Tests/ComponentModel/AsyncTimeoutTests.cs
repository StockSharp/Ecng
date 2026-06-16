namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class AsyncTimeoutTests : BaseTestClass
{
	[TestMethod]
	public async Task Fires_WhenNotDisposed()
	{
		var timeout = new AsyncTimeout(TimeSpan.FromMilliseconds(50));
		var fired = new TaskCompletionSource();

		timeout.Register(() => fired.TrySetResult());

		var completed = await Task.WhenAny(fired.Task, Task.Delay(2000, CancellationToken));
		(completed == fired.Task).AssertTrue("The timeout action should have fired.");
	}

	[TestMethod]
	public async Task Disposed_DoesNotFire()
	{
		var timeout = new AsyncTimeout(TimeSpan.FromMilliseconds(50));
		var fired = false;

		// Dispose immediately, well before the 50 ms window elapses.
		using (timeout.Register(() => fired = true))
		{
		}

		await Task.Delay(300, CancellationToken);

		fired.AssertFalse("A disposed timeout registration must not fire.");
	}

	[TestMethod]
	public async Task ThrowingCallback_DoesNotCrash()
	{
		var timeout = new AsyncTimeout(TimeSpan.FromMilliseconds(50));

		timeout.Register(() => throw new InvalidOperationException("boom"));

		// An unobserved exception from the timer callback would crash the process; reaching the
		// assertion after the timeout has fired proves it was swallowed.
		await Task.Delay(300, CancellationToken);

		true.AssertTrue();
	}
}
