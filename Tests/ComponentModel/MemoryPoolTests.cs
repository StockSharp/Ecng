namespace Ecng.Tests.ComponentModel;

using System;
using System.Drawing;
using System.Threading.Tasks;

using Ecng.ComponentModel;

[TestClass]
public class MemoryPoolTests
{
	private MemoryPool _pool;

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
		var memory = _pool.Allocate(100);
		memory.Length.AssertEqual(100);
		_pool.TotalCount.AssertEqual(0);
		_pool.TotalBytes.AssertEqual(0);
	}

	[TestMethod]
	public void AllocateReuseMemory()
	{
		var memory1 = _pool.Allocate(200);
		_pool.Free(memory1);

		var memory2 = _pool.Allocate(200);
		memory2.Length.AssertEqual(200);
		_pool.TotalCount.AssertEqual(0);
		_pool.TotalBytes.AssertEqual(0);
	}

	[TestMethod]
	public void FreeEmptyMemory()
	{
		_pool.Free(Memory<byte>.Empty);
		_pool.TotalCount.AssertEqual(0);
		_pool.TotalBytes.AssertEqual(0);
	}

	[TestMethod]
	public void FreeMaxPerLengthReached()
	{
		for (var i = 0; i < _pool.MaxPerLength; i++)
		{
			_pool.Free(_pool.Allocate(300));
		}

		var extraMemory = _pool.Allocate(300);
		_pool.Free(extraMemory);

		_pool.TotalCount.AssertEqual(1);
		_pool.TotalBytes.AssertEqual(300);
	}

	[TestMethod]
	public void FreeMaxCountReached()
	{
		var packetsPerSize = _pool.MaxCount / 5;

		for (var size = 100; size <= 500; size += 100)
		{
			for (var i = 0; i < packetsPerSize; i++)
			{
				_pool.Free(_pool.Allocate(size));
			}
		}

		var extraMemory = _pool.Allocate(100);
		_pool.Free(extraMemory);

		_pool.TotalCount.AssertEqual(5);

		var totalBytes = 0;

		for (var size = 100; size <= 500; size += 100)
			totalBytes += size;
		
		_pool.TotalBytes.AssertEqual(totalBytes);
	}

	[TestMethod]
	public void RandomLimitReached()
	{
		for (var i = 0; i < 1000000; i++)
		{
			_pool.Free(_pool.Allocate(RandomGen.GetInt(1, 1000)));
		}

		_pool.TotalCount.AssertEqual(_pool.MaxCount);
	}

	[TestMethod]
	public void FixedLimitReached()
	{
		for (var i = 0; i < 1000; i++)
		{
			_pool.Free(_pool.Allocate(i + 1));
		}

		_pool.TotalCount.AssertEqual(_pool.MaxCount);
	}

	[TestMethod]
	public void Clear()
	{
		_pool.Free(_pool.Allocate(100));
		_pool.Free(_pool.Allocate(200));

		_pool.TotalCount.AssertEqual(2);
		_pool.TotalBytes.AssertEqual(300);

		_pool.Clear();

		_pool.TotalCount.AssertEqual(0);
		_pool.TotalBytes.AssertEqual(0);
		_pool.Allocate(100);
		_pool.TotalCount.AssertEqual(0);
	}

	[TestMethod]
	public void AllocateNegativeLength()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _pool.Allocate(-1));
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
					var memory = _pool.Allocate(size);
					_pool.Free(memory);
				}
			});
		}

		Task.WaitAll(tasks);

		(_pool.TotalCount < 10).AssertTrue();
	}
}