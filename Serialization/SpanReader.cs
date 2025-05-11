namespace Ecng.Serialization;

using System;
#if NET5_0_OR_GREATER
using System.Text;
#endif

/// <summary>
/// A ref struct that reads primitive types from a span of bytes.
/// </summary>
public ref struct SpanReader
{
	private readonly ReadOnlySpan<byte> _span;
	private int _position;
	private readonly bool _isBigEndian;

	/// <summary>
	/// Initializes a new instance of the <see cref="SpanReader"/> struct.
	/// </summary>
	/// <param name="buffer">The array of bytes to read from.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	public SpanReader(byte[] buffer, bool isBigEndian = false)
		: this(buffer.AsSpan(), isBigEndian)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SpanReader"/> struct.
	/// </summary>
	/// <param name="memory">The memory of bytes to read from.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	public SpanReader(Memory<byte> memory, bool isBigEndian = false)
		: this(memory.Span, isBigEndian)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SpanReader"/> struct.
	/// </summary>
	/// <param name="span">The span of bytes to read from.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	public SpanReader(ReadOnlySpan<byte> span, bool isBigEndian = false)
	{
		_span = span;
		_position = 0;
		_isBigEndian = isBigEndian;
	}

	/// <summary>
	/// Gets the current position in the span.
	/// </summary>
	public readonly int Position => _position;

	/// <summary>
	/// Gets a value indicating whether the reader has reached the end of the span.
	/// </summary>
	public readonly bool End => _position >= _span.Length;

	/// <summary>
	/// Gets the number of bytes remaining to be read.
	/// </summary>
	public readonly int Remaining => _span.Length - _position;

	/// <summary>
	/// Reads a byte from the current position and advances the position by 1 byte.
	/// </summary>
	/// <returns>The byte at the current position.</returns>
	public byte ReadByte() => _span.ReadByte(ref _position);

	/// <summary>
	/// Reads a signed byte from the current position and advances the position by 1 byte.
	/// </summary>
	/// <returns>The signed byte at the current position.</returns>
	[CLSCompliant(false)]
	public sbyte ReadSByte() => _span.ReadSByte(ref _position);

	/// <summary>
	/// Reads a boolean value from the current position and advances the position by 1 byte.
	/// </summary>
	/// <returns>The boolean value at the current position.</returns>
	public bool ReadBoolean() => _span.ReadBoolean(ref _position);

	/// <summary>
	/// Reads a 16-bit integer from the current position and advances the position by 2 bytes.
	/// </summary>
	/// <returns>The 16-bit integer at the current position.</returns>
	public short ReadInt16() => _span.ReadInt16(_isBigEndian, ref _position);

	/// <summary>
	/// Reads an unsigned 16-bit integer from the current position and advances the position by 2 bytes.
	/// </summary>
	/// <returns>The unsigned 16-bit integer at the current position.</returns>
	[CLSCompliant(false)]
	public ushort ReadUInt16() => _span.ReadUInt16(_isBigEndian, ref _position);

	/// <summary>
	/// Reads a 32-bit integer from the current position and advances the position by 4 bytes.
	/// </summary>
	/// <returns>The 32-bit integer at the current position.</returns>
	public int ReadInt32() => _span.ReadInt32(_isBigEndian, ref _position);

	/// <summary>
	/// Reads an unsigned 32-bit integer from the current position and advances the position by 4 bytes.
	/// </summary>
	/// <returns>The unsigned 32-bit integer at the current position.</returns>
	[CLSCompliant(false)]
	public uint ReadUInt32() => _span.ReadUInt32(_isBigEndian, ref _position);

	/// <summary>
	/// Reads a 64-bit integer from the current position and advances the position by 8 bytes.
	/// </summary>
	/// <returns>The 64-bit integer at the current position.</returns>
	public long ReadInt64() => _span.ReadInt64(_isBigEndian, ref _position);

	/// <summary>
	/// Reads an unsigned 64-bit integer from the current position and advances the position by 8 bytes.
	/// </summary>
	/// <returns>The unsigned 64-bit integer at the current position.</returns>
	[CLSCompliant(false)]
	public ulong ReadUInt64() => _span.ReadUInt64(_isBigEndian, ref _position);

	/// <summary>
	/// Reads a decimal value from the current position and advances the position by 16 bytes.
	/// </summary>
	/// <returns>The decimal value at the current position.</returns>
	public decimal ReadDecimal() => _span.ReadDecimal(_isBigEndian, ref _position);

	/// <summary>
	/// Reads a DateTime value from the current position and advances the position.
	/// </summary>
	/// <returns>The DateTime value at the current position.</returns>
	public DateTime ReadDateTime() => new(_span.ReadInt64(_isBigEndian, ref _position));

	/// <summary>
	/// Reads a TimeSpan value from the current position and advances the position.
	/// </summary>
	/// <returns>The TimeSpan value at the current position.</returns>
	public TimeSpan ReadTimeSpan() => new(_span.ReadInt64(_isBigEndian, ref _position));

#if NET5_0_OR_GREATER
	/// <summary>
	/// Reads a character from the current position and advances the position.
	/// </summary>
	/// <returns>The character at the current position.</returns>
	public char ReadChar() => _span.ReadChar(ref _position);

	/// <summary>
	/// Reads a half-precision floating-point value from the current position and advances the position by 2 bytes.
	/// </summary>
	/// <returns>The half-precision floating-point value at the current position.</returns>
	public Half ReadHalf() => _span.ReadHalf(_isBigEndian, ref _position);

	/// <summary>
	/// Reads a single-precision floating-point value from the current position and advances the position by 4 bytes.
	/// </summary>
	/// <returns>The single-precision floating-point value at the current position.</returns>
	public float ReadSingle() => _span.ReadSingle(_isBigEndian, ref _position);

	/// <summary>
	/// Reads a double-precision floating-point value from the current position and advances the position by 8 bytes.
	/// </summary>
	/// <returns>The double-precision floating-point value at the current position.</returns>
	public double ReadDouble() => _span.ReadDouble(_isBigEndian, ref _position);

	/// <summary>
	/// Reads a GUID value from the current position and advances the position by 16 bytes.
	/// </summary>
	/// <returns>The GUID value at the current position.</returns>
	public Guid ReadGuid() => _span.ReadGuid(ref _position);

	/// <summary>
	/// Reads a string from the current position and advances the position.
	/// </summary>
	/// <param name="length">The length of the string in bytes. The length must be a multiple of the character size.</param>
	/// <param name="encoding">The encoding to use for the string. This parameter cannot be null.</param>
	/// <returns>The string at the current position.</returns>
	public string ReadString(int length, Encoding encoding)
	{
		if (encoding is null)
			throw new ArgumentNullException(nameof(encoding));

		if (length == 0)
			return string.Empty;

		return encoding.GetString(ReadSpan(length));
	}
#endif

	/// <summary>
	/// Reads a structure of type <typeparamref name="T"/> from the current position and advances the position by the size of T.
	/// </summary>
	/// <typeparam name="T">The type of structure to read.</typeparam>
	/// <returns>The structure of type <typeparamref name="T"/> at the current position.</returns>
	public T ReadStruct<T>(int size)
		where T : struct
		=> _span.ReadStruct<T>(size, ref _position);

	/// <summary>
	/// Reads an array of structures of type <typeparamref name="T"/> from the current position and advances the position.
	/// </summary>
	/// <typeparam name="T">The type of structures to read.</typeparam>
	/// <param name="array">An array containing the structures of type <typeparamref name="T"/> read from the current position.</param>
	/// <param name="elementSize">The size of each structure in bytes.</param>
	/// <param name="count">The number of structures to read.</param>
	public void ReadStructArray<T>(T[] array, int elementSize, int count)
		where T : struct
		=> _span.ReadStructArray(array, elementSize, count, ref _position);

	/// <summary>
	/// Skips a specified number of bytes from the current position.
	/// </summary>
	/// <param name="count">The number of bytes to skip. Must be non-negative.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative.</exception>
	public void Skip(int count)
	{
		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count));

		_position += count;
	}

	/// <summary>
	/// Reads a span of bytes of the specified length from the current position and advances the position.
	/// </summary>
	/// <param name="length">The number of bytes to read.</param>
	/// <returns>A span containing the bytes read from the current position.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when length is negative or would exceed the bounds of the span.</exception>
	public ReadOnlySpan<byte> ReadSpan(int length)
	{
		if (length < 0 || _position + length > _span.Length)
			throw new ArgumentOutOfRangeException(nameof(length));

		var result = _span.Slice(_position, length);
		_position += length;
		return result;
	}
}