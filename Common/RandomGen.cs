namespace Ecng.Common;

/// <summary>
/// Provides methods for generating random values of various types.
/// </summary>
public static class RandomGen
{
	// The vocabulary lives on IRandomProvider now; this type is that vocabulary spoken to the
	// source nobody holds the seed of. Keeping a second copy of the arithmetic here is what let
	// the two drift apart - one reaching int.MaxValue and the other not, one inclusive at the top
	// of a ulong range and the other a value short.
	private static IRandomProvider Provider => DefaultRandomProvider.Instance;

	/// <summary>
	/// Returns a random double value between 0.0 and 1.0.
	/// </summary>
	/// <returns>A random double.</returns>
	public static double GetDouble()
		=> Provider.GetDouble();

	/// <summary>
	/// Returns a random double value between 0.0 and the specified maximum value.
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random double between 0.0 and max.</returns>
	public static double GetDouble(double max)
		=> Provider.GetDouble(max);

	/// <summary>
	/// Returns a random double value between the specified minimum and maximum values.
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random double between min and max.</returns>
	public static double GetDouble(double min, double max)
		=> Provider.GetDouble(min, max);

	/// <summary>
	/// Returns a random single-precision floating-point number between 0.0 and 1.0.
	/// </summary>
	/// <returns>A random float.</returns>
	public static float GetFloat()
		=> Provider.GetFloat();

	/// <summary>
	/// Returns a random single-precision floating-point number between 0.0 and the specified maximum value.
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random float between 0.0 and max.</returns>
	public static float GetFloat(float max)
		=> Provider.GetFloat(max);

	/// <summary>
	/// Returns a random single-precision floating-point number between the specified minimum and maximum values.
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random float between min and max.</returns>
	public static float GetFloat(float min, float max)
		=> Provider.GetFloat(min, max);

	/// <summary>
	/// Returns an array of random bytes with the specified count.
	/// </summary>
	/// <param name="count">The number of random bytes to generate.</param>
	/// <returns>An array of random bytes.</returns>
	public static byte[] GetBytes(int count)
		=> Provider.GetBytes(count);

	/// <summary>
	/// Fills the provided array with random bytes.
	/// </summary>
	/// <param name="buffer">The array to fill with random bytes.</param>
	public static void GetBytes(byte[] buffer)
		=> Provider.GetBytes(buffer);

	/// <summary>
	/// Returns a random non-negative integer.
	/// </summary>
	/// <returns>A random integer.</returns>
	public static int GetInt()
		=> Provider.GetInt();

	/// <summary>
	/// Returns a random integer between 0 and the specified maximum value (inclusive).
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random integer between 0 and max (inclusive).</returns>
	public static int GetInt(int max)
		=> Provider.GetInt(max);

	/// <summary>
	/// Returns a random integer between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random integer between min and max (inclusive).</returns>
	public static int GetInt(int min, int max)
		=> Provider.GetInt(min, max);

	/// <summary>
	/// Returns a random unsigned integer between 0 and uint.MaxValue.
	/// </summary>
	/// <returns>A random unsigned integer.</returns>
	[CLSCompliant(false)]
	public static uint GetUInt()
		=> Provider.GetUInt();

	/// <summary>
	/// Returns a random unsigned integer between 0 and the specified maximum value (inclusive).
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random unsigned integer between 0 and max.</returns>
	[CLSCompliant(false)]
	public static uint GetUInt(uint max)
		=> Provider.GetUInt(max);

	/// <summary>
	/// Returns a random unsigned integer between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random unsigned integer between min and max.</returns>
	[CLSCompliant(false)]
	public static uint GetUInt(uint min, uint max)
		=> Provider.GetUInt(min, max);

	/// <summary>
	/// Returns a random unsigned long value between 0 and ulong.MaxValue.
	/// </summary>
	/// <returns>A random unsigned long value.</returns>
	[CLSCompliant(false)]
	public static ulong GetULong()
		=> Provider.GetULong();

