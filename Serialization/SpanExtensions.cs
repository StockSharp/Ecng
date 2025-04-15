namespace Ecng.Serialization;

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Provides extension methods for reading from and writing to spans.
/// </summary>
public static class SpanExtensions
{
	private const int _halfSize = 2; // sizeof(Half)
	private const int _floatSize = sizeof(float);
	private const int _doubleSize = sizeof(double);
	private const int _shortSize = sizeof(short);
	private const int _intSize = sizeof(int);
	private const int _longSize = sizeof(long);
	private const int _ushortSize = sizeof(ushort);
	private const int _uintSize = sizeof(uint);
	private const int _ulongSize = sizeof(ulong);
	private const int _byteSize = sizeof(byte);
	private const int _sbyteSize = sizeof(sbyte);
	private const int _boolSize = sizeof(bool);
	private const int _charSize = sizeof(char);

	/// <summary>
	/// Reads a byte from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by 1.</param>
	/// <returns>The byte value read.</returns>
	public static byte ReadByte(this ReadOnlySpan<byte> span, ref int position)
	{
		var value = span[position];
		position += _byteSize;
		return value;
	}

	/// <summary>
	/// Reads a signed byte from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by 1.</param>
	/// <returns>The sbyte value read.</returns>
	[CLSCompliant(false)]
	public static sbyte ReadSByte(this ReadOnlySpan<byte> span, ref int position)
	{
		var value = (sbyte)span[position];
		position += _sbyteSize;
		return value;
	}

	/// <summary>
	/// Reads a boolean value from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by the size of a boolean.</param>
	/// <returns>The boolean value read.</returns>
	public static bool ReadBoolean(this ReadOnlySpan<byte> span, ref int position)
	{
		var value = span[position] != 0;
		position += _boolSize;
		return value;
	}

	/// <summary>
	/// Reads a 16-bit signed integer from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by 2.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <returns>The 16-bit signed integer read.</returns>
	public static short ReadInt16(this ReadOnlySpan<byte> span, ref int position, bool isBigEndian = false)
	{
		var value = isBigEndian
			? BinaryPrimitives.ReadInt16BigEndian(span.Slice(position, _shortSize))
			: BinaryPrimitives.ReadInt16LittleEndian(span.Slice(position, _shortSize));
		
		position += _shortSize;
		
		return value;
	}

	/// <summary>
	/// Reads a 16-bit unsigned integer from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by 2.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <returns>The 16-bit unsigned integer read.</returns>
	[CLSCompliant(false)]
	public static ushort ReadUInt16(this ReadOnlySpan<byte> span, ref int position, bool isBigEndian = false)
	{
		var value = isBigEndian
			? BinaryPrimitives.ReadUInt16BigEndian(span.Slice(position, _ushortSize))
			: BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(position, _ushortSize));
		
		position += _ushortSize;
		
		return value;
	}

	/// <summary>
	/// Reads a 32-bit signed integer from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by 4.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <returns>The 32-bit signed integer read.</returns>
	public static int ReadInt32(this ReadOnlySpan<byte> span, ref int position, bool isBigEndian = false)
	{
		var value = isBigEndian
			? BinaryPrimitives.ReadInt32BigEndian(span.Slice(position, _intSize))
			: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(position, _intSize));
		
		position += _intSize;
		
		return value;
	}

	/// <summary>
	/// Reads a 32-bit unsigned integer from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by 4.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <returns>The 32-bit unsigned integer read.</returns>
	[CLSCompliant(false)]
	public static uint ReadUInt32(this ReadOnlySpan<byte> span, ref int position, bool isBigEndian = false)
	{
		var value = isBigEndian
			? BinaryPrimitives.ReadUInt32BigEndian(span.Slice(position, _uintSize))
			: BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(position, _uintSize));
		
		position += _uintSize;
		
		return value;
	}

	/// <summary>
	/// Reads a 64-bit signed integer from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by 8.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <returns>The 64-bit signed integer read.</returns>
	public static long ReadInt64(this ReadOnlySpan<byte> span, ref int position, bool isBigEndian = false)
	{
		var value = isBigEndian
			? BinaryPrimitives.ReadInt64BigEndian(span.Slice(position, _longSize))
			: BinaryPrimitives.ReadInt64LittleEndian(span.Slice(position, _longSize));
		
		position += _longSize;
		
		return value;
	}

	/// <summary>
	/// Reads a 64-bit unsigned integer from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by 8.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <returns>The 64-bit unsigned integer read.</returns>
	[CLSCompliant(false)]
	public static ulong ReadUInt64(this ReadOnlySpan<byte> span, ref int position, bool isBigEndian = false)
	{
		var value = isBigEndian
			? BinaryPrimitives.ReadUInt64BigEndian(span.Slice(position, _ulongSize))
			: BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(position, _ulongSize));
		
		position += _ulongSize;
		
		return value;
	}

