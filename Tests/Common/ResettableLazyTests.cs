namespace Ecng.Tests.Common;

using Nito.AsyncEx;

[TestClass]
public class ResettableLazyTests
{
	[TestMethod]
	public void Reset()
	{
		var invokeCount = 0;

		int GetValue()
			=> ++invokeCount;

		var lazy = new ResettableLazy<int>(GetValue);
		lazy.Value.AssertEqual(1);
		lazy.Value.AssertEqual(1);
		lazy.IsValueCreated.AssertTrue();

		lazy.Reset();
		lazy.IsValueCreated.AssertFalse();
		lazy.Value.AssertEqual(2);
		lazy.Value.AssertEqual(2);
	}

	[TestMethod]
	public void SetValue()
	{
		var lazy = new ResettableLazy<int>(() => 42);
		lazy.SetValue(100);
		lazy.Value.AssertEqual(100);
		lazy.IsValueCreated.AssertTrue();
	}

	[TestMethod]
	public void SetValueAfterCreation()
	{
		var lazy = new ResettableLazy<int>(() => 42);
		lazy.Value.AssertEqual(42);
		lazy.SetValue(100);
		lazy.Value.AssertEqual(100);
	}

	[TestMethod]
	public void ThreadSafetyModeNone()
	{
		var invokeCount = 0;
		var lazy = new ResettableLazy<int>(
			() => ++invokeCount,
			LazyThreadSafetyMode.None);

		lazy.Value.AssertEqual(1);
		lazy.Reset();
		lazy.Value.AssertEqual(2);
	}

	[TestMethod]
	public void ThreadSafetyModePublicationOnly()
	{
		var invokeCount = 0;
		var lazy = new ResettableLazy<int>(
			() => ++invokeCount,
			LazyThreadSafetyMode.PublicationOnly);

		lazy.Value.AssertEqual(1);
		lazy.Reset();
		lazy.Value.AssertEqual(2);
	}

	[TestMethod]
	public void ThreadSafetyModeExecutionAndPublication()
	{
		var invokeCount = 0;
		var lazy = new ResettableLazy<int>(
			() => ++invokeCount,
			LazyThreadSafetyMode.ExecutionAndPublication);

		lazy.Value.AssertEqual(1);
		lazy.Reset();
		lazy.Value.AssertEqual(2);
	}

	[TestMethod]
	public void ImplicitConversion()
	{
		var lazy = new ResettableLazy<int>(() => 42);
		int value = lazy;
		value.AssertEqual(42);
	}

	[TestMethod]
	public void ImplicitConversionNull()
	{
		ResettableLazy<int> lazy = null;
		int value = lazy;
		value.AssertEqual(0);
	}

	[TestMethod]
	public void ToStringNotCreated()
	{
		var lazy = new ResettableLazy<int>(() => 42);
		lazy.ToString().AssertEqual("Value is not created.");
	}

	[TestMethod]
	public void ToStringCreated()
	{
		var lazy = new ResettableLazy<int>(() => 42);
		var _ = lazy.Value;
		lazy.ToString().AssertEqual("42");
	}

	[TestMethod]
	public void ToStringNullValue()
	{
		var lazy = new ResettableLazy<string>(() => null);
		var _ = lazy.Value;
		lazy.ToString().AssertEqual(string.Empty);
	}

	[TestMethod]
	public void NullFactoryThrows()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => new ResettableLazy<int>(null));
	}

	[TestMethod]
	public void ResetReferenceType()
	{
		var invokeCount = 0;
		var lazy = new ResettableLazy<string>(() => $"Value_{++invokeCount}");

		lazy.Value.AssertEqual("Value_1");
		lazy.Reset();
		lazy.Value.AssertEqual("Value_2");
	}

	[TestMethod]
	public async Task ConcurrentAccess()
	{
		var invokeCount = 0;
		var lazy = new ResettableLazy<int>(() =>
		{
			Thread.Sleep(10);
			return Interlocked.Increment(ref invokeCount);
		});

		var tasks = new Task<int>[10];
		for (int i = 0; i < tasks.Length; i++)
		{
			tasks[i] = Task.Run(() => lazy.Value);
		}

		await tasks.WhenAll();

		// Factory should be called only once
		invokeCount.AssertEqual(1);

		// All tasks should get the same value
		foreach (var task in tasks)
		{
			task.Result.AssertEqual(1);
		}
	}

	[TestMethod]
	public void ResetAfterException()
	{
		var invokeCount = 0;
		var lazy = new ResettableLazy<int>(() =>
		{
			invokeCount++;
			if (invokeCount == 1)
				throw new InvalidOperationException("First call");
			return invokeCount;
		});

		Assert.ThrowsExactly<InvalidOperationException>(() => _ = lazy.Value);
		lazy.IsValueCreated.AssertFalse();

		lazy.Reset();
		lazy.Value.AssertEqual(2);
		lazy.IsValueCreated.AssertTrue();
	}
}
