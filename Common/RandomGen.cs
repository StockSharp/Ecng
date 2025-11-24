namespace Ecng.Common;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
#if !NET6_0_OR_GREATER
using System.Threading;
#endif

/// <summary>
/// Provides methods for generating random values of various types.
/// </summary>
public static class RandomGen
{
#if NET6_0_OR_GREATER
	private static Random Random => Random.Shared;
#else
	[ThreadStatic]
	private static Random _threadRandom;
	private static long _globalSeed = DateTime.UtcNow.Ticks;

	private static Random CreateRandom()
		=> new((int)(Interlocked.Increment(ref _globalSeed) ^ Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId ^ (DateTime.UtcNow.Ticks >> 32)));

	private static Random Random => _threadRandom ??= CreateRandom();
#endif

	private static ulong NextUInt64()
	{
		var rng = Random;
		return ((ulong)(uint)rng.Next(1 << 22)) |
			(((ulong)(uint)rng.Next(1 << 22)) << 22) |
			(((ulong)(uint)rng.Next(1 << 20)) << 44);
	}

	/// <summary>
	/// Returns a random double value between 0.0 and 1.0.
	/// </summary>
	/// <returns>A random double.</returns>
	public static double GetDouble()
		=> Random.NextDouble();

	/// <summary>
	/// Returns a random double value between 0.0 and the specified maximum value.
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random double between 0.0 and max.</returns>
	public static double GetDouble(double max)
		=> GetDouble(0.0, max);

	/// <summary>
	/// Returns a random double value between the specified minimum and maximum values.
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random double between min and max.</returns>
	public static double GetDouble(double min, double max)
	{
		if (double.IsNaN(min))
			throw new ArgumentOutOfRangeException(nameof(min), min, "Value must be a number.");

		if (double.IsNaN(max))
			throw new ArgumentOutOfRangeException(nameof(max), max, "Value must be a number.");

		if (double.IsInfinity(min))
			throw new ArgumentOutOfRangeException(nameof(min), min, "Values must be finite.");

		if (double.IsInfinity(max))
			throw new ArgumentOutOfRangeException(nameof(max), max, "Values must be finite.");

		if (min > max)
			throw new ArgumentOutOfRangeException(nameof(min), min, "min > max");

		if (min == max)
			return min;

		var range = max - min;

		if (double.IsInfinity(range))
			throw new ArgumentOutOfRangeException(nameof(max), max, "Range is too large.");

		return Random.NextDouble() * range + min;
	}

	/// <summary>
	/// Returns a random single-precision floating-point number between 0.0 and 1.0.
	/// </summary>
	/// <returns>A random float.</returns>
	public static float GetFloat()
		=> (float)GetDouble();

	/// <summary>
	/// Returns a random single-precision floating-point number between 0.0 and the specified maximum value.
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random float between 0.0 and max.</returns>
	public static float GetFloat(float max)
		=> GetFloat(0f, max);

	/// <summary>
	/// Returns a random single-precision floating-point number between the specified minimum and maximum values.
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random float between min and max.</returns>
	public static float GetFloat(float min, float max)
		=> (float)GetDouble((double)min, (double)max);

	/// <summary>
	/// Returns an array of random bytes with the specified count.
	/// </summary>
	/// <param name="count">The number of random bytes to generate.</param>
	/// <returns>An array of random bytes.</returns>
	public static byte[] GetBytes(int count)
	{
		var buffer = new byte[count];
		GetBytes(buffer);
		return buffer;
	}

	/// <summary>
	/// Fills the provided array with random bytes.
	/// </summary>
	/// <param name="buffer">The array to fill with random bytes.</param>
	public static void GetBytes(byte[] buffer)
	{
		Random.NextBytes(buffer);
	}

	/// <summary>
	/// Returns a random non-negative integer.
	/// </summary>
	/// <returns>A random integer.</returns>
	public static int GetInt()
		=> Random.Next();

	/// <summary>
	/// Returns a random integer between 0 and the specified maximum value (inclusive).
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random integer between 0 and max (inclusive).</returns>
	public static int GetInt(int max)
		=> GetInt(0, max);