#if NET5_0_OR_GREATER
	/// <summary>
	/// Reads a Unicode character from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by the size of a char.</param>
	/// <returns>The char value read.</returns>
	public static char ReadChar(this ReadOnlySpan<byte> span, ref int position)
    {
        var value = BitConverter.ToChar(span.Slice(position, _charSize));
        position += _charSize;
        return value;
    }

    /// <summary>
    /// Reads a half-precision floating-point number from the specified read-only span at the current position.
    /// </summary>
    /// <param name="span">The read-only span of bytes.</param>
    /// <param name="position">The position to read from; will be advanced by the size of a Half.</param>
    /// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
    /// <returns>The Half value read.</returns>
    public static Half ReadHalf(this ReadOnlySpan<byte> span, ref int position, bool isBigEndian = false)
    {
        var value = isBigEndian
            ? BinaryPrimitives.ReadHalfBigEndian(span.Slice(position, _halfSize))
            : BinaryPrimitives.ReadHalfLittleEndian(span.Slice(position, _halfSize));
        
		position += _halfSize;
        
		return value;
    }

    /// <summary>
    /// Reads a single-precision floating-point number from the specified read-only span at the current position.
    /// </summary>
    /// <param name="span">The read-only span of bytes.</param>
    /// <param name="position">The position to read from; will be advanced by the size of a float.</param>
    /// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
    /// <returns>The float value read.</returns>
    public static float ReadSingle(this ReadOnlySpan<byte> span, ref int position, bool isBigEndian = false)
    {
        var value = isBigEndian
            ? BinaryPrimitives.ReadSingleBigEndian(span.Slice(position, _floatSize))
            : BinaryPrimitives.ReadSingleLittleEndian(span.Slice(position, _floatSize));
        
		position += _floatSize;
        
		return value;
    }

    /// <summary>
    /// Reads a double-precision floating-point number from the specified read-only span at the current position.
    /// </summary>
    /// <param name="span">The read-only span of bytes.</param>
    /// <param name="position">The position to read from; will be advanced by the size of a double.</param>
    /// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
    /// <returns>The double value read.</returns>
    public static double ReadDouble(this ReadOnlySpan<byte> span, ref int position, bool isBigEndian = false)
    {
        var value = isBigEndian
            ? BinaryPrimitives.ReadDoubleBigEndian(span.Slice(position, _doubleSize))
            : BinaryPrimitives.ReadDoubleLittleEndian(span.Slice(position, _doubleSize));
       
		position += _doubleSize;
        
		return value;
    }

    /// <summary>
    /// Reads a GUID from the specified read-only span at the current position.
    /// </summary>
    /// <param name="span">The read-only span of bytes.</param>
    /// <param name="position">The position to read from; will be advanced by 16 bytes.</param>
    /// <returns>The GUID read.</returns>
    public static Guid ReadGuid(this ReadOnlySpan<byte> span, ref int position)
    {
        var value = new Guid(span.Slice(position, 16));
        position += 16;
        return value;
    }
