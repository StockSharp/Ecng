namespace Ecng.Serialization;

using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

#if !NET5_0_OR_GREATER
using Ecng.Common;
#endif

/// <summary>
/// Provides extension methods for reading from and writing to spans.
/// </summary>
public static class SpanExtensions
{
#if NET5_0_OR_GREATER
	private const int _halfSize = 2; // sizeof(Half)
#endif
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
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to read from; will be advanced by 2.</param>
	/// <returns>The 16-bit signed integer read.</returns>
	public static short ReadInt16(this ReadOnlySpan<byte> span, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _shortSize);

		var value = isBigEndian
			? BinaryPrimitives.ReadInt16BigEndian(slice)
			: BinaryPrimitives.ReadInt16LittleEndian(slice);

		position += _shortSize;

		return value;
	}

	/// <summary>
	/// Reads a 16-bit unsigned integer from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to read from; will be advanced by 2.</param>
	/// <returns>The 16-bit unsigned integer read.</returns>
	[CLSCompliant(false)]
	public static ushort ReadUInt16(this ReadOnlySpan<byte> span, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _ushortSize);

		var value = isBigEndian
			? BinaryPrimitives.ReadUInt16BigEndian(slice)
			: BinaryPrimitives.ReadUInt16LittleEndian(slice);

		position += _ushortSize;

		return value;
	}

	/// <summary>
	/// Reads a 32-bit signed integer from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to read from; will be advanced by 4.</param>
	/// <returns>The 32-bit signed integer read.</returns>
	public static int ReadInt32(this ReadOnlySpan<byte> span, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _intSize);

		var value = isBigEndian
			? BinaryPrimitives.ReadInt32BigEndian(slice)
			: BinaryPrimitives.ReadInt32LittleEndian(slice);

		position += _intSize;

		return value;
	}

	/// <summary>
	/// Reads a 32-bit unsigned integer from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to read from; will be advanced by 4.</param>
	/// <returns>The 32-bit unsigned integer read.</returns>
	[CLSCompliant(false)]
	public static uint ReadUInt32(this ReadOnlySpan<byte> span, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _uintSize);

		var value = isBigEndian
			? BinaryPrimitives.ReadUInt32BigEndian(slice)
			: BinaryPrimitives.ReadUInt32LittleEndian(slice);

		position += _uintSize;

		return value;
	}

	/// <summary>
	/// Reads a 64-bit signed integer from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to read from; will be advanced by 8.</param>
	/// <returns>The 64-bit signed integer read.</returns>
	public static long ReadInt64(this ReadOnlySpan<byte> span, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _longSize);

		var value = isBigEndian
			? BinaryPrimitives.ReadInt64BigEndian(slice)
			: BinaryPrimitives.ReadInt64LittleEndian(slice);

		position += _longSize;

		return value;
	}

	/// <summary>
	/// Reads a 64-bit unsigned integer from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to read from; will be advanced by 8.</param>
	/// <returns>The 64-bit unsigned integer read.</returns>
	[CLSCompliant(false)]
	public static ulong ReadUInt64(this ReadOnlySpan<byte> span, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _ulongSize);

		var value = isBigEndian
			? BinaryPrimitives.ReadUInt64BigEndian(slice)
			: BinaryPrimitives.ReadUInt64LittleEndian(slice);

		position += _ulongSize;

		return value;
	}

	/// <summary>
	/// Reads a Unicode character from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="position">The position to read from; will be advanced by the size of a char.</param>
	/// <returns>The char value read.</returns>
	public static char ReadChar(this ReadOnlySpan<byte> span, ref int position)
	{
		var slice = span.Slice(position, _charSize);

		var value =
#if NET5_0_OR_GREATER
			BitConverter.ToChar(slice)
#else
			slice.ToArray().To<char>()
#endif
		;
		position += _charSize;
		return value;
	}

#if NET5_0_OR_GREATER
	/// <summary>
	/// Reads a half-precision floating-point number from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to read from; will be advanced by the size of a Half.</param>
	/// <returns>The Half value read.</returns>
	public static Half ReadHalf(this ReadOnlySpan<byte> span, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _halfSize);

		var value = isBigEndian
			? BinaryPrimitives.ReadHalfBigEndian(slice)
			: BinaryPrimitives.ReadHalfLittleEndian(slice);

		position += _halfSize;

		return value;
	}
