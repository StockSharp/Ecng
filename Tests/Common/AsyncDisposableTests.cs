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
	/// BUG: AsyncDisposable.Dispose() bridges to the async path via
	/// DisposeAsync().AsTask().Wait(), so any exception thrown by DisposeManaged()
	/// is wrapped in an AggregateException — the synchronous and asynchronous
	/// dispose of the same object surface different exception types.
	/// Expected: synchronous Dispose() surfaces the same bare exception that
	/// DisposeAsync() surfaces (InvalidOperationException here), matching the
	/// GetAwaiter().GetResult() unwrapping recommended by the audit.
	/// Actual: Dispose() throws AggregateException wrapping InvalidOperationException.
	/// Common\AsyncDisposable.cs:53.
	/// </summary>
	[TestMethod]
	public void AsyncDisposable_SyncDispose_SurfacesBareException()
	{
		var disposable = new ThrowingAsyncDisposable();

		ThrowsExactly<InvalidOperationException>(() => disposable.Dispose());
	}

	/// <summary>
	/// Documents the asynchronous side of finding 24: DisposeAsync() already
	/// surfaces the bare InvalidOperationException. The synchronous Dispose()
	/// path (asserted above) must match this behaviour rather than wrapping it
	/// in an AggregateException. Common\AsyncDisposable.cs:41,53.
	/// </summary>
	[TestMethod]
	public async Task AsyncDisposable_AsyncDispose_SurfacesBareException()
	{
		var disposable = new ThrowingAsyncDisposable();

		await ThrowsExactlyAsync<InvalidOperationException>(async () => await disposable.DisposeAsync());
	}
}