#endif

	/// <summary>
	/// Reads a decimal value from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by 16 bytes.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <returns>The decimal value read.</returns>
	public static decimal ReadDecimal(this ReadOnlySpan<byte> span, ref int position, bool isBigEndian = false)
	{
		var lo = span.ReadInt32(ref position, isBigEndian);
		var mid = span.ReadInt32(ref position, isBigEndian);
		var hi = span.ReadInt32(ref position, isBigEndian);
		var flags = span.ReadInt32(ref position, isBigEndian);
		return new(lo, mid, hi, (flags & 0x80000000) != 0, (byte)((flags >> 16) & 0x7F));
	}

	/// <summary>
	/// Reads a structure of type <typeparamref name="T"/> from the specified read-only span at the current position.
	/// </summary>
	/// <typeparam name="T">The structure type to read.</typeparam>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by the size of the structure.</param>
	/// <returns>The structure read from the span.</returns>
	public static T ReadStruct<T>(this ReadOnlySpan<byte> span, ref int position)
		where T : struct
	{
		var size = Unsafe.SizeOf<T>();
		var result = MemoryMarshal.Read<T>(span.Slice(position, size));
		position += size;
		return result;
	}

	/// <summary>
	/// Reads an array of structures of type <typeparamref name="T"/> from the specified read-only span at the current position.
	/// </summary>
	/// <typeparam name="T">The structure type to read.</typeparam>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="array">An array of structures read from the span.</param>
	/// <param name="count">The number of elements to read.</param>
	/// <param name="position">The position to read from; will be advanced by the total size of the array.</param>
	public static void ReadStructArray<T>(this ReadOnlySpan<byte> span, T[] array, int count, ref int position)
		where T : struct
	{
		if (array is null)
			throw new ArgumentNullException(nameof(array));

		var elementSize = Unsafe.SizeOf<T>();
		var totalSize = elementSize * count;

		var dataSpan = span.Slice(position, totalSize);

		for (var i = 0; i < count; i++)
		{
			array[i] = MemoryMarshal.Read<T>(dataSpan.Slice(i * elementSize, elementSize));
		}

		position += totalSize;
	}

	/// <summary>
	/// Writes a byte to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by 1.</param>
	/// <param name="value">The byte value to write.</param>
	public static void WriteByte(this Span<byte> span, ref int position, byte value)
	{
		span[position] = value;
		position += _byteSize;
	}

	/// <summary>
	/// Writes a signed byte to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by 1.</param>
	/// <param name="value">The sbyte value to write.</param>
	[CLSCompliant(false)]
	public static void WriteSByte(this Span<byte> span, ref int position, sbyte value)
	{
		span[position] = (byte)value;
		position += _sbyteSize;
	}

	/// <summary>
	/// Writes a boolean value to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by the size of a boolean.</param>
	/// <param name="value">The boolean value to write.</param>
	public static void WriteBoolean(this Span<byte> span, ref int position, bool value)
	{
		span[position] = value ? (byte)1 : (byte)0;
		position += _boolSize;
	}

	/// <summary>
	/// Writes a 16-bit signed integer to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by 2.</param>
	/// <param name="value">The 16-bit signed integer value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	public static void WriteInt16(this Span<byte> span, ref int position, short value, bool isBigEndian = false)
	{
		if (isBigEndian)
			BinaryPrimitives.WriteInt16BigEndian(span.Slice(position, _shortSize), value);
		else
			BinaryPrimitives.WriteInt16LittleEndian(span.Slice(position, _shortSize), value);
		
		position += _shortSize;
	}

	/// <summary>
	/// Writes a 16-bit unsigned integer to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by 2.</param>
	/// <param name="value">The 16-bit unsigned integer value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	[CLSCompliant(false)]
	public static void WriteUInt16(this Span<byte> span, ref int position, ushort value, bool isBigEndian = false)
	{
		if (isBigEndian)
			BinaryPrimitives.WriteUInt16BigEndian(span.Slice(position, _ushortSize), value);
		else
			BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(position, _ushortSize), value);
		
		position += _ushortSize;
	}

	/// <summary>
	/// Writes a 32-bit signed integer to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by 4.</param>
	/// <param name="value">The 32-bit signed integer value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	public static void WriteInt32(this Span<byte> span, ref int position, int value, bool isBigEndian = false)
	{
		if (isBigEndian)
			BinaryPrimitives.WriteInt32BigEndian(span.Slice(position, _intSize), value);
		else
			BinaryPrimitives.WriteInt32LittleEndian(span.Slice(position, _intSize), value);
		
		position += _intSize;
	}

	/// <summary>
	/// Writes a 32-bit unsigned integer to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by 4.</param>
	/// <param name="value">The 32-bit unsigned integer value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	[CLSCompliant(false)]
	public static void WriteUInt32(this Span<byte> span, ref int position, uint value, bool isBigEndian = false)
	{
		if (isBigEndian)
			BinaryPrimitives.WriteUInt32BigEndian(span.Slice(position, _uintSize), value);
		else
			BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(position, _uintSize), value);
		
		position += _uintSize;
	}

	/// <summary>
	/// Writes a 64-bit signed integer to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by 8.</param>
	/// <param name="value">The 64-bit signed integer value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	public static void WriteInt64(this Span<byte> span, ref int position, long value, bool isBigEndian = false)
	{
		if (isBigEndian)
			BinaryPrimitives.WriteInt64BigEndian(span.Slice(position, _longSize), value);
		else
			BinaryPrimitives.WriteInt64LittleEndian(span.Slice(position, _longSize), value);
		
		position += _longSize;
	}

	/// <summary>
	/// Writes a 64-bit unsigned integer to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by 8.</param>
	/// <param name="value">The 64-bit unsigned integer value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	[CLSCompliant(false)]
	public static void WriteUInt64(this Span<byte> span, ref int position, ulong value, bool isBigEndian = false)
	{
		if (isBigEndian)
			BinaryPrimitives.WriteUInt64BigEndian(span.Slice(position, _ulongSize), value);
		else
			BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(position, _ulongSize), value);
		
		position += _ulongSize;
	}

