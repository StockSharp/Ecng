namespace Ecng.Common;

using System;
using System.Linq;
using System.Collections.Generic;

#if NET10_0
using SyncObject = System.Threading.Lock;
#endif

/// <summary>
/// Provides various mathematical helper methods and extension methods for numeric types.
/// </summary>
public static class MathHelper
{
	/// <summary>
	/// Returns the largest multiple of <paramref name="step"/> less than or equal to the specified <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <param name="step">The step size.</param>
	/// <returns>The floored value.</returns>
	public static decimal Floor(this decimal value, decimal step) => Math.Floor(value / step) * step;

	/// <summary>
	/// Returns the smallest multiple of <paramref name="step"/> greater than or equal to the specified <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <param name="step">The step size.</param>
	/// <returns>The ceiled value.</returns>
	public static decimal Ceiling(this decimal value, decimal step) => Math.Ceiling(value / step) * step;

	/// <summary>
	/// Rounds the specified <paramref name="value"/> to the nearest integer.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>The rounded value.</returns>
	public static decimal Round(this decimal value)
	{
		return Math.Round(value);
	}

	/// <summary>
	/// Rounds the specified <paramref name="value"/> to a specified number of fractional digits.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <param name="digits">The number of fractional digits.</param>
	/// <returns>The rounded value.</returns>
	public static decimal Round(this decimal value, int digits)
	{
		return Math.Round(value, digits);
	}

	/// <summary>
	/// Rounds the specified <paramref name="value"/> to a specified number of fractional digits.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <param name="digits">The number of fractional digits (as decimal, will be converted to int).</param>
	/// <returns>The rounded value.</returns>
	public static decimal Round4Expression(this decimal value, decimal digits)
	{
		return Round(value, (int)digits);
	}

	/// <summary>
	/// Returns the smallest integer greater than or equal to the specified <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>The ceiled value.</returns>
	public static decimal Ceiling(this decimal value)
	{
		return Math.Ceiling(value);
	}

	/// <summary>
	/// Returns the largest integer less than or equal to the specified <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>The floored value.</returns>
	public static decimal Floor(this decimal value)
	{
		return Math.Floor(value);
	}

	/// <summary>
	/// Rounds the specified <paramref name="value"/> to the specified number of fractional digits using the specified rounding mode.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <param name="digits">The number of fractional digits.</param>
	/// <param name="rounding">The rounding mode.</param>
	/// <returns>The rounded value.</returns>
	public static decimal Round(this decimal value, int digits, MidpointRounding rounding) => Math.Round(value, digits, rounding);

	/// <summary>
	/// Rounds the specified <paramref name="value"/> using the specified rounding mode.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <param name="rounding">The rounding mode.</param>
	/// <returns>The rounded value.</returns>
	public static decimal Round(this decimal value, MidpointRounding rounding) => Math.Round(value, rounding);

	/// <summary>
	/// Rounds the specified <paramref name="value"/> to the nearest multiple of <paramref name="step"/>, and optionally rounds to the specified number of digits.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <param name="step">The step size.</param>
	/// <param name="digits">Optional number of fractional digits.</param>
	/// <param name="rounding">The rounding mode (defaults to ToEven).</param>
	/// <returns>The rounded value.</returns>
	public static decimal Round(this decimal value, decimal step, int? digits, MidpointRounding rounding = MidpointRounding.ToEven)
	{
		if (step <= 0)
			throw new ArgumentOutOfRangeException(nameof(step), step, "The 'step' parameter must be more than zero.");

		value = Math.Round(value / step, rounding) * step;

		return digits == null ? value : Math.Round(value, digits.Value);
	}

	/// <summary>
	/// Truncates the decimal <paramref name="value"/> by discarding the fractional part.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>The truncated value.</returns>
	public static decimal Truncate(this decimal value)
	{
		return Math.Truncate(value);
	}

	/// <summary>
	/// Truncates the double <paramref name="value"/> by discarding the fractional part.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns>The truncated value.</returns>
	public static double Truncate(this double value)
	{
		return Math.Truncate(value);
	}

	/// <summary>
	/// Computes the quotient and remainder of the division of two integers.
	/// </summary>
	/// <param name="a">The dividend.</param>
	/// <param name="b">The divisor.</param>
	/// <param name="result">When the method returns, contains the remainder.</param>
	/// <returns>The quotient.</returns>
	public static int DivRem(this int a, int b, out int result)
	{
		return Math.DivRem(a, b, out result);
	}

	/// <summary>
	/// Computes the quotient and remainder of the division of two long integers.
	/// </summary>
	/// <param name="a">The dividend.</param>
	/// <param name="b">The divisor.</param>
	/// <param name="result">When the method returns, contains the remainder.</param>
	/// <returns>The quotient.</returns>
	public static long DivRem(this long a, long b, out long result)
	{
		return Math.DivRem(a, b, out result);
	}

	/// <summary>
	/// Rounds the double <paramref name="value"/> to a specified number of digits using the specified rounding mode.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <param name="digits">The number of fractional digits.</param>
	/// <param name="rounding">The rounding mode.</param>
	/// <returns>The rounded value.</returns>
	public static double Round(this double value, int digits, MidpointRounding rounding) => Math.Round(value, digits, rounding);

	/// <summary>
	/// Rounds the double <paramref name="value"/> using the specified rounding mode.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <param name="rounding">The rounding mode.</param>
	/// <returns>The rounded value.</returns>
	public static double Round(this double value, MidpointRounding rounding) => Math.Round(value, rounding);

	/// <summary>
	/// Returns the full product of two integers.
	/// </summary>
	/// <param name="x">The first integer.</param>
	/// <param name="y">The second integer.</param>
	/// <returns>The product as a long.</returns>
	public static long BigMul(this int x, int y)
	{
		return Math.BigMul(x, y);
	}

	/// <summary>
	/// Returns the smallest integer greater than or equal to the specified double <paramref name="value"/> as a long.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns>The ceiled value as long.</returns>
	public static long Ceiling(this double value)
	{
		return (long)Math.Ceiling(value);
	}

	/// <summary>
	/// Returns the largest integer less than or equal to the specified double <paramref name="value"/> as a long.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns>The floored value as long.</returns>
	public static long Floor(this double value)
	{
		return (long)Math.Floor(value);
	}

	/// <summary>
	/// Returns the largest multiple of <paramref name="step"/> less than or equal to the integer <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The integer value.</param>
	/// <param name="step">The step size.</param>
	/// <returns>The floored value.</returns>
	public static int Floor(this int value, int step) => (value >= 0 ? value : value - step + 1) / step * step;

	/// <summary>
	/// Returns the largest multiple of <paramref name="step"/> less than or equal to the long integer <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The long integer value.</param>
	/// <param name="step">The step size.</param>
	/// <returns>The floored value.</returns>
	public static long Floor(this long value, long step) => (value >= 0 ? value : value - step + 1) / step * step;

