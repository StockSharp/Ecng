namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Localization;

/// <summary>
/// Thread-safe memory pool for reusing <see cref="Memory{T}"/> memories of specific sizes.
/// </summary>
public class MemoryPool
{
	private readonly SyncObject _lock = new();
	private readonly Dictionary<int, Queue<Memory<byte>>> _pool = [];

	private int _maxPerLength = 100;

	/// <summary>
	/// Gets or sets the maximum number of memories to store for each size.
	/// </summary>
	public int MaxPerLength
	{
		get => _maxPerLength;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.".Localize());

			_maxPerLength = value;
		}
	}

	private int _maxCount = 1000;

	/// <summary>
	/// Gets or sets the maximum number of memories to store for each size.
	/// </summary>
	public int MaxCount
	{
		get => _maxCount;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.".Localize());

			_maxCount = value;
		}
	}

	private int _totalCount;

	/// <summary>
	/// Gets the total number of memories currently stored in the pool across all sizes.
	/// </summary>
	public int TotalCount => _totalCount;

	private int _totalBytes;

	/// <summary>
	/// Gets the total number of bytes currently stored in the pool across all sizes.
	/// </summary>
	public long TotalBytes => _totalBytes;

	/// <summary>
	/// Allocates a memory of the specified size, reusing an existing one if available.
	/// </summary>
	/// <param name="length">The size of the memory to allocate.</param>
	/// <returns>A <see cref="Memory{T}"/> memory of the requested size.</returns>
	public Memory<byte> Allocate(int length)
	{
		if (length < 1)
			throw new ArgumentOutOfRangeException(nameof(length), length, "Invalid value.".Localize());

		lock (_lock)
		{
			if (_pool.TryGetValue(length, out var bag) && bag.Count > 0)
			{
				var memory = bag.Dequeue();

				_totalCount--;
				_totalBytes -= memory.Length;

				if (bag.Count == 0 && _pool.Count > 10000)
					_pool.Remove(length);

				return memory;
			}
		}
		

		return new(new byte[length]);
	}

	/// <summary>
	/// Returns a memory to the pool for reuse.
	/// </summary>
	/// <param name="memory">The memory to return to the pool.</param>
	public void Free(Memory<byte> memory)
	{
		if (memory.IsEmpty)
			return;

		lock (_lock)
		{
			if (_totalCount >= MaxCount)
				return;

			var memLength = memory.Length;

			var bag = _pool.SafeAdd(memLength);

			if (bag.Count >= MaxPerLength)
				return;

			bag.Enqueue(memory);

			_totalCount++;
			_totalBytes += memory.Length;
		}
	}

	/// <summary>
	/// Clears the pool, removing all stored memories.
	/// </summary>
	public void Clear()
	{
		lock (_lock)
		{
			_pool.Clear();

			_totalCount = 0;
			_totalBytes = 0;
		}
	}
}