	/// <summary>
	/// Returns a random unsigned long value between 0 and the specified maximum value (inclusive).
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random unsigned long value between 0 and max.</returns>
	[CLSCompliant(false)]
	public static ulong GetULong(ulong max)
		=> Provider.GetULong(max);

	/// <summary>
	/// Returns a random unsigned long value between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random unsigned long value between min and max.</returns>
	[CLSCompliant(false)]
	public static ulong GetULong(ulong min, ulong max)
		=> Provider.GetULong(min, max);

	/// <summary>
	/// Returns a random 16-bit signed integer between short.MinValue and short.MaxValue.
	/// </summary>
	/// <returns>A random short value.</returns>
	public static short GetShort()
		=> Provider.GetShort();

	/// <summary>
	/// Returns a random 16-bit signed integer between 0 and the specified maximum value (inclusive).
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random short value between 0 and max.</returns>
	public static short GetShort(short max)
		=> Provider.GetShort(max);

	/// <summary>
	/// Returns a random 16-bit signed integer between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random short value between min and max.</returns>
	public static short GetShort(short min, short max)
		=> Provider.GetShort(min, max);

	/// <summary>
	/// Returns a random 16-bit unsigned integer between 0 and ushort.MaxValue.
	/// </summary>
	/// <returns>A random unsigned short value.</returns>
	[CLSCompliant(false)]
	public static ushort GetUShort()
		=> Provider.GetUShort();

	/// <summary>
	/// Returns a random 16-bit unsigned integer between 0 and the specified maximum value (inclusive).
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random unsigned short value between 0 and max.</returns>
	[CLSCompliant(false)]
	public static ushort GetUShort(ushort max)
		=> Provider.GetUShort(max);

	/// <summary>
	/// Returns a random 16-bit unsigned integer between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random unsigned short value between min and max.</returns>
	[CLSCompliant(false)]
	public static ushort GetUShort(ushort min, ushort max)
		=> Provider.GetUShort(min, max);

	/// <summary>
	/// Returns a random byte between 0 and byte.MaxValue.
	/// </summary>
	/// <returns>A random byte.</returns>
	public static byte GetByte()
		=> Provider.GetByte();

	/// <summary>
	/// Returns a random byte between 0 and the specified maximum value (inclusive).
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random byte between 0 and max.</returns>
	public static byte GetByte(byte max)
		=> Provider.GetByte(max);

	/// <summary>
	/// Returns a random byte between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random byte between min and max.</returns>
	public static byte GetByte(byte min, byte max)
		=> Provider.GetByte(min, max);

	/// <summary>
	/// Returns a random signed byte between sbyte.MinValue and sbyte.MaxValue.
	/// </summary>
	/// <returns>A random signed byte.</returns>
	[CLSCompliant(false)]
	public static sbyte GetSByte()
		=> Provider.GetSByte();

	/// <summary>
	/// Returns a random signed byte between 0 and the specified maximum value (inclusive).
	/// </summary>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random signed byte between 0 and max.</returns>
	[CLSCompliant(false)]
	public static sbyte GetSByte(sbyte max)
		=> Provider.GetSByte(max);

	/// <summary>
	/// Returns a random signed byte between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random signed byte between min and max.</returns>
	[CLSCompliant(false)]
	public static sbyte GetSByte(sbyte min, sbyte max)
		=> Provider.GetSByte(min, max);

	/// <summary>
	/// Returns a random long value between long.MinValue and long.MaxValue.
	/// </summary>
	/// <returns>A random long value.</returns>
	public static long GetLong()
		=> Provider.GetLong();

	/// <summary>
	/// Returns a random long value between the specified minimum and maximum values (inclusive).
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <returns>A random long value between min and max (inclusive).</returns>
	public static long GetLong(long min, long max)
		=> Provider.GetLong(min, max);

	/// <summary>
	/// Returns a random boolean value.
	/// </summary>
	/// <returns>A random boolean.</returns>
	public static bool GetBool()
		=> Provider.GetBool();

	/// <summary>
	/// Returns a random enum value of type T.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <returns>A random enum value.</returns>
	public static T GetEnum<T>()
		where T : struct
		=> Provider.GetEnum<T>();

