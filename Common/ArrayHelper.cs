namespace Ecng.Common;

using System;

/// <summary>
/// Utility class for common array operations.
/// </summary>
public static class ArrayHelper
{
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
		return Array.CreateInstance(type, count);
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