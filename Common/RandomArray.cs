namespace Ecng.Common;

using System;
using System.Linq;
using System.Threading;

/// <summary>
/// Provides functionality to generate a random array of values of type T.
/// </summary>
/// <typeparam name="T">The type of the elements in the array. Must be a value type that implements IComparable.</typeparam>
public class RandomArray<T>
	where T : struct, IComparable<T>
{
	private readonly Lock _lock = new();
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
		_data = new T[count];

		// Special case for byte - optimize by using GetBytes
		if (typeof(T) == typeof(byte))
		{
			RandomGen.GetBytes(count).CopyTo(_data, 0);
			return;
		}

		// Special case for enums
		if (typeof(T).IsEnum)
		{
			FillArray(() => RandomGen.GetEnum<T>());
			return;
		}

		var generator = GetValueGenerator();
		FillArray(generator);
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

		// If min == max, array is already filled with constant value
		if (min.CompareTo(max) == 0)
			return;

		var generator = GetRangedValueGenerator(min, max);
		FillArray(generator);
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
		using (_lock.EnterScope())
		{
			var next = _data[_index++];

			if (_index == _data.Length)
				_index = 0;

			return next;
		}
	}

	private void FillArray(Func<T> generator)
	{
		for (var i = 0; i < _data.Length; i++)
			_data[i] = generator();
	}

	private static Func<T> GetValueGenerator()
	{
		var type = typeof(T);

		if (type == typeof(double))
			return () => RandomGen.GetDouble().To<T>();
		if (type == typeof(float))
			return () => RandomGen.GetFloat().To<T>();
		if (type == typeof(decimal))
			return () => RandomGen.GetDecimal(0, 1, 8).To<T>();
		if (type == typeof(int))
			return () => RandomGen.GetInt().To<T>();
		if (type == typeof(long))
			return () => RandomGen.GetLong().To<T>();
		if (type == typeof(short))
			return () => RandomGen.GetShort().To<T>();
		if (type == typeof(uint))
			return () => RandomGen.GetUInt().To<T>();
		if (type == typeof(ulong))
			return () => RandomGen.GetULong().To<T>();
		if (type == typeof(ushort))
			return () => RandomGen.GetUShort().To<T>();
		if (type == typeof(sbyte))
			return () => RandomGen.GetSByte().To<T>();
		if (type == typeof(char))
			return () => ((char)RandomGen.GetInt(32, 127)).To<T>(); // Printable ASCII characters
		if (type == typeof(bool))
			return () => RandomGen.GetBool().To<T>();

		throw new NotSupportedException($"Type {type.Name} is not supported for random generation.");
	}

	private static Func<T> GetRangedValueGenerator(T min, T max)
	{
		var type = typeof(T);

		if (type == typeof(int))
		{
			var minVal = min.To<int>();
			var maxVal = max.To<int>();
			return () => RandomGen.GetInt(minVal, maxVal).To<T>();
		}

		if (type == typeof(long))
		{
			var minVal = min.To<long>();
			var maxVal = max.To<long>();
			return () => RandomGen.GetLong(minVal, maxVal).To<T>();
		}

		if (type == typeof(short))
		{
			var minVal = min.To<short>();
			var maxVal = max.To<short>();
			return () => RandomGen.GetShort(minVal, maxVal).To<T>();
		}

		if (type == typeof(byte))
		{
			var minVal = min.To<byte>();
			var maxVal = max.To<byte>();
			return () => ((byte)RandomGen.GetInt(minVal, maxVal)).To<T>();
		}

		if (type == typeof(sbyte))
		{
			var minVal = min.To<sbyte>();
			var maxVal = max.To<sbyte>();
			return () => RandomGen.GetSByte(minVal, maxVal).To<T>();
		}

		if (type == typeof(uint))
		{
			var minVal = min.To<uint>();
			var maxVal = max.To<uint>();
			return () => RandomGen.GetUInt(minVal, maxVal).To<T>();
		}

		if (type == typeof(ulong))
		{
			var minVal = min.To<ulong>();
			var maxVal = max.To<ulong>();
			return () => RandomGen.GetULong(minVal, maxVal).To<T>();
		}

		if (type == typeof(ushort))
		{
			var minVal = min.To<ushort>();
			var maxVal = max.To<ushort>();
			return () => RandomGen.GetUShort(minVal, maxVal).To<T>();
		}

		if (type == typeof(double))
		{
			var minVal = min.To<double>();
			var maxVal = max.To<double>();
			return () => RandomGen.GetDouble(minVal, maxVal).To<T>();
		}

		if (type == typeof(float))
		{
			var minVal = min.To<float>();
			var maxVal = max.To<float>();
			return () => RandomGen.GetFloat(minVal, maxVal).To<T>();
		}

		if (type == typeof(decimal))
		{
			var minVal = min.To<decimal>();
			var maxVal = max.To<decimal>();
			return () => RandomGen.GetDecimal(minVal, maxVal, 8).To<T>();
		}

		if (type == typeof(TimeSpan))
		{
			var minVal = min.To<TimeSpan>();
			var maxVal = max.To<TimeSpan>();
			return () => RandomGen.GetTime(minVal, maxVal).To<T>();
		}

		if (type == typeof(DateTime))
		{
			var minVal = min.To<DateTime>();
			var maxVal = max.To<DateTime>();
			return () =>
			{
				var ticks = minVal.Ticks + (long)(RandomGen.GetDouble() * (maxVal.Ticks - minVal.Ticks));
				return new DateTime(ticks).UtcKind().To<T>();
			};
		}

		throw new NotSupportedException($"Type {type.Name} is not supported for ranged random generation.");
	}
}