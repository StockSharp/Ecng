namespace Ecng.Tests.Collections;

[TestClass]
public class SynchronizedSetTests : BaseTestClass
{
	[TestMethod]
	public void IndexingAndDuplicates()
	{
		var set = new SynchronizedSet<int>(true);
		set.AddRange([1, 2, 3]);
		set[0].AssertEqual(1);
		set.IndexOf(3).AssertEqual(2);
		set.Remove(2).AssertTrue();
		set.TryAdd(3).AssertFalse();
		set.ThrowIfDuplicate = true;
		ThrowsExactly<InvalidOperationException>(() => set.Add(1));
		ThrowsExactly<InvalidOperationException>(() => set.TryAdd(1));
	}

	[TestMethod]
	public void IndexingDisabled()
	{
		var set = new SynchronizedSet<int>
		{
			1
		};
		ThrowsExactly<InvalidOperationException>(() => _ = set[0]);
		ThrowsExactly<InvalidOperationException>(() => set.IndexOf(1));
		ThrowsExactly<InvalidOperationException>(() => set.RemoveAt(0));
	}

	[TestMethod]
	public void RangeEvents()
	{
		var set = new SynchronizedSet<int>();
		var added = new List<int>();
		var removed = new List<int>();
		set.AddedRange += items => added.AddRange(items);
		set.RemovedRange += items => removed.AddRange(items);
		set.AddRange([1, 2, 3]);
		added.AssertEqual([1, 2, 3]);
		set.RemoveRange([1, 3]);
		removed.AssertEqual([1, 3]);
	}

	[TestMethod]
	public void UnionIntersectExceptSymmetric()
	{
		var set = new SynchronizedSet<int>(true);
		set.AddRange([1, 2, 3]);
		set.UnionWith([3, 4]);
		set.OrderBy(t => t).AssertEqual(new int[] { 1, 2, 3, 4 });
		set.IntersectWith([2, 4]);
		set.OrderBy(t => t).AssertEqual(new int[] { 2, 4 });
		set.ExceptWith([4]);
		set.AssertEqual([2]);
		set.SymmetricExceptWith([2, 3]);
		set.OrderBy(t => t).AssertEqual(new int[] { 3 });
	}

	[TestMethod]
	public void SetComparisons()
	{
		var set = new SynchronizedSet<int>();
		set.AddRange([1, 2, 3]);
		set.IsSubsetOf([0, 1, 2, 3, 4]).AssertTrue();
		set.IsSupersetOf([1, 2]).AssertTrue();
		set.IsProperSupersetOf([1, 2]).AssertTrue();
		set.IsProperSubsetOf([1, 2, 3, 4]).AssertTrue();
		set.Overlaps([3, 4, 5]).AssertTrue();
		set.SetEquals([3, 2, 1]).AssertTrue();
	}

	[TestMethod]
	public void IndexedRemoveRange()
	{
		var set = new SynchronizedSet<int>(true);
		set.AddRange([1, 2, 3, 4]);
		var removed = set.RemoveRange(1, 2);
		removed.AssertEqual(2);
		set.AssertEqual([1, 4]);
	}

	[TestMethod]
	public void IntersectWith_PreservesComparer()
	{
		// This test verifies that set operations preserve the comparer.
		var set = new SynchronizedSet<string>(StringComparer.OrdinalIgnoreCase);
		set.AddRange(["Apple", "Banana", "Cherry"]);

		// IntersectWith should use case-insensitive comparison
		set.IntersectWith(["APPLE", "banana", "Date"]);

		// With case-insensitive comparison, Apple and Banana should remain
		set.Count.AssertEqual(2);
		set.Contains("Apple").AssertTrue();
		set.Contains("Banana").AssertTrue();
	}

	[TestMethod]
	public void ExceptWith_PreservesComparer()
	{
		var set = new SynchronizedSet<string>(StringComparer.OrdinalIgnoreCase);
		set.AddRange(["Apple", "Banana", "Cherry"]);

		// ExceptWith should use case-insensitive comparison
		set.ExceptWith(["APPLE", "CHERRY"]);

		// Only Banana should remain
		set.Count.AssertEqual(1);
		set.Contains("Banana").AssertTrue();
	}

	[TestMethod]
	public void SetEquals_PreservesComparer()
	{
		var set = new SynchronizedSet<string>(StringComparer.OrdinalIgnoreCase);
		set.AddRange(["Apple", "Banana"]);

		// SetEquals should use case-insensitive comparison
		set.SetEquals(["APPLE", "BANANA"]).AssertTrue();
		set.SetEquals(["apple", "banana"]).AssertTrue();
		set.SetEquals(["Apple", "Cherry"]).AssertFalse();
	}

	[TestMethod]
	public void ConcurrentAdd()
	{
		var set = new SynchronizedSet<int>();
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
					{
						set.Add(threadIndex * itemsPerThread + i);
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
		set.Count.AssertEqual(threadCount * itemsPerThread);
	}

	[TestMethod]
	public void ConcurrentAddRemove()
	{
		var set = new SynchronizedSet<int>();
		const int iterations = 1000;
		var exceptions = new List<Exception>();

		var addThread = new Thread(() =>
		{
			try
			{
				for (var i = 0; i < iterations; i++)
					set.TryAdd(i % 100);
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
					set.Remove(i % 100);
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
		var set = new SynchronizedSet<int>();
		set.AddRange(Enumerable.Range(0, 100));
		const int iterations = 100;
		var exceptions = new List<Exception>();

		var modifyThread = new Thread(() =>
		{
			try
			{
				for (var i = 0; i < iterations; i++)
				{
					set.TryAdd(100 + i);
					set.Remove(i);
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
					var snapshot = set.ToArray();
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
	public void ConcurrentContainsCheck()
	{
		var set = new SynchronizedSet<int>();
		set.AddRange(Enumerable.Range(0, 1000));
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
						_ = set.Contains(i);
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