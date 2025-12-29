namespace Ecng.Tests.Common;

[TestClass]
public class WatchTests : BaseTestClass
{
	[TestMethod]
	public void NullAction()
	{
		// Arrange
		Action action = null;

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() => Watch.Do(action), "expected ArgumentNullException when action is null");
	}

	[TestMethod]
	public void ValidAction()
	{
		// Arrange
		var executed = false;
		void action()
		{
			Thread.Sleep(10);
			executed = true;
		}

		// Act
		var elapsed = Watch.Do(action);

		// Assert
		executed.AssertTrue("action was not executed");

		var totalMls = elapsed.TotalMilliseconds;
		(totalMls >= 10).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be >=10");
		(totalMls < 1000).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be <1000");
	}

	[TestMethod]
	public void EmptyAction()
	{
		// Arrange
		static void action()
		{ }

		// Act
		var elapsed = Watch.Do(action);

		// Assert
		var totalMls = elapsed.TotalMilliseconds;
		var totalSec = elapsed.TotalSeconds;
		(totalMls >= 0).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be >=0");
		(totalSec < 1).AssertTrue($"elapsed.TotalSeconds={totalSec} should be <1");
	}

	[TestMethod]
	public void ActionThrowsException()
	{
		// Arrange
		var expectedException = new InvalidOperationException("test exception");
		void action() => throw expectedException;

		// Act & Assert
		var thrown = ThrowsExactly<InvalidOperationException>(() => Watch.Do(action), "ActionThrowsException: expected InvalidOperationException");
		thrown.Message.AssertEqual("test exception", "exception message mismatch");
	}

	[TestMethod]
	public void ActionWith100msDelay()
	{
		// Arrange
		static void action() => Thread.Sleep(100);

		// Act
		var elapsed = Watch.Do(action);

		// Assert
		var totalMls = (int)elapsed.TotalMilliseconds;
		(elapsed.TotalMilliseconds >= 90).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be >=90 ms");
		(elapsed.TotalMilliseconds <= 1000).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be <=1000 ms");
	}

	[TestMethod]
	public Task NullAsyncAction()
	{
		// Arrange
		Func<Task> func = null;

		// Act & Assert
		return ThrowsExactlyAsync<ArgumentNullException>(() => Watch.DoAsync(func), "expected ArgumentNullException when func is null");
	}

	[TestMethod]
	public async Task ValidAsyncAction()
	{
		// Arrange
		var executed = false;
		async Task action()
		{
			await Task.Delay(10, CancellationToken);
			executed = true;
		}

		// Act
		var elapsed = await Watch.DoAsync(action);

		// Assert
		executed.AssertTrue("action was not executed");

		var totalMls = elapsed.TotalMilliseconds;
		(totalMls >= 10).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be >=10");
		(totalMls < 1000).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be <1000");
	}

	[TestMethod]
	public async Task EmptyAsyncAction()
	{
		// Arrange
		static Task action() => Task.CompletedTask;

		// Act
		var elapsed = await Watch.DoAsync(action);

		// Assert
		var totalMls = elapsed.TotalMilliseconds;
		var totalSec = elapsed.TotalSeconds;
		(totalMls >= 0).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be >=0");
		(totalSec < 1).AssertTrue($"elapsed.TotalSeconds={totalSec} should be <1");
	}

	[TestMethod]
	public async Task AsyncActionThrowsException()
	{
		// Arrange
		var expectedException = new InvalidOperationException("test async exception");
		Task action() => throw expectedException;

		// Act & Assert
		var thrown = await ThrowsExactlyAsync<InvalidOperationException>(() => Watch.DoAsync(action), "AsyncActionThrowsException: expected InvalidOperationException");
		thrown.Message.AssertEqual("test async exception", "exception message mismatch");
	}

	[TestMethod]
	public async Task AsyncActionWith100msDelay()
	{
		// Arrange
		Task action() => Task.Delay(100, CancellationToken);

		// Act
		var elapsed = await Watch.DoAsync(action);

		// Assert
		var totalMls = (int)elapsed.TotalMilliseconds;
		(elapsed.TotalMilliseconds >= 90).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be >=90 ms");
		(elapsed.TotalMilliseconds <= 1000).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be <=1000 ms");
	}

	[TestMethod]
	public Task NullTypedTaskFunc()
	{
		// Arrange
		Func<Task<int>> func = null;

		// Act & Assert
		return ThrowsExactlyAsync<ArgumentNullException>(() => Watch.DoAsync(func), "expected ArgumentNullException when func is null");
	}

	[TestMethod]
	public async Task ValidTypedTaskFunc()
	{
		// Arrange
		async Task<int> func()
		{
			await Task.Delay(50, CancellationToken);
			return 42;
		}

		// Act
		var (result, elapsed) = await Watch.DoAsync(func);

		// Assert
		result.AssertEqual(42, "result should be 42");

		var totalMls = elapsed.TotalMilliseconds;
		(totalMls >= 40).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be >=40");
		(totalMls < 1000).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be <1000");
	}

	[TestMethod]
	public async Task TypedTaskFuncThrowsException()
	{
		// Arrange
		var expectedException = new InvalidOperationException("test typed task exception");
		Task<string> func() => throw expectedException;

		// Act & Assert
		var thrown = await ThrowsExactlyAsync<InvalidOperationException>(() => Watch.DoAsync(func), "TypedTaskFuncThrowsException: expected InvalidOperationException");
		thrown.Message.AssertEqual("test typed task exception", "exception message mismatch");
	}

	[TestMethod]
	public async Task NullValueTaskFunc()
	{
		// Arrange
		Func<ValueTask> func = null;

		// Act & Assert
		await ThrowsExactlyAsync<ArgumentNullException>(() => Watch.DoAsync(func).AsTask(), "expected ArgumentNullException when func is null");
	}

	[TestMethod]
	public async Task ValidValueTaskFunc()
	{
		// Arrange
		var executed = false;
		async ValueTask action()
		{
			await Task.Delay(50, CancellationToken);
			executed = true;
		}

		// Act
		var elapsed = await Watch.DoAsync(action);

		// Assert
		executed.AssertTrue("action was not executed");

		var totalMls = elapsed.TotalMilliseconds;
		(totalMls >= 40).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be >=40");
		(totalMls < 1000).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be <1000");
	}

	[TestMethod]
	public async Task ValueTaskFuncThrowsException()
	{
		// Arrange
		var expectedException = new InvalidOperationException("test valuetask exception");
		ValueTask action() => throw expectedException;

		// Act & Assert
		var thrown = await ThrowsExactlyAsync<InvalidOperationException>(() => Watch.DoAsync(action).AsTask(), "ValueTaskFuncThrowsException: expected InvalidOperationException");
		thrown.Message.AssertEqual("test valuetask exception", "exception message mismatch");
	}

	[TestMethod]
	public async Task NullTypedValueTaskFunc()
	{
		// Arrange
		Func<ValueTask<string>> func = null;

		// Act & Assert
		await ThrowsExactlyAsync<ArgumentNullException>(() => Watch.DoAsync(func).AsTask(), "expected ArgumentNullException when func is null");
	}

	[TestMethod]
	public async Task ValidTypedValueTaskFunc()
	{
		// Arrange
		async ValueTask<string> func()
		{
			await Task.Delay(50, CancellationToken);
			return "test result";
		}

		// Act
		var (result, elapsed) = await Watch.DoAsync(func);

		// Assert
		result.AssertEqual("test result", "result should be 'test result'");

		var totalMls = elapsed.TotalMilliseconds;
		(totalMls >= 40).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be >=40");
		(totalMls < 1000).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be <1000");
	}

	[TestMethod]
	public async Task TypedValueTaskFuncThrowsException()
	{
		// Arrange
		var expectedException = new InvalidOperationException("test typed valuetask exception");
		ValueTask<bool> func() => throw expectedException;

		// Act & Assert
		var thrown = await ThrowsExactlyAsync<InvalidOperationException>(() => Watch.DoAsync(func).AsTask(), "TypedValueTaskFuncThrowsException: expected InvalidOperationException");
		thrown.Message.AssertEqual("test typed valuetask exception", "exception message mismatch");
	}

	[TestMethod]
	public async Task TypedTaskWith100msDelay()
	{
		// Arrange
		async Task<double> func()
		{
			await Task.Delay(100, CancellationToken);
			return 3.14;
		}

		// Act
		var (result, elapsed) = await Watch.DoAsync(func);

		// Assert
		result.AssertEqual(3.14, "result should be 3.14");

		var totalMls = (int)elapsed.TotalMilliseconds;
		(elapsed.TotalMilliseconds >= 90).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be >=90 ms");
		(elapsed.TotalMilliseconds <= 1000).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be <=1000 ms");
	}

	[TestMethod]
	public async Task ValueTaskWith100msDelay()
	{
		// Arrange
		ValueTask action() => new(Task.Delay(100, CancellationToken));

		// Act
		var elapsed = await Watch.DoAsync(action);

		// Assert
		var totalMls = (int)elapsed.TotalMilliseconds;
		(elapsed.TotalMilliseconds >= 90).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be >=90 ms");
		(elapsed.TotalMilliseconds <= 2000).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be <=2000 ms");
	}

	[TestMethod]
	public async Task TypedValueTaskWith100msDelay()
	{
		// Arrange
		async ValueTask<int> func()
		{
			await Task.Delay(100, CancellationToken);
			return 123;
		}

		// Act
		var (result, elapsed) = await Watch.DoAsync(func);

		// Assert
		result.AssertEqual(123, "result should be 123");

		var totalMls = (int)elapsed.TotalMilliseconds;
		(elapsed.TotalMilliseconds >= 90).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be >=90 ms");
		(elapsed.TotalMilliseconds <= 1000).AssertTrue($"elapsed.TotalMilliseconds={totalMls} should be <=1000 ms");
	}
}