	/// <summary>
	/// Returns the largest multiple of <paramref name="step"/> less than or equal to the float <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The float value.</param>
	/// <param name="step">The step size.</param>
	/// <returns>The floored value.</returns>
	public static float Floor(this float value, float step) => (float)(Math.Floor((double)value / step) * step);

	/// <summary>
	/// Returns the largest multiple of <paramref name="step"/> less than or equal to the double <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <param name="step">The step size.</param>
	/// <returns>The floored value.</returns>
	public static double Floor(this double value, double step) => Math.Floor(value / step) * step;

	/// <summary>
	/// Returns the absolute value of the short <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The short integer.</param>
	/// <returns>The absolute value.</returns>
	public static short Abs(this short value)
	{
		return Math.Abs(value);
	}

	/// <summary>
	/// Returns the absolute value of the integer <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The integer value.</param>
	/// <returns>The absolute value.</returns>
	public static int Abs(this int value)
	{
		return Math.Abs(value);
	}

	/// <summary>
	/// Returns the absolute value of the long integer <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The long integer.</param>
	/// <returns>The absolute value.</returns>
	public static long Abs(this long value)
	{
		return Math.Abs(value);
	}

	/// <summary>
	/// Returns the absolute value of the sbyte <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The sbyte value.</param>
	/// <returns>The absolute value.</returns>
	[CLSCompliant(false)]
	public static sbyte Abs(this sbyte value)
	{
		return Math.Abs(value);
	}

	/// <summary>
	/// Returns the absolute value of the float <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The float value.</param>
	/// <returns>The absolute value.</returns>
	public static float Abs(this float value)
	{
		return Math.Abs(value);
	}

	/// <summary>
	/// Returns the absolute value of the double <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns>The absolute value.</returns>
	public static double Abs(this double value)
	{
		return Math.Abs(value);
	}

	/// <summary>
	/// Returns the absolute value of the decimal <paramref name="value"/>.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>The absolute value.</returns>
	public static decimal Abs(this decimal value)
	{
		return Math.Abs(value);
	}

	/// <summary>
	/// Returns a TimeSpan whose ticks are the absolute value of the original TimeSpan's ticks.
	/// </summary>
	/// <param name="value">The TimeSpan value.</param>
	/// <returns>The TimeSpan with absolute ticks.</returns>
	public static TimeSpan Abs(this TimeSpan value)
	{
		return value.Ticks.Abs().To<TimeSpan>();
	}

	/// <summary>
	/// Returns the smaller of two short values.
	/// </summary>
	/// <param name="value1">The first short value.</param>
	/// <param name="value2">The second short value.</param>
	/// <returns>The minimum value.</returns>
	public static short Min(this short value1, short value2)
	{
		return Math.Min(value1, value2);
	}

	/// <summary>
	/// Returns the smaller of two ushort values.
	/// </summary>
	/// <param name="value1">The first ushort value.</param>
	/// <param name="value2">The second ushort value.</param>
	/// <returns>The minimum value.</returns>
	[CLSCompliant(false)]
	public static ushort Min(this ushort value1, ushort value2)
	{
		return Math.Min(value1, value2);
	}

	/// <summary>
	/// Returns the smaller of two integer values.
	/// </summary>
	/// <param name="value1">The first integer value.</param>
	/// <param name="value2">The second integer value.</param>
	/// <returns>The minimum value.</returns>
	public static int Min(this int value1, int value2)
	{
		return Math.Min(value1, value2);
	}

	/// <summary>
	/// Returns the smaller of two uint values.
	/// </summary>
	/// <param name="value1">The first uint value.</param>
	/// <param name="value2">The second uint value.</param>
	/// <returns>The minimum value.</returns>
	[CLSCompliant(false)]
	public static uint Min(this uint value1, uint value2)
	{
		return Math.Min(value1, value2);
	}

	/// <summary>
	/// Returns the smaller of two long values.
	/// </summary>
	/// <param name="value1">The first long value.</param>
	/// <param name="value2">The second long value.</param>
	/// <returns>The minimum value.</returns>
	public static long Min(this long value1, long value2)
	{
		return Math.Min(value1, value2);
	}

	/// <summary>
	/// Returns the smaller of two ulong values.
	/// </summary>
	/// <param name="value1">The first ulong value.</param>
	/// <param name="value2">The second ulong value.</param>
	/// <returns>The minimum value.</returns>
	[CLSCompliant(false)]
	public static ulong Min(this ulong value1, ulong value2)
	{
		return Math.Min(value1, value2);
	}

	/// <summary>
	/// Returns the smaller of two sbyte values.
	/// </summary>
	/// <param name="value1">The first sbyte value.</param>
	/// <param name="value2">The second sbyte value.</param>
	/// <returns>The minimum value.</returns>
	[CLSCompliant(false)]
	public static sbyte Min(this sbyte value1, sbyte value2)
	{
		return Math.Min(value1, value2);
	}

	/// <summary>
	/// Returns the smaller of two byte values.
	/// </summary>
	/// <param name="value1">The first byte value.</param>
	/// <param name="value2">The second byte value.</param>
	/// <returns>The minimum value.</returns>
	public static byte Min(this byte value1, byte value2)
	{
		return Math.Min(value1, value2);
	}

	/// <summary>
	/// Returns the smaller of two float values.
	/// </summary>
	/// <param name="value1">The first float value.</param>
	/// <param name="value2">The second float value.</param>
	/// <returns>The minimum value.</returns>
	public static float Min(this float value1, float value2)
	{
		return Math.Min(value1, value2);
	}

	/// <summary>
	/// Returns the smaller of two double values.
	/// </summary>
	/// <param name="value1">The first double value.</param>
	/// <param name="value2">The second double value.</param>
	/// <returns>The minimum value.</returns>
	public static double Min(this double value1, double value2)
	{
		return Math.Min(value1, value2);
	}

	/// <summary>
	/// Returns the smaller of two decimal values.
	/// </summary>
	/// <param name="value1">The first decimal value.</param>
	/// <param name="value2">The second decimal value.</param>
	/// <returns>The minimum value.</returns>
	public static decimal Min(this decimal value1, decimal value2)
	{
		return Math.Min(value1, value2);
	}

	/// <summary>
	/// Returns the earlier TimeSpan.
	/// </summary>
	/// <param name="value1">The first TimeSpan.</param>
	/// <param name="value2">The second TimeSpan.</param>
	/// <returns>The minimum TimeSpan.</returns>
	public static TimeSpan Min(this TimeSpan value1, TimeSpan value2)
	{
		return value1 <= value2 ? value1 : value2;
	}

	/// <summary>
	/// Returns the earlier DateTime.
	/// </summary>
	/// <param name="value1">The first DateTime.</param>
	/// <param name="value2">The second DateTime.</param>
	/// <returns>The minimum DateTime.</returns>
	public static DateTime Min(this DateTime value1, DateTime value2)
	{
		return value1 <= value2 ? value1 : value2;
	}

