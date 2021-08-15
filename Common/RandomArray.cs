namespace Ecng.Common
{
	using System;
	using System.Linq;

	public class RandomArray<T>
		where T : struct, IComparable<T>
	{
		private readonly SyncObject _lock = new();
		private readonly T[] _data;
		private int _index;

		public RandomArray(int count)
		{
			Count = count;

			_data = Enumerable.Repeat<T>(default, count).ToArray();

			if (typeof(T) == typeof(double))
			{
				for (var i = 0; i < _data.Length; i++)
					_data[i] = RandomGen.GetDouble().To<T>();
			}
			else if (typeof(T) == typeof(int))
			{
				for (var i = 0; i < _data.Length; i++)
					_data[i] = RandomGen.GetInt().To<T>();
			}
			else if (typeof(T) == typeof(bool))
			{
				for (var i = 0; i < _data.Length; i++)
					_data[i] = RandomGen.GetBool().To<T>();
			}
			else if (typeof(T).IsEnum)
			{
				for (var i = 0; i < _data.Length; i++)
					_data[i] = RandomGen.GetEnum<T>();
			}
			else if (typeof(T) == typeof(byte))
			{
				RandomGen.GetBytes(count).CopyTo(_data, 0);
			}
			else
				throw new NotSupportedException();
		}

		public RandomArray(T min, T max, int count)
		{
			Min = min;
			Max = max;
			Count = count;

			if (min.CompareTo(max) > 0)
				throw new ArgumentException("min > max");

			_data = Enumerable.Repeat(min, count).ToArray();

			if (min.CompareTo(max) != 0)
			{
				if (typeof(T) == typeof(int))
				{
					var minInt = min.To<int>();
					var maxInt = max.To<int>();

					for (var i = 0; i < _data.Length; i++)
						_data[i] = RandomGen.GetInt(minInt, maxInt).To<T>();
				}
				else if (typeof(T) == typeof(TimeSpan))
				{
					var minTimeSpan = min.To<TimeSpan>();
					var maxTimeSpan = max.To<TimeSpan>();

					for (var i = 0; i < _data.Length; i++)
						_data[i] = RandomGen.GetTime(minTimeSpan, maxTimeSpan).To<T>();
				}
				else
					throw new NotSupportedException();
			}
		}

		public int Count { get; }
		public T Min { get; }
		public T Max { get; }

		public T Next()
		{
			lock (_lock)
			{
				var next = _data[_index++];

				if (_index == _data.Length)
					_index = 0;

				return next;
			}
		}
	}
}