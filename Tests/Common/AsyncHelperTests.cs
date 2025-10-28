namespace Ecng.Tests.Common;

[TestClass]
public class AsyncHelperTests : BaseTestClass
{
	[TestMethod]
	public async Task WithCancellationCancel()
	{
		using var cts = new CancellationTokenSource();
		var task = Task.Delay(1000, CancellationToken).WithCancellation(cts.Token);
		cts.Cancel();
		await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await task);
	}

	[TestMethod]
	public async Task WhenAllValueTasks()
	{
		var res = await AsyncHelper.WhenAll([new ValueTask<int>(1), new ValueTask<int>(2)]);
		res.AssertEqual([1, 2]);
	}

	[TestMethod]
	public void RunSync()
	{
		var result = AsyncHelper.Run(() => new ValueTask<int>(3));
		result.AssertEqual(3);
	}

	[TestMethod]
	public async Task CheckNull()
	{
		await AsyncHelper.CheckNull((Task)null);
		await AsyncHelper.CheckNull((ValueTask?)null);
	}

	[TestMethod]
	public void CreateChildTokenCancel()
	{
		using var cts = new CancellationTokenSource();
		var (childCts, token) = cts.Token.CreateChildToken();
		cts.Cancel();
		token.IsCancellationRequested.AssertTrue();
		childCts.Dispose();
	}

	[TestMethod]
	public async Task AsValueTaskConversions()
	{
		var vt = new ValueTask<int>(4);
		await vt.AsValueTask();

		var task = 5.FromResult();
		(await task.AsValueTask()).AssertEqual(5);
	}

	[TestMethod]
	public async Task WhenAllFailure()
	{
		var err = new InvalidOperationException();
		var tasks = new[] { new ValueTask<int>(Task.FromException<int>(err)) };
		await Assert.ThrowsExactlyAsync<AggregateException>(async () => await AsyncHelper.WhenAll(tasks));
	}

	[TestMethod]
	public void GetResultAndTcs()
	{
		var task = 6.FromResult();
		task.GetResult<int>().AssertEqual(6);

		var tcs = 7.ToCompleteSource();
		tcs.Task.Result.AssertEqual(7);
	}

	[TestMethod]
	public async Task TimeoutTokenAndWhenCanceled()
	{
		var token = TimeSpan.FromMilliseconds(10).CreateTimeoutToken();
		await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await token.WhenCanceled());
	}

	[TestMethod]
	public async Task CatchHandleError()
	{
		bool error = false, final = false;
		await AsyncHelper.CatchHandle(
			() => Task.FromException(new InvalidOperationException()),
			CancellationToken,
			e => error = true,
			finalizer: () => final = true);
		error.AssertTrue();
		final.AssertTrue();
	}
}