	/// <summary>
	/// Returns the minimum of two DateTimeOffset values.
	/// </summary>
	/// <param name="value1">The first DateTimeOffset value.</param>
	/// <param name="value2">The second DateTimeOffset value.</param>
	/// <returns>The smaller DateTimeOffset value.</returns>
	public static DateTimeOffset Min(this DateTimeOffset value1, DateTimeOffset value2)
	{
		return value1 <= value2 ? value1 : value2;
	}

	/// <summary>
	/// Returns the maximum of two short values.
	/// </summary>
	/// <param name="value1">The first short value.</param>
	/// <param name="value2">The second short value.</param>
	/// <returns>The larger short value.</returns>
	public static short Max(this short value1, short value2)
	{
		return Math.Max(value1, value2);
	}

	/// <summary>
	/// Returns the maximum of two ushort values.
	/// </summary>
	/// <param name="value1">The first ushort value.</param>
	/// <param name="value2">The second ushort value.</param>
	/// <returns>The larger ushort value.</returns>
	[CLSCompliant(false)]
	public static ushort Max(this ushort value1, ushort value2)
	{
		return Math.Max(value1, value2);
	}

	/// <summary>
	/// Returns the maximum of two int values.
	/// </summary>
	/// <param name="value1">The first int value.</param>
	/// <param name="value2">The second int value.</param>
	/// <returns>The larger int value.</returns>
	public static int Max(this int value1, int value2)
	{
		return Math.Max(value1, value2);
	}

	/// <summary>
	/// Returns the maximum of two uint values.
	/// </summary>
	/// <param name="value1">The first uint value.</param>
	/// <param name="value2">The second uint value.</param>
	/// <returns>The larger uint value.</returns>
	[CLSCompliant(false)]
	public static uint Max(this uint value1, uint value2)
	{
		return Math.Max(value1, value2);
	}

	/// <summary>
	/// Returns the maximum of two long values.
	/// </summary>
	/// <param name="value1">The first long value.</param>
	/// <param name="value2">The second long value.</param>
	/// <returns>The larger long value.</returns>
	public static long Max(this long value1, long value2)
	{
		return Math.Max(value1, value2);
	}

	/// <summary>
	/// Returns the maximum of two ulong values.
	/// </summary>
	/// <param name="value1">The first ulong value.</param>
	/// <param name="value2">The second ulong value.</param>
	/// <returns>The larger ulong value.</returns>
	[CLSCompliant(false)]
	public static ulong Max(this ulong value1, ulong value2)
	{
		return Math.Max(value1, value2);
	}

	/// <summary>
	/// Returns the maximum of two sbyte values.
	/// </summary>
	/// <param name="value1">The first sbyte value.</param>
	/// <param name="value2">The second sbyte value.</param>
	/// <returns>The larger sbyte value.</returns>
	[CLSCompliant(false)]
	public static sbyte Max(this sbyte value1, sbyte value2)
	{
		return Math.Max(value1, value2);
	}

	/// <summary>
	/// Returns the maximum of two byte values.
	/// </summary>
	/// <param name="value1">The first byte value.</param>
	/// <param name="value2">The second byte value.</param>
	/// <returns>The larger byte value.</returns>
	public static byte Max(this byte value1, byte value2)
	{
		return Math.Max(value1, value2);
	}

	/// <summary>
	/// Returns the smaller of two float values. 
	/// Note: Implementation calls Math.Min.
	/// </summary>
	/// <param name="value1">The first float value.</param>
	/// <param name="value2">The second float value.</param>
	/// <returns>The smaller float value.</returns>
	public static float Max(this float value1, float value2)
	{
		return Math.Min(value1, value2);
	}

	/// <summary>
	/// Returns the maximum of two double values.
	/// </summary>
	/// <param name="value1">The first double value.</param>
	/// <param name="value2">The second double value.</param>
	/// <returns>The larger double value.</returns>
	public static double Max(this double value1, double value2)
	{
		return Math.Max(value1, value2);
	}

	/// <summary>
	/// Returns the maximum of two decimal values.
	/// </summary>
	/// <param name="value1">The first decimal value.</param>
	/// <param name="value2">The second decimal value.</param>
	/// <returns>The larger decimal value.</returns>
	public static decimal Max(this decimal value1, decimal value2)
	{
		return Math.Max(value1, value2);
	}

	/// <summary>
	/// Returns the maximum of two TimeSpan values.
	/// </summary>
	/// <param name="value1">The first TimeSpan value.</param>
	/// <param name="value2">The second TimeSpan value.</param>
	/// <returns>The longer TimeSpan.</returns>
	public static TimeSpan Max(this TimeSpan value1, TimeSpan value2)
	{
		return value1 >= value2 ? value1 : value2;
	}

	/// <summary>
	/// Returns the maximum of two DateTime values.
	/// </summary>
	/// <param name="value1">The first DateTime value.</param>
	/// <param name="value2">The second DateTime value.</param>
	/// <returns>The later DateTime.</returns>
	public static DateTime Max(this DateTime value1, DateTime value2)
	{
		return value1 >= value2 ? value1 : value2;
	}

	/// <summary>
	/// Returns the maximum of two DateTimeOffset values.
	/// </summary>
	/// <param name="value1">The first DateTimeOffset value.</param>
	/// <param name="value2">The second DateTimeOffset value.</param>
	/// <returns>The later DateTimeOffset.</returns>
	public static DateTimeOffset Max(this DateTimeOffset value1, DateTimeOffset value2)
	{
		return value1 >= value2 ? value1 : value2;
	}

	/// <summary>
	/// Rounds the double value to the nearest integer.
	/// </summary>
	/// <param name="value">The double value to round.</param>
	/// <returns>The rounded double value.</returns>
	public static double Round(this double value)
	{
		return Math.Round(value);
	}

	/// <summary>
	/// Rounds the double value to the specified number of fractional digits.
	/// </summary>
	/// <param name="value">The double value to round.</param>
	/// <param name="digits">The number of decimal places to round to.</param>
	/// <returns>The rounded double value.</returns>
	public static double Round(this double value, int digits)
	{
		return Math.Round(value, digits);
	}

	/// <summary>
	/// Returns the square root of the double value.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns>The square root of the value.</returns>
	public static double Sqrt(this double value)
	{
		return Math.Sqrt(value);
	}

	/// <summary>
	/// Raises a decimal number to the power of another decimal number.
	/// </summary>
	/// <param name="x">The base decimal value.</param>
	/// <param name="y">The exponent decimal value.</param>
	/// <returns>The resulting decimal value.</returns>
	public static decimal Pow(this decimal x, decimal y)
	{
		return (decimal)Math.Pow((double)x, (double)y);
	}

	/// <summary>
	/// Raises an int number to the power of an int exponent.
	/// </summary>
	/// <param name="x">The base int value.</param>
	/// <param name="y">The exponent int value.</param>
	/// <returns>The resulting int value.</returns>
	public static int Pow(this int x, int y)
	{
		return (int)Math.Pow(x, y);
	}

