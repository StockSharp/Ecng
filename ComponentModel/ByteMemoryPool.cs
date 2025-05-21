namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;
using System.Buffers;
using System.Threading;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Localization;

/// <summary>
/// Thread-safe memory pool for reusing <see cref="Memory{T}"/> memories of specific sizes.
/// </summary>
public class ByteMemoryPool : MemoryPool<byte>
{
	private struct MemoryOwner(ByteMemoryPool parent, Memory<byte> memory) : IMemoryOwner<byte>, IReasonDisposable
	{
		private volatile int _disposed;
		private readonly ByteMemoryPool _parent = parent ?? throw new ArgumentNullException(nameof(parent));

		private readonly Memory<byte> _memory = memory;
		public readonly Memory<byte> Memory
		{
			get
			{
				if (_disposed != 0)
					throw new ObjectDisposedException(nameof(MemoryOwner));

				return _memory;
			}
		}

		public string Reason { get; private set; }

		void IDisposable.Dispose()
			=> Dispose(nameof(IDisposable.Dispose));

		public bool Dispose(string reason)
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0)
				return false;

			Reason = reason;

			var result = _parent.Free(_memory);

			if (result == FreeResult.LimitExceeded)
				_parent.LimitExceed?.Invoke();

			return true;
		}
	}

	private volatile bool _disposed;
	private volatile bool _limitExceeded;
	private readonly SyncObject _lock = new();
	private readonly Dictionary<int, Queue<Memory<byte>>> _pool = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="ByteMemoryPool"/> class with the specified maximum buffer size.
	/// </summary>
	/// <param name="maxBufferSize">The maximum size of the buffer that can be rented from the pool.</param>
	public ByteMemoryPool(int maxBufferSize = ushort.MaxValue)
	{
		if (maxBufferSize < 1)
			throw new ArgumentOutOfRangeException(nameof(maxBufferSize), maxBufferSize, "Invalid value.".Localize());

		MaxBufferSize = maxBufferSize;
	}

	/// <summary>
	/// The event that is triggered when the memory pool exceeds its limit.
	/// </summary>
	public event Action LimitExceed;

	/// <inheritdoc />
	public override int MaxBufferSize { get; }

	private int _defaultSize = FileSizes.KB * 10;

	/// <summary>
	/// The default size of the memory to rent when no specific size is requested.
	/// </summary>
	public int DefaultSize
	{
		get => _defaultSize;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.".Localize());

			_defaultSize = value;
		}
	}

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

	/// <inheritdoc />
	public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
	{
		if (minBufferSize == -1)
			minBufferSize = DefaultSize;
		else if (minBufferSize < 1 || minBufferSize > MaxBufferSize)
			throw new ArgumentOutOfRangeException(nameof(minBufferSize), minBufferSize, "Invalid value.".Localize());

		static int round(int size)
		{
			if (size == 1)
				return 1;
			else if (size < 1)
				throw new ArgumentOutOfRangeException(nameof(size), size, "Invalid value.".Localize());

			// Check for potential overflow before operations
			if (size > (1 << 30))  // Max safe power of 2 for int
				return int.MaxValue;

			size--;

			size |= size >> 1;
			size |= size >> 2;
			size |= size >> 4;
			size |= size >> 8;
			size |= size >> 16;

			return size + 1;
		}

		var size = round(minBufferSize);

		if (size < minBufferSize)
			throw new InvalidOperationException(size.ToString());
		else if (size > MaxBufferSize)
			size = MaxBufferSize;

		lock (_lock)
		{
			if (_disposed)
				throw new ObjectDisposedException(nameof(ByteMemoryPool));

			if (_pool.TryGetValue(size, out var bag) && bag.Count > 0)
			{
				var memory = bag.Dequeue();

				_totalCount--;
				_totalBytes -= memory.Length;

				if (bag.Count == 0 && _pool.Count > 100)
					_pool.Remove(size);

				return new MemoryOwner(this, memory);
			}
		}
		
		return new MemoryOwner(this, new(new byte[size]));
	}

	private enum FreeResult
	{
		Success,
		LimitExceeded,
		OverLimit
	}

	private FreeResult Free(Memory<byte> memory)
	{
		if (memory.IsEmpty)
			return FreeResult.Success;

		lock (_lock)
		{
			if (_disposed)
				return FreeResult.Success;

			if (_totalCount >= MaxCount)
			{
				if (!_limitExceeded)
				{
					_limitExceeded = true;
					return FreeResult.LimitExceeded;
				}

				return FreeResult.OverLimit;
			}

			var memLength = memory.Length;
			var bag = _pool.SafeAdd(memLength);

			if (bag.Count >= MaxPerLength)
			{
				if (!_limitExceeded)
				{
					_limitExceeded = true;
					return FreeResult.LimitExceeded;
				}

				return FreeResult.OverLimit;
			}

			bag.Enqueue(memory);

			_totalCount++;
			_totalBytes += memory.Length;

			_limitExceeded = false;
		}

		return FreeResult.Success;
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

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			lock (_lock)
			{
				if (_disposed)
					return;

				_disposed = true;
				Clear();
			}
		}
	}
}