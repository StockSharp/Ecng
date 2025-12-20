namespace Ecng.Tests.Collections;

[TestClass]
public class SynchronizedDictionaryTests
{
	[TestMethod]
	public void BasicOperations()
	{
		var dict = new SynchronizedDictionary<int, string>
		{
			{ 1, "A" }
		};
		dict[2] = "B";
		dict.Count.AssertEqual(2);
		dict.ContainsKey(1).AssertTrue();
		dict[1].AssertEqual("A");
		dict.Remove(2).AssertTrue();
		dict.Count.AssertEqual(1);
	}

	[TestMethod]
	public void TryGetAndClear()
	{
		var dict = new SynchronizedDictionary<int, string>
		{
			{ 1, "A" }
		};
		dict.TryGetValue(1, out var v).AssertTrue();
		v.AssertEqual("A");
		dict.Clear();
		dict.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Enumeration()
	{
		var dict = new SynchronizedDictionary<int, string>
		{
			{ 1, "A" },
			{ 2, "B" }
		};
		var items = dict.ToArray();
		items.Length.AssertEqual(2);
		items.Any(p => p.Key == 1 && p.Value == "A").AssertTrue();
		items.Any(p => p.Key == 2 && p.Value == "B").AssertTrue();
	}

	[TestMethod]
	public void ConcurrentAddAndRead()
	{
		var dict = new SynchronizedDictionary<int, int>();
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
						var key = threadIndex * itemsPerThread + i;
						dict[key] = key * 2;
					}
				}
				catch (Exception ex)
				{
					lock (exceptions) exceptions.Add(ex);
				}
			});
		}

		foreach (var t in threads) t.Start();
		foreach (var t in threads) t.Join();

		exceptions.Count.AssertEqual(0);
		dict.Count.AssertEqual(threadCount * itemsPerThread);
	}

	[TestMethod]
	public void ConcurrentAddRemove()
	{
		var dict = new SynchronizedDictionary<int, string>();
		const int iterations = 1000;
		var exceptions = new List<Exception>();

		var addThread = new Thread(() =>
		{
			try
			{
				for (var i = 0; i < iterations; i++)
					dict[i % 100] = $"Value{i}";
			}
			catch (Exception ex)
			{
				lock (exceptions) exceptions.Add(ex);
			}
		});

		var removeThread = new Thread(() =>
		{
			try
			{
				for (var i = 0; i < iterations; i++)
					dict.Remove(i % 100);
			}
			catch (Exception ex)
			{
				lock (exceptions) exceptions.Add(ex);
			}
		});

		addThread.Start();
		removeThread.Start();
		addThread.Join();
		removeThread.Join();

		exceptions.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ConcurrentTryGetValue()
	{
		var dict = new SynchronizedDictionary<int, int>();
		for (var i = 0; i < 1000; i++)
			dict[i] = i * 2;

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
						if (dict.TryGetValue(i, out var value))
							value.AssertEqual(i * 2);
					}
				}
				catch (Exception ex)
				{
					lock (exceptions) exceptions.Add(ex);
				}
			});
		}

		foreach (var t in threads) t.Start();
		foreach (var t in threads) t.Join();

		exceptions.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ConcurrentEnumeration()
	{
		var dict = new SynchronizedDictionary<int, int>();
		for (var i = 0; i < 100; i++)
			dict[i] = i;

		const int iterations = 100;
		var exceptions = new List<Exception>();

		var modifyThread = new Thread(() =>
		{
			try
			{
				for (var i = 0; i < iterations; i++)
				{
					dict[100 + i] = 100 + i;
					dict.Remove(i);
				}
			}
			catch (Exception ex)
			{
				lock (exceptions) exceptions.Add(ex);
			}
		});

		var enumerateThread = new Thread(() =>
		{
			try
			{
				for (var i = 0; i < iterations; i++)
				{
					var snapshot = dict.ToArray();
					(snapshot.Length >= 0).AssertTrue();
				}
			}
			catch (Exception ex)
			{
				lock (exceptions) exceptions.Add(ex);
			}
		});

		modifyThread.Start();
		enumerateThread.Start();
		modifyThread.Join();
		enumerateThread.Join();

		exceptions.Count.AssertEqual(0);
	}
}