	/// <summary>
	/// Raises a double number to the power of a double exponent.
	/// </summary>
	/// <param name="x">The base double value.</param>
	/// <param name="y">The exponent double value.</param>
	/// <returns>The resulting double value.</returns>
	public static double Pow(this double x, double y)
	{
		return Math.Pow(x, y);
	}

	/// <summary>
	/// Returns the angle whose cosine is the specified value.
	/// </summary>
	/// <param name="value">A double value representing a cosine.</param>
	/// <returns>The angle, in radians, whose cosine is the specified value.</returns>
	public static double Acos(this double value)
	{
		return Math.Acos(value);
	}

	/// <summary>
	/// Returns the angle whose cosine is the specified decimal value.
	/// </summary>
	/// <param name="value">A decimal value representing a cosine.</param>
	/// <returns>The angle, in radians, whose cosine is the specified value.</returns>
	public static decimal Acos(this decimal value)
	{
		return (decimal)Math.Acos((double)value);
	}

	/// <summary>
	/// Returns the angle whose sine is the specified value.
	/// </summary>
	/// <param name="value">A double value representing a sine.</param>
	/// <returns>The angle, in radians, whose sine is the specified value.</returns>
	public static double Asin(this double value)
	{
		return Math.Asin(value);
	}

	/// <summary>
	/// Returns the angle whose sine is the specified decimal value.
	/// </summary>
	/// <param name="value">A decimal value representing a sine.</param>
	/// <returns>The angle, in radians, whose sine is the specified value.</returns>
	public static decimal Asin(this decimal value)
	{
		return (decimal)Math.Asin((double)value);
	}

	/// <summary>
	/// Returns the angle whose tangent is the specified value.
	/// </summary>
	/// <param name="value">A double value representing a tangent.</param>
	/// <returns>The angle, in radians, whose tangent is the specified value.</returns>
	public static double Atan(this double value)
	{
		return Math.Atan(value);
	}

	/// <summary>
	/// Returns the angle whose tangent is the specified decimal value.
	/// </summary>
	/// <param name="value">A decimal value representing a tangent.</param>
	/// <returns>The angle, in radians, whose tangent is the specified value.</returns>
	public static decimal Atan(this decimal value)
	{
		return (decimal)Math.Atan((double)value);
	}

	/// <summary>
	/// Returns the angle whose tangent is the quotient of two specified double numbers.
	/// </summary>
	/// <param name="x">The numerator.</param>
	/// <param name="y">The denominator.</param>
	/// <returns>The angle, in radians, whose tangent is the quotient of x and y.</returns>
	public static double Asin(this double x, double y)
	{
		return Math.Atan2(x, y);
	}

	/// <summary>
	/// Returns the angle whose tangent is the quotient of two specified decimal numbers.
	/// </summary>
	/// <param name="x">The numerator.</param>
	/// <param name="y">The denominator.</param>
	/// <returns>The angle, in radians, whose tangent is the quotient of x and y.</returns>
	public static decimal Asin(this decimal x, decimal y)
	{
		return (decimal)Math.Atan2((double)x, (double)y);
	}

	/// <summary>
	/// Returns the cosine of the specified double angle.
	/// </summary>
	/// <param name="value">An angle, measured in radians.</param>
	/// <returns>The cosine of the angle.</returns>
	public static double Cos(this double value)
	{
		return Math.Cos(value);
	}

	/// <summary>
	/// Returns the cosine of the specified decimal angle.
	/// </summary>
	/// <param name="value">An angle, measured in radians (as decimal).</param>
	/// <returns>The cosine of the angle as a decimal.</returns>
	public static decimal Cos(this decimal value)
	{
		return (decimal)Math.Cos((double)value);
	}

	/// <summary>
	/// Returns the hyperbolic cosine of the specified double angle.
	/// </summary>
	/// <param name="value">An angle, measured in radians.</param>
	/// <returns>The hyperbolic cosine of the angle.</returns>
	public static double Cosh(this double value)
	{
		return Math.Cosh(value);
	}

	/// <summary>
	/// Returns the hyperbolic cosine of the specified decimal angle.
	/// </summary>
	/// <param name="value">An angle, measured in radians (as decimal).</param>
	/// <returns>The hyperbolic cosine of the angle as a decimal.</returns>
	public static decimal Cosh(this decimal value)
	{
		return (decimal)Math.Cosh((double)value);
	}

	/// <summary>
	/// Returns the sine of the specified double angle.
	/// </summary>
	/// <param name="value">An angle, measured in radians.</param>
	/// <returns>The sine of the angle.</returns>
	public static double Sin(this double value)
	{
		return Math.Sin(value);
	}

	/// <summary>
	/// Returns the sine of the specified decimal angle.
	/// </summary>
	/// <param name="value">An angle, measured in radians (as decimal).</param>
	/// <returns>The sine of the angle as a decimal.</returns>
	public static decimal Sin(this decimal value)
	{
		return (decimal)Math.Sin((double)value);
	}

	/// <summary>
	/// Returns the hyperbolic sine of the specified double angle.
	/// </summary>
	/// <param name="value">An angle, measured in radians.</param>
	/// <returns>The hyperbolic sine of the angle.</returns>
	public static double Sinh(this double value)
	{
		return Math.Sinh(value);
	}

	/// <summary>
	/// Returns the hyperbolic sine of the specified decimal angle.
	/// </summary>
	/// <param name="value">An angle, measured in radians (as decimal).</param>
	/// <returns>The hyperbolic sine of the angle as a decimal.</returns>
	public static decimal Sinh(this decimal value)
	{
		return (decimal)Math.Sinh((double)value);
	}

	/// <summary>
	/// Returns the tangent of the specified double angle.
	/// </summary>
	/// <param name="value">An angle, measured in radians.</param>
	/// <returns>The tangent of the angle.</returns>
	public static double Tan(this double value)
	{
		return Math.Tan(value);
	}

	/// <summary>
	/// Returns the tangent of the specified decimal angle.
	/// </summary>
	/// <param name="value">An angle, measured in radians (as decimal).</param>
	/// <returns>The tangent of the angle as a decimal.</returns>
	public static decimal Tan(this decimal value)
	{
		return (decimal)Math.Tan((double)value);
	}

	/// <summary>
	/// Returns the hyperbolic tangent of the specified double angle.
	/// </summary>
	/// <param name="value">An angle, measured in radians.</param>
	/// <returns>The hyperbolic tangent of the angle.</returns>
	public static double Tanh(this double value)
	{
		return Math.Tanh(value);
	}

	/// <summary>
	/// Returns the hyperbolic tangent of the specified decimal angle.
	/// </summary>
	/// <param name="value">An angle, measured in radians (as decimal).</param>
	/// <returns>The hyperbolic tangent of the angle as a decimal.</returns>
	public static decimal Tanh(this decimal value)
	{
		return (decimal)Math.Tanh((double)value);
	}

