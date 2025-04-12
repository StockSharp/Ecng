namespace Ecng.Serialization;

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Provides extension methods for reading from and writing to spans.
/// </summary>
public static class SpanExtensions
{
	/// <summary>
	/// Reads a byte from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by 1.</param>
	/// <returns>The byte value read.</returns>
	public static byte ReadByte(this ReadOnlySpan<byte> span, ref int position)
	{
		var value = span[position];
		position += sizeof(byte);
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
		position += sizeof(sbyte);
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
		position += sizeof(bool);
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
			? BinaryPrimitives.ReadInt16BigEndian(span.Slice(position, sizeof(short)))
			: BinaryPrimitives.ReadInt16LittleEndian(span.Slice(position, sizeof(short)));
		position += sizeof(short);
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
			? BinaryPrimitives.ReadUInt16BigEndian(span.Slice(position, sizeof(ushort)))
			: BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(position, sizeof(ushort)));
		position += sizeof(ushort);
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
			? BinaryPrimitives.ReadInt32BigEndian(span.Slice(position, sizeof(int)))
			: BinaryPrimitives.ReadInt32LittleEndian(span.Slice(position, sizeof(int)));
		position += sizeof(int);
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
			? BinaryPrimitives.ReadUInt32BigEndian(span.Slice(position, sizeof(uint)))
			: BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(position, sizeof(uint)));
		position += sizeof(uint);
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
			? BinaryPrimitives.ReadInt64BigEndian(span.Slice(position, sizeof(long)))
			: BinaryPrimitives.ReadInt64LittleEndian(span.Slice(position, sizeof(long)));
		position += sizeof(long);
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
			? BinaryPrimitives.ReadUInt64BigEndian(span.Slice(position, sizeof(ulong)))
			: BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(position, sizeof(ulong)));
		position += sizeof(ulong);
		return value;
	}

#if NETSTANDARD2_1
    /// <summary>
    /// Reads a Unicode character from the specified read-only span at the current position.
    /// </summary>
    /// <param name="span">The read-only span of bytes.</param>
    /// <param name="position">The position to read from; will be advanced by the size of a char.</param>
    /// <returns>The char value read.</returns>
    public static char ReadChar(this ReadOnlySpan<byte> span, ref int position)
    {
        var value = BitConverter.ToChar(span.Slice(position, sizeof(char)));
        position += sizeof(char);
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
            ? BinaryPrimitives.ReadHalfBigEndian(span.Slice(position, sizeof(Half)))
            : BinaryPrimitives.ReadHalfLittleEndian(span.Slice(position, sizeof(Half)));
        position += sizeof(Half);
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
            ? BinaryPrimitives.ReadSingleBigEndian(span.Slice(position, sizeof(float)))
            : BinaryPrimitives.ReadSingleLittleEndian(span.Slice(position, sizeof(float)));
        position += sizeof(float);
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
            ? BinaryPrimitives.ReadDoubleBigEndian(span.Slice(position, sizeof(double)))
            : BinaryPrimitives.ReadDoubleLittleEndian(span.Slice(position, sizeof(double)));
        position += sizeof(double);
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

    /// <summary>
    /// Reads a UTF-8 encoded string from the specified read-only span at the current position.
    /// </summary>
    /// <param name="span">The read-only span of bytes.</param>
    /// <param name="position">The position to read from; the position advances by 4 plus the length of the string in bytes.</param>
    /// <returns>The string read, or null if the length is negative.</returns>
    public static string ReadString(this ReadOnlySpan<byte> span, ref int position)
    {
        var length = span.ReadInt32(ref position);
        if (length < 0)
            return null;

        var value = Encoding.UTF8.GetString(span.Slice(position, length));
        position += length;
        return value;
    }
#endif

	/// <summary>
	/// Reads a decimal value from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by 16 bytes.</param>
	/// <returns>The decimal value read.</returns>
	public static decimal ReadDecimal(this ReadOnlySpan<byte> span, ref int position)
	{
		var lo = span.ReadInt32(ref position);
		var mid = span.ReadInt32(ref position);
		var hi = span.ReadInt32(ref position);
		var flags = span.ReadInt32(ref position);
		return new decimal(lo, mid, hi, (flags & 0x80000000) != 0, (byte)((flags >> 16) & 0x7F));
	}

	/// <summary>
	/// Reads a DateTime value from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by 8 bytes.</param>
	/// <returns>The DateTime value read.</returns>
	public static DateTime ReadDateTime(this ReadOnlySpan<byte> span, ref int position)
	{
		var ticks = span.ReadInt64(ref position);
		return new DateTime(ticks);
	}

	/// <summary>
	/// Reads a TimeSpan value from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by 8 bytes.</param>
	/// <returns>The TimeSpan value read.</returns>
	public static TimeSpan ReadTimeSpan(this ReadOnlySpan<byte> span, ref int position)
	{
		var ticks = span.ReadInt64(ref position);
		return new TimeSpan(ticks);
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
		position += sizeof(byte);
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
		position += sizeof(sbyte);
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
		position += sizeof(bool);
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
			BinaryPrimitives.WriteInt16BigEndian(span.Slice(position, sizeof(short)), value);
		else
			BinaryPrimitives.WriteInt16LittleEndian(span.Slice(position, sizeof(short)), value);
		position += sizeof(short);
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
			BinaryPrimitives.WriteUInt16BigEndian(span.Slice(position, sizeof(ushort)), value);
		else
			BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(position, sizeof(ushort)), value);
		position += sizeof(ushort);
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
			BinaryPrimitives.WriteInt32BigEndian(span.Slice(position, sizeof(int)), value);
		else
			BinaryPrimitives.WriteInt32LittleEndian(span.Slice(position, sizeof(int)), value);
		position += sizeof(int);
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
			BinaryPrimitives.WriteUInt32BigEndian(span.Slice(position, sizeof(uint)), value);
		else
			BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(position, sizeof(uint)), value);
		position += sizeof(uint);
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
			BinaryPrimitives.WriteInt64BigEndian(span.Slice(position, sizeof(long)), value);
		else
			BinaryPrimitives.WriteInt64LittleEndian(span.Slice(position, sizeof(long)), value);
		position += sizeof(long);
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
			BinaryPrimitives.WriteUInt64BigEndian(span.Slice(position, sizeof(ulong)), value);
		else
			BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(position, sizeof(ulong)), value);
		position += sizeof(ulong);
	}

