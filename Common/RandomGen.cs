namespace Ecng.Common;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Runtime.CompilerServices;

/// <summary>
/// Provides methods for generating random values of various types.
/// </summary>
public static class RandomGen
{
	private static readonly SyncObject _sync = new();
	private static readonly Random _value = new((int)DateTime.Now.Ticks);

	/// <summary>
	/// Returns a random double value between 0.0 and 1.0.
	/// </summary>
	/// <returns>A random double.</returns>
	public static double GetDouble()
	{
		lock (_sync)
			return _value.NextDouble();
	}

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
		lock (_sync)
			_value.NextBytes(buffer);
	}

	/// <summary>
	/// Returns a random non-negative integer.
	/// </summary>
	/// <returns>A random integer.</returns>
	public static int GetInt()
	{
		lock (_sync)
			return _value.Next();
	}

	/// <summary>
	/// Returns a random integer between 0 and the specified maximum value (inclusive).
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random integer between 0 and max (inclusive).</returns>
	public static int GetInt(int max)
	{
		lock (_sync)
			return _value.Next(max + 1);
	}

	/// <summary>
	/// Returns a random integer between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random integer between min and max (inclusive).</returns>
	public static int GetInt(int min, int max)
	{
		lock (_sync)
			return _value.Next(min, max + 1);
	}

	/// <summary>
	/// Returns a random long value between long.MinValue and long.MaxValue.
	/// </summary>
	/// <returns>A random long value.</returns>
	public static long GetLong() => GetLong(long.MinValue, long.MaxValue);

	/// <summary>
	/// Returns a random long value between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random long value between min and max (inclusive).</returns>
	public static long GetLong(long min, long max)
	{
		//if (min >= int.MinValue && max <= int.MaxValue)
		//	return GetInt((int)min, (int)max);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int Log2Ceiling(ulong value)
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static int NumberOfSetBits(ulong i)
			{
				i -= (i >> 1) & 0x5555555555555555UL;
				i = (i & 0x3333333333333333UL) + ((i >> 2) & 0x3333333333333333UL);
				return (int)(unchecked(((i + (i >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
			}

			var result = (int)Math.Log(value, 2);

			if (NumberOfSetBits(value) != 1)
				result++;

			return result;
		}

		static ulong GetULong()
		{
			lock (_sync)
			{
				return ((ulong)(uint)_value.Next(1 << 22)) |
				(((ulong)(uint)_value.Next(1 << 22)) << 22) |
				(((ulong)(uint)_value.Next(1 << 20)) << 44);
			}
		}

		var exclusiveRange = (ulong)(max - min);

		if (exclusiveRange > 1)
		{
			// Narrow down to the smallest range [0, 2^bits] that contains maxValue - minValue.
			// Then repeatedly generate a value in that outer range until we get one within the inner range.
			var bits = Log2Ceiling(exclusiveRange);

			while (true)
			{
				var result = GetULong() >> (sizeof(long) * 8 - bits);

				if (result < exclusiveRange)
					return (long)result + min;
			}
		}

		return min + GetInt((int)exclusiveRange);
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
		=> array.ElementAt(GetInt(0, array.Count() - 1));

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
		for (var k = 0; k < 10; k++)
		{
			var i1 = Enumerable.Repeat(9, GetInt(1, integer)).Select(i => GetInt(9).ToString()).Join(string.Empty).To<long>();
			var i2 = Enumerable.Repeat(9, GetInt(1, fractional)).Select(i => GetInt(9).ToString()).Join(string.Empty).To<long>();
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
		var value = GetDouble() * ((double)max - (double)min) + (double)min;
		return (decimal)value.Round(precision);
	}
}