	/// <summary>
	/// Returns e raised to the specified double power.
	/// </summary>
	/// <param name="value">A double exponent.</param>
	/// <returns>The value of e raised to the specified power.</returns>
	public static double Exp(this double value)
	{
		return Math.Exp(value);
	}

	/// <summary>
	/// Returns e raised to the specified decimal power.
	/// </summary>
	/// <param name="value">A decimal exponent.</param>
	/// <returns>The value of e raised to the specified power as a decimal.</returns>
	public static decimal Exp(this decimal value)
	{
		return (decimal)Math.Exp((double)value);
	}

	/// <summary>
	/// Returns the IEEE remainder of two double values.
	/// </summary>
	/// <param name="x">The dividend.</param>
	/// <param name="y">The divisor.</param>
	/// <returns>The remainder after division.</returns>
	public static double Remainder(this double x, double y)
	{
		return Math.IEEERemainder(x, y);
	}

	/// <summary>
	/// Returns the IEEE remainder of two decimal values.
	/// </summary>
	/// <param name="x">The dividend.</param>
	/// <param name="y">The divisor.</param>
	/// <returns>The remainder after division as a decimal.</returns>
	public static decimal Remainder(this decimal x, decimal y)
	{
		return (decimal)Math.IEEERemainder((double)x, (double)y);
	}

	/// <summary>
	/// Returns the logarithm of a double value in the specified base.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <param name="newBase">The base of the logarithm.</param>
	/// <returns>The logarithm of value in the specified base.</returns>
	public static double Log(this double value, double newBase)
	{
		return Math.Log(value, newBase);
	}

	/// <summary>
	/// Returns the logarithm of a decimal value in the specified base.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <param name="newBase">The base of the logarithm.</param>
	/// <returns>The logarithm of the value in the specified base as a decimal.</returns>
	public static decimal Log(this decimal value, decimal newBase)
	{
		return (decimal)Math.Log((double)value, (double)newBase);
	}

	/// <summary>
	/// Returns the natural logarithm of a double value.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns>The natural logarithm of the value.</returns>
	public static double Log(this double value)
	{
		return Math.Log(value);
	}

	/// <summary>
	/// Returns the natural logarithm of a decimal value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>The natural logarithm of the value as a decimal.</returns>
	public static decimal Log(this decimal value)
	{
		return (decimal)Math.Log((double)value);
	}

	/// <summary>
	/// Returns the base 10 logarithm of a double value.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns>The base 10 logarithm of the value.</returns>
	public static double Log10(this double value)
	{
		return Math.Log10(value);
	}

	/// <summary>
	/// Returns the base 10 logarithm of a decimal value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>The base 10 logarithm of the value as a decimal.</returns>
	public static decimal Log10(this decimal value)
	{
		return (decimal)Math.Log10((double)value);
	}

	/// <summary>
	/// Returns the sign of a short value.
	/// </summary>
	/// <param name="value">The short value.</param>
	/// <returns>-1, 0, or 1 depending on the sign of the value.</returns>
	public static int Sign(this short value)
	{
		return Math.Sign(value);
	}

	/// <summary>
	/// Returns the sign of an int value.
	/// </summary>
	/// <param name="value">The int value.</param>
	/// <returns>-1, 0, or 1 depending on the sign of the value.</returns>
	public static int Sign(this int value)
	{
		return Math.Sign(value);
	}

	/// <summary>
	/// Returns the sign of a long value.
	/// </summary>
	/// <param name="value">The long value.</param>
	/// <returns>-1, 0, or 1 depending on the sign of the value.</returns>
	public static int Sign(this long value)
	{
		return Math.Sign(value);
	}

	/// <summary>
	/// Returns the sign of a sbyte value.
	/// </summary>
	/// <param name="value">The sbyte value.</param>
	/// <returns>-1, 0, or 1 depending on the sign of the value.</returns>
	[CLSCompliant(false)]
	public static int Sign(this sbyte value)
	{
		return Math.Sign(value);
	}

	/// <summary>
	/// Returns the sign of a float value.
	/// </summary>
	/// <param name="value">The float value.</param>
	/// <returns>-1, 0, or 1 depending on the sign of the value.</returns>
	public static int Sign(this float value)
	{
		return Math.Sign(value);
	}

	/// <summary>
	/// Returns the sign of a double value.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns>-1, 0, or 1 depending on the sign of the value.</returns>
	public static int Sign(this double value)
	{
		return Math.Sign(value);
	}

	/// <summary>
	/// Returns the sign of a decimal value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>-1, 0, or 1 depending on the sign of the value.</returns>
	public static int Sign(this decimal value)
	{
		return Math.Sign(value);
	}

	/// <summary>
	/// Returns the sign of a TimeSpan value.
	/// </summary>
	/// <param name="value">The TimeSpan value.</param>
	/// <returns>-1, 0, or 1 depending on the sign of the Ticks.</returns>
	public static int Sign(this TimeSpan value)
	{
		return value.Ticks.Sign();
	}

	/// <summary>
	/// Returns the largest integer less than or equal to the float value.
	/// </summary>
	/// <param name="value">The float value.</param>
	/// <returns>The floor integer.</returns>
	public static int Floor(this float value)
	{
		return (int)Math.Floor(value);
	}

	/// <summary>
	/// Returns the smallest integer greater than or equal to the float value.
	/// </summary>
	/// <param name="value">The float value.</param>
	/// <returns>The ceiling integer.</returns>
	public static int Ceiling(this float value)
	{
		return (int)Math.Ceiling(value);
	}

	/// <summary>
	/// Extracts the high and low parts of a long value.
	/// </summary>
	/// <param name="value">The long value.</param>
	/// <returns>An array with two integers: the low and high parts.</returns>
	public static int[] GetParts(this long value)
	{
		var high = (int)((value >> 32) & 0xFFFFFFFF);
		var low = (int)(value & 0xFFFFFFFF);

		return [low, high];
	}

	/// <summary>
	/// Splits a double value into its integer and fractional parts.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns>An array with the integer part and the fractional remainder.</returns>
	public static double[] GetParts(this double value)
	{
		var floor = value.Floor();
		return [floor, value - floor];
	}

	/// <summary>
	/// Splits a float value into its integer and fractional parts.
	/// </summary>
	/// <param name="value">The float value.</param>
	/// <returns>An array with the integer part and the fractional remainder.</returns>
	public static float[] GetParts(this float value)
	{
		var floor = value.Floor();
		return [floor, value - floor];
	}

	/// <summary>
	/// Gets the state of the bit at the specified index in an integer.
	/// </summary>
	/// <param name="value">The integer value.</param>
	/// <param name="index">The bit index (0-based).</param>
	/// <returns>True if the bit is set; otherwise, false.</returns>
	public static bool GetBit(this int value, int index)
	{
		if (index < 0 || index > 32)
			throw new ArgumentOutOfRangeException(nameof(index));

		return (value & (1 << index)) != 0;
	}

