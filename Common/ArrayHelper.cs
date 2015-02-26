namespace Ecng.Common
{
	using System;

	/// <summary>
	/// Class-helper that provided some routine extension based methods.
	/// </summary>
	public static class ArrayHelper
	{
		private static class EmptyArrayHolder<T>
		{
			public static readonly T[] Array = new T[0];
		}

		public static T[] Empty<T>()
		{
			return EmptyArrayHolder<T>.Array;
		}

		/// <summary>
		/// Sets all of elements in the <see cref="Array"/> to zero, to false, or to null, depending on the element type.
		/// </summary>
		/// <param name="array">The <see cref="Array"/> whose elements need to be cleared.</param>
		public static void Clear(this Array array)
		{
			if (array == null)
				throw new ArgumentNullException("array");

			Clear(array, 0, array.Length);
		}

		/// <summary>
		/// Sets a range of elements in the <see cref="Array"/> to zero, to false, or to null, depending on the element type.
		/// </summary>
		/// <param name="array">The <see cref="Array"/> whose elements need to be cleared.</param>
		/// <param name="index">The starting index of the range of elements to clear.</param>
		/// <param name="count">The number of elements to clear.</param>
		public static void Clear(this Array array, int index, int count)
		{
			Array.Clear(array, index, count);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public static T[] Range<T>(this T[] array, int index)
		{
			if (array == null)
				throw new ArgumentNullException("array");

			return array.Range(index, array.Length - index);
		}

		public static T[] Range<T>(this T[] array, int index, int count)
		{
			var range = new T[count];
			Array.Copy(array, index, range, 0, count);
			return range;
		}

		/// <summary>
		/// Creates a one-dimensional <see cref="Array"/> of the specified <see cref="Type"/> and length, with zero-based indexing.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> of the <see cref="Array"/> to create.</param>
		/// <param name="count">The size of the System.Array to create.</param>
		/// <returns>A new one-dimensional <see cref="Array"/> of the specified <see cref="Type"/> with the specified length, using zero-based indexing.</returns>
		public static Array CreateArray(this Type type, int count)
		{
			return Array.CreateInstance(type, count);
		}

		/// <summary>
		/// Searches for the specified object and returns the index of the first occurrence within the entire one-dimensional <see cref="Array"/>.
		/// </summary>
		/// <typeparam name="T">Type of elements in the <see cref="Array"/>.</typeparam>
		/// <param name="array">The one-dimensional <see cref="Array"/> to search.</param>
		/// <param name="item">The object to locate in array.</param>
		/// <returns>The index of the first occurrence of value within the entire array, if found; otherwise, the lower bound of the array minus 1.</returns>
		public static int IndexOf<T>(this T[] array, T item)
		{
			return Array.IndexOf(array, item);
		}

		public static T[] Clone<T>(this T[] array)
		{
			if (array == null)
				throw new ArgumentNullException("array");

			return (T[])array.Clone();
		}

		public static T[] Reverse<T>(this T[] array)
		{
			if (array == null)
				throw new ArgumentNullException("array");

			var clone = (T[])array.Clone();
			Array.Reverse(clone);
			return clone;
		}

		public static T[] Concat<T>(this T[] first, T[] second)
		{
			if (first == null)
				throw new ArgumentNullException("first");

			if (second == null)
				throw new ArgumentNullException("second");

			var result = new T[first.Length + second.Length];

			if (result.Length == 0)
				return result;

			Array.Copy(first, result, first.Length);
			Array.Copy(second, 0, result, first.Length, second.Length);

			return result;
		}
	}
}