	/// <summary>
	/// Returns a random integer between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random integer between min and max (inclusive).</returns>
	public static int GetInt(int min, int max)
	{
		if (min > max)
			throw new ArgumentOutOfRangeException(nameof(min), min, "min > max");

		if (max < int.MaxValue)
			max++;

		return Random.Next(min, max);
	}

	/// <summary>
	/// Returns a random unsigned integer between 0 and uint.MaxValue.
	/// </summary>
	/// <returns>A random unsigned integer.</returns>
	[CLSCompliant(false)]
	public static uint GetUInt()
		=> GetUInt(uint.MinValue, uint.MaxValue);

	/// <summary>
	/// Returns a random unsigned integer between 0 and the specified maximum value (inclusive).
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random unsigned integer between 0 and max.</returns>
	[CLSCompliant(false)]
	public static uint GetUInt(uint max)
		=> GetUInt(0u, max);

	/// <summary>
	/// Returns a random unsigned integer between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random unsigned integer between min and max.</returns>
	[CLSCompliant(false)]
	public static uint GetUInt(uint min, uint max)
		=> (uint)GetULong(min, max);

	/// <summary>
	/// Returns a random unsigned long value between 0 and ulong.MaxValue.
	/// </summary>
	/// <returns>A random unsigned long value.</returns>
	[CLSCompliant(false)]
	public static ulong GetULong()
		=> GetULong(ulong.MinValue, ulong.MaxValue);

	/// <summary>
	/// Returns a random unsigned long value between 0 and the specified maximum value (inclusive).
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random unsigned long value between 0 and max.</returns>
	[CLSCompliant(false)]
	public static ulong GetULong(ulong max)
		=> GetULong(0UL, max);

	/// <summary>
	/// Returns a random unsigned long value between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random unsigned long value between min and max.</returns>
	[CLSCompliant(false)]
	public static ulong GetULong(ulong min, ulong max)
	{
		if (min > max)
			throw new ArgumentOutOfRangeException(nameof(min), min, "min > max");

		if (min == max)
			return min;

		var range = max - min;

		if (range == ulong.MaxValue)
			return NextUInt64();

		range++;

		var limit = ulong.MaxValue - (ulong.MaxValue % range);

		ulong value;

		do
		{
			value = NextUInt64();
		}
		while (value >= limit);

		return (value % range) + min;
	}

	/// <summary>
	/// Returns a random 16-bit signed integer between short.MinValue and short.MaxValue.
	/// </summary>
	/// <returns>A random short value.</returns>
	public static short GetShort()
		=> GetShort(short.MinValue, short.MaxValue);

	/// <summary>
	/// Returns a random 16-bit signed integer between 0 and the specified maximum value (inclusive).
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random short value between 0 and max.</returns>
	public static short GetShort(short max)
		=> GetShort(0, max);

	/// <summary>
	/// Returns a random 16-bit signed integer between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random short value between min and max.</returns>
	public static short GetShort(short min, short max)
		=> (short)GetInt(min, max);

	/// <summary>
	/// Returns a random 16-bit unsigned integer between 0 and ushort.MaxValue.
	/// </summary>
	/// <returns>A random unsigned short value.</returns>
	[CLSCompliant(false)]
	public static ushort GetUShort()
		=> GetUShort(ushort.MinValue, ushort.MaxValue);

	/// <summary>
	/// Returns a random 16-bit unsigned integer between 0 and the specified maximum value (inclusive).
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random unsigned short value between 0 and max.</returns>
	[CLSCompliant(false)]
	public static ushort GetUShort(ushort max)
		=> GetUShort(ushort.MinValue, max);

	/// <summary>
	/// Returns a random 16-bit unsigned integer between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random unsigned short value between min and max.</returns>
	[CLSCompliant(false)]
	public static ushort GetUShort(ushort min, ushort max)
		=> (ushort)GetInt(min, max);

