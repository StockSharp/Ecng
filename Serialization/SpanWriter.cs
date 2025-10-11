namespace Ecng.Serialization;

using System;
using System.Text;

using Ecng.Common;

/// <summary>
/// Provides functionality for writing primitive data types to a span of bytes.
/// </summary>
public ref struct SpanWriter
{
	private readonly Span<byte> _span;
	private int _position;
	private readonly bool _isBigEndian;
	private readonly bool _allowNegativeShift;

	/// <summary>
	/// Initializes a new instance of the <see cref="SpanWriter"/> struct.
	/// </summary>
	/// <param name="buffer">The array to write data to.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <param name="allowNegativeShift">If true, allows negative Skip to move position backward.</param>
	public SpanWriter(byte[] buffer, bool isBigEndian = false, bool allowNegativeShift = false)
		: this(buffer.AsSpan(), isBigEndian, allowNegativeShift)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SpanWriter"/> struct.
	/// </summary>
	/// <param name="memory">The memory to write data to.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <param name="allowNegativeShift">If true, allows negative Skip to move position backward.</param>
	public SpanWriter(Memory<byte> memory, bool isBigEndian = false, bool allowNegativeShift = false)
		: this(memory.Span, isBigEndian, allowNegativeShift)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SpanWriter"/> struct with the specified span.
	/// </summary>
	/// <param name="span">The span to write data to.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	/// <param name="allowNegativeShift">If true, allows negative Skip to move position backward.</param>
	public SpanWriter(Span<byte> span, bool isBigEndian = false, bool allowNegativeShift = false)
	{
		_span = span;
		_position = 0;
		_isBigEndian = isBigEndian;
		_allowNegativeShift = allowNegativeShift;
	}

	/// <summary>
	/// Gets the current position within the span.
	/// </summary>
	public readonly int Position => _position;

	/// <summary>
	/// Gets a value indicating whether the span is full and no more data can be written.
	/// </summary>
	public readonly bool IsFull => _position >= _span.Length;

	/// <summary>
	/// Gets the number of bytes remaining in the span.
	/// </summary>
	public readonly int Remaining => _span.Length - _position;

	/// <summary>
	/// Gets a <see cref="ReadOnlySpan{Byte}"/> containing the data written so far.
	/// </summary>
	/// <returns>A <see cref="ReadOnlySpan{Byte}"/> containing the written data.</returns>
	public readonly ReadOnlySpan<byte> GetWrittenSpan() => _span.Slice(0, _position);

	/// <summary>
	/// Writes a byte value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The byte value to write.</param>
	public void WriteByte(byte value) => _span.WriteByte(value, ref _position);

	/// <summary>
	/// Writes a signed byte value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The signed byte value to write.</param>
	[CLSCompliant(false)]
	public void WriteSByte(sbyte value) => _span.WriteSByte(value, ref _position);

	/// <summary>
	/// Writes a boolean value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The boolean value to write.</param>
	public void WriteBoolean(bool value) => _span.WriteBoolean(value, ref _position);

	/// <summary>
	/// Writes a 16-bit integer value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The 16-bit integer value to write.</param>
	public void WriteInt16(short value) => _span.WriteInt16(value, _isBigEndian, ref _position);

	/// <summary>
	/// Writes an unsigned 16-bit integer value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The unsigned 16-bit integer value to write.</param>
	[CLSCompliant(false)]
	public void WriteUInt16(ushort value) => _span.WriteUInt16(value, _isBigEndian, ref _position);

	/// <summary>
	/// Writes a 32-bit integer value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The 32-bit integer value to write.</param>
	public void WriteInt32(int value) => _span.WriteInt32(value, _isBigEndian, ref _position);

	/// <summary>
	/// Writes an unsigned 32-bit integer value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The unsigned 32-bit integer value to write.</param>
	[CLSCompliant(false)]
	public void WriteUInt32(uint value) => _span.WriteUInt32(value, _isBigEndian, ref _position);

	/// <summary>
	/// Writes a 64-bit integer value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The 64-bit integer value to write.</param>
	public void WriteInt64(long value) => _span.WriteInt64(value, _isBigEndian, ref _position);

	/// <summary>
	/// Writes an unsigned 64-bit integer value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The unsigned 64-bit integer value to write.</param>
	[CLSCompliant(false)]
	public void WriteUInt64(ulong value) => _span.WriteUInt64(value, _isBigEndian, ref _position);

	/// <summary>
	/// Writes a decimal value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The decimal value to write.</param>
	public void WriteDecimal(decimal value) => _span.WriteDecimal(value, _isBigEndian, ref _position);

	/// <summary>
	/// Writes a DateTime value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The DateTime value to write.</param>
	public void WriteDateTime(DateTime value) => _span.WriteInt64(value.Ticks, _isBigEndian, ref _position);

	/// <summary>
	/// Writes a TimeSpan value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The TimeSpan value to write.</param>
	public void WriteTimeSpan(TimeSpan value) => _span.WriteInt64(value.Ticks, _isBigEndian, ref _position);

	/// <summary>
	/// Writes a string value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The string value to write.</param>
	/// <param name="encoding">The encoding to use for the string. This parameter cannot be null.</param>
	public void WriteString(string value, Encoding encoding)
	{
		if (value.IsEmpty())
			return;

		if (encoding is null)
			throw new ArgumentNullException(nameof(encoding));

		var slice = _span.Slice(_position);

#if NET5_0_OR_GREATER
		_position += encoding.GetBytes(value, slice);
#else
		var buffer = encoding.GetBytes(value);
		buffer.CopyTo(slice);
		_position += buffer.Length;
#endif
	}

	/// <summary>
	/// Writes a character value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The character value to write.</param>
	public void WriteChar(char value) => _span.WriteChar(value, ref _position);

#if NET5_0_OR_GREATER
	/// <summary>
	/// Writes a Half value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The Half value to write.</param>
	public void WriteHalf(Half value) => _span.WriteHalf(value, _isBigEndian, ref _position);
#endif

	/// <summary>
	/// Writes a single-precision floating-point value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The single-precision floating-point value to write.</param>
	public void WriteSingle(float value) => _span.WriteSingle(value, _isBigEndian, ref _position);

	/// <summary>
	/// Writes a double-precision floating-point value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The double-precision floating-point value to write.</param>
	public void WriteDouble(double value) => _span.WriteDouble(value, _isBigEndian, ref _position);

	/// <summary>
	/// Writes a GUID value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The GUID value to write.</param>
	public void WriteGuid(Guid value) => _span.WriteGuid(value, ref _position);

	/// <summary>
	/// Writes a value type structure to the span at the current position and advances the position.
	/// </summary>
	/// <typeparam name="T">The type of the structure to write, which must be a value type.</typeparam>
	/// <param name="value">The structure to write.</param>
	/// <param name="size">The structure size in bytes. This parameter is used to determine how many bytes to write.</param>
	public void WriteStruct<T>(T value, int size)
		where T : struct
		=> _span.WriteStruct(value, size, ref _position);

	/// <summary>
	/// Writes an array of value type structures to the span at the current position and advances the position.
	/// </summary>
	/// <typeparam name="T">The type of the structures in the array, which must be a value type.</typeparam>
	/// <param name="array">The array of structures to write.</param>
	/// <param name="elementSize">The structure size in bytes. This parameter is used to determine how many bytes to write.</param>
	public void WriteStructArray<T>(T[] array, int elementSize)
		where T : struct
		=> _span.WriteStructArray(array, elementSize, ref _position);

	/// <summary>
	/// Advances the position by the specified count without writing any data.
	/// </summary>
	/// <param name="count">The number of bytes to skip.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative.</exception>
	public void Skip(int count)
	{
		if (count < 0 && !_allowNegativeShift)
			throw new ArgumentOutOfRangeException(nameof(count), "Negative shift is not allowed.");

		var newPos = _position + count;

		if (newPos > _span.Length)
			throw new ArgumentOutOfRangeException(nameof(count));

		_position = newPos;
	}

	/// <summary>
	/// Copies the contents of the source span to the destination span at the current position and advances the position.
	/// </summary>
	/// <param name="source">The source span containing the data to write.</param>
	public void WriteSpan(ReadOnlySpan<byte> source)
	{
		source.CopyTo(_span.Slice(_position));
		_position += source.Length;
	}
}