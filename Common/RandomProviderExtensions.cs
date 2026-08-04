namespace Ecng.Common;

/// <summary>
/// The whole random vocabulary, asked of a source that can be handed around.
/// </summary>
/// <remarks>
/// Everything here is built from the four primitives on <see cref="IRandomProvider"/>, so a caller
/// holding a seeded source gets the same values back on a second run. <see cref="RandomGen"/> is
/// this same vocabulary spoken to the shared unseeded source - it forwards here rather than
/// keeping its own copy of the arithmetic, which is how the two used to disagree.
/// </remarks>
public static class RandomProviderExtensions
{
	private static IRandomProvider Check(this IRandomProvider provider)
		=> provider ?? throw new ArgumentNullException(nameof(provider));

	// Sixty-four uniform bits, taken through the primitives so any source can supply them.
	private static ulong Full64(this IRandomProvider provider)
		=> unchecked((ulong)provider.NextLong(long.MinValue, long.MaxValue));

	/// <summary>Returns a value in [0, 1).</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A double.</returns>
	public static double GetDouble(this IRandomProvider provider)
		=> provider.Check().NextDouble();

	/// <summary>Returns a value between zero and the bound.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A double.</returns>
	public static double GetDouble(this IRandomProvider provider, double max)
		=> provider.GetDouble(0d, max);

	/// <summary>Returns a value in the range.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A double.</returns>
	public static double GetDouble(this IRandomProvider provider, double min, double max)
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

