namespace Ecng.Tests.Linq;

using System.Runtime.CompilerServices;

using Ecng.Linq;

[TestClass]
public class AsyncEnumerableExtensionsTests : BaseTestClass
{
	private static readonly TimeSpan _1ms = TimeSpan.FromMilliseconds(1);

	private static async IAsyncEnumerable<int> GetAsyncData([EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		for (int i = 1; i <= 5; i++)
		{
			await _1ms.Delay(cancellationToken);
			yield return i;
		}
	}

	#region WhereWithPrevious

	[TestMethod]
	public async Task WhereWithPrevious_Basic()
	{
		var token = CancellationToken;
		var result = await GetAsyncData(token)
			.WhereWithPrevious((prev, curr) => curr > prev)
			.ToArrayAsync(token);

		// All elements pass since each is greater than previous (1,2,3,4,5)
		result.AssertEqual([1, 2, 3, 4, 5]);
	}

	[TestMethod]
	public async Task WhereWithPrevious_FiltersSome()
	{
		var token = CancellationToken;
		var source = new[] { 1, 3, 2, 5, 4 }.ToAsyncEnumerable();

		var result = await source
			.WhereWithPrevious((prev, curr) => curr > prev)
			.ToArrayAsync(token);

		// 1 (first, always included), 3 (3>1), 5 (5>2)
		result.AssertEqual([1, 3, 5]);
	}

	[TestMethod]
	public async Task WhereWithPrevious_EmptySource()
	{
		var token = CancellationToken;
		var source = Array.Empty<int>().ToAsyncEnumerable();

		var result = await source
			.WhereWithPrevious((prev, curr) => curr > prev)
			.ToArrayAsync(token);

		result.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task WhereWithPrevious_SingleElement()
	{
		var token = CancellationToken;
		var source = new[] { 42 }.ToAsyncEnumerable();

		var result = await source
			.WhereWithPrevious((prev, curr) => curr > prev)
			.ToArrayAsync(token);

		// Single element is always included
		result.AssertEqual([42]);
	}

	[TestMethod]
	public async Task WhereWithPrevious_NonePass()
	{
		var token = CancellationToken;
		var source = new[] { 5, 4, 3, 2, 1 }.ToAsyncEnumerable();

		var result = await source
			.WhereWithPrevious((prev, curr) => curr > prev)
			.ToArrayAsync(token);

		// Only first element, none others pass
		result.AssertEqual([5]);
	}

	[TestMethod]
	public async Task WhereWithPrevious_NullSource_Throws()
	{
		var token = CancellationToken;
		await ThrowsExactlyAsync<ArgumentNullException>(() =>
		{
			IAsyncEnumerable<int> source = null;
			return source.WhereWithPrevious((p, c) => true).ToArrayAsync(token).AsTask();
		});
	}

	[TestMethod]
	public async Task WhereWithPrevious_NullPredicate_Throws()
	{
		var token = CancellationToken;
		await ThrowsExactlyAsync<ArgumentNullException>(() =>
		{
			var source = new[] { 1, 2, 3 }.ToAsyncEnumerable();
			return source.WhereWithPrevious(null).ToArrayAsync(token).AsTask();
		});
	}

	#endregion

	#region Cast

	[TestMethod]
	public async Task Cast_IntToLong()
	{
		var token = CancellationToken;
		var source = new object[] { 1, 2, 3 }.ToAsyncEnumerable();

		var result = await source
			.Cast<object, int>()
			.ToArrayAsync(token);

		result.AssertEqual([1, 2, 3]);
	}

	[TestMethod]
	public async Task Cast_DerivedToBase()
	{
		var token = CancellationToken;
		var source = new[] { new DerivedClass { Value = 1 }, new DerivedClass { Value = 2 } }.ToAsyncEnumerable();

		var result = await source
			.Cast<DerivedClass, BaseClass>()
			.ToArrayAsync(token);

		result.Length.AssertEqual(2);
		result[0].Value.AssertEqual(1);
		result[1].Value.AssertEqual(2);
	}

	[TestMethod]
	public async Task Cast_InvalidCast_Throws()
	{
		var token = CancellationToken;
		var source = new object[] { "not an int" }.ToAsyncEnumerable();

		await ThrowsExactlyAsync<InvalidCastException>(async () =>
		{
			await source.Cast<object, int>().ToArrayAsync(token);
		});
	}

	[TestMethod]
	public async Task Cast_EmptySource()
	{
		var token = CancellationToken;
		var source = Array.Empty<object>().ToAsyncEnumerable();

		var result = await source
			.Cast<object, int>()
			.ToArrayAsync(token);

		result.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task Cast_NullSource_Throws()
	{
		var token = CancellationToken;
		await ThrowsExactlyAsync<ArgumentNullException>(() =>
		{
			IAsyncEnumerable<object> source = null;
			return source.Cast<object, int>().ToArrayAsync(token).AsTask();
		});
	}

	[TestMethod]
	public async Task Cast_WithConverter_Basic()
	{
		var token = CancellationToken;
		var source = new[] { 1, 2, 3 }.ToAsyncEnumerable();

		var result = await source
			.Cast(x => x.ToString())
			.ToArrayAsync(token);

		result.AssertEqual(["1", "2", "3"]);
	}

	[TestMethod]
	public async Task Cast_WithConverter_ComplexTransform()
	{
		var token = CancellationToken;
		var source = new[] { "hello", "world" }.ToAsyncEnumerable();

		var result = await source
			.Cast(s => s.Length)
			.ToArrayAsync(token);

		result.AssertEqual([5, 5]);
	}

	[TestMethod]
	public async Task Cast_WithConverter_ToObject()
	{
		var token = CancellationToken;
		var source = new[] { 1, 2, 3 }.ToAsyncEnumerable();

		var result = await source
			.Cast(x => new DerivedClass { Value = x * 10 })
			.ToArrayAsync(token);

		result.Length.AssertEqual(3);
		result[0].Value.AssertEqual(10);
		result[1].Value.AssertEqual(20);
		result[2].Value.AssertEqual(30);
	}

	[TestMethod]
	public async Task Cast_WithConverter_EmptySource()
	{
		var token = CancellationToken;
		var source = Array.Empty<int>().ToAsyncEnumerable();

		var result = await source
			.Cast(x => x.ToString())
			.ToArrayAsync(token);

		result.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task Cast_WithConverter_NullSource_Throws()
	{
		var token = CancellationToken;
		await ThrowsExactlyAsync<ArgumentNullException>(() =>
		{
			IAsyncEnumerable<int> source = null;
			return source.Cast(x => x.ToString()).ToArrayAsync(token).AsTask();
		});
	}

	[TestMethod]
	public async Task Cast_WithConverter_NullConverter_Throws()
	{
		var token = CancellationToken;
		await ThrowsExactlyAsync<ArgumentNullException>(() =>
		{
			var source = new[] { 1, 2, 3 }.ToAsyncEnumerable();
			return source.Cast<int, string>(null).ToArrayAsync(token).AsTask();
		});
	}

	[TestMethod]
	public async Task Cast_WithConverter_ConverterThrows()
	{
		var token = CancellationToken;
		var source = new[] { 1, 2, 0, 3 }.ToAsyncEnumerable();

		await ThrowsExactlyAsync<DivideByZeroException>(async () =>
		{
			await source.Cast(x => 10 / x).ToArrayAsync(token);
		});
	}

	private class BaseClass
	{
		public int Value { get; set; }
	}

	private class DerivedClass : BaseClass
	{
	}

	#endregion

	#region ToEnumerable

	[TestMethod]
	public void ToEnumerable_Basic()
	{
		var source = new[] { 1, 2, 3 }.ToAsyncEnumerable();
		var result = source.ToEnumerable().ToArray();
		result.AssertEqual([1, 2, 3]);
	}

	[TestMethod]
	public void ToEnumerable_Empty()
	{
		var source = Array.Empty<int>().ToAsyncEnumerable();
		var result = source.ToEnumerable().ToArray();
		result.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ToEnumerable_LazyEvaluation()
	{
		var yielded = new List<int>();
		async IAsyncEnumerable<int> Generate()
		{
			for (var i = 1; i <= 5; i++)
			{
				yielded.Add(i);
				yield return i;
			}
		}

		var enumerable = Generate().ToEnumerable();

		// Before iteration, nothing should be yielded
		yielded.Count.AssertEqual(0);

		// Take only first 2 elements
		var first2 = enumerable.Take(2).ToArray();
		first2.AssertEqual([1, 2]);

		// Only 2 elements should have been yielded (lazy evaluation)
		yielded.Count.AssertEqual(2);
	}

	[TestMethod]
	public void ToEnumerable_DisposeAsyncCalledOnBreak()
	{
		var disposed = false;

		var enumerable = CreateAsyncEnumerable().ToEnumerable();

		// Break early from foreach - should still dispose the async enumerator
		foreach (var item in enumerable)
		{
			if (item == 2)
				break;
		}

		// DisposeAsync should have been called via finally block
		disposed.AssertTrue();

		async IAsyncEnumerable<int> CreateAsyncEnumerable()
		{
			try
			{
				for (var i = 1; i <= 10; i++)
					yield return i;
			}
			finally
			{
				// This runs when DisposeAsync is called
				disposed = true;
			}
		}
	}

	[TestMethod]
	public void ToEnumerable_WithCancellation()
	{
		using var cts = new CancellationTokenSource();

		async IAsyncEnumerable<int> Generate([EnumeratorCancellation] CancellationToken ct = default)
		{
			for (var i = 1; i <= 100; i++)
			{
				ct.ThrowIfCancellationRequested();
				yield return i;
			}
		}

		var enumerable = Generate().ToEnumerable(cts.Token);
		var count = 0;

		ThrowsExactly<OperationCanceledException>(() =>
		{
			foreach (var item in enumerable)
			{
				count++;
				if (count == 3)
					cts.Cancel();
			}
		});

		count.AssertEqual(3);
	}

	[TestMethod]
	public void ToEnumerable_NullThrows()
	{
		IAsyncEnumerable<int> source = null;
		ThrowsExactly<ArgumentNullException>(() => source.ToEnumerable());
	}

	[TestMethod]
	public void ToEnumerable_ForeachWorks()
	{
		var source = new[] { "a", "b", "c" }.ToAsyncEnumerable();
		var result = new List<string>();

		foreach (var item in source.ToEnumerable())
			result.Add(item);

		result.AssertEqual(["a", "b", "c"]);
	}

	#endregion
}
