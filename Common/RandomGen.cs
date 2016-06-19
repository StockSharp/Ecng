namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public static class RandomGen
	{
		private static readonly SyncObject _sync = new SyncObject();
		private static readonly Random _value = new Random((int)DateTime.Now.Ticks);

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
	}
}