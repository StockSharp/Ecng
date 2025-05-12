namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class MemoryPoolTests
{
	private ByteMemoryPool _pool;

	[TestInitialize]
	public void Initialize()
	{
		_pool = new()
		{
			MaxPerLength = 10,
			MaxCount = 50
		};
	}

	[TestMethod]
	public void AllocateNewMemory()
	{
		using var owner = _pool.Rent(100);
		owner.Memory.Length.AssertEqual(128);
		_pool.TotalCount.AssertEqual(0);
		_pool.TotalBytes.AssertEqual(0);
	}

	[TestMethod]
	public void AllocateReuseMemory()
	{
		using (var _ = _pool.Rent(200))
		{
		}

		using var owner = _pool.Rent(200);
		owner.Memory.Length.AssertEqual(256);
		_pool.TotalCount.AssertEqual(0);
		_pool.TotalBytes.AssertEqual(0);
	}

	[TestMethod]
	public void FreeMaxPerLengthReached()
	{
		for (var i = 0; i < _pool.MaxPerLength; i++)
		{
			using var _ = _pool.Rent(300);
		}

		using (var _ = _pool.Rent(300))
		{
		}

		_pool.TotalCount.AssertEqual(1);
		_pool.TotalBytes.AssertEqual(512);
	}

	[TestMethod]
	public void FreeMaxCountReached()
	{
		var packetsPerSize = _pool.MaxCount / 5;

		for (var size = 100; size <= 500; size += 100)
		{
			for (var i = 0; i < packetsPerSize; i++)
			{
				using var _ = _pool.Rent(size);
			}
		}

		using (var _ = _pool.Rent(100))
		{
		}

		_pool.TotalCount.AssertEqual(3);
		_pool.TotalBytes.AssertEqual(896);
	}

	[TestMethod]
	public void RandomLimitReached()
	{
		for (var i = 0; i < 1000000; i++)
		{
			using var _ = _pool.Rent(RandomGen.GetInt(1, 1000));
		}

		_pool.TotalCount.AssertEqual(11);
	}

	[TestMethod]
	public void FixedLimitReached()
	{
		for (var i = 0; i < 1000; i++)
		{
			using var _ = _pool.Rent(i + 1);
		}

		_pool.TotalCount.AssertEqual(11);
	}

	[TestMethod]
	public void Clear()
	{
		using (var _ = _pool.Rent(100))
		{
		}

		using (var _ = _pool.Rent(200))
		{
		}

		_pool.TotalCount.AssertEqual(2);
		_pool.TotalBytes.AssertEqual(384);

		_pool.Clear();

		_pool.TotalCount.AssertEqual(0);
		_pool.TotalBytes.AssertEqual(0);
		_pool.Rent(100);
		_pool.TotalCount.AssertEqual(0);
	}

	[TestMethod]
	public void AllocateDefaultLength()
	{
		using var _ = _pool.Rent(-1);
		using var __ = _pool.Rent();
	}

	[TestMethod]
	public void AllocateNegativeLength()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _pool.Rent(-2));
	}

	[TestMethod]
	public void MaxPerLengthZero()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _pool.MaxPerLength = 0);
	}

	[TestMethod]
	public void MaxCountZero()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _pool.MaxCount = 0);
	}

	[TestMethod]
	public void Concurrent()
	{
		const int threadCount = 10;
		const int operationsPerThread = 100;

		var tasks = new Task[threadCount];

		for (var i = 0; i < threadCount; i++)
		{
			tasks[i] = Task.Run(() =>
			{
				for (var j = 0; j < operationsPerThread; j++)
				{
					var size = 100 + (j % 3) * 100; // 100, 200, 300
					using var memory = _pool.Rent(size);
				}
			});
		}

		Task.WaitAll(tasks);

		(_pool.TotalCount < 10).AssertTrue();
	}
}