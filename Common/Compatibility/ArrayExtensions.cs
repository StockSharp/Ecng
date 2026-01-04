#if !NET8_0_OR_GREATER
namespace System;

using System.Linq;

/// <summary>
/// Compatibility extensions for arrays.
/// </summary>
public static class ArrayExtensions
{
	/// <summary>
	/// Returns a reversed copy of the specified array.
	/// </summary>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="array">The source array.</param>
	/// <returns>A new array with the elements in reverse order.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the array is null.</exception>
	public static T[] Reverse<T>(this T[] array)
	{
		return [.. Enumerable.Reverse(array)];
	}
}
#endif