#endif

	/// <summary>
	/// Reads a single-precision floating-point number from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to read from; will be advanced by the size of a float.</param>
	/// <returns>The float value read.</returns>
	public static float ReadSingle(this ReadOnlySpan<byte> span, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _floatSize);

#if NET5_0_OR_GREATER
		var value = isBigEndian
			? BinaryPrimitives.ReadSingleBigEndian(slice)
			: BinaryPrimitives.ReadSingleLittleEndian(slice);
#else
		var bytes = slice.ToArray();

		if (isBigEndian != BitConverter.IsLittleEndian)
			Array.Reverse(bytes);

		var value = BitConverter.ToSingle(bytes, 0);
#endif

		position += _floatSize;

		return value;
	}

	/// <summary>
	/// Reads a double-precision floating-point number from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to read from; will be advanced by the size of a double.</param>
	/// <returns>The double value read.</returns>
	public static double ReadDouble(this ReadOnlySpan<byte> span, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _doubleSize);

#if NET5_0_OR_GREATER
		var value = isBigEndian
			? BinaryPrimitives.ReadDoubleBigEndian(slice)
			: BinaryPrimitives.ReadDoubleLittleEndian(slice);
#else
		var bytes = slice.ToArray();

		if (isBigEndian != BitConverter.IsLittleEndian)
			Array.Reverse(bytes);

		var value = BitConverter.ToDouble(bytes, 0);
