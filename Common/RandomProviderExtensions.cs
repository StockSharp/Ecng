namespace Ecng.Common;

/// <summary>
/// The operations <see cref="RandomGen"/> offers, asked of a source that can be handed around.
/// </summary>
/// <remarks>
/// Everything here is built from the four primitives on <see cref="IRandomProvider"/>, so a caller
/// that holds a seeded source gets the whole vocabulary and the same series back. The static
/// <see cref="RandomGen"/> remains for callers with nothing to reproduce.
/// </remarks>
public static class RandomProviderExtensions
{
	private static IRandomProvider Check(this IRandomProvider provider)
		=> provider ?? throw new ArgumentNullException(nameof(provider));

	/// <summary>Returns a value in [0, 1).</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A double.</returns>
	public static double GetDouble(this IRandomProvider provider)
		=> provider.Check().NextDouble();

	/// <summary>Returns a value in the range.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A double.</returns>
	public static double GetDouble(this IRandomProvider provider, double min, double max)
		=> min + provider.Check().NextDouble() * (max - min);

	/// <summary>Returns a value in [0, 1).</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A float.</returns>
	public static float GetFloat(this IRandomProvider provider)
		=> (float)provider.Check().NextDouble();

	/// <summary>Returns a value in the range.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A float.</returns>
	public static float GetFloat(this IRandomProvider provider, float min, float max)
		=> (float)provider.GetDouble(min, max);

	/// <summary>Returns a value in the range, rounded to the given number of decimals.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <param name="scale">Decimal places to keep.</param>
	/// <returns>A decimal.</returns>
	public static decimal GetDecimal(this IRandomProvider provider, decimal min, decimal max, int scale)
		=> Math.Round(min + (decimal)provider.Check().NextDouble() * (max - min), scale);

	/// <summary>Returns any int.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>An int.</returns>
	public static int GetInt(this IRandomProvider provider)
		=> provider.Check().Next(int.MinValue, int.MaxValue);

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
		=> provider.Check().NextLong(long.MinValue, long.MaxValue);

	/// <summary>Returns a value in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A long.</returns>
	public static long GetLong(this IRandomProvider provider, long min, long max)
		=> provider.Check().NextLong(min, max);

	/// <summary>Returns any short.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A short.</returns>
	public static short GetShort(this IRandomProvider provider)
		=> (short)provider.Check().Next(short.MinValue, short.MaxValue);

	/// <summary>Returns a value in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A short.</returns>
	public static short GetShort(this IRandomProvider provider, short min, short max)
		=> (short)provider.Check().Next(min, max);

	/// <summary>Returns any ushort.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A ushort.</returns>
	public static ushort GetUShort(this IRandomProvider provider)
		=> (ushort)provider.Check().Next(ushort.MinValue, ushort.MaxValue);

	/// <summary>Returns a value in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A ushort.</returns>
	public static ushort GetUShort(this IRandomProvider provider, ushort min, ushort max)
		=> (ushort)provider.Check().Next(min, max);

	/// <summary>Returns any sbyte.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>An sbyte.</returns>
	public static sbyte GetSByte(this IRandomProvider provider)
		=> (sbyte)provider.Check().Next(sbyte.MinValue, sbyte.MaxValue);

	/// <summary>Returns a value in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>An sbyte.</returns>
	public static sbyte GetSByte(this IRandomProvider provider, sbyte min, sbyte max)
		=> (sbyte)provider.Check().Next(min, max);

	/// <summary>Returns any byte.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A byte.</returns>
	public static byte GetByte(this IRandomProvider provider)
		=> (byte)provider.Check().Next(byte.MinValue, byte.MaxValue);

	/// <summary>Returns a value in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A byte.</returns>
	public static byte GetByte(this IRandomProvider provider, byte min, byte max)
		=> (byte)provider.Check().Next(min, max);

	/// <summary>Returns any uint.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A uint.</returns>
	public static uint GetUInt(this IRandomProvider provider)
		=> (uint)provider.Check().NextLong(uint.MinValue, uint.MaxValue);

	/// <summary>Returns a value in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A uint.</returns>
	public static uint GetUInt(this IRandomProvider provider, uint min, uint max)
		=> (uint)provider.Check().NextLong(min, max);

	/// <summary>Returns any ulong.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A ulong.</returns>
	public static ulong GetULong(this IRandomProvider provider)
		=> provider.GetULong(ulong.MinValue, ulong.MaxValue);

	/// <summary>Returns a value in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A ulong.</returns>
	public static ulong GetULong(this IRandomProvider provider, ulong min, ulong max)
	{
		if (min > max)
			throw new ArgumentOutOfRangeException(nameof(min), min, "The lower bound is above the upper one.");

		// The whole ulong range does not fit a long, so the span is walked as a fraction of itself
		// rather than by asking for a signed value in it.
		var span = max - min;
		return min + (ulong)(provider.Check().NextDouble() * span);
	}

	/// <summary>Returns true or false.</summary>
	/// <param name="provider">Source.</param>
	/// <returns>A bool.</returns>
	public static bool GetBool(this IRandomProvider provider)
		=> provider.Check().Next(0, 1) == 1;

	/// <summary>Returns one of the values of an enum.</summary>
	/// <typeparam name="T">Enum type.</typeparam>
	/// <param name="provider">Source.</param>
	/// <returns>A value of <typeparamref name="T"/>.</returns>
	public static T GetEnum<T>(this IRandomProvider provider)
		where T : struct
	{
		var values = Enum.GetValues(typeof(T));
		return (T)values.GetValue(provider.Check().Next(0, values.Length - 1));
	}

	/// <summary>Returns random bytes.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="count">How many.</param>
	/// <returns>The bytes.</returns>
	public static byte[] GetBytes(this IRandomProvider provider, int count)
	{
		var buffer = new byte[count];
		provider.Check().NextBytes(buffer);
		return buffer;
	}

	/// <summary>Returns a span in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A <see cref="TimeSpan"/>.</returns>
	public static TimeSpan GetTime(this IRandomProvider provider, TimeSpan min, TimeSpan max)
		=> provider.GetLong(min.Ticks, max.Ticks).To<TimeSpan>();

	/// <summary>Returns a moment in the range, both ends included.</summary>
	/// <param name="provider">Source.</param>
	/// <param name="min">Lower bound.</param>
	/// <param name="max">Upper bound.</param>
	/// <returns>A <see cref="DateTime"/>.</returns>
	public static DateTime GetDate(this IRandomProvider provider, DateTime min, DateTime max)
		=> min + provider.GetTime(default, max - min);

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

		return values[provider.Check().Next(0, values.Count - 1)];
	}
}
