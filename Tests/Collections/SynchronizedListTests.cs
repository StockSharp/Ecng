namespace Ecng.Tests.Collections;

[TestClass]
public class SynchronizedListTests : BaseTestClass
{
	[TestMethod]
	public void RangeOperations()
	{
		var list = new SynchronizedList<int>();
		list.AddRange(Enumerable.Range(1, 5));
		list.Count.AssertEqual(5);
		list.GetRange(1, 3).AssertEqual([2, 3, 4]);
		list.RemoveRange([1, 2]);
		list.ToArray().AssertEqual([3, 4, 5]);
		list.RemoveRange(1, 2).AssertEqual(2);
		list.ToArray().AssertEqual([3]);
	}

	[TestMethod]
	public void Events()
	{
		var list = new SynchronizedList<int>();
		var added = new List<int>();
		var removed = new List<int>();
		list.AddedRange += items => added.AddRange(items);
		list.RemovedRange += items => removed.AddRange(items);
		list.AddRange([1, 2, 3]);
		added.AssertEqual([1, 2, 3]);
		list.RemoveRange([1, 3]);
		removed.AssertEqual([1, 3]);
	}

	[TestMethod]
	public void NullCheck()
	{
		var list = new SynchronizedList<string> { CheckNullableItems = true };
		ThrowsExactly<ArgumentNullException>(() => list.AddRange(["a", null]));
	}

	[TestMethod]
	public void InsertAndIndexer()
	{
		var list = new SynchronizedList<int>();
		list.AddRange([2, 3]);
		list.Insert(0, 1);
		list[0].AssertEqual(1);
		list[2].AssertEqual(3);
		list.IndexOf(3).AssertEqual(2);
		list.RemoveAt(1);
		list.ToArray().AssertEqual([1, 3]);
	}

	[TestMethod]
	public void RemoveRangeBounds()
	{
		var list = new SynchronizedList<int>();
		ThrowsExactly<ArgumentOutOfRangeException>(() => list.RemoveRange(-1, 1));
		ThrowsExactly<ArgumentOutOfRangeException>(() => list.RemoveRange(0, 0));
	}

	[TestMethod]
	public void RemoveRangeByIndex_FiresCallbacks()
	{
		// This test verifies that RemoveRange(index, count) properly fires
		// OnRemoving/OnRemoved callbacks and RemovedRange event.
		// Without the fix, these callbacks were not called.
		var list = new SynchronizedList<int>();
		var removedViaEvent = new List<int>();
		list.RemovedRange += items => removedViaEvent.AddRange(items);

		list.AddRange([1, 2, 3, 4, 5]);

		// Remove items at index 1 with count 3 (removes 2, 3, 4)
		var removedCount = list.RemoveRange(1, 3);

		removedCount.AssertEqual(3);
		list.ToArray().AssertEqual([1, 5]);

		// With the fix, RemovedRange event should be fired
		removedViaEvent.AssertEqual([2, 3, 4]);
	}

	[TestMethod]
	public void RemoveRangeByIndex_CallsOnRemoving()
	{
		// Create a custom list that tracks OnRemoving calls
		var removingCalls = new List<int>();
		var list = new TestSynchronizedList<int>(
			onRemoving: item => { removingCalls.Add(item); return true; }
		);

		list.AddRange([10, 20, 30, 40]);
		list.RemoveRange(1, 2);

		// OnRemoving should have been called for items 20 and 30
		removingCalls.AssertEqual([20, 30]);
	}

	private class TestSynchronizedList<T>(Func<T, bool> onRemoving) : SynchronizedList<T>
	{
		protected override bool OnRemoving(T item)
		{
			return onRemoving(item);
		}
	}

	[TestMethod]
	public void ConcurrentAdd()
	{
		var list = new SynchronizedList<int>();
		const int threadCount = 10;
		const int itemsPerThread = 100;
		var threads = new Thread[threadCount];
		var exceptions = new List<Exception>();

		for (var t = 0; t < threadCount; t++)
		{
			var threadIndex = t;
			threads[t] = new Thread(() =>
			{
				try
				{
					for (var i = 0; i < itemsPerThread; i++)
						list.Add(threadIndex * itemsPerThread + i);
				}
				catch (Exception ex)
				{
					lock (exceptions)
						exceptions.Add(ex);
				}
			});
		}

		foreach (var t in threads)
			t.Start();
		foreach (var t in threads)
			t.Join();

		exceptions.Count.AssertEqual(0);
		list.Count.AssertEqual(threadCount * itemsPerThread);
	}

	[TestMethod]
	public void ConcurrentAddRemove()
	{
		var list = new SynchronizedList<int>();
		list.AddRange(Enumerable.Range(0, 100));
		const int iterations = 500;
		var exceptions = new List<Exception>();

		var addThread = new Thread(() =>
		{
			try
			{
				for (var i = 0; i < iterations; i++)
					list.Add(100 + i);
			}
			catch (Exception ex)
			{
				lock (exceptions)
					exceptions.Add(ex);
			}
		});

		var removeThread = new Thread(() =>
		{
			try
			{
				for (var i = 0; i < iterations; i++)
				{
					if (list.Count > 0)
						list.Remove(list[0]);
				}
			}
			catch (Exception ex)
			{
				lock (exceptions)
					exceptions.Add(ex);
			}
		});

		addThread.Start();
		removeThread.Start();
		addThread.Join();
		removeThread.Join();

		exceptions.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ConcurrentEnumeration()
	{
		var list = new SynchronizedList<int>();
		list.AddRange(Enumerable.Range(0, 100));
		const int iterations = 100;
		var exceptions = new List<Exception>();

		var modifyThread = new Thread(() =>
		{
			try
			{
				for (var i = 0; i < iterations; i++)
				{
					list.Add(100 + i);
					if (list.Count > 50)
						list.RemoveAt(0);
				}
			}
			catch (Exception ex)
			{
				lock (exceptions)
					exceptions.Add(ex);
			}
		});

		var enumerateThread = new Thread(() =>
		{
			try
			{
				for (var i = 0; i < iterations; i++)
				{
					var snapshot = list.ToArray();
					(snapshot.Length >= 0).AssertTrue();
				}
			}
			catch (Exception ex)
			{
				lock (exceptions)
					exceptions.Add(ex);
			}
		});

		modifyThread.Start();
		enumerateThread.Start();
		modifyThread.Join();
		enumerateThread.Join();

		exceptions.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ConcurrentIndexAccess()
	{
		var list = new SynchronizedList<int>();
		list.AddRange(Enumerable.Range(0, 1000));
		const int threadCount = 5;
		var threads = new Thread[threadCount];
		var exceptions = new List<Exception>();

		for (var t = 0; t < threadCount; t++)
		{
			threads[t] = new Thread(() =>
			{
				try
				{
					for (var i = 0; i < 1000; i++)
					{
						if (i < list.Count)
							_ = list[i];
					}
				}
				catch (Exception ex)
				{
					lock (exceptions)
						exceptions.Add(ex);
				}
			});
		}

		foreach (var t in threads)
			t.Start();
		foreach (var t in threads)
			t.Join();

		exceptions.Count.AssertEqual(0);
	}
}