	/// <summary>
	/// Returns a random byte between 0 and byte.MaxValue.
	/// </summary>
	/// <returns>A random byte.</returns>
	public static byte GetByte()
		=> GetByte(byte.MinValue, byte.MaxValue);

	/// <summary>
	/// Returns a random byte between 0 and the specified maximum value (inclusive).
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random byte between 0 and max.</returns>
	public static byte GetByte(byte max)
		=> GetByte(byte.MinValue, max);

	/// <summary>
	/// Returns a random byte between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random byte between min and max.</returns>
	public static byte GetByte(byte min, byte max)
		=> (byte)GetInt(min, max);

	/// <summary>
	/// Returns a random signed byte between sbyte.MinValue and sbyte.MaxValue.
	/// </summary>
	/// <returns>A random signed byte.</returns>
	[CLSCompliant(false)]
	public static sbyte GetSByte()
		=> GetSByte(sbyte.MinValue, sbyte.MaxValue);

	/// <summary>
	/// Returns a random signed byte between 0 and the specified maximum value (inclusive).
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random signed byte between 0 and max.</returns>
	[CLSCompliant(false)]
	public static sbyte GetSByte(sbyte max)
		=> GetSByte(0, max);

	/// <summary>
	/// Returns a random signed byte between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random signed byte between min and max.</returns>
	[CLSCompliant(false)]
	public static sbyte GetSByte(sbyte min, sbyte max)
		=> (sbyte)GetInt(min, max);

	/// <summary>
	/// Returns a random long value between long.MinValue and long.MaxValue.
	/// </summary>
	/// <returns>A random long value.</returns>
	public static long GetLong()
		=> GetLong(long.MinValue, long.MaxValue);

	/// <summary>
	/// Returns a random long value between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random long value between min and max (inclusive).</returns>
	public static long GetLong(long min, long max)
	{
		if (min > max)
			throw new ArgumentOutOfRangeException(nameof(min), min, "min > max");

		if (min == max)
			return min;

		var range = (ulong)(max - min);

		if (range == ulong.MaxValue)
			return unchecked((long)NextUInt64());

		range++;

		var limit = ulong.MaxValue - (ulong.MaxValue % range);

		ulong value;

		do
		{
			value = NextUInt64();
		}
		while (value >= limit);

		return (long)(value % range) + min;
	}

	/// <summary>
	/// Returns a random boolean value.
	/// </summary>
	/// <returns>A random boolean.</returns>
	public static bool GetBool() => GetInt(1) == 1;

	/// <summary>
	/// Returns a random enum value of type T.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <returns>A random enum value.</returns>
	public static T GetEnum<T>()
		where T : struct
		=> GetEnum(Enumerator.GetValues<T>());

	/// <summary>
	/// Returns a random enum value from the specified enum type.
	/// </summary>
	/// <param name="enumType">The enum type.</param>
	/// <returns>A random enum value.</returns>
	public static object GetEnum(Type enumType)
		=> GetElement(Enumerator.GetValues(enumType));

	/// <summary>
	/// Returns a random enum value from the specified collection of values.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <param name="values">A collection of enum values.</param>
	/// <returns>A random enum value from the collection.</returns>
	public static T GetEnum<T>(IEnumerable<T> values)
		where T : struct
		=> GetEnum(default, values.Max(value => value.To<long>()).To<T>());

	/// <summary>
	/// Returns a random enum value between the specified minimum and maximum enum values.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <param name="min">The minimum enum value.</param>
	/// <param name="max">The maximum enum value.</param>
	/// <returns>A random enum value between min and max.</returns>
	public static T GetEnum<T>(T min, T max)
		where T : struct
		=> GetLong(min.To<long>(), max.To<long>()).To<T>();

	/// <summary>
	/// Returns a random element from the specified collection.
	/// </summary>
	/// <typeparam name="T">The type of elements in the collection.</typeparam>
	/// <param name="array">The collection of elements.</param>
	/// <returns>A random element from the collection.</returns>
	public static T GetElement<T>(IEnumerable<T> array)
	{
		var tmp = array.ToArray();
		return tmp[GetInt(0, tmp.Length - 1)];
	}

