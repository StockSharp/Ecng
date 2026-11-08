namespace Ecng.Common;

using System.Collections.Concurrent;

/// <summary>
/// Utility class for common array operations.
/// </summary>
public static class ArrayHelper
{
	private static readonly ConcurrentDictionary<Type, Func<int, Array>> _registeredArrayFactories = new();

	static ArrayHelper()
	{
		// Common arrays exist as closed generic instantiations in the native binary.
		// Avoid Array.CreateInstance(Type, int) for standard wire/message value types.
		RegisterArrayType<object>();
		RegisterArrayType<string>();
		RegisterArrayType<bool>();
		RegisterArrayType<byte>();
		RegisterArrayType<sbyte>();
		RegisterArrayType<char>();
		RegisterArrayType<short>();
		RegisterArrayType<ushort>();
		RegisterArrayType<int>();
		RegisterArrayType<uint>();
		RegisterArrayType<long>();
		RegisterArrayType<ulong>();
		RegisterArrayType<float>();
		RegisterArrayType<double>();
		RegisterArrayType<decimal>();
		RegisterArrayType<DateTime>();
		RegisterArrayType<DateTimeOffset>();
		RegisterArrayType<TimeSpan>();
		RegisterArrayType<Guid>();
	}

	/// <summary>
	/// Registers a statically compiled array factory for <typeparamref name="T"/>.
	/// This avoids runtime array type construction on Native AOT.
	/// </summary>
	public static void RegisterArrayType<T>()
		=> _registeredArrayFactories.TryAdd(typeof(T), static length => new T[length]);

	/// <summary>
	/// Registers a custom statically compiled factory for one-dimensional arrays.
	/// Only one factory may be registered for each element type.
	/// </summary>
	public static void RegisterArrayFactory<T>(Func<int, T[]> factory)
	{
		ArgumentNullException.ThrowIfNull(factory);

		if (!_registeredArrayFactories.TryAdd(typeof(T), length => factory(length)))
			throw new InvalidOperationException($"An array factory for '{typeof(T)}' is already registered.");
	}

	/// <summary>
	/// Sets all of the elements in the specified <see cref="Array"/> to zero, false, or null, depending on the element type.
	/// </summary>
	/// <param name="array">The array whose elements need to be cleared.</param>
	/// <exception cref="ArgumentNullException">Thrown when the array is null.</exception>
	public static void Clear(this Array array)
	{
		if (array is null)
			throw new ArgumentNullException(nameof(array));

		Clear(array, 0, array.Length);
	}

	/// <summary>
	/// Sets a range of elements in the specified <see cref="Array"/> to zero, false, or null, depending on the element type.
	/// </summary>
	/// <param name="array">The array whose elements need to be cleared.</param>
	/// <param name="index">The starting index of the range of elements to clear.</param>
	/// <param name="count">The number of elements to clear.</param>
	/// <exception cref="ArgumentNullException">Thrown when the array is null.</exception>
	public static void Clear(this Array array, int index, int count)
	{
		Array.Clear(array, index, count);
	}

	/// <summary>
	/// Creates a one-dimensional <see cref="Array"/> of the specified <see cref="Type"/> and length, with zero-based indexing.
	/// </summary>
	/// <param name="type">The type of the array to create.</param>
	/// <param name="count">The size of the array to create.</param>
	/// <returns>A new array of the specified type and length.</returns>
	public static Array CreateArray(this Type type, int count)
	{
		ArgumentNullException.ThrowIfNull(type);

		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count));

		return _registeredArrayFactories.TryGetValue(type, out var factory)
			? factory(count)
			: Array.CreateInstance(type, count);
	}

	/// <summary>
	/// Searches for the specified object and returns the index of the first occurrence within the entire one-dimensional array.
	/// </summary>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="array">The one-dimensional array to search.</param>
	/// <param name="item">The object to locate in the array.</param>
	/// <returns>
	/// The index of the first occurrence of value within the entire array, if found; otherwise, the lower bound of the array minus 1.
	/// </returns>
	public static int IndexOf<T>(this T[] array, T item)
	{
		return Array.IndexOf(array, item);
	}

	/// <summary>
	/// Concatenates two arrays into a single new array.
	/// </summary>
	/// <typeparam name="T">The type of elements in the arrays.</typeparam>
	/// <param name="first">The first array to concatenate.</param>
	/// <param name="second">The second array to concatenate.</param>
	/// <returns>A new array that contains the elements of the first array followed by the elements of the second array.</returns>
	/// <exception cref="ArgumentNullException">Thrown when either of the arrays is null.</exception>
	public static T[] Concat<T>(this T[] first, T[] second)
	{
		if (first is null)
			throw new ArgumentNullException(nameof(first));

		if (second is null)
			throw new ArgumentNullException(nameof(second));

		var result = new T[first.Length + second.Length];

		if (result.Length == 0)
			return result;

		Array.Copy(first, result, first.Length);
		Array.Copy(second, 0, result, first.Length, second.Length);

		return result;
	}

}