	/// <summary>
	/// Returns a random enum value from the specified enum type.
	/// </summary>
	/// <param name="enumType">The enum type.</param>
	/// <returns>A random enum value.</returns>
	public static object GetEnum(Type enumType)
		=> Provider.GetEnum(enumType);

	/// <summary>
	/// Returns a random enum value from the specified collection of values.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <param name="values">A collection of enum values.</param>
	/// <returns>A random enum value from the collection.</returns>
	public static T GetEnum<T>(IEnumerable<T> values)
		where T : struct
		=> Provider.GetEnum(values);

	/// <summary>
	/// Returns a random enum value between the specified minimum and maximum enum values.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <param name="min">The minimum enum value.</param>
	/// <param name="max">The maximum enum value.</param>
	/// <returns>A random enum value between min and max.</returns>
	public static T GetEnum<T>(T min, T max)
		where T : struct
		=> Provider.GetEnum(min, max);

	/// <summary>
	/// Returns a random element from the specified collection.
	/// </summary>
	/// <typeparam name="T">The type of elements in the collection.</typeparam>
	/// <param name="array">The collection of elements.</param>
	/// <returns>A random element from the collection.</returns>
	public static T GetElement<T>(IEnumerable<T> array)
		=> Provider.GetElement(array);

	/// <summary>
	/// Returns a random Base64 encoded string generated from a random salt.
	/// </summary>
	/// <param name="min">The minimum length for generating the salt.</param>
	/// <param name="max">The maximum length for generating the salt.</param>
	/// <returns>A random Base64 encoded string.</returns>
	public static string GetString(int min, int max)
		=> Provider.GetString(min, max);

	/// <summary>
	/// Returns a random DateTime value between DateTime.MinValue and DateTime.MaxValue.
	/// </summary>
	/// <returns>A random DateTime.</returns>
	public static DateTime GetDate()
		=> Provider.GetDate();

	/// <summary>
	/// Returns a random DateTime value between the specified minimum and maximum values.
	/// </summary>
	/// <param name="min">The minimum DateTime value.</param>
	/// <param name="max">The maximum DateTime value.</param>
	/// <returns>A random DateTime between min and max.</returns>
	public static DateTime GetDate(DateTime min, DateTime max)
		=> Provider.GetDate(min, max);

	/// <summary>
	/// Returns a random TimeSpan value between TimeSpan.MinValue and TimeSpan.MaxValue.
	/// </summary>
	/// <returns>A random TimeSpan.</returns>
	public static TimeSpan GetTime()
		=> Provider.GetTime();

	/// <summary>
	/// Returns a random TimeSpan value between the specified minimum and maximum values.
	/// </summary>
	/// <param name="min">The minimum TimeSpan value.</param>
	/// <param name="max">The maximum TimeSpan value.</param>
	/// <returns>A random TimeSpan between min and max.</returns>
	public static TimeSpan GetTime(TimeSpan min, TimeSpan max)
		=> Provider.GetTime(min, max);

	/// <summary>
	/// Returns a random non-zero decimal value with a specified number of integer and fractional digits.
	/// Tries up to 10 times to generate a valid number.
	/// </summary>
	/// <param name="integer">The maximum number of digits in the integer part. Default is 8.</param>
	/// <param name="fractional">The maximum number of digits in the fractional part. Default is 8.</param>
	/// <returns>A random decimal value.</returns>
	/// <exception cref="InvalidOperationException">Thrown when a valid decimal value cannot be generated in 10 attempts.</exception>
	public static decimal GetDecimal(int integer = 8, int fractional = 8)
		=> Provider.GetDecimal(integer, fractional);

	/// <summary>
	/// Returns a random decimal value between the specified minimum and maximum values with the given precision.
	/// </summary>
	/// <param name="min">The minimum decimal value.</param>
	/// <param name="max">The maximum decimal value.</param>
	/// <param name="precision">The number of decimal places to round the value to.</param>
	/// <returns>A random decimal value between min and max rounded to the specified precision.</returns>
	public static decimal GetDecimal(decimal min, decimal max, int precision)
		=> Provider.GetDecimal(min, max, precision);
}
