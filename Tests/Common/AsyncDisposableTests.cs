namespace Ecng.Tests.Common;

[TestClass]
public class AsyncDisposableTests : BaseTestClass
{
	private sealed class ThrowingAsyncDisposable : AsyncDisposable
	{
		protected override async ValueTask DisposeManaged()
		{
			await default(ValueTask).NoWait();
			throw new InvalidOperationException("dispose failure");
		}
	}

	/// <summary>
	/// Regression test for AsyncDisposable synchronous dispose: ensures Dispose()
	/// surfaces the same bare exception as DisposeAsync() (InvalidOperationException
	/// here) instead of wrapping it in an AggregateException. (Was: Dispose() bridged
	/// to the async path via DisposeAsync().AsTask().Wait(), wrapping the exception,
	/// Common\AsyncDisposable.cs:53.)
	/// </summary>
	[TestMethod]
	public void AsyncDisposable_SyncDispose_SurfacesBareException()
	{
		var disposable = new ThrowingAsyncDisposable();

		ThrowsExactly<InvalidOperationException>(() => disposable.Dispose());
	}

	/// <summary>
	/// Regression test for AsyncDisposable asynchronous dispose: ensures DisposeAsync()
	/// surfaces the bare InvalidOperationException, matching the synchronous Dispose()
	/// path asserted above. Common\AsyncDisposable.cs:41,53.
	/// </summary>
	[TestMethod]
	public async Task AsyncDisposable_AsyncDispose_SurfacesBareException()
	{
		var disposable = new ThrowingAsyncDisposable();

		await ThrowsExactlyAsync<InvalidOperationException>(async () => await disposable.DisposeAsync());
	}
}
