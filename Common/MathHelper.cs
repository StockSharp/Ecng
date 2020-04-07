namespace Ecng.Common
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	public static class MathHelper
	{
		public static decimal Floor(this decimal value, decimal step)
		{
			return value - value % step;
		}

		public static decimal Round(this decimal value)
		{
			return Math.Round(value);
		}

		public static decimal Round(this decimal value, int digits)
		{
			return Math.Round(value, digits);
		}

#if !SILVERLIGHT
		public static decimal Ceiling(this decimal value)
		{
			return Math.Ceiling(value);
		}

		public static decimal Floor(this decimal value)
		{
			return Math.Floor(value);
		}

		public static decimal Round(this decimal value, int digits, MidpointRounding rounding)
		{
			return Math.Round(value, digits, rounding);
		}

		public static decimal Round(this decimal value, MidpointRounding rounding)
		{
			return Math.Round(value, rounding);
		}

		public static decimal Round(this decimal value, decimal step, int digits, MidpointRounding? rounding = null)
		{
			if (step <= 0)
				throw new ArgumentOutOfRangeException(nameof(step), step, "The 'step' parameter must be more than zero.");

			var retVal = Floor(value, step);

			if (retVal != value)
			{
				var lowBound = retVal;
				var hiBound = lowBound;

				if (value >= 0)
					hiBound += step;
				else
					hiBound -= step;

				switch (rounding)
				{
					case MidpointRounding.AwayFromZero:
						retVal = lowBound;
						break;
					case MidpointRounding.ToEven:
						retVal = hiBound;
						break;
					case null:
						retVal = ((value - lowBound).Abs() > (hiBound - value).Abs()) ? hiBound : lowBound;
						break;
				}
			}

			return Round(retVal, digits);
		}

		public static decimal Truncate(this decimal value)
		{
			return Math.Truncate(value);
		}

		public static double Truncate(this double value)
		{
			return Math.Truncate(value);
		}

		public static int DivRem(this int a, int b, out int result)
		{
			return Math.DivRem(a, b, out result);
		}

		public static long DivRem(this long a, long b, out long result)
		{
			return Math.DivRem(a, b, out result);
		}

		public static double Round(this double value, int digits, MidpointRounding rounding)
		{
			return Math.Round(value, digits, rounding);
		}

		public static double Round(this double value, MidpointRounding rounding)
		{
			return Math.Round(value, rounding);
		}

		public static long BigMul(this int x, int y)
		{
			return Math.BigMul(x, y);
		}
#endif
		public static long Ceiling(this double value)
		{
			return (long)Math.Ceiling(value);
		}

		public static long Floor(this double value)
		{
			return (long)Math.Floor(value);
		}

		public static int Floor(this int value, int step)
		{
			return value - value % step;
		}

		public static long Floor(this long value, long step)
		{
			return value - value % step;
		}

		public static float Floor(this float value, float step)
		{
			return value - value % step;
		}

		public static double Floor(this double value, double step)
		{
			return value - value % step;
		}

		public static short Abs(this short value)
		{
			return Math.Abs(value);
		}

		public static int Abs(this int value)
		{
			return Math.Abs(value);
		}

		public static long Abs(this long value)
		{
			return Math.Abs(value);
		}

		[CLSCompliant(false)]
		public static sbyte Abs(this sbyte value)
		{
			return Math.Abs(value);
		}

		public static float Abs(this float value)
		{
			return Math.Abs(value);
		}

		public static double Abs(this double value)
		{
			return Math.Abs(value);
		}

		public static decimal Abs(this decimal value)
		{
			return Math.Abs(value);
		}

		public static TimeSpan Abs(this TimeSpan value)
		{
			return value.Ticks.Abs().To<TimeSpan>();
		}

		public static short Min(this short value1, short value2)
		{
			return Math.Min(value1, value2);
		}

		[CLSCompliant(false)]
		public static ushort Min(this ushort value1, ushort value2)
		{
			return Math.Min(value1, value2);
		}

		public static int Min(this int value1, int value2)
		{
			return Math.Min(value1, value2);
		}

		[CLSCompliant(false)]
		public static uint Min(this uint value1, uint value2)
		{
			return Math.Min(value1, value2);
		}

		public static long Min(this long value1, long value2)
		{
			return Math.Min(value1, value2);
		}

		[CLSCompliant(false)]
		public static ulong Min(this ulong value1, ulong value2)
		{
			return Math.Min(value1, value2);
		}

		[CLSCompliant(false)]
		public static sbyte Min(this sbyte value1, sbyte value2)
		{
			return Math.Min(value1, value2);
		}

		public static byte Min(this byte value1, byte value2)
		{
			return Math.Min(value1, value2);
		}

		public static float Min(this float value1, float value2)
		{
			return Math.Min(value1, value2);
		}

		public static double Min(this double value1, double value2)
		{
			return Math.Min(value1, value2);
		}

		public static decimal Min(this decimal value1, decimal value2)
		{
			return Math.Min(value1, value2);
		}

		public static TimeSpan Min(this TimeSpan value1, TimeSpan value2)
		{
			return value1 <= value2 ? value1 : value2;
		}

		public static DateTime Min(this DateTime value1, DateTime value2)
		{
			return value1 <= value2 ? value1 : value2;
		}

		public static short Max(this short value1, short value2)
		{
			return Math.Max(value1, value2);
		}

		[CLSCompliant(false)]
		public static ushort Max(this ushort value1, ushort value2)
		{
			return Math.Max(value1, value2);
		}

		public static int Max(this int value1, int value2)
		{
			return Math.Max(value1, value2);
		}

		[CLSCompliant(false)]
		public static uint Max(this uint value1, uint value2)
		{
			return Math.Max(value1, value2);
		}

		public static long Max(this long value1, long value2)
		{
			return Math.Max(value1, value2);
		}

		[CLSCompliant(false)]
		public static ulong Max(this ulong value1, ulong value2)
		{
			return Math.Max(value1, value2);
		}

		[CLSCompliant(false)]
		public static sbyte Max(this sbyte value1, sbyte value2)
		{
			return Math.Max(value1, value2);
		}

		public static byte Max(this byte value1, byte value2)
		{
			return Math.Max(value1, value2);
		}

		public static float Max(this float value1, float value2)
		{
			return Math.Min(value1, value2);
		}

		public static double Max(this double value1, double value2)
		{
			return Math.Max(value1, value2);
		}

		public static decimal Max(this decimal value1, decimal value2)
		{
			return Math.Max(value1, value2);
		}

		public static TimeSpan Max(this TimeSpan value1, TimeSpan value2)
		{
			return value1 >= value2 ? value1 : value2;
		}

		public static DateTime Max(this DateTime value1, DateTime value2)
		{
			return value1 >= value2 ? value1 : value2;
		}

		public static double Round(this double value)
		{
			return Math.Round(value);
		}

		public static double Round(this double value, int digits)
		{
			return Math.Round(value, digits);
		}

		public static double Sqrt(this double value)
		{
			return Math.Sqrt(value);
		}

		public static decimal Pow(this decimal x, decimal y)
		{
			return (decimal)Math.Pow((double)x, (double)y);
		}

		public static int Pow(this int x, int y)
		{
			return (int)Math.Pow(x, y);
		}

		public static double Pow(this double x, double y)
		{
			return Math.Pow(x, y);
		}

		public static double Acos(this double value)
		{
			return Math.Acos(value);
		}

		public static decimal Acos(this decimal value)
		{
			return (decimal)Math.Acos((double)value);
		}

		public static double Asin(this double value)
		{
			return Math.Asin(value);
		}

		public static decimal Asin(this decimal value)
		{
			return (decimal)Math.Asin((double)value);
		}

		public static double Atan(this double value)
		{
			return Math.Atan(value);
		}

		public static decimal Atan(this decimal value)
		{
			return (decimal)Math.Atan((double)value);
		}

		public static double Asin(this double x, double y)
		{
			return Math.Atan2(x, y);
		}

		public static decimal Asin(this decimal x, decimal y)
		{
			return (decimal)Math.Atan2((double)x, (double)y);
		}

		public static double Cos(this double value)
		{
			return Math.Cos(value);
		}

		public static decimal Cos(this decimal value)
		{
			return (decimal)Math.Cos((double)value);
		}

		public static double Cosh(this double value)
		{
			return Math.Cosh(value);
		}

		public static decimal Cosh(this decimal value)
		{
			return (decimal)Math.Cosh((double)value);
		}

		public static double Sin(this double value)
		{
			return Math.Sin(value);
		}

		public static decimal Sin(this decimal value)
		{
			return (decimal)Math.Sin((double)value);
		}

		public static double Sinh(this double value)
		{
			return Math.Sinh(value);
		}

		public static decimal Sinh(this decimal value)
		{
			return (decimal)Math.Sinh((double)value);
		}

		public static double Tan(this double value)
		{
			return Math.Tan(value);
		}

		public static decimal Tan(this decimal value)
		{
			return (decimal)Math.Tan((double)value);
		}

		public static double Tanh(this double value)
		{
			return Math.Tanh(value);
		}

		public static decimal Tanh(this decimal value)
		{
			return (decimal)Math.Tanh((double)value);
		}

		public static double Exp(this double value)
		{
			return Math.Exp(value);
		}

		public static decimal Exp(this decimal value)
		{
			return (decimal)Math.Exp((double)value);
		}

		public static double Remainder(this double x, double y)
		{
			return Math.IEEERemainder(x, y);
		}

		public static decimal Remainder(this decimal x, decimal y)
		{
			return (decimal)Math.IEEERemainder((double)x, (double)y);
		}

		public static double Log(this double value, double newBase)
		{
			return Math.Log(value, newBase);
		}

		public static decimal Log(this decimal value, decimal newBase)
		{
			return (decimal)Math.Log((double)value, (double)newBase);
		}

		public static double Log(this double value)
		{
			return Math.Log(value);
		}

		public static decimal Log(this decimal value)
		{
			return (decimal)Math.Log((double)value);
		}

		public static double Log10(this double value)
		{
			return Math.Log10(value);
		}

		public static decimal Log10(this decimal value)
		{
			return (decimal)Math.Log10((double)value);
		}

		public static int Sign(this short value)
		{
			return Math.Sign(value);
		}

		public static int Sign(this int value)
		{
			return Math.Sign(value);
		}

		public static int Sign(this long value)
		{
			return Math.Sign(value);
		}

		[CLSCompliant(false)]
		public static int Sign(this sbyte value)
		{
			return Math.Sign(value);
		}

		public static int Sign(this float value)
		{
			return Math.Sign(value);
		}

		public static int Sign(this double value)
		{
			return Math.Sign(value);
		}

		public static int Sign(this decimal value)
		{
			return Math.Sign(value);
		}

		public static int Sign(this TimeSpan value)
		{
			return value.Ticks.Sign();
		}

		public static int Floor(this float value)
		{
			return (int)Math.Floor(value);
		}

		public static int Ceiling(this float value)
		{
			return (int)Math.Ceiling(value);
		}

		public static int[] GetParts(this long value)
		{
			var high = (int)((value >> 32) & 0xFFFFFFFF);
			var low = (int)(value & 0xFFFFFFFF);

			return new[] { low, high };
		}

		public static double[] GetParts(this double value)
		{
			var floor = value.Floor();
			return new[] { floor, value - floor };
		}

		public static float[] GetParts(this float value)
		{
			var floor = value.Floor();
			return new[] { floor, value - floor };
		}

		public static bool GetBit(this int value, int index)
		{
			if (index < 0 || index > 32)
				throw new ArgumentOutOfRangeException(nameof(index));

			return (value & (1 << index)) != 0;
		}

		public static int SetBit(this int value, int index, bool bit)
		{
			if (index < 0 || index > 32)
				throw new ArgumentOutOfRangeException(nameof(index));

			// http://www.daniweb.com/forums/thread196013.html

			if (bit)
				value |= (1 << index); //set bit index 1
			else
				value &= ~(1 << index); //set bit index 0

			return value;
		}

		public static bool GetBit(this long value, int index)
		{
			if (index < 0 || index > 64)
				throw new ArgumentOutOfRangeException(nameof(index));

			return (value & (1 << index)) != 0;
		}

		public static long SetBit(this long value, int index, bool bit)
		{
			if (index < 0 || index > 64)
				throw new ArgumentOutOfRangeException(nameof(index));

			// http://www.daniweb.com/forums/thread196013.html

			if (bit)
				value |= (1L << index); //set bit index 1
			else
				value &= ~(1L << index); //set bit index 0

			return value;
		}

		public static bool GetBit(this byte value, int index)
		{
			return (value & (1 << index - 1)) != 0;
		}

		public static byte SetBit(this byte value, int index, bool bit)
		{
			if (bit)
				value |= (byte)(1 << index); //set bit index 1
			else
				value &= (byte)~(1 << index); //set bit index 0

			return value;
		}

		public static bool HasBits(this int value, int part)
		{
			return (value & part) == part;
		}

		public static bool HasBits(this long value, long part)
		{
			return (value & part) == part;
		}

		// http://stackoverflow.com/questions/389993/extracting-mantissa-and-exponent-from-double-in-c
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

		public static void ExtractMantissaExponent(this decimal value, out long mantissa, out int exponent)
		{
			var info = value.GetDecimalInfo();
			mantissa = info.Mantissa;
			exponent = info.Scale;
		}

		// http://www.java-forums.org/advanced-java/4130-rounding-double-two-decimal-places.html
		public static double RoundToNearest(this double value)
		{
			const double coef = 100;
			var result = value * coef;
			result = result.Round();
			result = result / coef;
			return result;
		}

		//public static int GetDecimals(this double value)
		//{
		//    return ((decimal)value).GetDecimals();
		//}

		//public static int GetDecimals(this decimal value)
		//{
		//    // see SqlDecimal.Scale;
		//    return (decimal.GetBits(value)[3] & 0x00FF0000) >> 16;
		//}

		public static decimal RemoveTrailingZeros(this decimal value)
		{
			return (decimal)((double)value);
		}

		public struct DecimalInfo
		{
			public long Mantissa;
			public int Precision;
			public int Scale;
			public int TrailingZeros;

			public int EffectiveScale => Scale - TrailingZeros;
		}

		// http://stackoverflow.com/questions/763942/calculate-system-decimal-precision-and-scale
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
				Mantissa = (long)mantissa,
				Precision = precision,
				TrailingZeros = trailingZeros,
				Scale = (int)scale,
			};
		}

		private static readonly SyncObject _syncObject = new SyncObject();
		private static readonly Dictionary<decimal, int> _decimalsCache = new Dictionary<decimal, int>(); 

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
		/// 
		/// </summary>
		/// <param name="angle"></param>
		/// <returns></returns>
		public static double ToRadians(this double angle)
		{
			return angle * (Math.PI / 180);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="radian"></param>
		/// <returns></returns>
		public static double ToAngles(this double radian)
		{
			return radian / (Math.PI / 180);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		/// <returns></returns>
		public static double[] GetRoots(double a, double b, double c)
		{
			var sqrt = (b * b - 4 * a * c).Sqrt();
			if (sqrt >= 0)
			{
				var divisor = 2 * a;

				var x1 = (-b + sqrt) / divisor;
				var x2 = (-b - sqrt) / divisor;

				return new[] { x1, x2 };
			}
			else
				return ArrayHelper.Empty<double>();
		}

		public static long AsRaw(this double value)
		{
			return BitConverter.DoubleToInt64Bits(value);
		}

		public static double AsRaw(this long value)
		{
			return BitConverter.Int64BitsToDouble(value);
		}

		// http://nerdboys.com/2009/12/17/an-implementation-of-bitconverter-singletoint32bits/
		public static float AsRaw(this int value)
		{
			return value.To<byte[]>().To<float>();
		}

		public static int AsRaw(this float value)
		{
			return value.To<byte[]>().To<int>();
		}

		public static bool IsNaN(this double value)
		{
			return double.IsNaN(value);
		}

		public static bool IsInfinity(this double value)
		{
			return double.IsInfinity(value);
		}

		public static bool IsNegativeInfinity(this double value)
		{
			return double.IsNegativeInfinity(value);
		}

		public static bool IsPositiveInfinity(this double value)
		{
			return double.IsPositiveInfinity(value);
		}

		public static bool IsNaN(this float value)
		{
			return double.IsNaN(value);
		}

		public static bool IsInfinity(this float value)
		{
			return double.IsInfinity(value);
		}

		public static bool IsNegativeInfinity(this float value)
		{
			return double.IsNegativeInfinity(value);
		}

		public static bool IsPositiveInfinity(this float value)
		{
			return double.IsPositiveInfinity(value);
		}

		public static decimal GetMiddle(this short from, short to)
		{
			return ((decimal)from).GetMiddle(to);
		}

		public static decimal GetMiddle(this int from, int to)
		{
			return ((decimal)from).GetMiddle(to);
		}

		public static decimal GetMiddle(this long from, long to)
		{
			return ((decimal)from).GetMiddle(to);
		}

		public static decimal GetMiddle(this float from, float to)
		{
			return ((decimal)from).GetMiddle((decimal)to);
		}

		public static decimal GetMiddle(this double from, double to)
		{
			return ((decimal)from).GetMiddle((decimal)to);
		}

		public static decimal GetMiddle(this decimal from, decimal to)
		{
			//if (from > to)
			//	throw new ArgumentOutOfRangeException("from");

			return (from + to) / 2;
		}

		//public static Point<int> GetEndNearestPoint(Line line, int distance)
		//{
		//    double angle = GetAngle(line.Start, line.End);

		//    if (angle == 0)
		//        return new Point<int>(line.End.X - distance, line.End.Y);
		//    else if (angle == 90)
		//        return new Point<int>(line.End.X, line.Start.Y + distance);
		//    else if (angle == 180)
		//        return new Point<int>(line.End.X + distance, line.End.Y);
		//    else if (angle == 270)
		//        return new Point<int>(line.End.X, line.End.Y - distance);
		//    else
		//    {
		//        int coef;

		//        if (angle > 90 && angle < 180)
		//        {
		//            angle = 180 - angle;
		//            coef = -1;
		//        }
		//        else if (angle > 180 && angle < 270)
		//        {
		//            angle = angle - 180;
		//            coef = -1;
		//        }
		//        else if (angle > 270 && angle < 360)
		//        {
		//            angle = 360 - angle;
		//            coef = 1;
		//        }
		//        else
		//        {
		//            coef = 1;
		//        }

		//        double xLength = distance * Math.Cos(ToRadians(angle));

		//        int x = (int)(line.End.X - coef * xLength);
		//        return new Point<int>(x, line.GetY(x));
		//    }
		//}

		//public static Point<int> GetNearest(Point<int> center, Point<int>[] points)
		//{
		//    if (center == null)
		//        throw new ArgumentNullException("center");

		//    if (points == null)
		//        throw new ArgumentNullException("points");

		//    if (points.Length < 1)
		//        throw new ArgumentException("points");

		//    double minLength = double.MaxValue;
		//    Point<int> nearest = points[0];

		//    foreach (var point in points)
		//    {
		//        double length = new Line(center, point).Length;

		//        if (minLength > length)
		//        {
		//            minLength = length;
		//            nearest = point;
		//        }
		//    }

		//    return nearest;
		//}

		private static readonly decimal[] _posPow10 =
		{
			1M,
			10M,
			100M,
			1000M,
			10000M,
			100000M,
			1000000M,
			10000000M,
			100000000M,
			1000000000M,
			10000000000M,
			100000000000M,
		};

		private static readonly decimal[] _negPow10 = _posPow10.Select(v => 1M / v).ToArray();

		// https://stackoverflow.com/q/9993417/8029915
		public static decimal ToDecimal(long mantissa, int exponent)
		{
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
	}
}