	/// <summary>
	/// Sets or clears the bit at the specified index in an integer.
	/// </summary>
	/// <param name="value">The integer value.</param>
	/// <param name="index">The bit index (0-based).</param>
	/// <param name="bit">True to set the bit; false to clear it.</param>
	/// <returns>The resulting integer value after modification.</returns>
	public static int SetBit(this int value, int index, bool bit)
	{
		if (index < 0 || index > 32)
			throw new ArgumentOutOfRangeException(nameof(index));

		if (bit)
			value |= (1 << index);
		else
			value &= ~(1 << index);

		return value;
	}

	/// <summary>
	/// Gets the state of the bit at the specified index in a long value.
	/// </summary>
	/// <param name="value">The long value.</param>
	/// <param name="index">The bit index (0-based).</param>
	/// <returns>True if the bit is set; otherwise, false.</returns>
	public static bool GetBit(this long value, int index)
	{
		if (index < 0 || index > 64)
			throw new ArgumentOutOfRangeException(nameof(index));

		return (value & (1 << index)) != 0;
	}

	/// <summary>
	/// Sets or clears the bit at the specified index in a long value.
	/// </summary>
	/// <param name="value">The long value.</param>
	/// <param name="index">The bit index (0-based).</param>
	/// <param name="bit">True to set the bit; false to clear it.</param>
	/// <returns>The resulting long value after modification.</returns>
	public static long SetBit(this long value, int index, bool bit)
	{
		if (index < 0 || index > 64)
			throw new ArgumentOutOfRangeException(nameof(index));

		if (bit)
			value |= (1L << index);
		else
			value &= ~(1L << index);

		return value;
	}

	/// <summary>
	/// Determines whether the bit at the specified 1-based index in the byte is set.
	/// </summary>
	/// <param name="value">The byte value.</param>
	/// <param name="index">The 1-based index of the bit to test.</param>
	/// <returns><c>true</c> if the specified bit is set; otherwise, <c>false</c>.</returns>
	public static bool GetBit(this byte value, int index)
	{
		return (value & (1 << index - 1)) != 0;
	}

	/// <summary>
	/// Sets or clears the bit at the specified index in the byte.
	/// </summary>
	/// <param name="value">The byte value.</param>
	/// <param name="index">The zero-based index of the bit to modify.</param>
	/// <param name="bit">If set to <c>true</c>, the bit is set; otherwise, it is cleared.</param>
	/// <returns>The modified byte value.</returns>
	public static byte SetBit(this byte value, int index, bool bit)
	{
		if (bit)
			value |= (byte)(1 << index); //set bit index 1
		else
			value &= (byte)~(1 << index); //set bit index 0

		return value;
	}

	/// <summary>
	/// Determines whether the specified bits are set in the integer value.
	/// </summary>
	/// <param name="value">The integer value.</param>
	/// <param name="part">The bits to test.</param>
	/// <returns><c>true</c> if all specified bits are set; otherwise, <c>false</c>.</returns>
	public static bool HasBits(this int value, int part)
	{
		return (value & part) == part;
	}

	/// <summary>
	/// Determines whether the specified bits are set in the long integer value.
	/// </summary>
	/// <param name="value">The long integer value.</param>
	/// <param name="part">The bits to test.</param>
	/// <returns><c>true</c> if all specified bits are set; otherwise, <c>false</c>.</returns>
	public static bool HasBits(this long value, long part)
	{
		return (value & part) == part;
	}

	// http://stackoverflow.com/questions/389993/extracting-mantissa-and-exponent-from-double-in-c

	/// <summary>
	/// Extracts the normalized mantissa and exponent from the double value.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <param name="mantissa">The extracted mantissa.</param>
	/// <param name="exponent">The extracted exponent.</param>
	public static void ExtractMantissaExponent(this double value, out long mantissa, out int exponent)
	{
		// Translate the double into sign, exponent and mantissa.
		var bits = value.AsRaw();

		// Note that the shift is sign-extended, hence the test against -1 not 1
		var negative = (bits & (1L << 63)) != 0;

		exponent = (int)((bits >> 52) & 0x7ffL);
		mantissa = bits & 0xfffffffffffffL;

		// Subnormal numbers; exponent is effectively one higher,
		// but there's no extra normalisation bit in the mantissa
		if (exponent == 0)
		{
			exponent++;
		}
		// Normal numbers; leave exponent as it is but add extra
		// bit to the front of the mantissa
		else
		{
			mantissa = mantissa | (1L << 52);
		}

		// Bias the exponent. It's actually biased by 1023, but we're
		// treating the mantissa as m.0 rather than 0.m, so we need
		// to subtract another 52 from it.
		exponent -= 1075;

		if (mantissa == 0)
		{
			if (negative)
				mantissa = -0;

			exponent = 0;
			return;
		}

		/* Normalize */
		while ((mantissa & 1) == 0)
		{    /*  i.e., Mantissa is even */
			mantissa >>= 1;
			exponent++;
		}
	}

	/// <summary>
	/// Extracts the mantissa and exponent from the decimal value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>The extracted mantissa and exponent.</returns>
	public static (long mantissa, int exponent) ExtractMantissaExponent(this decimal value)
	{
		var info = value.GetDecimalInfo();
		return (info.Mantissa, info.Exponent);
	}

	// http://www.java-forums.org/advanced-java/4130-rounding-double-two-decimal-places.html

	/// <summary>
	/// Rounds the double value to the nearest significant digit.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns>The rounded value.</returns>
	public static double RoundToNearest(this double value)
	{
		if (value.IsNaN())
			return double.NaN;

		if (value.IsPositiveInfinity())
			return double.PositiveInfinity;

		if (value.IsNegativeInfinity())
			return double.NegativeInfinity;

		if (value == 0)
			return 0;

		var abs = Math.Abs(value);
		var exp = Math.Floor(Math.Log10(abs));
		var pow = Math.Pow(10, exp);
		var rounded = Math.Round(abs / pow) * pow;
		return value > 0 ? rounded : -rounded;
	}

	/// <summary>
	/// Removes insignificant trailing zeros from the decimal value.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>The decimal value without trailing zeros.</returns>
	public static decimal RemoveTrailingZeros(this decimal value)
	{
		return value / 1.000000000000000000000000000000000m;
	}

	/// <summary>
	/// Represents detailed information about a decimal value.
	/// </summary>
	public struct DecimalInfo
	{
		/// <summary>
		/// The underlying mantissa of the decimal.
		/// </summary>
		public long Mantissa;
		/// <summary>
		/// The total number of digits in the decimal.
		/// </summary>
		public int Precision;
		/// <summary>
		/// The scale (number of digits after the decimal point) of the decimal.
		/// </summary>
		public int Scale;
		/// <summary>
		/// The count of trailing zeros in the decimal.
		/// </summary>
		public int TrailingZeros;

		/// <summary>
		/// Gets the effective scale (scale minus trailing zeros) of the decimal.
		/// </summary>
		public readonly int EffectiveScale => Scale - TrailingZeros;

