namespace Ecng.Common;

using System;
using System.Linq;

/// <summary>
/// Provides functionality to generate a random array of values of type T.
/// </summary>
/// <typeparam name="T">The type of the elements in the array. Must be a value type that implements IComparable.</typeparam>
public class RandomArray<T>
	where T : struct, IComparable<T>
{
	private readonly SyncObject _lock = new();
	private readonly T[] _data;
	private int _index;

	/// <summary>
	/// Initializes a new instance of the <see cref="RandomArray{T}"/> class with a specified count.
	/// </summary>
	/// <param name="count">The number of elements in the random array.</param>
	/// <exception cref="NotSupportedException">Thrown when type T is not supported for random generation.</exception>
	public RandomArray(int count)
	{
		Count = count;

		_data = [.. Enumerable.Repeat<T>(default, count)];

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

	/// <summary>
	/// Initializes a new instance of the <see cref="RandomArray{T}"/> class within a specified range.
	/// </summary>
	/// <param name="min">The minimum value of the range.</param>
	/// <param name="max">The maximum value of the range.</param>
	/// <param name="count">The number of elements in the random array.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
	/// <exception cref="NotSupportedException">Thrown when type T is not supported for ranged random generation.</exception>
	public RandomArray(T min, T max, int count)
	{
		Min = min;
		Max = max;
		Count = count;

		if (min.CompareTo(max) > 0)
			throw new ArgumentException("min > max");

		_data = [.. Enumerable.Repeat(min, count)];

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

	/// <summary>
	/// Gets the total number of elements in the random array.
	/// </summary>
	public int Count { get; }

	/// <summary>
	/// Gets the minimum value (or default seed value) used in the random array generation.
	/// </summary>
	public T Min { get; }

	/// <summary>
	/// Gets the maximum value used in the random array generation.
	/// </summary>
	public T Max { get; }

	/// <summary>
	/// Returns the next random element from the array.
	/// </summary>
	/// <returns>A random element of type T from the array.</returns>
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