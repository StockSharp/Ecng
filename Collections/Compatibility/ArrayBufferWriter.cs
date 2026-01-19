#if NETSTANDARD2_0
namespace System.Buffers;

/// <summary>
/// Polyfill for ArrayBufferWriter which doesn't exist in netstandard2.0.
/// Represents a heap-based, array-backed output sink into which <typeparamref name="T"/> data can be written.
/// </summary>
/// <typeparam name="T">The type of items in the buffer.</typeparam>
public sealed class ArrayBufferWriter<T> : IBufferWriter<T>
{
	private const int DefaultInitialBufferSize = 256;

	private T[] _buffer;
	private int _index;

	/// <summary>
	/// Initializes a new instance of the <see cref="ArrayBufferWriter{T}"/> class.
	/// </summary>
	public ArrayBufferWriter()
		: this(DefaultInitialBufferSize)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ArrayBufferWriter{T}"/> class with the specified initial capacity.
	/// </summary>
	/// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
	public ArrayBufferWriter(int initialCapacity)
	{
		if (initialCapacity <= 0)
			throw new ArgumentException("Capacity must be greater than zero.", nameof(initialCapacity));

		_buffer = new T[initialCapacity];
		_index = 0;
	}

	/// <summary>
	/// Gets the data written to the underlying buffer so far, as a <see cref="ReadOnlyMemory{T}"/>.
	/// </summary>
	public ReadOnlyMemory<T> WrittenMemory => _buffer.AsMemory(0, _index);

	/// <summary>
	/// Gets the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
	/// </summary>
	public ReadOnlySpan<T> WrittenSpan => _buffer.AsSpan(0, _index);

	/// <summary>
	/// Gets the amount of data written to the underlying buffer so far.
	/// </summary>
	public int WrittenCount => _index;

	/// <summary>
	/// Gets the total amount of space within the underlying buffer.
	/// </summary>
	public int Capacity => _buffer.Length;

	/// <summary>
	/// Gets the amount of space available that can still be written into without forcing the underlying buffer to grow.
	/// </summary>
	public int FreeCapacity => _buffer.Length - _index;

	/// <summary>
	/// Clears the data written to the underlying buffer.
	/// </summary>
	public void Clear()
	{
		_buffer.AsSpan(0, _index).Clear();
		_index = 0;
	}

	/// <summary>
	/// Notifies the <see cref="ArrayBufferWriter{T}"/> that count items were written.
	/// </summary>
	/// <param name="count">The number of items written.</param>
	public void Advance(int count)
	{
		if (count < 0)
			throw new ArgumentException("Count must be non-negative.", nameof(count));

		if (_index > _buffer.Length - count)
			throw new InvalidOperationException("Cannot advance past the end of the buffer.");

		_index += count;
	}

	/// <summary>
	/// Returns a <see cref="Memory{T}"/> to write to that is at least the requested size.
	/// </summary>
	/// <param name="sizeHint">The minimum length of the returned <see cref="Memory{T}"/>.</param>
	/// <returns>A <see cref="Memory{T}"/> of at least <paramref name="sizeHint"/> in length.</returns>
	public Memory<T> GetMemory(int sizeHint = 0)
	{
		CheckAndResizeBuffer(sizeHint);
		return _buffer.AsMemory(_index);
	}

	/// <summary>
	/// Returns a <see cref="Span{T}"/> to write to that is at least the requested size.
	/// </summary>
	/// <param name="sizeHint">The minimum length of the returned <see cref="Span{T}"/>.</param>
	/// <returns>A <see cref="Span{T}"/> of at least <paramref name="sizeHint"/> in length.</returns>
	public Span<T> GetSpan(int sizeHint = 0)
	{
		CheckAndResizeBuffer(sizeHint);
		return _buffer.AsSpan(_index);
	}

	/// <summary>
	/// Writes the specified data to the buffer.
	/// </summary>
	/// <param name="data">The data to write.</param>
	public void Write(ReadOnlySpan<T> data)
	{
		if (data.IsEmpty)
			return;

		CheckAndResizeBuffer(data.Length);
		data.CopyTo(_buffer.AsSpan(_index));
		_index += data.Length;
	}

	private void CheckAndResizeBuffer(int sizeHint)
	{
		if (sizeHint < 0)
			throw new ArgumentException("Size hint must be non-negative.", nameof(sizeHint));

		if (sizeHint == 0)
			sizeHint = 1;

		if (sizeHint > FreeCapacity)
		{
			var growBy = Math.Max(sizeHint, _buffer.Length);
			var newSize = checked(_buffer.Length + growBy);
			Array.Resize(ref _buffer, newSize);
		}
	}
}
#endif