#endif

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
		var slice = span.Slice(position, 16);
		var value = new Guid(
#if NET5_0_OR_GREATER
			slice
#else
			slice.ToArray()
#endif
		);
		position += 16;
		return value;
	}

	/// <summary>
	/// Reads a decimal value from the specified read-only span at the current position.
	/// </summary>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="isBigEndian">If true, reads in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to read from; will be advanced by 16 bytes.</param>
	/// <returns>The decimal value read.</returns>
	public static decimal ReadDecimal(this ReadOnlySpan<byte> span, bool isBigEndian, ref int position)
	{
		var lo = span.ReadInt32(isBigEndian, ref position);
		var mid = span.ReadInt32(isBigEndian, ref position);
		var hi = span.ReadInt32(isBigEndian, ref position);
		var flags = span.ReadInt32(isBigEndian, ref position);
		return new(lo, mid, hi, (flags & 0x80000000) != 0, (byte)((flags >> 16) & 0x7F));
	}

	/// <summary>
	/// Reads a structure of type <typeparamref name="T"/> from the specified read-only span at the current position.
	/// </summary>
	/// <typeparam name="T">The structure type to read.</typeparam>
	/// <param name="span">The read-only span of bytes.</param>
	/// <param name="size">The size of the structure in bytes. This parameter is used to determine how many bytes to read.</param>
	/// <param name="position">The position to read from; will be advanced by the size of the structure.</param>
	/// <returns>The structure read from the span.</returns>
	public static T ReadStruct<T>(this ReadOnlySpan<byte> span, int size, ref int position)
		where T : struct
	{
		if (size == 0)
			return default;

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
	/// <param name="elementSize">The size of each structure in bytes.</param>
	/// <param name="count">The number of elements to read.</param>
	/// <param name="position">The position to read from; will be advanced by the total size of the array.</param>
	public static void ReadStructArray<T>(this ReadOnlySpan<byte> span, T[] array, int elementSize, int count, ref int position)
		where T : struct
	{
		if (array is null)
			throw new ArgumentNullException(nameof(array));

		var totalSize = elementSize * count;

		if (totalSize == 0)
			return;

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
	/// <param name="value">The byte value to write.</param>
	/// <param name="position">The position to write to; will be advanced by 1.</param>
	public static void WriteByte(this Span<byte> span, byte value, ref int position)
	{
		span[position] = value;
		position += _byteSize;
	}

	/// <summary>
	/// Writes a signed byte to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="value">The sbyte value to write.</param>
	/// <param name="position">The position to write to; will be advanced by 1.</param>
	[CLSCompliant(false)]
	public static void WriteSByte(this Span<byte> span, sbyte value, ref int position)
	{
		span[position] = (byte)value;
		position += _sbyteSize;
	}

	/// <summary>
	/// Writes a boolean value to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="value">The boolean value to write.</param>
	/// <param name="position">The position to write to; will be advanced by the size of a boolean.</param>
	public static void WriteBoolean(this Span<byte> span, bool value, ref int position)
	{
		span[position] = value ? (byte)1 : (byte)0;
		position += _boolSize;
	}

	/// <summary>
	/// Writes a 16-bit signed integer to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="value">The 16-bit signed integer value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to write to; will be advanced by 2.</param>
	public static void WriteInt16(this Span<byte> span, short value, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _shortSize);

		if (isBigEndian)
			BinaryPrimitives.WriteInt16BigEndian(slice, value);
		else
			BinaryPrimitives.WriteInt16LittleEndian(slice, value);

		position += _shortSize;
	}

	/// <summary>
	/// Writes a 16-bit unsigned integer to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="value">The 16-bit unsigned integer value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to write to; will be advanced by 2.</param>
	[CLSCompliant(false)]
	public static void WriteUInt16(this Span<byte> span, ushort value, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _ushortSize);

		if (isBigEndian)
			BinaryPrimitives.WriteUInt16BigEndian(slice, value);
		else
			BinaryPrimitives.WriteUInt16LittleEndian(slice, value);

		position += _ushortSize;
	}

	/// <summary>
	/// Writes a 32-bit signed integer to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="value">The 32-bit signed integer value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to write to; will be advanced by 4.</param>
	public static void WriteInt32(this Span<byte> span, int value, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _intSize);

		if (isBigEndian)
			BinaryPrimitives.WriteInt32BigEndian(slice, value);
		else
			BinaryPrimitives.WriteInt32LittleEndian(slice, value);

		position += _intSize;
	}

	/// <summary>
	/// Writes a 32-bit unsigned integer to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="value">The 32-bit unsigned integer value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to write to; will be advanced by 4.</param>
	[CLSCompliant(false)]
	public static void WriteUInt32(this Span<byte> span, uint value, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _uintSize);

		if (isBigEndian)
			BinaryPrimitives.WriteUInt32BigEndian(slice, value);
		else
			BinaryPrimitives.WriteUInt32LittleEndian(slice, value);

		position += _uintSize;
	}

	/// <summary>
	/// Writes a 64-bit signed integer to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="value">The 64-bit signed integer value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to write to; will be advanced by 8.</param>
	public static void WriteInt64(this Span<byte> span, long value, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _longSize);

		if (isBigEndian)
			BinaryPrimitives.WriteInt64BigEndian(slice, value);
		else
			BinaryPrimitives.WriteInt64LittleEndian(slice, value);

		position += _longSize;
	}

	/// <summary>
	/// Writes a 64-bit unsigned integer to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="value">The 64-bit unsigned integer value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to write to; will be advanced by 8.</param>
	[CLSCompliant(false)]
	public static void WriteUInt64(this Span<byte> span, ulong value, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _ulongSize);

		if (isBigEndian)
			BinaryPrimitives.WriteUInt64BigEndian(slice, value);
		else
			BinaryPrimitives.WriteUInt64LittleEndian(slice, value);

		position += _ulongSize;
	}

	/// <summary>
	/// Writes a Unicode character to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="value">The char value to write.</param>
	/// <param name="position">The position to write to; will be advanced by the size of a char.</param>
	public static void WriteChar(this Span<byte> span, char value, ref int position)
	{
		var slice = span.Slice(position, _charSize);

#if NET5_0_OR_GREATER
		BitConverter.TryWriteBytes(slice, value);
#else
		value.To<byte[]>().CopyTo(slice);
#endif

		position += _charSize;
	}

#if NET5_0_OR_GREATER
	/// <summary>
	/// Writes a half-precision floating-point number to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="value">The Half value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to write to; will be advanced by the size of a Half.</param>
	public static void WriteHalf(this Span<byte> span, Half value, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _halfSize);

		if (isBigEndian)
			BinaryPrimitives.WriteHalfBigEndian(slice, value);
		else
			BinaryPrimitives.WriteHalfLittleEndian(slice, value);

		position += _halfSize;
	}
#endif

	/// <summary>
	/// Writes a single-precision floating-point number to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="value">The float value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to write to; will be advanced by the size of a float.</param>
	public static void WriteSingle(this Span<byte> span, float value, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _floatSize);

