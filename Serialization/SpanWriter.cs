namespace Ecng.Serialization;

using System;

/// <summary>
/// Provides functionality for writing primitive data types to a span of bytes.
/// </summary>
public ref struct SpanWriter
{
	private readonly Span<byte> _span;
	private int _position;
	private readonly bool _isBigEndian;

	/// <summary>
	/// Initializes a new instance of the <see cref="SpanWriter"/> struct with the specified span.
	/// </summary>
	/// <param name="span">The span to write data to.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	public SpanWriter(Span<byte> span, bool isBigEndian = false)
	{
		_span = span;
		_position = 0;
		_isBigEndian = isBigEndian;
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
	/// Writes a byte value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The byte value to write.</param>
	public void WriteByte(byte value) => _span.WriteByte(ref _position, value);

	/// <summary>
	/// Writes a signed byte value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The signed byte value to write.</param>
	[CLSCompliant(false)]
	public void WriteSByte(sbyte value) => _span.WriteSByte(ref _position, value);

	/// <summary>
	/// Writes a boolean value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The boolean value to write.</param>
	public void WriteBoolean(bool value) => _span.WriteBoolean(ref _position, value);

	/// <summary>
	/// Writes a 16-bit integer value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The 16-bit integer value to write.</param>
	public void WriteInt16(short value) => _span.WriteInt16(ref _position, value, _isBigEndian);

	/// <summary>
	/// Writes an unsigned 16-bit integer value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The unsigned 16-bit integer value to write.</param>
	[CLSCompliant(false)]
	public void WriteUInt16(ushort value) => _span.WriteUInt16(ref _position, value, _isBigEndian);

	/// <summary>
	/// Writes a 32-bit integer value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The 32-bit integer value to write.</param>
	public void WriteInt32(int value) => _span.WriteInt32(ref _position, value, _isBigEndian);

	/// <summary>
	/// Writes an unsigned 32-bit integer value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The unsigned 32-bit integer value to write.</param>
	[CLSCompliant(false)]
	public void WriteUInt32(uint value) => _span.WriteUInt32(ref _position, value, _isBigEndian);

	/// <summary>
	/// Writes a 64-bit integer value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The 64-bit integer value to write.</param>
	public void WriteInt64(long value) => _span.WriteInt64(ref _position, value, _isBigEndian);

	/// <summary>
	/// Writes an unsigned 64-bit integer value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The unsigned 64-bit integer value to write.</param>
	[CLSCompliant(false)]
	public void WriteUInt64(ulong value) => _span.WriteUInt64(ref _position, value, _isBigEndian);

	/// <summary>
	/// Writes a decimal value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The decimal value to write.</param>
	public void WriteDecimal(decimal value) => _span.WriteDecimal(ref _position, value);

	/// <summary>
	/// Writes a DateTime value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The DateTime value to write.</param>
	public void WriteDateTime(DateTime value) => _span.WriteDateTime(ref _position, value);

	/// <summary>
	/// Writes a TimeSpan value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The TimeSpan value to write.</param>
	public void WriteTimeSpan(TimeSpan value) => _span.WriteTimeSpan(ref _position, value);

	/// <summary>
	/// Writes a string value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The string value to write.</param>
	public void WriteString(string value) => _span.WriteString(ref _position, value);

#if NETSTANDARD2_1
	/// <summary>
	/// Writes a character value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The character value to write.</param>
	public void WriteChar(char value) => _span.WriteChar(ref _position, value);
    
	/// <summary>
	/// Writes a Half value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The Half value to write.</param>
	public void WriteHalf(Half value) => _span.WriteHalf(ref _position, value, _isBigEndian);
    
	/// <summary>
	/// Writes a single-precision floating-point value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The single-precision floating-point value to write.</param>
	public void WriteSingle(float value) => _span.WriteSingle(ref _position, value, _isBigEndian);
    
	/// <summary>
	/// Writes a double-precision floating-point value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The double-precision floating-point value to write.</param>
	public void WriteDouble(double value) => _span.WriteDouble(ref _position, value, _isBigEndian);
    
	/// <summary>
	/// Writes a GUID value to the span at the current position and advances the position.
	/// </summary>
	/// <param name="value">The GUID value to write.</param>
	public void WriteGuid(Guid value) => _span.WriteGuid(ref _position, value);
#endif

	/// <summary>
	/// Writes a value type structure to the span at the current position and advances the position.
	/// </summary>
	/// <typeparam name="T">The type of the structure to write, which must be a value type.</typeparam>
	/// <param name="value">The structure to write.</param>
	public void WriteStruct<T>(T value)
		where T : struct
		=> _span.WriteStruct(ref _position, value);

	/// <summary>
	/// Writes an array of value type structures to the span at the current position and advances the position.
	/// </summary>
	/// <typeparam name="T">The type of the structures in the array, which must be a value type.</typeparam>
	/// <param name="array">The array of structures to write.</param>
	public void WriteStructArray<T>(T[] array)
		where T : struct
		=> _span.WriteStructArray(ref _position, array);

	/// <summary>
	/// Advances the position by the specified count without writing any data.
	/// </summary>
	/// <param name="count">The number of bytes to skip.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative.</exception>
	public void Skip(int count)
	{
		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count));

		_position += count;
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