#if NET5_0_OR_GREATER
	/// <summary>
	/// Writes a Unicode character to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by the size of a char.</param>
	/// <param name="value">The char value to write.</param>
	public static void WriteChar(this Span<byte> span, ref int position, char value)
    {
        BitConverter.TryWriteBytes(span.Slice(position, _charSize), value);
        position += _charSize;
    }

    /// <summary>
    /// Writes a half-precision floating-point number to the specified span at the current position.
    /// </summary>
    /// <param name="span">The span of bytes.</param>
    /// <param name="position">The position to write to; will be advanced by the size of a Half.</param>
    /// <param name="value">The Half value to write.</param>
    /// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
    public static void WriteHalf(this Span<byte> span, ref int position, Half value, bool isBigEndian = false)
    {
        if (isBigEndian)
            BinaryPrimitives.WriteHalfBigEndian(span.Slice(position, _halfSize), value);
        else
            BinaryPrimitives.WriteHalfLittleEndian(span.Slice(position, _halfSize), value);
        
		position += _halfSize;
    }

    /// <summary>
    /// Writes a single-precision floating-point number to the specified span at the current position.
    /// </summary>
    /// <param name="span">The span of bytes.</param>
    /// <param name="position">The position to write to; will be advanced by the size of a float.</param>
    /// <param name="value">The float value to write.</param>
    /// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
    public static void WriteSingle(this Span<byte> span, ref int position, float value, bool isBigEndian = false)
    {
        if (isBigEndian)
            BinaryPrimitives.WriteSingleBigEndian(span.Slice(position, _floatSize), value);
        else
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(position, _floatSize), value);
        
		position += _floatSize;
    }

    /// <summary>
    /// Writes a double-precision floating-point number to the specified span at the current position.
    /// </summary>
    /// <param name="span">The span of bytes.</param>
    /// <param name="position">The position to write to; will be advanced by the size of a double.</param>
    /// <param name="value">The double value to write.</param>
    /// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
    public static void WriteDouble(this Span<byte> span, ref int position, double value, bool isBigEndian = false)
    {
        if (isBigEndian)
            BinaryPrimitives.WriteDoubleBigEndian(span.Slice(position, _doubleSize), value);
        else
            BinaryPrimitives.WriteDoubleLittleEndian(span.Slice(position, _doubleSize), value);
       
		position += _doubleSize;
    }

    /// <summary>
    /// Writes a GUID to the specified span at the current position.
    /// </summary>
    /// <param name="span">The span of bytes.</param>
    /// <param name="position">The position to write to; will be advanced by 16 bytes.</param>
    /// <param name="value">The GUID value to write.</param>
    public static void WriteGuid(this Span<byte> span, ref int position, Guid value)
    {
        value.TryWriteBytes(span.Slice(position, 16));
        position += 16;
    }
#endif

	/// <summary>
	/// Writes a decimal value to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by 16 bytes.</param>
	/// <param name="value">The decimal value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	public static void WriteDecimal(this Span<byte> span, ref int position, decimal value, bool isBigEndian = false)
	{
		var bits = decimal.GetBits(value);
		span.WriteInt32(ref position, bits[0], isBigEndian);
		span.WriteInt32(ref position, bits[1], isBigEndian);
		span.WriteInt32(ref position, bits[2], isBigEndian);
		span.WriteInt32(ref position, bits[3], isBigEndian);
	}

	/// <summary>
	/// Writes a structure of type <typeparamref name="T"/> to the specified span at the current position.
	/// </summary>
	/// <typeparam name="T">The structure type to write.</typeparam>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by the size of the structure.</param>
	/// <param name="value">The structure value to write.</param>
	[CLSCompliant(false)]
	public static void WriteStruct<T>(this Span<byte> span, ref int position, T value)
		where T : struct
	{
		var size = Unsafe.SizeOf<T>();
		MemoryMarshal.Write(span.Slice(position, size), ref value);
		position += size;
	}

	/// <summary>
	/// Writes an array of structures of type <typeparamref name="T"/> to the specified span at the current position.
	/// </summary>
	/// <typeparam name="T">The structure type to write.</typeparam>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by the total size of the array.</param>
	/// <param name="array">The array of structures to write.</param>
	public static void WriteStructArray<T>(this Span<byte> span, ref int position, T[] array)
		where T : struct
	{
		if (array == null)
			throw new ArgumentNullException(nameof(array));

		var elementSize = Unsafe.SizeOf<T>();
		var totalSize = elementSize * array.Length;

		var targetSpan = span.Slice(position, totalSize);

		for (var i = 0; i < array.Length; i++)
		{
			MemoryMarshal.Write(targetSpan.Slice(i * elementSize, elementSize), ref array[i]);
		}

		position += totalSize;
	}
}