#if NET5_0_OR_GREATER
		if (isBigEndian)
			BinaryPrimitives.WriteSingleBigEndian(slice, value);
		else
			BinaryPrimitives.WriteSingleLittleEndian(slice, value);
#else
		var bytes = value.To<byte[]>();

		if (isBigEndian != BitConverter.IsLittleEndian)
			Array.Reverse(bytes);

		bytes.CopyTo(slice);
#endif

		position += _floatSize;
	}

	/// <summary>
	/// Writes a double-precision floating-point number to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="value">The double value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to write to; will be advanced by the size of a double.</param>
	public static void WriteDouble(this Span<byte> span, double value, bool isBigEndian, ref int position)
	{
		var slice = span.Slice(position, _doubleSize);

#if NET5_0_OR_GREATER
		if (isBigEndian)
			BinaryPrimitives.WriteDoubleBigEndian(slice, value);
		else
			BinaryPrimitives.WriteDoubleLittleEndian(slice, value);
#else
		var bytes = value.To<byte[]>();

		if (isBigEndian != BitConverter.IsLittleEndian)
			Array.Reverse(bytes);

		bytes.CopyTo(slice);
#endif

		position += _doubleSize;
	}

	/// <summary>
	/// Writes a GUID to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="value">The GUID value to write.</param>
	/// <param name="position">The position to write to; will be advanced by 16 bytes.</param>
	public static void WriteGuid(this Span<byte> span, Guid value, ref int position)
	{
		var slice = span.Slice(position, 16);

#if NET5_0_OR_GREATER
		value.TryWriteBytes(slice);
#else
		value.To<byte[]>().CopyTo(span);
#endif

		position += 16;
	}

	/// <summary>
	/// Writes a decimal value to the specified span at the current position.
	/// </summary>
	/// <param name="span">The span of bytes.</param>
	/// <param name="value">The decimal value to write.</param>
	/// <param name="isBigEndian">If true, writes in big-endian order; otherwise, little-endian.</param>
	/// <param name="position">The position to write to; will be advanced by 16 bytes.</param>
	public static void WriteDecimal(this Span<byte> span, decimal value, bool isBigEndian, ref int position)
	{
		var bits = decimal.GetBits(value);
		span.WriteInt32(bits[0], isBigEndian, ref position);
		span.WriteInt32(bits[1], isBigEndian, ref position);
		span.WriteInt32(bits[2], isBigEndian, ref position);
		span.WriteInt32(bits[3], isBigEndian, ref position);
	}

	/// <summary>
	/// Writes a structure of type <typeparamref name="T"/> to the specified span at the current position.
	/// </summary>
	/// <typeparam name="T">The structure type to write.</typeparam>
	/// <param name="span">The span of bytes.</param>
	/// <param name="value">The structure value to write.</param>
	/// <param name="size">The structure size in bytes. This parameter is used to determine how many bytes to write.</param>
	/// <param name="position">The position to write to; will be advanced by the size of the structure.</param>
	[CLSCompliant(false)]
	public static void WriteStruct<T>(this Span<byte> span, T value, int size, ref int position)
		where T : struct
	{
		if (size == 0)
			return;

		MemoryMarshal.Write(span.Slice(position, size), ref value);
		position += size;
	}

	/// <summary>
	/// Writes an array of structures of type <typeparamref name="T"/> to the specified span at the current position.
	/// </summary>
	/// <typeparam name="T">The structure type to write.</typeparam>
	/// <param name="span">The span of bytes.</param>
	/// <param name="array">The array of structures to write.</param>
	/// <param name="elementSize">The structure size in bytes. This parameter is used to determine how many bytes to write.</param>
	/// <param name="position">The position to write to; will be advanced by the total size of the array.</param>
	public static void WriteStructArray<T>(this Span<byte> span, T[] array, int elementSize, ref int position)
		where T : struct
	{
		if (array == null)
			throw new ArgumentNullException(nameof(array));

		var totalSize = elementSize * array.Length;

		if (totalSize == 0)
			return;

		var targetSpan = span.Slice(position, totalSize);

		for (var i = 0; i < array.Length; i++)
		{
			MemoryMarshal.Write(targetSpan.Slice(i * elementSize, elementSize), ref array[i]);
		}

		position += totalSize;
	}
}