#if NETSTANDARD2_1
    /// <summary>
    /// Writes a Unicode character to the specified span at the current position.
    /// </summary>
    /// <param name="span">The span of bytes.</param>
    /// <param name="position">The position to write to; will be advanced by the size of a char.</param>
    /// <param name="value">The char value to write.</param>
    public static void WriteChar(this Span<byte> span, ref int position, char value)
    {
        BitConverter.TryWriteBytes(span.Slice(position, sizeof(char)), value);
        position += sizeof(char);
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
            BinaryPrimitives.WriteHalfBigEndian(span.Slice(position, sizeof(Half)), value);
        else
            BinaryPrimitives.WriteHalfLittleEndian(span.Slice(position, sizeof(Half)), value);
        position += sizeof(Half);
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
            BinaryPrimitives.WriteSingleBigEndian(span.Slice(position, sizeof(float)), value);
        else
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(position, sizeof(float)), value);
        position += sizeof(float);
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
            BinaryPrimitives.WriteDoubleBigEndian(span.Slice(position, sizeof(double)), value);
        else
            BinaryPrimitives.WriteDoubleLittleEndian(span.Slice(position, sizeof(double)), value);
        position += sizeof(double);
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
	public static void WriteDecimal(this Span<byte> span, ref int position, decimal value)
	{
		var bits = decimal.GetBits(value);
		span.WriteInt32(ref position, bits[0]);
		span.WriteInt32(ref position, bits[1]);
		span.WriteInt32(ref position, bits[2]);
		span.WriteInt32(ref position, bits[3]);
	}

	/// <summary>
	/// Writes a DateTime value to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by 8 bytes.</param>
	/// <param name="value">The DateTime value to write.</param>
	public static void WriteDateTime(this Span<byte> span, ref int position, DateTime value)
	{
		span.WriteInt64(ref position, value.Ticks);
	}

	/// <summary>
	/// Writes a TimeSpan value to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by 8 bytes.</param>
	/// <param name="value">The TimeSpan value to write.</param>
	public static void WriteTimeSpan(this Span<byte> span, ref int position, TimeSpan value)
	{
		span.WriteInt64(ref position, value.Ticks);
	}

	/// <summary>
	/// Writes a UTF-8 encoded string to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="position">The position to write to; will be advanced by 4 plus the length of the string in bytes.</param>
	/// <param name="value">The string value to write. If null, -1 is written as the length.</param>
	public static void WriteString(this Span<byte> span, ref int position, string value)
	{
		if (value == null)
		{
			span.WriteInt32(ref position, -1);
			return;
		}

		var bytes = Encoding.UTF8.GetBytes(value);
		span.WriteInt32(ref position, bytes.Length);
		bytes.AsSpan().CopyTo(span.Slice(position));
		position += bytes.Length;
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