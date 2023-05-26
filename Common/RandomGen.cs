namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Globalization;
	using System.Runtime.CompilerServices;

	public static class RandomGen
	{
		private static readonly SyncObject _sync = new();
		private static readonly Random _value = new((int)DateTime.Now.Ticks);

		public static double GetDouble()
		{
			lock (_sync)
				return _value.NextDouble();
		}

		public static byte[] GetBytes(int count)
		{
			var buffer = new byte[count];
			GetBytes(buffer);
			return buffer;
		}

		public static void GetBytes(byte[] buffer)
		{
			lock (_sync)
				_value.NextBytes(buffer);
		}

		public static int GetInt()
		{
			lock (_sync)
				return _value.Next();
		}

		public static int GetInt(int max)
		{
			lock (_sync)
				return _value.Next(max + 1);
		}

		public static int GetInt(int min, int max)
		{
			lock (_sync)
				return _value.Next(min, max + 1);
		}

		public static long GetLong() => GetLong(long.MinValue, long.MaxValue);

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
				// Narrow down to the smallest range [0, 2^bits] that contains maxValue - minValue
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

		public static bool GetBool() => GetInt(1) == 1;

		public static T GetEnum<T>()
			where T : struct
			=> GetEnum(Enumerator.GetValues<T>());

		public static T GetEnum<T>(IEnumerable<T> values)
			where T : struct
			=> GetEnum(default, values.Max(value => value.To<long>()).To<T>());

		public static T GetEnum<T>(T min, T max)
			where T : struct
			=> GetLong(min.To<long>(), max.To<long>()).To<T>();

		public static T GetElement<T>(IEnumerable<T> array)
			=> array.ElementAt(GetInt(0, array.Count() - 1));

		public static string GetString(int min, int max)
			=> TypeHelper.GenerateSalt(GetInt(min, max)).Base64();

		public static DateTime GetDate()
			=> GetDate(DateTime.MinValue, DateTime.MaxValue);

		public static DateTime GetDate(DateTime min, DateTime max)
			=> min + GetTime(default, max - min);

		public static TimeSpan GetTime()
			=> GetTime(TimeSpan.MinValue, TimeSpan.MaxValue);

		public static TimeSpan GetTime(TimeSpan min, TimeSpan max)
			=> GetLong(min.Ticks, max.Ticks).To<TimeSpan>();

		public static decimal GetDecimal(int integer = 8, int fractional = 8)
		{
			for (var k = 0; k < 10; k++)
			{
				var i1 = Enumerable.Repeat(9, GetInt(1, integer)).Select(i => GetInt(9).ToString()).Join(string.Empty).To<long>();
				var i2 = Enumerable.Repeat(9, GetInt(1, integer)).Select(i => GetInt(9).ToString()).Join(string.Empty).To<long>();
				var value = decimal.Parse(i1 + "." + i2, CultureInfo.InvariantCulture);

				if (value != 0)
					return value;
			}

			throw new InvalidOperationException();
		}

		public static decimal GetDecimal(decimal min, decimal max, int precision)
		{
			var value = RandomGen.GetDouble() * ((double)max - (double)min) + (double)min;
			return (decimal)value.Round(precision);
		}
	}
}