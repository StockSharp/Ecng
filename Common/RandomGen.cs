namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Globalization;

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

		public static bool GetBool()
		{
			return GetInt(1) == 1;
		}

		public static T GetEnum<T>()
			where T : struct
		{
			return GetEnum(Enumerator.GetValues<T>());
		}

		public static T GetEnum<T>(IEnumerable<T> values)
			where T : struct
		{
			return GetInt(0, values.Max(value => value.To<int>())).To<T>();
		}

		public static T GetElement<T>(IEnumerable<T> array)
		{
			return array.ElementAt(GetInt(0, array.Count() - 1));
		}

		public static TimeSpan GetTime(TimeSpan min, TimeSpan max)
		{
			return TimeSpan.FromTicks(GetInt(0, (int)(max.Ticks - min.Ticks)) + min.Ticks);
		}

		public static decimal GetDecimal(int integer = 8, int fractional = 8)
		{
			for (var k = 0; k < 10; k++)
			{
				var i1 = Enumerable.Repeat(9, RandomGen.GetInt(1, integer)).Select(i => RandomGen.GetInt(9).ToString()).Join("").To<long>();
				var i2 = Enumerable.Repeat(9, RandomGen.GetInt(1, integer)).Select(i => RandomGen.GetInt(9).ToString()).Join("").To<long>();
				var value = decimal.Parse(i1 + "." + i2, CultureInfo.InvariantCulture);

				if (value != 0)
					return value;
			}

			throw new InvalidOperationException();
		}
	}
}