	/// <summary>
	/// Returns a random Base64 encoded string generated from a random salt.
	/// </summary>
	/// <param name="min">The minimum length for generating the salt.</param>
	/// <param name="max">The maximum length for generating the salt.</param>
	/// <returns>A random Base64 encoded string.</returns>
	public static string GetString(int min, int max)
		=> TypeHelper.GenerateSalt(GetInt(min, max)).Base64();

	/// <summary>
	/// Returns a random DateTime value between DateTime.MinValue and DateTime.MaxValue.
	/// </summary>
	/// <returns>A random DateTime.</returns>
	public static DateTime GetDate()
		=> GetDate(DateTime.MinValue, DateTime.MaxValue);

	/// <summary>
	/// Returns a random DateTime value between the specified minimum and maximum values.
	/// </summary>
	/// <param name="min">The minimum DateTime value.</param>
	/// <param name="max">The maximum DateTime value.</param>
	/// <returns>A random DateTime between min and max.</returns>
	public static DateTime GetDate(DateTime min, DateTime max)
		=> min + GetTime(default, max - min);

	/// <summary>
	/// Returns a random TimeSpan value between TimeSpan.MinValue and TimeSpan.MaxValue.
	/// </summary>
	/// <returns>A random TimeSpan.</returns>
	public static TimeSpan GetTime()
		=> GetTime(TimeSpan.MinValue, TimeSpan.MaxValue);

	/// <summary>
	/// Returns a random TimeSpan value between the specified minimum and maximum values.
	/// </summary>
	/// <param name="min">The minimum TimeSpan value.</param>
	/// <param name="max">The maximum TimeSpan value.</param>
	/// <returns>A random TimeSpan between min and max.</returns>
	public static TimeSpan GetTime(TimeSpan min, TimeSpan max)
		=> GetLong(min.Ticks, max.Ticks).To<TimeSpan>();

	/// <summary>
	/// Returns a random non-zero decimal value with a specified number of integer and fractional digits.
	/// Tries up to 10 times to generate a valid number.
	/// </summary>
	/// <param name="integer">The maximum number of digits in the integer part. Default is 8.</param>
	/// <param name="fractional">The maximum number of digits in the fractional part. Default is 8.</param>
	/// <returns>A random decimal value.</returns>
	/// <exception cref="InvalidOperationException">Thrown when a valid decimal value cannot be generated in 10 attempts.</exception>
	public static decimal GetDecimal(int integer = 8, int fractional = 8)
	{
		if (integer < 1 || integer > 28)
			throw new ArgumentOutOfRangeException(nameof(integer), integer, "Must be in range 1..28");

		if (fractional < 0 || fractional > 28)
			throw new ArgumentOutOfRangeException(nameof(fractional), fractional, "Must be in range 0..28");

		for (var k = 0; k < 10; k++)
		{
			var i1 = Enumerable.Repeat(9, GetInt(1, integer)).Select(i => GetInt(9).ToString()).Join(string.Empty).To<long>();
			var i2 = fractional == 0 ? 0 : Enumerable.Repeat(9, GetInt(1, fractional)).Select(i => GetInt(9).ToString()).Join(string.Empty).To<long>();
			var value = decimal.Parse(i1 + "." + i2, CultureInfo.InvariantCulture);

			if (value != 0)
				return value;
		}

		throw new InvalidOperationException();
	}

	/// <summary>
	/// Returns a random decimal value between the specified minimum and maximum values with the given precision.
	/// </summary>
	/// <param name="min">The minimum decimal value.</param>
	/// <param name="max">The maximum decimal value.</param>
	/// <param name="precision">The number of decimal places to round the value to.</param>
	/// <returns>A random decimal value between min and max rounded to the specified precision.</returns>
	public static decimal GetDecimal(decimal min, decimal max, int precision)
	{
		var value = GetDouble((double)min, (double)max);
		return (decimal)value.Round(precision);
	}
}