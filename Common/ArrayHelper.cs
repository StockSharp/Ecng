namespace Ecng.Common;

using System;
using System.Collections.Generic;

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
	/// Returns a subarray that starts at the specified index and extends to the end of the array.
	/// </summary>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="array">The source array.</param>
	/// <param name="index">The starting index of the subarray.</param>
	/// <returns>A new array containing the elements from the specified index to the end of the source array.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the array is null.</exception>
	public static T[] Range<T>(this T[] array, int index)
	{
		if (array is null)
			throw new ArgumentNullException(nameof(array));

		return array.Range(index, array.Length - index);
	}

	/// <summary>
	/// Returns a subarray that begins at the specified index and contains the specified number of elements.
	/// </summary>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="array">The source array.</param>
	/// <param name="index">The starting index of the subarray.</param>
	/// <param name="count">The number of elements to include in the subarray.</param>
	/// <returns>A new array containing the specified range of elements from the source array.</returns>
	public static T[] Range<T>(this T[] array, int index, int count)
	{
		var range = new T[count];
		Array.Copy(array, index, range, 0, count);
		return range;
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
	/// Creates a shallow copy of the specified array.
	/// </summary>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="array">The source array.</param>
	/// <returns>A new array that is a shallow copy of the source array.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the array is null.</exception>
	public static T[] Clone<T>(this T[] array)
	{
		if (array is null)
			throw new ArgumentNullException(nameof(array));

		return (T[])array.Clone();
	}

	/// <summary>
	/// Returns a reversed copy of the specified array.
	/// </summary>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="array">The source array.</param>
	/// <returns>A new array with the elements in reverse order.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the array is null.</exception>
	public static T[] Reverse<T>(this T[] array)
	{
		if (array is null)
			throw new ArgumentNullException(nameof(array));

		var clone = (T[])array.Clone();
		Array.Reverse(clone);
		return clone;
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

	/// <summary>
	/// Creates a copy of the source array.
	/// </summary>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="source">The source array.</param>
	/// <returns>A new array containing the same elements as the source array.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the source array is null.</exception>
	public static T[] CopyArray<T>(this T[] source)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		var copy = new T[source.Length];
		source.CopyTo(copy, 0);
		return copy;
	}

	/// <summary>
	/// Creates a copy of the elements from the specified collection into a new array.
	/// </summary>
	/// <typeparam name="T">The type of elements in the collection.</typeparam>
	/// <param name="source">The source collection.</param>
	/// <returns>A new array containing the elements of the source collection.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the source collection is null.</exception>
	public static T[] CopyArray<T>(this ICollection<T> source)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		var copy = new T[source.Count];
		source.CopyTo(copy, 0);
		return copy;
	}
}