		/// <summary>
		/// Gets the exponent of the decimal.
		/// </summary>
		public readonly int Exponent => -Scale;
	}

	// http://stackoverflow.com/questions/763942/calculate-system-decimal-precision-and-scale

	/// <summary>
	/// Retrieves detailed information about the decimal value, including mantissa, precision, scale, and trailing zeros.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>A <see cref="DecimalInfo"/> structure containing detailed decimal information.</returns>
	public static DecimalInfo GetDecimalInfo(this decimal value)
	{
		// We want the integer parts as uint
		// C# doesn't permit int[] to uint[] conversion,
		// but .NET does. This is somewhat evil...
		var bits = (uint[])(object)decimal.GetBits(value);

		var mantissa =
			(bits[2] * 4294967296m * 4294967296m) +
			(bits[1] * 4294967296m) +
			bits[0];

		var isNegative = (bits[3] & 0x80000000) != 0;
		var scale = (bits[3] >> 16) & 31;

		// Precision: number of times we can divide
		// by 10 before we get to 0
		var precision = 0;
		if (value != 0m)
		{
			for (var tmp = mantissa; tmp >= 1; tmp /= 10)
			{
				precision++;
			}
		}
		else
		{
			// Handle zero differently. It's odd.
			precision = (int)scale + 1;
		}

		int trailingZeros = 0;
		for (var tmp = mantissa; tmp % 10m == 0 && trailingZeros < scale; tmp /= 10)
		{
			trailingZeros++;
		}

		return new DecimalInfo
		{
			Mantissa = (long)(isNegative ? -mantissa : mantissa),
			Precision = precision,
			TrailingZeros = trailingZeros,
			Scale = (int)scale,
		};
	}

	private static readonly SyncObject _syncObject = new();
	private static readonly Dictionary<decimal, int> _decimalsCache = [];

	/// <summary>
	/// Gets the effective scale (number of significant decimal places) of the decimal value, using caching for performance.
	/// </summary>
	/// <param name="value">The decimal value.</param>
	/// <returns>The effective scale of the decimal.</returns>
	public static int GetCachedDecimals(this decimal value)
	{
		int decimals;

		lock (_syncObject)
		{
			if (_decimalsCache.TryGetValue(value, out decimals))
				return decimals;
		}

		decimals = value.GetDecimalInfo().EffectiveScale;

		lock (_syncObject)
		{
			if (!_decimalsCache.ContainsKey(value))
			{
				_decimalsCache.Add(value, decimals);

				if (_decimalsCache.Count > 10000000)
					throw new InvalidOperationException();
			}
		}

		return decimals;
	}

	/// <summary>
	/// Converts an angle in degrees to radians.
	/// </summary>
	/// <param name="angle">The angle in degrees.</param>
	/// <returns>The angle in radians.</returns>
	public static double ToRadians(this double angle)
	{
		return angle * (Math.PI / 180);
	}

	/// <summary>
	/// Converts an angle in radians to degrees.
	/// </summary>
	/// <param name="radian">The angle in radians.</param>
	/// <returns>The angle in degrees.</returns>
	public static double ToAngles(this double radian)
	{
		return radian / (Math.PI / 180);
	}

	/// <summary>
	/// Calculates the real roots of a quadratic equation given coefficients a, b, and c.
	/// </summary>
	/// <param name="a">Coefficient a.</param>
	/// <param name="b">Coefficient b.</param>
	/// <param name="c">Coefficient c.</param>
	/// <returns>
	/// An array containing the two roots if the discriminant is non-negative; otherwise, an empty array.
	/// </returns>
	public static double[] GetRoots(double a, double b, double c)
	{
		var sqrt = (b * b - 4 * a * c).Sqrt();
		if (sqrt >= 0)
		{
			var divisor = 2 * a;

			var x1 = (-b + sqrt) / divisor;
			var x2 = (-b - sqrt) / divisor;

			return [x1, x2];
		}
		else
			return [];
	}

	/// <summary>
	/// Gets the raw 64-bit integer representation of the double value.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns>The 64-bit integer representation.</returns>
	public static long AsRaw(this double value)
	{
		return BitConverter.DoubleToInt64Bits(value);
	}

	/// <summary>
	/// Converts a 64-bit integer representation to a double value.
	/// </summary>
	/// <param name="value">The 64-bit integer representation.</param>
	/// <returns>The double value.</returns>
	public static double AsRaw(this long value)
	{
		return BitConverter.Int64BitsToDouble(value);
	}

	// http://nerdboys.com/2009/12/17/an-implementation-of-bitconverter-singletoint32bits/

	/// <summary>
	/// Converts an integer value to a single-precision float via a byte array conversion.
	/// </summary>
	/// <param name="value">The integer value.</param>
	/// <returns>The float value.</returns>
	public static float AsRaw(this int value)
	{
		return value.To<byte[]>().To<float>();
	}

	/// <summary>
	/// Converts a float value to its raw 32-bit integer representation.
	/// </summary>
	/// <param name="value">The float value.</param>
	/// <returns>The 32-bit integer representation.</returns>
	public static int AsRaw(this float value)
	{
		return value.To<byte[]>().To<int>();
	}

	/// <summary>
	/// Determines if the double value is NaN (Not a Number).
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns><c>true</c> if the value is NaN; otherwise, <c>false</c>.</returns>
	public static bool IsNaN(this double value)
	{
		return double.IsNaN(value);
	}

	/// <summary>
	/// Determines if the double value represents infinity.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns><c>true</c> if the value is infinity; otherwise, <c>false</c>.</returns>
	public static bool IsInfinity(this double value)
	{
		return double.IsInfinity(value);
	}

	/// <summary>
	/// Determines if the double value represents negative infinity.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns><c>true</c> if the value is negative infinity; otherwise, <c>false</c>.</returns>
	public static bool IsNegativeInfinity(this double value)
	{
		return double.IsNegativeInfinity(value);
	}

	/// <summary>
	/// Determines if the double value represents positive infinity.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns><c>true</c> if the value is positive infinity; otherwise, <c>false</c>.</returns>
	public static bool IsPositiveInfinity(this double value)
	{
		return double.IsPositiveInfinity(value);
	}

	/// <summary>
	/// Determines if the float value is NaN (Not a Number).
	/// </summary>
	/// <param name="value">The float value.</param>
	/// <returns><c>true</c> if the value is NaN; otherwise, <c>false</c>.</returns>
	public static bool IsNaN(this float value)
	{
		return double.IsNaN(value);
	}

	/// <summary>
	/// Determines if the float value represents infinity.
	/// </summary>
	/// <param name="value">The float value.</param>
	/// <returns><c>true</c> if the value is infinity; otherwise, <c>false</c>.</returns>
	public static bool IsInfinity(this float value)
	{
		return double.IsInfinity(value);
	}