		return provider.Check().NextDouble() * range + min;
	}

	/// <summary>Returns a value in [0, 1).</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A float.</returns>
	public static float GetFloat(this IRandomProvider provider)
		=> (float)provider.GetDouble();

	/// <summary>Returns a value between zero and the bound.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A float.</returns>
	public static float GetFloat(this IRandomProvider provider, float max)
		=> provider.GetFloat(0f, max);

	/// <summary>Returns a value in the range.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A float.</returns>
	public static float GetFloat(this IRandomProvider provider, float min, float max)
		=> (float)provider.GetDouble((double)min, max);

	/// <summary>Returns random bytes.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="count">How many.</param>
	/// <returns>The bytes.</returns>
	public static byte[] GetBytes(this IRandomProvider provider, int count)
	{
		var buffer = new byte[count];
		provider.GetBytes(buffer);
		return buffer;
	}

	/// <summary>Fills the buffer with random bytes.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="buffer">Buffer to fill.</param>
	public static void GetBytes(this IRandomProvider provider, byte[] buffer)
		=> provider.Check().NextBytes(buffer);

	/// <summary>Returns a value that is not negative.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>An int.</returns>
	public static int GetInt(this IRandomProvider provider)
		=> provider.GetInt(0, int.MaxValue);

	/// <summary>Returns a value between zero and the bound, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>An int.</returns>
	public static int GetInt(this IRandomProvider provider, int max)
		=> provider.GetInt(0, max);

	/// <summary>Returns a value in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>An int.</returns>
	public static int GetInt(this IRandomProvider provider, int min, int max)
		=> provider.Check().Next(min, max);

	/// <summary>Returns any long.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A long.</returns>
	public static long GetLong(this IRandomProvider provider)
		=> provider.GetLong(long.MinValue, long.MaxValue);

	/// <summary>Returns a value in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A long.</returns>
	public static long GetLong(this IRandomProvider provider, long min, long max)
		=> provider.Check().NextLong(min, max);

	/// <summary>Returns any ulong.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A ulong.</returns>
	[CLSCompliant(false)]
	public static ulong GetULong(this IRandomProvider provider)
		=> provider.GetULong(ulong.MinValue, ulong.MaxValue);

	/// <summary>Returns a value between zero and the bound, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A ulong.</returns>
	[CLSCompliant(false)]
	public static ulong GetULong(this IRandomProvider provider, ulong max)
		=> provider.GetULong(ulong.MinValue, max);

	/// <summary>Returns a value in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A ulong.</returns>
	[CLSCompliant(false)]
	public static ulong GetULong(this IRandomProvider provider, ulong min, ulong max)
	{
		if (min > max)
			throw new ArgumentOutOfRangeException(nameof(min), min, "min > max");

		provider.Check();

		if (min == max)
			return min;

		// The span can be the whole width of the type, which no signed draw covers, so the value
		// comes from the raw bits and is reduced into the span - discarding the tail that does not
		// divide evenly, since the remainder alone would favour the start of the range.
		var range = max - min;

		if (range == ulong.MaxValue)
			return provider.Full64();

		range++;

		var limit = ulong.MaxValue - (ulong.MaxValue % range);

		ulong value;

		do
		{
			value = provider.Full64();
		}
		while (value >= limit);

		return (value % range) + min;
	}

	/// <summary>Returns any uint.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A uint.</returns>
	[CLSCompliant(false)]
	public static uint GetUInt(this IRandomProvider provider)
		=> provider.GetUInt(uint.MinValue, uint.MaxValue);

	/// <summary>Returns a value between zero and the bound, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A uint.</returns>
	[CLSCompliant(false)]
	public static uint GetUInt(this IRandomProvider provider, uint max)
		=> provider.GetUInt(uint.MinValue, max);

	/// <summary>Returns a value in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A uint.</returns>
	[CLSCompliant(false)]
	public static uint GetUInt(this IRandomProvider provider, uint min, uint max)
		=> (uint)provider.GetULong(min, max);

	/// <summary>Returns any short.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A short.</returns>
	public static short GetShort(this IRandomProvider provider)
		=> provider.GetShort(short.MinValue, short.MaxValue);

	/// <summary>Returns a value between zero and the bound, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A short.</returns>
	public static short GetShort(this IRandomProvider provider, short max)
		=> provider.GetShort(0, max);

	/// <summary>Returns a value in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A short.</returns>
	public static short GetShort(this IRandomProvider provider, short min, short max)
		=> (short)provider.GetInt(min, max);

	/// <summary>Returns any ushort.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A ushort.</returns>
	[CLSCompliant(false)]
	public static ushort GetUShort(this IRandomProvider provider)
		=> provider.GetUShort(ushort.MinValue, ushort.MaxValue);

	/// <summary>Returns a value between zero and the bound, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A ushort.</returns>
	[CLSCompliant(false)]
	public static ushort GetUShort(this IRandomProvider provider, ushort max)
		=> provider.GetUShort(ushort.MinValue, max);

	/// <summary>Returns a value in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A ushort.</returns>
	[CLSCompliant(false)]
	public static ushort GetUShort(this IRandomProvider provider, ushort min, ushort max)
		=> (ushort)provider.GetInt(min, max);

	/// <summary>Returns any byte.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A byte.</returns>
	public static byte GetByte(this IRandomProvider provider)
		=> provider.GetByte(byte.MinValue, byte.MaxValue);

	/// <summary>Returns a value between zero and the bound, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A byte.</returns>
	public static byte GetByte(this IRandomProvider provider, byte max)
		=> provider.GetByte(byte.MinValue, max);

	/// <summary>Returns a value in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A byte.</returns>
	public static byte GetByte(this IRandomProvider provider, byte min, byte max)
		=> (byte)provider.GetInt(min, max);

	/// <summary>Returns any sbyte.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>An sbyte.</returns>
	[CLSCompliant(false)]
	public static sbyte GetSByte(this IRandomProvider provider)
		=> provider.GetSByte(sbyte.MinValue, sbyte.MaxValue);

	/// <summary>Returns a value between zero and the bound, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>An sbyte.</returns>
	[CLSCompliant(false)]
	public static sbyte GetSByte(this IRandomProvider provider, sbyte max)
		=> provider.GetSByte(0, max);

	/// <summary>Returns a value in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>An sbyte.</returns>
	[CLSCompliant(false)]
	public static sbyte GetSByte(this IRandomProvider provider, sbyte min, sbyte max)
		=> (sbyte)provider.GetInt(min, max);

	/// <summary>Returns true or false.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A bool.</returns>
	public static bool GetBool(this IRandomProvider provider)
		=> provider.GetInt(1) == 1;

	/// <summary>Returns one of the values of an enum.</summary>
	/// <typeparam name="T">Enum type.</typeparam>
	/// <param name="provider">Source.</param>
	/// <returns>A value of <typeparamref name="T"/>.</returns>
	public static T GetEnum<T>(this IRandomProvider provider)
		where T : struct
		=> provider.GetEnum(Enumerator.GetValues<T>());

	/// <summary>Returns one of the values of an enum.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="enumType">Enum type.</param>
	/// <returns>A value of <paramref name="enumType"/>.</returns>
	public static object GetEnum(this IRandomProvider provider, Type enumType)
		=> provider.GetElement(Enumerator.GetValues(enumType));

	/// <summary>Returns one of the given enum values.</summary>
	/// <typeparam name="T">Enum type.</typeparam>
	/// <param name="provider">Source.</param>
	/// <param name="values">The values to choose from.</param>
	/// <returns>One of <paramref name="values"/>.</returns>
	public static T GetEnum<T>(this IRandomProvider provider, IEnumerable<T> values)
		where T : struct
	{
		if (values is null)
			throw new ArgumentNullException(nameof(values));

		var arr = values as T[] ?? values.ToArray();

		if (arr.Length == 0)
			throw new InvalidOperationException("No values to choose from.");

		return arr[provider.GetInt(0, arr.Length - 1)];
	}

	/// <summary>Returns an enum value in the range, both ends included.</summary>
	/// <typeparam name="T">Enum type.</typeparam>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A value of <typeparamref name="T"/>.</returns>
	public static T GetEnum<T>(this IRandomProvider provider, T min, T max)
		where T : struct
		=> provider.GetLong(min.To<long>(), max.To<long>()).To<T>();

	/// <summary>Returns one of the elements.</summary>
	/// <typeparam name="T">Element type.</typeparam>
	/// <param name="provider">Source.</param>
	/// <param name="values">The elements to choose from.</param>
	/// <returns>One of <paramref name="values"/>.</returns>
	public static T GetElement<T>(this IRandomProvider provider, IList<T> values)
	{
		if (values is null)
			throw new ArgumentNullException(nameof(values));

		if (values.Count == 0)
			throw new ArgumentOutOfRangeException(nameof(values), "There is nothing to choose from.");

		return values[provider.GetInt(0, values.Count - 1)];
	}

	/// <summary>Returns one of the elements.</summary>
	/// <typeparam name="T">Element type.</typeparam>
	/// <param name="provider">Source.</param>
	/// <param name="values">The elements to choose from.</param>
	/// <returns>One of <paramref name="values"/>.</returns>
	public static T GetElement<T>(this IRandomProvider provider, IEnumerable<T> values)
		=> provider.GetElement(values as IList<T> ?? [.. values]);

	/// <summary>Returns a string of random characters.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Shortest length.</param>
	/// <param name="max">Longest length.</param>
	/// <returns>A string.</returns>
	public static string GetString(this IRandomProvider provider, int min, int max)
		=> TypeHelper.GenerateSalt(provider.GetInt(min, max)).Base64();

	/// <summary>Returns any moment.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A <see cref="DateTime"/>.</returns>
	public static DateTime GetDate(this IRandomProvider provider)
		=> provider.GetDate(DateTime.MinValue, DateTime.MaxValue);

	/// <summary>Returns a moment in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A <see cref="DateTime"/>.</returns>
	public static DateTime GetDate(this IRandomProvider provider, DateTime min, DateTime max)
		=> min + provider.GetTime(default, max - min);

	/// <summary>Returns any span.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A <see cref="TimeSpan"/>.</returns>
	public static TimeSpan GetTime(this IRandomProvider provider)
		=> provider.GetTime(TimeSpan.MinValue, TimeSpan.MaxValue);

	/// <summary>Returns a span in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A <see cref="TimeSpan"/>.</returns>
	public static TimeSpan GetTime(this IRandomProvider provider, TimeSpan min, TimeSpan max)
		=> provider.GetLong(min.Ticks, max.Ticks).To<TimeSpan>();

	/// <summary>Returns a value in the range, rounded to the given number of decimals.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <param name="precision">Decimal places to keep.</param>
	/// <returns>A decimal.</returns>
	public static decimal GetDecimal(this IRandomProvider provider, decimal min, decimal max, int precision)
	{
		var value = provider.GetDouble((double)min, (double)max);
		return (decimal)value.Round(precision);
	}

	/// <summary>
	/// Returns a value with the stated number of digits either side of the point, never zero.
	/// </summary>
	/// <param name="provider">Source.</param>
	/// <param name="integer">Most digits before the point.</param>
	/// <param name="fractional">Most digits after it.</param>
	/// <returns>A decimal.</returns>
	public static decimal GetDecimal(this IRandomProvider provider, int integer = 8, int fractional = 8)
	{
		if (integer < 1 || integer > 28)
			throw new ArgumentOutOfRangeException(nameof(integer), integer, "Must be in range 1..28");

		if (fractional < 0 || fractional > 28)
			throw new ArgumentOutOfRangeException(nameof(fractional), fractional, "Must be in range 0..28");

		provider.Check();

		var fractionalDigits = fractional == 0 ? 0 : provider.GetInt(0, fractional.Min(27));
		var integerDigits = provider.GetInt(1, integer.Min(28 - fractionalDigits));

		string CreateDigits(int count, bool allowLeadingZero)
		{
			if (count == 0)
				return "0";

			var builder = new StringBuilder(count);
			builder.Append((char)('0' + (allowLeadingZero ? provider.GetInt(9) : provider.GetInt(1, 9))));

			for (var i = 1; i < count; i++)
				builder.Append((char)('0' + provider.GetInt(9)));

			return builder.ToString();
		}

		var integerPart = CreateDigits(integerDigits, false);
		var fractionalPart = CreateDigits(fractionalDigits, true);

		return decimal.Parse($"{integerPart}.{fractionalPart}", CultureInfo.InvariantCulture);
	}
}