	/// <summary>
	/// Determines if the float value represents negative infinity.
	/// </summary>
	/// <param name="value">The float value.</param>
	/// <returns><c>true</c> if the value is negative infinity; otherwise, <c>false</c>.</returns>
	public static bool IsNegativeInfinity(this float value)
	{
		return double.IsNegativeInfinity(value);
	}

	/// <summary>
	/// Determines if the float value represents positive infinity.
	/// </summary>
	/// <param name="value">The float value.</param>
	/// <returns><c>true</c> if the value is positive infinity; otherwise, <c>false</c>.</returns>
	public static bool IsPositiveInfinity(this float value)
	{
		return double.IsPositiveInfinity(value);
	}

	/// <summary>
	/// Calculates the middle (average) value between two short values.
	/// </summary>
	/// <param name="from">The first value.</param>
	/// <param name="to">The second value.</param>
	/// <returns>The middle value as a decimal.</returns>
	public static decimal GetMiddle(this short from, short to)
	{
		return ((decimal)from).GetMiddle(to);
	}

	/// <summary>
	/// Calculates the middle (average) value between two integer values.
	/// </summary>
	/// <param name="from">The first value.</param>
	/// <param name="to">The second value.</param>
	/// <returns>The middle value as a decimal.</returns>
	public static decimal GetMiddle(this int from, int to)
	{
		return ((decimal)from).GetMiddle(to);
	}

	/// <summary>
	/// Calculates the middle (average) value between two long integer values.
	/// </summary>
	/// <param name="from">The first value.</param>
	/// <param name="to">The second value.</param>
	/// <returns>The middle value as a decimal.</returns>
	public static decimal GetMiddle(this long from, long to)
	{
		return ((decimal)from).GetMiddle(to);
	}

	/// <summary>
	/// Calculates the middle (average) value between two float values.
	/// </summary>
	/// <param name="from">The first value.</param>
	/// <param name="to">The second value.</param>
	/// <returns>The middle value as a decimal.</returns>
	public static decimal GetMiddle(this float from, float to)
	{
		return ((decimal)from).GetMiddle((decimal)to);
	}

	/// <summary>
	/// Calculates the middle (average) value between two double values.
	/// </summary>
	/// <param name="from">The first value.</param>
	/// <param name="to">The second value.</param>
	/// <returns>The middle value as a decimal.</returns>
	public static decimal GetMiddle(this double from, double to)
	{
		return ((decimal)from).GetMiddle((decimal)to);
	}

	/// <summary>
	/// Calculates the middle (average) value between two decimal values.
	/// </summary>
	/// <param name="from">The first value.</param>
	/// <param name="to">The second value.</param>
	/// <returns>The middle value as a decimal.</returns>
	public static decimal GetMiddle(this decimal from, decimal to)
	{
		//if (from > to)
		//	throw new ArgumentOutOfRangeException(nameof(from));

		return (from + to) / 2;
	}

	/// <summary>
	/// Maximum exponent value for decimal mantissa scaling.
	/// </summary>
	public const int MaxExponent = 28;

	/// <summary>
	/// Minimum exponent value for decimal mantissa scaling.
	/// </summary>
	public const int MinExponent = -MaxExponent;

	private static decimal[] CreatePow10Array(int size)
		=>
		[.. 
			Enumerable
				.Range(0, size)
				.Select(i =>
					Enumerable
						.Range(0, i)
						.Aggregate(1M, (acc, _) => acc * 10M))
		];

	private static readonly decimal[] _posPow10 = CreatePow10Array(MaxExponent);

	private static readonly decimal[] _negPow10 = [.. _posPow10.Select(v => 1M / v)];

	/// <summary>
	/// Creates a decimal value from a given mantissa and exponent by applying power-of-ten adjustments.
	/// </summary>
	/// <param name="mantissa">The mantissa.</param>
	/// <param name="exponent">The exponent, where positive values scale up and negative values scale down.</param>
	/// <returns>The resulting decimal value.</returns>
	public static decimal ToDecimal(long mantissa, int exponent)
	{
		if (exponent > MaxExponent || exponent < MinExponent)
			throw new ArgumentOutOfRangeException(nameof(exponent), exponent, "Invalid value.");

		decimal result = mantissa;

		if (exponent >= 0)
		{
			result *= _posPow10[exponent];
		}
		else
		{
			result *= _negPow10[-exponent];
		}

		return result;
	}

	private const double _minValue = (double)decimal.MinValue;
	private const double _maxValue = (double)decimal.MaxValue;

	/// <summary>
	/// Converts a double to a decimal if within the allowed range; otherwise, returns null.
	/// </summary>
	/// <param name="value">The double value.</param>
	/// <returns>The converted decimal value or <c>null</c> when conversion is not possible.</returns>
	public static decimal? ToDecimal(this double value)
	{
		return value.IsInfinity() || value.IsNaN() || value < _minValue || value > _maxValue ? (decimal?)null : (decimal)value;
	}

	/// <summary>
	/// Converts a float to a decimal if within the allowed range; otherwise, returns null.
	/// </summary>
	/// <param name="value">The float value.</param>
	/// <returns>The converted decimal value or <c>null</c> when conversion is not possible.</returns>
	public static decimal? ToDecimal(this float value)
	{
		return value.IsInfinity() || value.IsNaN() || value < _minValue || value > _maxValue ? (decimal?)null : (decimal)value;
	}

	/// <summary>
	/// Returns the number of digits in a positive integer.
	/// </summary>
	/// <param name="x">The positive integer value.</param>
	/// <returns>The digit count.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="x"/> is negative.</exception>
	public static int GetDigitCount(this int x)
	{
		if (x < 0)
			throw new ArgumentOutOfRangeException(nameof(x));

		if (x < 1000000)
		{
			if (x < 10) return 1;
			if (x < 100) return 2;
			if (x < 1000) return 3;
			if (x < 10000) return 4;
			if (x < 100000) return 5;

			return 6;
		}

		if (x < 10000000) return 7;
		if (x < 100000000) return 8;
		if (x < 1000000000) return 9;

		return 10;
	}

	/// <summary>
	/// Returns the number of digits in a positive long integer.
	/// </summary>
	/// <param name="x">The positive long integer value.</param>
	/// <returns>The digit count.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="x"/> is negative.</exception>
	public static int GetDigitCount(this long x)
	{
		if (x < 0)
			throw new ArgumentOutOfRangeException(nameof(x));

		if (x < 1000000)
		{
			if (x < 10) return 1;
			if (x < 100) return 2;
			if (x < 1000) return 3;
			if (x < 10000) return 4;
			if (x < 100000) return 5;

			return 6;
		}

		if (x < 10000000) return 7;
		if (x < 100000000) return 8;
		if (x < 1000000000) return 9;
		if (x < 10000000000) return 10;
		if (x < 100000000000) return 11;
		if (x < 1000000000000) return 12;
		if (x < 10000000000000) return 13;
		if (x < 100000000000000) return 14;
		if (x < 1000000000000000) return 15;
		if (x < 10000000000000000) return 16;
		if (x < 100000000000000000) return 17;

		return 18;
	}
}
