namespace Ecng.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Nito.AsyncEx;

using Ecng.Common;

#if NET9_0_OR_GREATER
using LockScope = System.Threading.Lock.Scope;
#else
using LockScope = Ecng.Common.SyncObject.Scope;
#endif

/// <summary>
/// Provides extension methods and helper utilities for working with collections.
/// </summary>
public static class CollectionHelper
{
	/// <summary>
	/// A private implementation of <see cref="IEqualityComparer{T}"/> using a custom equality comparison function.
	/// </summary>
	/// <typeparam name="T">The type of objects to compare.</typeparam>
	private sealed class EqualityComparer<T>(Func<T, T, bool> comparer) : IEqualityComparer<T>
	{
		private readonly Func<T, T, bool> _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));

		/// <summary>
		/// Determines whether two objects are equal using the provided comparison function.
		/// </summary>
		public bool Equals(T x, T y) => _comparer(x, y);

		/// <summary>
		/// Returns a hash code for the specified object.
		/// </summary>
		public int GetHashCode(T obj) => obj.GetHashCode();
	}

	/// <summary>
	/// A private implementation of <see cref="IComparer{T}"/> using a custom comparison function.
	/// </summary>
	/// <typeparam name="T">The type of objects to compare.</typeparam>
	private sealed class Comparer<T>(Func<T, T, int> comparer) : IComparer<T>
	{
		private readonly Func<T, T, int> _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));

		/// <summary>
		/// Compares two objects and returns a value indicating their relative order.
		/// </summary>
		public int Compare(T x, T y) => _comparer(x, y);
	}

	/// <summary>
	/// Converts a function to an <see cref="IEqualityComparer{T}"/> instance.
	/// </summary>
	/// <typeparam name="T">The type of objects to compare.</typeparam>
	/// <param name="comparer">The function to determine equality between two objects.</param>
	/// <returns>An <see cref="IEqualityComparer{T}"/> instance.</returns>
	public static IEqualityComparer<T> ToComparer<T>(this Func<T, T, bool> comparer)
	{
		return new EqualityComparer<T>(comparer);
	}

	/// <summary>
	/// Converts a function to an <see cref="IComparer{T}"/> instance.
	/// </summary>
	/// <typeparam name="T">The type of objects to compare.</typeparam>
	/// <param name="comparer">The function to compare two objects.</param>
	/// <returns>An <see cref="IComparer{T}"/> instance.</returns>
	public static IComparer<T> ToComparer<T>(this Func<T, T, int> comparer)
	{
		return new Comparer<T>(comparer);
	}

	/// <summary>
	/// Converts a <see cref="Comparison{T}"/> delegate to an <see cref="IComparer{T}"/> instance.
	/// </summary>
	/// <typeparam name="T">The type of objects to compare.</typeparam>
	/// <param name="comparer">The comparison delegate.</param>
	/// <returns>An <see cref="IComparer{T}"/> instance.</returns>
	public static IComparer<T> ToComparer<T>(this Comparison<T> comparer)
	{
		return comparer.ToFunc().ToComparer();
	}

	/// <summary>
	/// Converts a <see cref="Comparison{T}"/> delegate to a function.
	/// </summary>
	/// <typeparam name="T">The type of objects to compare.</typeparam>
	/// <param name="comparer">The comparison delegate.</param>
	/// <returns>A function that compares two objects.</returns>
	public static Func<T, T, int> ToFunc<T>(this Comparison<T> comparer)
	{
		return (t1, t2) => comparer(t1, t2);
	}

	/// <summary>
	/// Determines whether two sequences are equal using a custom equality comparer function.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequences.</typeparam>
	/// <param name="first">The first sequence to compare.</param>
	/// <param name="second">The second sequence to compare.</param>
	/// <param name="comparer">The function to determine equality between elements.</param>
	/// <returns>True if the sequences are equal; otherwise, false.</returns>
	public static bool SequenceEqual<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, T, bool> comparer)
	{
		return first.SequenceEqual(second, comparer.ToComparer());
	}

	/// <summary>
	/// Sorts a collection using a <see cref="Comparison{T}"/> delegate.
	/// </summary>
	/// <typeparam name="T">The type of elements in the collection.</typeparam>
	/// <param name="collection">The collection to sort.</param>
	/// <param name="comparison">The comparison delegate to determine order.</param>
	/// <returns>An ordered enumerable of the collection.</returns>
	public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> collection, Comparison<T> comparison)
	{
		return collection.OrderBy(item => item, comparison.ToComparer());
	}

	/// <summary>
	/// Returns the index of the first element in a sequence that satisfies a specified condition.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="source">The sequence to search.</param>
	/// <param name="predicate">The condition to test each element against.</param>
	/// <returns>The zero-based index of the first matching element, or -1 if no element is found.</returns>
	public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
	{
		var index = 0;

		foreach (var t in source)
		{
			if (predicate(t))
				return index;

			index++;
		}

		return -1;
	}

	/// <summary>
	/// Adds a range of values to a collection, ensuring no duplicates if the collection already contains them.
	/// </summary>
	/// <typeparam name="T">The type of elements in the collection.</typeparam>
	/// <param name="collection">The collection to add values to.</param>
	/// <param name="values">The values to add.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
	public static void TryAdd<T>(this ICollection<T> collection, IEnumerable<T> values)
	{
		if (values is null)
			throw new ArgumentNullException(nameof(values));

		void InternalTryAdd()
		{
			foreach (var value in values)
			{
				if (!collection.Contains(value))
					collection.Add(value);
			}
		}

		if (collection is ISynchronizedCollection sync)
		{
			using (sync.EnterScope())
				InternalTryAdd();
		}
		else
		{
			InternalTryAdd();
		}
	}

	/// <summary>
	/// Adds a value to a collection if it does not already exist.
	/// </summary>
	/// <typeparam name="T">The type of elements in the collection.</typeparam>
	/// <param name="collection">The collection to add the value to.</param>
	/// <param name="value">The value to add.</param>
	/// <returns>True if the value was added; false if it already existed.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is null.</exception>
	public static bool TryAdd<T>(this ICollection<T> collection, T value)
	{
		if (collection is null)
			throw new ArgumentNullException(nameof(collection));

		bool InternalTryAdd()
		{
			if (collection.Contains(value))
				return false;

			collection.Add(value);
			return true;
		}

		if (collection is ISynchronizedCollection sync)
		{
			using (sync.EnterScope())
				return InternalTryAdd();
		}

		return InternalTryAdd();
	}

	/// <summary>
	/// Adds a key-value pair to a dictionary if the key does not already exist.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="dict">The dictionary to add the pair to.</param>
	/// <param name="key">The key to add.</param>
	/// <param name="value">The value to add.</param>
	/// <returns>True if the pair was added; false if the key already existed.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="dict"/> is null.</exception>
	public static bool TryAdd2<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
	{
		if (dict is null)
			throw new ArgumentNullException(nameof(dict));

		bool InternalTryAdd()
		{
			if (dict.ContainsKey(key))
				return false;

			dict.Add(key, value);
			return true;
		}

		if (dict is ISynchronizedCollection sync)
		{
			using (sync.EnterScope())
				return InternalTryAdd();
		}

		return InternalTryAdd();
	}

	/// <summary>
	/// Concatenates two collections of the same type into a new instance.
	/// </summary>
	/// <typeparam name="T">The type of the collection, which must implement <see cref="ICollection{TItem}"/> and have a parameterless constructor.</typeparam>
	/// <typeparam name="TItem">The type of elements in the collection.</typeparam>
	/// <param name="first">The first collection.</param>
	/// <param name="second">The second collection.</param>
	/// <returns>A new collection containing all elements from both input collections.</returns>
	public static T ConcatEx<T, TItem>(this T first, T second)
		where T : ICollection<TItem>, new()
	{
		var retVal = new T();
		retVal.AddRange(first.Concat(second));
		return retVal;
	}

	/// <summary>
	/// Adds a range of items to a collection, using optimized methods when available.
	/// </summary>
	/// <typeparam name="T">The type of elements in the collection.</typeparam>
	/// <param name="source">The collection to add items to.</param>
	/// <param name="items">The items to add.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="items"/> is null.</exception>
	public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> items)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (items is null)
			throw new ArgumentNullException(nameof(items));

		if (source is List<T> list)
			list.AddRange(items);
		else if (source is ICollectionEx<T> ex)
			ex.AddRange(items);
		else if (source is ISet<T> set)
			set.UnionWith(items);
		else
		{
			foreach (var item in items)
				source.Add(item);
		}
	}

	/// <summary>
	/// Removes a range of items from a collection, using optimized methods when available.
	/// </summary>
	/// <typeparam name="T">The type of elements in the collection.</typeparam>
	/// <param name="source">The collection to remove items from.</param>
	/// <param name="items">The items to remove.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
	public static void RemoveRange<T>(this ICollection<T> source, IEnumerable<T> items)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));

		if (source is ICollectionEx<T> ex)
			ex.RemoveRange(items);
		else
			items.ForEach(i => source.Remove(i));
	}

	/// <summary>
	/// Removes items from a list that match a specified filter and adjusts the list size.
	/// </summary>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="list">The list to remove items from.</param>
	/// <param name="filter">The condition to identify items to remove.</param>
	/// <returns>The number of items removed.</returns>
	public static int RemoveWhere2<T>(this IList<T> list, Func<T, bool> filter)
	{
		// https://referencesource.microsoft.com/#mscorlib/system/collections/generic/list.cs,82567b42bbfc416e,references

		var newLen = 0;
		var len = list.Count;

		// Find the first item which needs to be removed.
		while(newLen < len && !filter(list[newLen]))
			newLen++;

		if(newLen >= len)
			return 0;

		var current = newLen + 1;

		while(current < len)
		{
			// Find the first item which needs to be kept.
			while(current < len && filter(list[current]))
				current++;

			if(current < len) {
				// copy item to the free slot.
				list[newLen++] = list[current++];
			}
		}

		while(list.Count > newLen)
			list.RemoveAt(list.Count - 1);

		return len - newLen;
	}

	/// <summary>
	/// Removes items from a collection that match a specified filter and returns the removed items.
	/// </summary>
	/// <typeparam name="T">The type of elements in the collection.</typeparam>
	/// <param name="collection">The collection to remove items from.</param>
	/// <param name="filter">The condition to identify items to remove.</param>
	/// <returns>An enumerable of the removed items.</returns>
	public static IEnumerable<T> RemoveWhere<T>(this ICollection<T> collection, Func<T, bool> filter)
	{
		var removingItems = collection.Where(filter).ToArray();

		foreach (var t in removingItems)
			collection.Remove(t);

		return removingItems;
	}

	/// <summary>
	/// Flattens a sequence of sequences into a single sequence.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequences.</typeparam>
	/// <param name="values">The sequence of sequences to flatten.</param>
	/// <returns>A single sequence containing all elements from the input sequences.</returns>
	public static IEnumerable<T> SelectMany<T>(this IEnumerable<IEnumerable<T>> values)
	{
		return values.SelectMany(value => value);
	}

	/// <summary>
	/// Orders a sequence of values in ascending order using the default comparer.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="values">The sequence to order.</param>
	/// <returns>An ordered sequence of the input values.</returns>
	public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> values)
	{
		return values.OrderBy(value => value);
	}

	/// <summary>
	/// Orders a sequence of values in descending order using the default comparer.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="values">The sequence to order.</param>
	/// <returns>An ordered sequence of the input values in descending order.</returns>
	public static IEnumerable<T> OrderByDescending<T>(this IEnumerable<T> values)
	{
		return values.OrderByDescending(value => value);
	}

	/// <summary>
	/// Computes a hash code for a sequence of elements.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="collection">The sequence to compute the hash code for.</param>
	/// <returns>A hash code for the sequence.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is null.</exception>
	public static int GetHashCodeEx<T>(this IEnumerable<T> collection)
	{
		if (collection is null)
			throw new ArgumentNullException(nameof(collection));

#if NETSTANDARD2_0
		unchecked
		{
			var hash = 17;

			foreach (var item in collection)
				hash = (hash * 31) + (item?.GetHashCode() ?? 0);

			return hash;
		}
#else
		var hc = new HashCode();

		foreach (var item in collection)
			hc.Add(item);

		return hc.ToHashCode();
#endif
	}

	/// <summary>
	/// Determines whether a sequence contains any null items.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence, which must be a reference type.</typeparam>
	/// <param name="items">The sequence to check.</param>
	/// <returns>True if the sequence contains null; otherwise, false.</returns>
	public static bool HasNullItem<T>(this IEnumerable<T> items)
		where T : class
	{
		return items.Contains(null);
	}

	/// <summary>
	/// Copies all items from a collection to an array and clears the collection.
	/// </summary>
	/// <typeparam name="T">The type of elements in the collection.</typeparam>
	/// <param name="items">The collection to copy and clear.</param>
	/// <returns>An array containing the copied items.</returns>
	public static T[] CopyAndClear<T>(this ICollection<T> items)
	{
		T[] InternalCopyAndClear()
		{
			var retVal = items.ToArray();
			items.Clear();
			return retVal;
		}

		if (items is not ISynchronizedCollection sync) return InternalCopyAndClear();

		using (sync.EnterScope())
			return InternalCopyAndClear();
	}

	/// <summary>
	/// Retrieves a value from a dictionary by key and removes the key-value pair.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="dict">The dictionary to operate on.</param>
	/// <param name="key">The key of the value to retrieve and remove.</param>
	/// <returns>The value associated with the key.</returns>
	public static TValue GetAndRemove<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
	{
		TValue InternalGetAndRemove()
		{
			var value = dict[key];
			dict.Remove(key);
			return value;
		}

		if (dict is not ISynchronizedCollection sync) return InternalGetAndRemove();

		using (sync.EnterScope())
			return InternalGetAndRemove();
	}

	/// <summary>
	/// Attempts to retrieve a value from a dictionary by key and remove it, returning null if not found (for value types).
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary, which must be a value type.</typeparam>
	/// <param name="dict">The dictionary to operate on.</param>
	/// <param name="key">The key of the value to retrieve and remove.</param>
	/// <returns>The value if found and removed; otherwise, null.</returns>
	public static TValue? TryGetAndRemove2<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
		where TValue : struct
	{
		return dict.TryGetAndRemove(key, out var value) ? value : (TValue?)null;
	}

	/// <summary>
	/// Attempts to retrieve a value from a dictionary by key and remove it, returning the default value if not found.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="dict">The dictionary to operate on.</param>
	/// <param name="key">The key of the value to retrieve and remove.</param>
	/// <returns>The value if found and removed; otherwise, the default value of <typeparamref name="TValue"/>.</returns>
	public static TValue TryGetAndRemove<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
	{
		return dict.TryGetAndRemove(key, out var value) ? value : default;
	}

	/// <summary>
	/// Attempts to retrieve a value from a dictionary by key and remove it, indicating success.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="dict">The dictionary to operate on.</param>
	/// <param name="key">The key of the value to retrieve and remove.</param>
	/// <param name="value">When this method returns, contains the value if found; otherwise, the default value.</param>
	/// <returns>True if the value was found and removed; otherwise, false.</returns>
	public static bool TryGetAndRemove<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, out TValue value)
	{
		bool InternalTryGetAndRemove(out TValue value2)
		{
			if (!dict.TryGetValue(key, out value2))
				return false;

			dict.Remove(key);
			return true;
		}

		if (dict is ISynchronizedCollection sync)
		{
			using (sync.EnterScope())
				return InternalTryGetAndRemove(out value);
		}

		return InternalTryGetAndRemove(out value);
	}

	/// <summary>
	/// Retrieves an element from a sequence counting from the end.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="source">The sequence to query.</param>
	/// <param name="index">The zero-based index from the end (0 is the last element).</param>
	/// <returns>The element at the specified position from the end.</returns>
	public static T ElementAtFromEnd<T>(this IEnumerable<T> source, int index)
	{
		return source.ElementAt(source.GetIndexFromEnd(index));
	}

	/// <summary>
	/// Retrieves an element from a sequence counting from the end, or the default value if out of range.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="source">The sequence to query.</param>
	/// <param name="index">The zero-based index from the end (0 is the last element).</param>
	/// <returns>The element at the specified position from the end, or the default value if out of range.</returns>
	public static T ElementAtFromEndOrDefault<T>(this IEnumerable<T> source, int index)
	{
		return source.ElementAtOrDefault(source.GetIndexFromEnd(index));
	}

	/// <summary>
	/// Calculates the index from the start of a sequence given an index from the end.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="source">The sequence to query.</param>
	/// <param name="index">The zero-based index from the end.</param>
	/// <returns>The zero-based index from the start.</returns>
	private static int GetIndexFromEnd<T>(this IEnumerable<T> source, int index)
	{
		return source.Count() - 1 - index;
	}

	/// <summary>
	/// Retrieves an element from a linked list counting from the end.
	/// </summary>
	/// <typeparam name="T">The type of elements in the linked list.</typeparam>
	/// <param name="list">The linked list to query.</param>
	/// <param name="index">The zero-based index from the end (0 is the last element).</param>
	/// <returns>The element at the specified position from the end.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="list"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the list is empty or the index is out of range.</exception>
	public static T ElementAtFromEnd<T>(this LinkedList<T> list, int index)
	{
		if (list is null)
			throw new ArgumentNullException(nameof(list));

		if (list.IsEmpty())
			throw new ArgumentOutOfRangeException(nameof(list));

		var curr = list.Last;

		while (index > 0)
		{
			curr = curr.Previous;
			index--;

			if (curr is null)
				throw new ArgumentOutOfRangeException(nameof(list));
		}

		return curr.Value;
	}

	/// <summary>
	/// Retrieves an element from a synchronized linked list counting from the end.
	/// </summary>
	/// <typeparam name="T">The type of elements in the linked list.</typeparam>
	/// <param name="list">The synchronized linked list to query.</param>
	/// <param name="index">The zero-based index from the end (0 is the last element).</param>
	/// <returns>The element at the specified position from the end.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="list"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the list is empty or the index is out of range.</exception>
	public static T ElementAtFromEnd<T>(this SynchronizedLinkedList<T> list, int index)
	{
		if (list is null)
			throw new ArgumentNullException(nameof(list));

		if (list.IsEmpty())
			throw new ArgumentOutOfRangeException(nameof(list));

		var curr = list.Last;

		while (index > 0)
		{
			curr = curr.Previous;
			index--;

			if (curr is null)
				throw new ArgumentOutOfRangeException(nameof(list));
		}

		return curr.Value;
	}

	/// <summary>
	/// Retrieves an element from a linked list counting from the end, or the default value if out of range.
	/// </summary>
	/// <typeparam name="T">The type of elements in the linked list.</typeparam>
	/// <param name="list">The linked list to query.</param>
	/// <param name="index">The zero-based index from the end (0 is the last element).</param>
	/// <returns>The element at the specified position from the end, or the default value if out of range.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="list"/> is null.</exception>
	public static T ElementAtFromEndOrDefault<T>(this LinkedList<T> list, int index)
	{
		if (list is null)
			throw new ArgumentNullException(nameof(list));

		var curr = list.Last;

		while (index > 0 && curr != null)
		{
			curr = curr.Previous;
			index--;
		}

		return curr is null ? default : curr.Value;
	}

	/// <summary>
	/// Retrieves an element from a synchronized linked list counting from the end, or the default value if out of range.
	/// </summary>
	/// <typeparam name="T">The type of elements in the linked list.</typeparam>
	/// <param name="list">The synchronized linked list to query.</param>
	/// <param name="index">The zero-based index from the end (0 is the last element).</param>
	/// <returns>The element at the specified position from the end, or the default value if out of range.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="list"/> is null.</exception>
	public static T ElementAtFromEndOrDefault<T>(this SynchronizedLinkedList<T> list, int index)
	{
		if (list is null)
			throw new ArgumentNullException(nameof(list));

		var curr = list.Last;

		while (index > 0 && curr != null)
		{
			curr = curr.Previous;
			index--;
		}

		return curr is null ? default : curr.Value;
	}

	/// <summary>
	/// Converts a sequence of key-value pairs into a <see cref="PairSet{TKey, TValue}"/> with the default equality comparer.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="source">The sequence of key-value pairs.</param>
	/// <returns>A <see cref="PairSet{TKey, TValue}"/> containing the key-value pairs.</returns>
	public static PairSet<TKey, TValue> ToPairSet<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
	{
		return source.ToPairSet(System.Collections.Generic.EqualityComparer<TKey>.Default);
	}

	/// <summary>
	/// Converts a sequence of key-value pairs into a <see cref="PairSet{TKey, TValue}"/> with a specified equality comparer.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="source">The sequence of key-value pairs.</param>
	/// <param name="comparer">The equality comparer for keys.</param>
	/// <returns>A <see cref="PairSet{TKey, TValue}"/> containing the key-value pairs.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
	public static PairSet<TKey, TValue> ToPairSet<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> comparer)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		var set = new PairSet<TKey, TValue>(comparer);

		foreach (var item in source)
		{
			set.Add(item.Key, item.Value);
		}

		return set;
	}

	/// <summary>
	/// Converts a sequence into a <see cref="PairSet{TKey, TValue}"/> using selector functions for keys and values.
	/// </summary>
	/// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="source">The source sequence.</param>
	/// <param name="keySelector">The function to extract keys from elements.</param>
	/// <param name="valueSelector">The function to extract values from elements.</param>
	/// <returns>A <see cref="PairSet{TKey, TValue}"/> containing the transformed key-value pairs.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/>, <paramref name="keySelector"/>, or <paramref name="valueSelector"/> is null.</exception>
	public static PairSet<TKey, TValue> ToPairSet<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, int, TKey> keySelector, Func<TSource, int, TValue> valueSelector)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (keySelector is null)
			throw new ArgumentNullException(nameof(keySelector));

		if (valueSelector is null)
			throw new ArgumentNullException(nameof(valueSelector));

		var set = new PairSet<TKey, TValue>();

		var index = 0;

		foreach (var item in source)
		{
			set.Add(keySelector(item, index), valueSelector(item, index));
			index++;
		}

		return set;
	}

	#region Dictionary Methods

	/// <summary>
	/// Copies key-value pairs from a sequence to a dictionary.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="source">The sequence of key-value pairs.</param>
	/// <param name="destination">The dictionary to copy to.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is null.</exception>
	public static void CopyTo<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IDictionary<TKey, TValue> destination)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (destination is null)
			throw new ArgumentNullException(nameof(destination));

		foreach (var pair in source)
			destination.Add(pair);
	}

	/// <summary>
	/// Converts a non-generic dictionary to a strongly-typed dictionary.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="dictionary">The non-generic dictionary.</param>
	/// <returns>A strongly-typed dictionary with the same key-value pairs.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> is null.</exception>
	public static IDictionary<TKey, TValue> TypedAs<TKey, TValue>(this IDictionary dictionary)
	{
		if (dictionary is null)
			throw new ArgumentNullException(nameof(dictionary));

		return dictionary.Cast<DictionaryEntry>().ToDictionary(item => item.Key.To<TKey>(), item => item.Value.To<TValue>());
	}

#if NET8_0_OR_GREATER == false
	/// <summary>
	/// Converts a sequence of key-value pairs to a dictionary using the default equality comparer.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="source">The sequence of key-value pairs.</param>
	/// <returns>A dictionary containing the key-value pairs.</returns>
	public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
	{
		return source.ToDictionary(pair => pair.Key, pair => pair.Value);
	}
#endif

	/// <summary>
	/// Converts a sequence of key-value pairs to a dictionary with a specified equality comparer.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="source">The sequence of key-value pairs.</param>
	/// <param name="comparer">The equality comparer for keys.</param>
	/// <returns>A dictionary containing the key-value pairs.</returns>
	public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> comparer)
	{
		return source.ToDictionary(pair => pair.Key, pair => pair.Value, comparer);
	}

	/// <summary>
	/// Converts a sequence of tuples to a dictionary using the default equality comparer.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="source">The sequence of tuples.</param>
	/// <returns>A dictionary containing the key-value pairs from the tuples.</returns>
	public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<Tuple<TKey, TValue>> source)
	{
		return source.ToDictionary(pair => pair.Item1, pair => pair.Item2);
	}

	/// <summary>
	/// Converts a sequence of tuples to a dictionary with a specified equality comparer.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="source">The sequence of tuples.</param>
	/// <param name="comparer">The equality comparer for keys.</param>
	/// <returns>A dictionary containing the key-value pairs from the tuples.</returns>
	public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<Tuple<TKey, TValue>> source, IEqualityComparer<TKey> comparer)
	{
		return source.ToDictionary(pair => pair.Item1, pair => pair.Item2, comparer);
	}

	/// <summary>
	/// Converts a key-value pair to a tuple.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <param name="pair">The key-value pair to convert.</param>
	/// <returns>A tuple containing the key and value.</returns>
	public static Tuple<TKey, TValue> ToTuple<TKey, TValue>(this KeyValuePair<TKey, TValue> pair)
	{
		return Tuple.Create(pair.Key, pair.Value);
	}

	/// <summary>
	/// Converts a tuple to a key-value pair.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <param name="pair">The tuple to convert.</param>
	/// <returns>A key-value pair containing the tuple's items.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="pair"/> is null.</exception>
	public static KeyValuePair<TKey, TValue> ToPair<TKey, TValue>(this Tuple<TKey, TValue> pair)
	{
		if (pair is null)
			throw new ArgumentNullException(nameof(pair));

		return new KeyValuePair<TKey, TValue>(pair.Item1, pair.Item2);
	}

	/// <summary>
	/// Converts a sequence into a dictionary using selector functions for keys and values.
	/// </summary>
	/// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="source">The source sequence.</param>
	/// <param name="keySelector">The function to extract keys from elements.</param>
	/// <param name="valueSelector">The function to extract values from elements.</param>
	/// <returns>A dictionary containing the transformed key-value pairs.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/>, <paramref name="keySelector"/>, or <paramref name="valueSelector"/> is null.</exception>
	public static IDictionary<TKey, TValue> ToDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, int, TKey> keySelector, Func<TSource, int, TValue> valueSelector)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (keySelector is null)
			throw new ArgumentNullException(nameof(keySelector));

		if (valueSelector is null)
			throw new ArgumentNullException(nameof(valueSelector));

		var dict = new Dictionary<TKey, TValue>();

		var index = 0;

		foreach (var item in source)
		{
			dict.Add(keySelector(item, index), valueSelector(item, index));
			index++;
		}

		return dict;
	}

	/// <summary>
	/// Converts a grouping into a dictionary where each key maps to its group of values.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="grouping">The grouping to convert.</param>
	/// <returns>A dictionary mapping keys to their groups.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="grouping"/> is null.</exception>
	public static IDictionary<TKey, IEnumerable<TValue>> ToDictionary<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> grouping)
	{
		if (grouping is null)
			throw new ArgumentNullException(nameof(grouping));

		return grouping.ToDictionary(g => g.Key, g => (IEnumerable<TValue>)g);
	}

	/// <summary>
	/// Retrieves all keys in a dictionary that map to a specific value.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="dictionary">The dictionary to query.</param>
	/// <param name="value">The value to match.</param>
	/// <returns>An enumerable of keys associated with the specified value.</returns>
	public static IEnumerable<TKey> GetKeys<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TValue value)
	{
		return from pair in dictionary where pair.Value.Equals(value) select pair.Key;
	}

	/// <summary>
	/// Adds a new key to a dictionary with a default value if it doesn't exist, and returns the value.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="dictionary">The dictionary to operate on.</param>
	/// <param name="key">The key to add or retrieve.</param>
	/// <returns>The existing or newly created value.</returns>
	public static TValue SafeAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
	{
		return dictionary.SafeAdd(key, out _);
	}

	/// <summary>
	/// A private cache class to provide fast activation of default instances for <see cref="SafeAdd{TKey, TValue}(IDictionary{TKey, TValue}, TKey, out bool)"/>.
	/// </summary>
	private static class FastActivatorCache<TKey, TValue>
	{
		public static readonly Func<TKey, TValue> Activator;

		static FastActivatorCache()
		{
			Activator = k => FastActivator<TValue>.CreateObject();
		}
	}

	/// <summary>
	/// Adds a new key to a dictionary with a default value if it doesn't exist, indicating whether it was new.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="dictionary">The dictionary to operate on.</param>
	/// <param name="key">The key to add or retrieve.</param>
	/// <param name="isNew">When this method returns, indicates whether the key was newly added.</param>
	/// <returns>The existing or newly created value.</returns>
	public static TValue SafeAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out bool isNew)
	{
		return dictionary.SafeAdd(key, FastActivatorCache<TKey,TValue>.Activator, out isNew);
	}

	/// <summary>
	/// Adds a new key to a dictionary with a value generated by a handler if it doesn't exist.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="dictionary">The dictionary to operate on.</param>
	/// <param name="key">The key to add or retrieve.</param>
	/// <param name="handler">The function to generate a value if the key is new.</param>
	/// <returns>The existing or newly created value.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> or <paramref name="handler"/> is null.</exception>
	public static TValue SafeAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> handler)
	{
		return dictionary.SafeAdd(key, handler, out _);
	}

	/// <summary>
	/// Adds a new key to a dictionary with a value generated by a handler if it doesn't exist, indicating whether it was new.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="dictionary">The dictionary to operate on.</param>
	/// <param name="key">The key to add or retrieve.</param>
	/// <param name="handler">The function to generate a value if the key is new.</param>
	/// <param name="isNew">When this method returns, indicates whether the key was newly added.</param>
	/// <returns>The existing or newly created value.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> or <paramref name="handler"/> is null.</exception>
	public static TValue SafeAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> handler, out bool isNew)
	{
		if (dictionary is null)
			throw new ArgumentNullException(nameof(dictionary));

		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		isNew = false;

		if (!dictionary.TryGetValue(key, out var value))
		{
			var l = (dictionary as ISynchronizedCollection)?.SyncRoot;
			SyncScope syncObj = l is null ? new(dictionary) : new(l);

			using (syncObj)
			{
				if (!dictionary.TryGetValue(key, out value))
				{
					value = handler(key);
					dictionary.Add(key, value);

					isNew = true;
				}
			}
		}

		return value;
	}

	/// <summary>
	/// Asynchronously adds a new key to a dictionary with a value generated by a handler if it doesn't exist, using a reader-writer lock.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="dictionary">The dictionary to operate on.</param>
	/// <param name="sync">The reader-writer lock for synchronization.</param>
	/// <param name="key">The key to add or retrieve.</param>
	/// <param name="handler">The asynchronous function to generate a value if the key is new.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <returns>The existing or newly created value.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/>, <paramref name="sync"/>, or <paramref name="handler"/> is null.</exception>
	[CLSCompliant(false)]
	public static async Task<TValue> SafeAddAsync<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, AsyncReaderWriterLock sync, TKey key, Func<TKey, CancellationToken, Task<TValue>> handler, CancellationToken cancellationToken)
	{
		if (dictionary is null)
			throw new ArgumentNullException(nameof(dictionary));

		if (sync is null)
			throw new ArgumentNullException(nameof(sync));

		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		TValue value;

		using (await sync.ReaderLockAsync(cancellationToken))
		{
			if (dictionary.TryGetValue(key, out value))
				return value;
		}

		value = await handler(key, cancellationToken);

		using var _ = await sync.WriterLockAsync(cancellationToken);

		if (dictionary.TryGetValue(key, out var temp))
			return temp;

		dictionary.Add(key, value);

		return value;
	}

	/// <summary>
	/// Asynchronously adds a new key to a dictionary of task completion sources, ensuring only one task is created per key, using a reader-writer lock.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="dictionary">The dictionary mapping keys to task completion sources.</param>
	/// <param name="sync">The reader-writer lock for synchronization.</param>
	/// <param name="key">The key to add or retrieve.</param>
	/// <param name="handler">The asynchronous function to generate a value if the key is new.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <returns>The task representing the value for the key.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/>, <paramref name="sync"/>, or <paramref name="handler"/> is null.</exception>
	[CLSCompliant(false)]
	public static async Task<TValue> SafeAddAsync<TKey, TValue>(this IDictionary<TKey, TaskCompletionSource<TValue>> dictionary, AsyncReaderWriterLock sync, TKey key, Func<TKey, CancellationToken, Task<TValue>> handler, CancellationToken cancellationToken)
	{
		if (dictionary is null)
			throw new ArgumentNullException(nameof(dictionary));

		if (sync is null)
			throw new ArgumentNullException(nameof(sync));

		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		async Task<Task<TValue>> InternalSafeAddAsync()
		{
			TaskCompletionSource<TValue> source;

			using (await sync.ReaderLockAsync(cancellationToken))
			{
				if (dictionary.TryGetValue(key, out source))
					return source.Task;
			}

			using (await sync.WriterLockAsync(cancellationToken))
			{
				if (dictionary.TryGetValue(key, out source))
					return source.Task;

				source = new TaskCompletionSource<TValue>();
				_ = Task.Factory.StartNew(async () => source.SetResult(await handler(key, cancellationToken)));

				dictionary.Add(key, source);
				return source.Task;
			}
		}

		return await (await InternalSafeAddAsync());
	}

	/// <summary>
	/// Asynchronously adds a new key to a dictionary of task completion sources, ensuring only one task is created per key, using synchronized locking.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="dictionary">The dictionary mapping keys to task completion sources.</param>
	/// <param name="key">The key to add or retrieve.</param>
	/// <param name="handler">The asynchronous function to generate a value if the key is new.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <returns>The task representing the value for the key.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> or <paramref name="handler"/> is null.</exception>
	public static Task<TValue> SafeAddAsync<TKey, TValue>(this IDictionary<TKey, TaskCompletionSource<TValue>> dictionary, TKey key, Func<TKey, CancellationToken, Task<TValue>> handler, CancellationToken cancellationToken)
	{
		if (dictionary is null)
			throw new ArgumentNullException(nameof(dictionary));

		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		var l = (dictionary as ISynchronizedCollection)?.SyncRoot;

		TaskCompletionSource<TValue> source;

		using (l is null ? new SyncScope(dictionary) : new(l))
		{
			if (dictionary.TryGetValue(key, out source))
				return source.Task;

			source = new();
			dictionary.Add(key, source);
		}
		
		void remove()
		{
			using SyncScope syncObj = l is null ? new(dictionary) : new(l);
			dictionary.Remove(key);
		}

		try
		{
			handler(key, cancellationToken).ContinueWith(t =>
			{
				if (t.IsFaulted || t.IsCanceled)
					remove();

				source.TryCompleteFromCompletedTask(t);

			}, TaskContinuationOptions.ExecuteSynchronously);
		}
		catch
		{
			remove();
			throw;
		}

		return source.Task;
	}

	/// <summary>
	/// Attempts to retrieve a value from a dictionary by key, returning the default value if not found.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="dict">The dictionary to query.</param>
	/// <param name="key">The key to look up.</param>
	/// <returns>The value if found; otherwise, the default value of <typeparamref name="TValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="dict"/> is null.</exception>
	public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
		//where V : class
	{
		if (dict is null)
			throw new ArgumentNullException(nameof(dict));

		dict.TryGetValue(key, out var value);
		return value;
	}

	/// <summary>
	/// Attempts to retrieve a value from a dictionary by key, returning null if not found (for value types).
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values, which must be a value type.</typeparam>
	/// <param name="dict">The dictionary to query.</param>
	/// <param name="key">The key to look up.</param>
	/// <returns>The value if found; otherwise, null.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="dict"/> is null.</exception>
	public static TValue? TryGetValue2<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
		where TValue : struct
	{
		if (dict is null)
			throw new ArgumentNullException(nameof(dict));

		if (dict.TryGetValue(key, out var value))
			return value;
		else
			return null;
	}

	/// <summary>
	/// Attempts to retrieve a key from a pair set by value, returning the default value if not found.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="pairSet">The pair set to query.</param>
	/// <param name="value">The value to look up.</param>
	/// <returns>The key if found; otherwise, the default value of <typeparamref name="TKey"/>.</returns>
	public static TKey TryGetKey<TKey, TValue>(this PairSet<TKey, TValue> pairSet, TValue value)
	{
		pairSet.TryGetKey(value, out var key);
		return key;
	}

	/// <summary>
	/// Attempts to retrieve a key from a pair set by value, returning null if not found (for value-type keys).
	/// </summary>
	/// <typeparam name="TKey">The type of keys, which must be a value type.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="pairSet">The pair set to query.</param>
	/// <param name="value">The value to look up.</param>
	/// <returns>The key if found; otherwise, null.</returns>
	public static TKey? TryGetKey2<TKey, TValue>(this PairSet<TKey, TValue> pairSet, TValue value)
		where TKey : struct
	{
		if (pairSet.TryGetKey(value, out var key))
			return key;
		else
			return null;
	}

	#endregion

	/// <summary>
	/// Enters a synchronized scope for thread-safe operations on the collection.
	/// </summary>
	/// <param name="collection"><see cref="ISynchronizedCollection"/></param>
	/// <returns>A <see cref="LockScope"/> that represents the synchronized scope.</returns>
	public static LockScope EnterScope(this ISynchronizedCollection collection)
		=> collection.CheckOnNull(nameof(collection)).SyncRoot.EnterScope();

	/// <summary>
	/// Executes a function on a synchronized collection with thread-safe access.
	/// </summary>
	/// <typeparam name="TCollection">The type of the synchronized collection.</typeparam>
	/// <typeparam name="TResult">The type of the result.</typeparam>
	/// <param name="collection">The synchronized collection to operate on.</param>
	/// <param name="func">The function to execute.</param>
	/// <returns>The result of the function.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> or <paramref name="func"/> is null.</exception>
	public static TResult SyncGet<TCollection, TResult>(this TCollection collection, Func<TCollection, TResult> func)
		where TCollection : class, ISynchronizedCollection
	{
		if (collection is null)
			throw new ArgumentNullException(nameof(collection));

		if (func is null)
			throw new ArgumentNullException(nameof(func));

		using (collection.EnterScope())
			return func(collection);
	}

	/// <summary>
	/// Executes an action on a synchronized collection with thread-safe access.
	/// </summary>
	/// <typeparam name="TCollection">The type of the synchronized collection.</typeparam>
	/// <param name="collection">The synchronized collection to operate on.</param>
	/// <param name="action">The action to execute.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> or <paramref name="action"/> is null.</exception>
	public static void SyncDo<TCollection>(this TCollection collection, Action<TCollection> action)
		where TCollection : class, ISynchronizedCollection
	{
		if (collection is null)
			throw new ArgumentNullException(nameof(collection));

		if (action is null)
			throw new ArgumentNullException(nameof(action));

		using (collection.EnterScope())
			action(collection);
	}

	/// <summary>
	/// Retrieves all keys in a synchronized dictionary that map to a specific value.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="dictionary">The synchronized dictionary to query.</param>
	/// <param name="value">The value to match.</param>
	/// <returns>An enumerable of keys associated with the specified value.</returns>
	public static IEnumerable<TKey> GetKeys<TKey, TValue>(this SynchronizedDictionary<TKey, TValue> dictionary, TValue value)
	{
		using (dictionary.EnterScope())
			return ((IDictionary<TKey, TValue>)dictionary).GetKeys(value);
	}

	/// <summary>
	/// Attempts to dequeue an item from a queue, returning null if the queue is empty (for reference types).
	/// </summary>
	/// <typeparam name="T">The type of elements in the queue, which must be a reference type.</typeparam>
	/// <param name="queue">The queue to dequeue from.</param>
	/// <returns>The dequeued item if available; otherwise, null.</returns>
	public static T TryDequeue<T>(this Queue<T> queue)
		where T : class
	{
		return queue.IsEmpty() ? null : queue.Dequeue();
	}

	/// <summary>
	/// Attempts to dequeue an item from a queue, returning null if the queue is empty (for value types).
	/// </summary>
	/// <typeparam name="T">The type of elements in the queue, which must be a value type.</typeparam>
	/// <param name="queue">The queue to dequeue from.</param>
	/// <returns>The dequeued item if available; otherwise, null.</returns>
	public static T? TryDequeue2<T>(this Queue<T> queue)
		where T : struct
	{
		return queue.IsEmpty() ? (T?)null : queue.Dequeue();
	}

	/// <summary>
	/// Attempts to dequeue an item from a synchronized queue, returning null if the queue is empty (for reference types).
	/// </summary>
	/// <typeparam name="T">The type of elements in the queue, which must be a reference type.</typeparam>
	/// <param name="queue">The synchronized queue to dequeue from.</param>
	/// <returns>The dequeued item if available; otherwise, null.</returns>
	public static T TryDequeue<T>(this SynchronizedQueue<T> queue)
		where T : class
	{
		using (queue.EnterScope())
			return queue.IsEmpty() ? null : queue.Dequeue();
	}

	/// <summary>
	/// Attempts to dequeue an item from a synchronized queue, returning null if the queue is empty (for value types).
	/// </summary>
	/// <typeparam name="T">The type of elements in the queue, which must be a value type.</typeparam>
	/// <param name="queue">The synchronized queue to dequeue from.</param>
	/// <returns>The dequeued item if available; otherwise, null.</returns>
	public static T? TryDequeue2<T>(this SynchronizedQueue<T> queue)
		where T : struct
	{
		using (queue.EnterScope())
			return queue.IsEmpty() ? (T?)null : queue.Dequeue();
	}

	/// <summary>
	/// Attempts to peek at the next item in a queue, returning null if the queue is empty (for reference types).
	/// </summary>
	/// <typeparam name="T">The type of elements in the queue, which must be a reference type.</typeparam>
	/// <param name="queue">The queue to peek into.</param>
	/// <returns>The next item if available; otherwise, null.</returns>
	public static T TryPeek<T>(this Queue<T> queue)
		where T : class
	{
		return queue.IsEmpty() ? null : queue.Peek();
	}

	/// <summary>
	/// Attempts to peek at the next item in a queue, returning null if the queue is empty (for value types).
	/// </summary>
	/// <typeparam name="T">The type of elements in the queue, which must be a value type.</typeparam>
	/// <param name="queue">The queue to peek into.</param>
	/// <returns>The next item if available; otherwise, null.</returns>
	public static T? TryPeek2<T>(this Queue<T> queue)
		where T : struct
	{
		return queue.IsEmpty() ? (T?)null : queue.Peek();
	}

	/// <summary>
	/// Attempts to peek at the next item in a synchronized queue, returning null if the queue is empty (for reference types).
	/// </summary>
	/// <typeparam name="T">The type of elements in the queue, which must be a reference type.</typeparam>
	/// <param name="queue">The synchronized queue to peek into.</param>
	/// <returns>The next item if available; otherwise, null.</returns>
	public static T TryPeek<T>(this SynchronizedQueue<T> queue)
		where T : class
	{
		using (queue.EnterScope())
			return queue.IsEmpty() ? null : queue.Peek();
	}

	/// <summary>
	/// Attempts to peek at the next item in a synchronized queue, returning null if the queue is empty (for value types).
	/// </summary>
	/// <typeparam name="T">The type of elements in the queue, which must be a value type.</typeparam>
	/// <param name="queue">The synchronized queue to peek into.</param>
	/// <returns>The next item if available; otherwise, null.</returns>
	public static T? TryPeek2<T>(this SynchronizedQueue<T> queue)
		where T : struct
	{
		using (queue.EnterScope())
			return queue.IsEmpty() ? (T?)null : queue.Peek();
	}

	/// <summary>
	/// Returns the first element of a sequence, or a specified alternate value if the sequence is empty.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="source">The sequence to query.</param>
	/// <param name="alternate">The value to return if the sequence is empty.</param>
	/// <returns>The first element, or <paramref name="alternate"/> if the sequence is empty.</returns>
	public static T FirstOr<T>(this IEnumerable<T> source, T alternate)
	{
		foreach (var t in source)
			return t;

		return alternate;
	}

	/// <summary>
	/// Returns the first element of a sequence, or null if the sequence is empty (for value types).
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence, which must be a value type.</typeparam>
	/// <param name="source">The sequence to query.</param>
	/// <returns>The first element, or null if the sequence is empty.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
	public static T? FirstOr<T>(this IEnumerable<T> source)
		where T : struct
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		foreach (var t in source)
			return t;

		return null;
	}

	/// <summary>
	/// Returns the last element of a sequence, or null if the sequence is empty (for value types).
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence, which must be a value type.</typeparam>
	/// <param name="source">The sequence to query.</param>
	/// <returns>The last element, or null if the sequence is empty.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
	public static T? LastOr<T>(this IEnumerable<T> source)
		where T : struct
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (source is IList<T> list)
		{
			var count = list.Count;

			if (count > 0)
				return list[count - 1];

			return null;
		}

		T? last = null;

		foreach (var t in source)
			last = t;

		return last;
	}

	/// <summary>
	/// Returns the element at a specified index in a sequence, or null if the index is out of range (for value types).
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence, which must be a value type.</typeparam>
	/// <param name="source">The sequence to query.</param>
	/// <param name="index">The zero-based index of the element to retrieve.</param>
	/// <returns>The element at the specified index, or null if the index is out of range.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
	public static T? ElementAtOr<T>(this IEnumerable<T> source, int index)
		where T : struct
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (index < 0)
			throw new ArgumentOutOfRangeException(nameof(index));

		if (source is IList<T> list)
		{
			if (index < list.Count)
				return list[index];
		}
		else
		{
			foreach (var i in source)
			{
				if (index == 0)
					return i;

				--index;
			}
		}

		return null;
	}

	/// <summary>
	/// Determines whether a sequence of characters is empty.
	/// </summary>
	/// <param name="source">The sequence of characters to check.</param>
	/// <returns>True if the sequence is null or empty; otherwise, false.</returns>
	[Obsolete("Use StringHelper.IsEmpty.")]
	public static bool IsEmpty(this IEnumerable<char> source)
		=> source is null || !source.Any();

	/// <summary>
	/// Determines whether a sequence is empty.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="source">The sequence to check.</param>
	/// <returns>True if the sequence is empty; otherwise, false.</returns>
	public static bool IsEmpty<T>(this IEnumerable<T> source)
	{
		if (source is ICollection<T> col)
			return col.Count == 0;

		if (source is ICollection col2)
			return col2.Count == 0;

#if !NET9_0_OR_GREATER
		if (source is IEnumerableEx ex)
			return ex.Count == 0;
#endif

		return !source.Any();
	}

	/// <summary>
	/// Determines whether a sequence is empty based on a predicate.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="source">The sequence to check.</param>
	/// <param name="predicate">The condition to test each element against.</param>
	/// <returns>True if no elements match the predicate; otherwise, false.</returns>
	public static bool IsEmpty<T>(this IEnumerable<T> source, Func<T, bool> predicate)
	{
		return !source.Any(predicate);
	}

	/// <summary>
	/// Determines whether a collection is empty.
	/// </summary>
	/// <typeparam name="T">The type of elements in the collection.</typeparam>
	/// <param name="source">The collection to check.</param>
	/// <returns>True if the collection is empty; otherwise, false.</returns>
	public static bool IsEmpty<T>(this ICollection<T> source)
	{
		return source.Count == 0;
	}

	/// <summary>
	/// Determines whether an array is empty.
	/// </summary>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="source">The array to check.</param>
	/// <returns>True if the array is empty; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
	public static bool IsEmpty<T>(this T[] source)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return source.Length == 0;
	}

	#region BitArray methods

	/// <summary>
	/// Converts a double value to its binary representation as an array of bits.
	/// </summary>
	/// <param name="value">The double value to convert.</param>
	/// <returns>An array of 64 bits representing the value.</returns>
	public static bool[] ToBits(this double value)
	{
		return value.ToBits(64);
	}

	/// <summary>
	/// Converts a double value to a specified number of bits.
	/// </summary>
	/// <param name="value">The double value to convert.</param>
	/// <param name="count">The number of bits to return.</param>
	/// <returns>An array of bits representing the value.</returns>
	public static bool[] ToBits(this double value, int count)
	{
		return value.ToBits(0, count);
	}

	/// <summary>
	/// Converts a double value to a specified range of bits.
	/// </summary>
	/// <param name="value">The double value to convert.</param>
	/// <param name="startBit">The starting bit position (0-based).</param>
	/// <param name="bitCount">The number of bits to return.</param>
	/// <returns>An array of bits representing the specified range.</returns>
	public static bool[] ToBits(this double value, int startBit, int bitCount)
	{
		return value.AsRaw().ToBits(startBit, bitCount);
	}

	/// <summary>
	/// Converts a float value to its binary representation as an array of bits.
	/// </summary>
	/// <param name="value">The float value to convert.</param>
	/// <returns>An array of 32 bits representing the value.</returns>
	public static bool[] ToBits(this float value)
	{
		return value.ToBits(32);
	}

	/// <summary>
	/// Converts a float value to a specified number of bits.
	/// </summary>
	/// <param name="value">The float value to convert.</param>
	/// <param name="bitCount">The number of bits to return.</param>
	/// <returns>An array of bits representing the value.</returns>
	public static bool[] ToBits(this float value, int bitCount)
	{
		return value.ToBits(0, bitCount);
	}

	/// <summary>
	/// Converts a float value to a specified range of bits.
	/// </summary>
	/// <param name="value">The float value to convert.</param>
	/// <param name="startBit">The starting bit position (0-based).</param>
	/// <param name="bitCount">The number of bits to return.</param>
	/// <returns>An array of bits representing the specified range.</returns>
	public static bool[] ToBits(this float value, int startBit, int bitCount)
	{
		return value.AsRaw().ToBits(startBit, bitCount);
	}

	/// <summary>
	/// Converts a long value to its binary representation as an array of bits.
	/// </summary>
	/// <param name="value">The long value to convert.</param>
	/// <returns>An array of 64 bits representing the value.</returns>
	public static bool[] ToBits(this long value)
	{
		return value.ToBits(64);
	}

	/// <summary>
	/// Converts a long value to a specified number of bits.
	/// </summary>
	/// <param name="value">The long value to convert.</param>
	/// <param name="bitCount">The number of bits to return.</param>
	/// <returns>An array of bits representing the value.</returns>
	public static bool[] ToBits(this long value, int bitCount)
	{
		//if (value > 2.Pow(bitCount - 1))
		//	throw new ArgumentOutOfRangeException(nameof(value));

		return value.ToBits(0, bitCount);
	}

	/// <summary>
	/// Converts a long value to a specified range of bits.
	/// </summary>
	/// <param name="value">The long value to convert.</param>
	/// <param name="startBit">The starting bit position (0-based).</param>
	/// <param name="bitCount">The number of bits to return.</param>
	/// <returns>An array of bits representing the specified range.</returns>
	public static bool[] ToBits(this long value, int startBit, int bitCount)
	{
		var ints = value.GetParts();

		var bits = new List<bool>();

		if (startBit < 32)
			bits.AddRange(ints[0].ToBits(startBit, bitCount.Min(32 - startBit)));

		if ((startBit + bitCount) > 32)
			bits.AddRange(ints[1].ToBits((startBit - 32).Max(0), (bitCount - 32)));

		return [.. bits];
	}

	/// <summary>
	/// Converts an int value to its binary representation as an array of bits.
	/// </summary>
	/// <param name="value">The int value to convert.</param>
	/// <returns>An array of 32 bits representing the value.</returns>
	public static bool[] ToBits(this int value)
	{
		return value.ToBits(32);
	}

	/// <summary>
	/// Converts an int value to a specified number of bits.
	/// </summary>
	/// <param name="value">The int value to convert.</param>
	/// <param name="bitCount">The number of bits to return.</param>
	/// <returns>An array of bits representing the value.</returns>
	public static bool[] ToBits(this int value, int bitCount)
	{
		//if (value > 2.Pow(bitCount - 1))
		//	throw new ArgumentOutOfRangeException(nameof(value));

		return value.ToBits(0, bitCount);
	}

	/// <summary>
	/// Converts an int value to a specified range of bits.
	/// </summary>
	/// <param name="value">The int value to convert.</param>
	/// <param name="startBit">The starting bit position (0-based).</param>
	/// <param name="bitCount">The number of bits to return.</param>
	/// <returns>An array of bits representing the specified range.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="startBit"/> is outside [0, 31] or <paramref name="bitCount"/> is negative or exceeds 32 when added to <paramref name="startBit"/>.</exception>
	public static bool[] ToBits(this int value, int startBit, int bitCount)
	{
		if (startBit > 31 || startBit < 0)
			throw new ArgumentOutOfRangeException(nameof(startBit));

		if (bitCount < 0)
			throw new ArgumentOutOfRangeException(nameof(bitCount));

		if ((startBit + bitCount) > 32)
			throw new ArgumentOutOfRangeException(nameof(bitCount));

		var bits = new bool[bitCount];

		for (var i = 0; i < bitCount; i++)
			bits[i] = value.GetBit(startBit + i);

		return bits;
	}

	/// <summary>
	/// Converts an array of bits to an integer starting from the beginning.
	/// </summary>
	/// <param name="bits">The array of bits to convert.</param>
	/// <returns>The integer value represented by the bits.</returns>
	public static int FromBits(this bool[] bits)
	{
		return bits.FromBits(0);
	}

	/// <summary>
	/// Converts an array of bits to an integer starting from a specified position.
	/// </summary>
	/// <param name="bits">The array of bits to convert.</param>
	/// <param name="startBit">The starting bit position (0-based).</param>
	/// <returns>The integer value represented by the bits.</returns>
	public static int FromBits(this bool[] bits, int startBit)
	{
		return (int)bits.FromBits2(startBit);
	}

	/// <summary>
	/// Converts an array of bits to a long integer starting from the beginning.
	/// </summary>
	/// <param name="bits">The array of bits to convert.</param>
	/// <returns>The long integer value represented by the bits.</returns>
	public static long FromBits2(this bool[] bits)
	{
		return bits.FromBits(0);
	}

	/// <summary>
	/// Converts an array of bits to a long integer starting from a specified position.
	/// </summary>
	/// <param name="bits">The array of bits to convert.</param>
	/// <param name="startBit">The starting bit position (0-based).</param>
	/// <returns>The long integer value represented by the bits.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="bits"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="startBit"/> is greater than or equal to the length of <paramref name="bits"/>.</exception>
	public static long FromBits2(this bool[] bits, int startBit)
	{
		if (bits is null)
			throw new ArgumentNullException(nameof(bits));

		if (startBit >= bits.Length)
			throw new ArgumentOutOfRangeException(nameof(startBit));

		var value = 0L;

		for (var i = 0; i < bits.Length; i++)
			value = value.SetBit(i + startBit, bits[i]);

		return value;
	}

	/// <summary>
	/// Adds a range of bits to an existing <see cref="BitArray"/>.
	/// </summary>
	/// <param name="array">The <see cref="BitArray"/> to extend.</param>
	/// <param name="bits">The bits to add.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> or <paramref name="bits"/> is null.</exception>
	public static void AddRange(this BitArray array, params bool[] bits)
	{
		if (array is null)
			throw new ArgumentNullException(nameof(array));

		if (bits is null)
			throw new ArgumentNullException(nameof(bits));

		var arrayLength = array.Length;

		array.Length += bits.Length;

		for (var i = 0; i < bits.Length; i++)
			array[arrayLength + i] = bits[i];
	}

	#endregion

#if !NET9_0_OR_GREATER
	/// <summary>
	/// A private implementation of <see cref="IEnumerableEx{T}"/> that wraps an enumerable with a predefined count.
	/// </summary>
	/// <typeparam name="T">The type of elements in the enumerable.</typeparam>
	[Obsolete]
	private class EnumerableEx<T> : SimpleEnumerable<T>, IEnumerableEx<T>
	{
		private readonly int _count;

		/// <summary>
		/// Initializes a new instance of the <see cref="EnumerableEx{T}"/> class.
		/// </summary>
		/// <param name="enumerable">The underlying enumerable to wrap.</param>
		/// <param name="count">The predefined count of elements.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
		public EnumerableEx(IEnumerable<T> enumerable, int count)
			: base(enumerable.GetEnumerator)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			_count = count;
		}

		/// <summary>
		/// Gets the predefined count of elements.
		/// </summary>
		int IEnumerableEx.Count => _count;
	}

	/// <summary>
	/// Converts an enumerable to an <see cref="IEnumerableEx{T}"/> with a count determined by enumeration.
	/// </summary>
	/// <typeparam name="T">The type of elements in the enumerable.</typeparam>
	/// <param name="values">The enumerable to convert.</param>
	/// <returns>An <see cref="IEnumerableEx{T}"/> with the specified count.</returns>
	[Obsolete]
	public static IEnumerableEx<T> ToEx<T>(this IEnumerable<T> values)
	{
		return values.ToEx(values.Count());
	}

	/// <summary>
	/// Converts an enumerable to an <see cref="IEnumerableEx{T}"/> with a specified count.
	/// </summary>
	/// <typeparam name="T">The type of elements in the enumerable.</typeparam>
	/// <param name="values">The enumerable to convert.</param>
	/// <param name="count">The predefined count of elements.</param>
	/// <returns>An <see cref="IEnumerableEx{T}"/> with the specified count.</returns>
	[Obsolete]
	public static IEnumerableEx<T> ToEx<T>(this IEnumerable<T> values, int count)
	{
		return new EnumerableEx<T>(values, count);
	}
#endif

	/// <summary>
	/// Converts a list to a synchronized list, or returns it if already synchronized.
	/// </summary>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="list">The list to synchronize.</param>
	/// <returns>A <see cref="SynchronizedList{T}"/> containing the list's elements, or null if <paramref name="list"/> is null.</returns>
	public static SynchronizedList<T> Sync<T>(this IList<T> list)
	{
		if (list is null)
			return null;

		if (list is not SynchronizedList<T> syncList)
		{
			syncList = [.. list];
		}

		return syncList;
	}

	/// <summary>
	/// Converts a dictionary to a synchronized dictionary, or returns it if already synchronized.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="dict">The dictionary to synchronize.</param>
	/// <returns>A <see cref="SynchronizedDictionary{TKey, TValue}"/> containing the dictionary's elements, or null if <paramref name="dict"/> is null.</returns>
	public static SynchronizedDictionary<TKey, TValue> Sync<TKey, TValue>(this IDictionary<TKey, TValue> dict)
	{
		if (dict is null)
			return null;

		if (dict is not SynchronizedDictionary<TKey, TValue> syncDict)
		{
			var typedDict = dict as Dictionary<TKey, TValue>;
			syncDict = new SynchronizedDictionary<TKey, TValue>(typedDict?.Comparer);
			syncDict.AddRange(dict);
		}

		return syncDict;
	}

	/// <summary>
	/// Converts a hash set to a synchronized set.
	/// </summary>
	/// <typeparam name="T">The type of elements in the set.</typeparam>
	/// <param name="list">The hash set to synchronize.</param>
	/// <returns>A <see cref="SynchronizedSet{T}"/> containing the set's elements, or null if <paramref name="list"/> is null.</returns>
	public static SynchronizedSet<T> Sync<T>(this HashSet<T> list)
	{
		if (list is null)
			return null;

		var syncList = new SynchronizedSet<T>();
		syncList.AddRange(list);
		return syncList;
	}

	/// <summary>
	/// Filters a sequence based on a predicate that compares each element with its previous element.
	/// </summary>
	/// <typeparam name="TSource">The type of elements in the sequence.</typeparam>
	/// <param name="source">The sequence to filter.</param>
	/// <param name="predicate">The function to test each element against its previous element.</param>
	/// <returns>An enumerable of elements where the predicate returns true when compared to the previous element.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="predicate"/> is null.</exception>
	public static IEnumerable<TSource> WhereWithPrevious<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, bool> predicate)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		using var iterator = source.GetEnumerator();
		if (!iterator.MoveNext())
			yield break;

		var previous = iterator.Current;
		yield return previous;

		while (iterator.MoveNext())
		{
			var current = iterator.Current;

			if (!predicate(previous, current))
				continue;

			yield return current;
			previous = current;
		}
	}

	/// <summary>
	/// Binds a source notifying list to a destination list, synchronizing additions, removals, insertions, and clear operations.
	/// </summary>
	/// <typeparam name="T">The type of elements in the lists.</typeparam>
	/// <param name="source">The source list that notifies changes.</param>
	/// <param name="destination">The destination list to synchronize with.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is null.</exception>
	public static void Bind<T>(this INotifyList<T> source, IList<T> destination)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (destination is null)
			throw new ArgumentNullException(nameof(destination));

		source.Added += destination.Add;
		source.Removed += item => destination.Remove(item);
		source.Inserted += destination.Insert;
		source.Cleared += destination.Clear;

		source.ForEach(destination.Add);
	}

	/// <summary>
	/// Computes the Damerau-Levenshtein distance between two sequences, with a threshold to limit computation.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequences, which must implement <see cref="IEquatable{T}"/>.</typeparam>
	/// <param name="source">The first sequence.</param>
	/// <param name="target">The second sequence.</param>
	/// <param name="threshold">The maximum distance to compute; returns int.MaxValue if exceeded.</param>
	/// <returns>The Damerau-Levenshtein distance, or int.MaxValue if it exceeds the threshold.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="target"/> is null.</exception>
	// http://stackoverflow.com/a/9454016
	public static int DamerauLevenshteinDistance<T>(T[] source, T[] target, int threshold)
		where T : IEquatable<T>
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (target is null)
			throw new ArgumentNullException(nameof(target));

		var length1 = source.Length;
		var length2 = target.Length;

		// Return trivial case - difference in string lengths exceeds threshold
		if (Math.Abs(length1 - length2) > threshold)
			return int.MaxValue;

		// Ensure arrays [i] / length1 use shorter length
		if (length1 > length2)
		{
			(target, source) = (source, target);
			(length1, length2) = (length2, length1);
		}

		var maxi = length1;
		var maxj = length2;

		var dCurrent = new int[maxi + 1];
		var dMinus1 = new int[maxi + 1];
		var dMinus2 = new int[maxi + 1];

		for (var i = 0; i <= maxi; i++)
			dCurrent[i] = i;

		var jm1 = 0;

		for (var j = 1; j <= maxj; j++)
		{
			// Rotate
			var dSwap = dMinus2;
			dMinus2 = dMinus1;
			dMinus1 = dCurrent;
			dCurrent = dSwap;

			// Initialize
			var minDistance = int.MaxValue;
			dCurrent[0] = j;
			var im1 = 0;
			var im2 = -1;

			for (var i = 1; i <= maxi; i++)
			{
				var cost = source[im1].Equals(target[jm1]) ? 0 : 1;

				var del = dCurrent[im1] + 1;
				var ins = dMinus1[i] + 1;
				var sub = dMinus1[im1] + cost;

				// Fastest execution for min value of 3 integers
				var min = (del > ins) ? (ins > sub ? sub : ins) : (del > sub ? sub : del);

				if (i > 1 && j > 1 && source[im2].Equals(target[jm1]) && source[im1].Equals(target[j - 2]))
					min = Math.Min(min, dMinus2[im2] + cost);

				dCurrent[i] = min;

				if (min < minDistance)
					minDistance = min;

				im1++;
				im2++;
			}

			jm1++;

			if (minDistance > threshold)
				return int.MaxValue;
		}

		var result = dCurrent[maxi];
		return result > threshold ? int.MaxValue : result;
	}

	/// <summary>
	/// Converts a sequence to a set using the default equality comparer.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="values">The sequence to convert.</param>
	/// <returns>A set containing the unique elements from the sequence.</returns>
	public static ISet<T> ToSet<T>(this IEnumerable<T> values)
	{
#if NETSTANDARD2_0
		return new HashSet<T>(values);
#else
		return values.ToHashSet();
#endif
	}

	/// <summary>
	/// Converts a sequence of strings to a case-insensitive set.
	/// </summary>
	/// <param name="values">The sequence of strings to convert.</param>
	/// <returns>A set containing the unique strings, ignoring case.</returns>
	public static ISet<string> ToIgnoreCaseSet(this IEnumerable<string> values)
	{
#if NETSTANDARD2_0
		return new HashSet<string>(values, StringComparer.InvariantCultureIgnoreCase);
#else
		return values.ToHashSet(StringComparer.InvariantCultureIgnoreCase);
#endif
	}

	/// <summary>
	/// Splits a sequence into batches of a specified size.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="source">The sequence to batch.</param>
	/// <param name="size">The size of each batch.</param>
	/// <returns>An enumerable of arrays, each containing up to <paramref name="size"/> elements.</returns>
	public static IEnumerable<T[]> Batch<T>(this IEnumerable<T> source, int size)
	{
#if NETSTANDARD2_0
		return Batch<T, T[]>(source, size, source => [.. source], () => false);
#else
		return source.Chunk(size);
#endif
	}

	/// <summary>
	/// Splits a sequence into batches with custom result selection and stopping condition.
	/// </summary>
	/// <typeparam name="TSource">The type of elements in the sequence.</typeparam>
	/// <typeparam name="TResult">The type of the result for each batch.</typeparam>
	/// <param name="source">The sequence to batch.</param>
	/// <param name="size">The maximum size of each batch.</param>
	/// <param name="resultSelector">The function to transform each batch into a result.</param>
	/// <param name="needStop">The function to determine if batching should stop prematurely.</param>
	/// <returns>An enumerable of results, each representing a batch.</returns>
	public static IEnumerable<TResult> Batch<TSource, TResult>(this IEnumerable<TSource> source, int size,
		Func<IEnumerable<TSource>, TResult> resultSelector, Func<bool> needStop)
	{
		TSource[] bucket = null;
		var count = 0;

		foreach (var item in source)
		{
			bucket ??= new TSource[size];

			bucket[count++] = item;

			// The bucket is fully buffered before it's yielded
			if (count != size)
			{
				if (needStop?.Invoke() != true)
					continue;
			}

			// Select is necessary so bucket contents are streamed too
			yield return resultSelector(bucket);

			bucket = null;
			count = 0;
		}

		// Return the last bucket with all remaining elements
		if (bucket != null && count > 0)
		{
			Array.Resize(ref bucket, count);
			yield return resultSelector(bucket);
		}
	}

	/// <summary>
	/// Appends a single value to the end of a sequence.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="values">The sequence to append to.</param>
	/// <param name="value">The value to append.</param>
	/// <returns>An enumerable with the value appended.</returns>
	[Obsolete("Use Enumerable.Append instead.")]
	public static IEnumerable<T> Append2<T>(this IEnumerable<T> values, T value)
		=> Enumerable.Append(values, value);

	/// <summary>
	/// Asynchronously flattens a sequence of tasks that each return a sequence into a single sequence.
	/// </summary>
	/// <typeparam name="T">The type of elements in the source sequence.</typeparam>
	/// <typeparam name="T1">The type of elements in the resulting sequence.</typeparam>
	/// <param name="enumeration">The source sequence of elements.</param>
	/// <param name="func">The asynchronous function to transform each element into a sequence.</param>
	/// <returns>A task that resolves to a flattened sequence of results.</returns>
	// https://stackoverflow.com/a/35874937
	public static async Task<IEnumerable<T1>> SelectManyAsync<T, T1>(this IEnumerable<T> enumeration, Func<T, Task<IEnumerable<T1>>> func)
		=> (await Task.WhenAll(enumeration.Select(func))).SelectMany(s => s);

	/// <summary>
	/// Converts a value tuple to a key-value pair.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <param name="_">The value tuple to convert.</param>
	/// <returns>A <see cref="KeyValuePair{TKey, TValue}"/> containing the tuple's key and value.</returns>
	public static KeyValuePair<TKey, TValue> ToPair<TKey, TValue>(this (TKey key, TValue value) _)
		=> new(_.key, _.value);

	/// <summary>
	/// Adds a value tuple as a key-value pair to a dictionary.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <param name="dict">The dictionary to add to.</param>
	/// <param name="tuple">The value tuple to add.</param>
	public static void Add<TKey, TValue>(this IDictionary<TKey, TValue> dict, (TKey key, TValue value) tuple)
		=> dict.Add(tuple.ToPair());

	/// <summary>
	/// Applies an action to each element in a sequence.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="source">The sequence to iterate over.</param>
	/// <param name="action">The action to apply to each element.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="action"/> is null.</exception>
	public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (action is null)
			throw new ArgumentNullException(nameof(action));

		foreach (var item in source)
			action(item);
	}

	/// <summary>
	/// Generates all possible permutations of values selected from keys.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="keys">The sequence of keys.</param>
	/// <param name="selector">The function to select possible values for each key.</param>
	/// <returns>An enumerable of arrays representing all permutations.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="keys"/> or <paramref name="selector"/> is null.</exception>
	// https://stackoverflow.com/a/27328512
	public static IEnumerable<TValue[]> Permutations<TKey, TValue>(this IEnumerable<TKey> keys, Func<TKey, IEnumerable<TValue>> selector)
	{
		if (keys is null)
			throw new ArgumentNullException(nameof(keys));

		if (selector is null)
			throw new ArgumentNullException(nameof(selector));

		var keyArray = keys.ToArray();

		if (keyArray.Length < 1)
			yield break;

		static IEnumerable<TValue[]> Permutations(TKey[] keys, int index, Func<TKey, IEnumerable<TValue>> selector, TValue[] values)
		{
			var key = keys[index];

			foreach (var value in selector(key))
			{
				values[index] = value;

				if (index < keys.Length - 1)
				{
					foreach (var array in Permutations(keys, index + 1, selector, values))
						yield return array;
				}
				else
				{
					// Clone the array
					yield return values.ToArray();
				}
			}
		}

		var values = new TValue[keyArray.Length];

		foreach (var array in Permutations(keyArray, 0, selector, values))
			yield return array;
	}

	/// <summary>
	/// Returns the single element of a sequence if it contains exactly one element, otherwise returns the default value.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="source">The sequence to query.</param>
	/// <returns>The single element if the sequence has exactly one; otherwise, the default value of <typeparamref name="T"/>.</returns>
	public static T SingleWhenOnly<T>(this IEnumerable<T> source)
	{
		if (source is ICollection<T> coll)
			return coll.Count == 1 ? coll.First() : default;
		else
			return source.Count() == 1 ? source.First() : default;
	}

#if NETSTANDARD2_0
	/// <summary>
	/// Returns all elements of a sequence except the last specified number of elements.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="source">The sequence to process.</param>
	/// <param name="count">The number of elements to skip from the end.</param>
	/// <returns>An enumerable with the last <paramref name="count"/> elements omitted.</returns>
	public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count)
		=> source.Take(source.Count() - count);

	/// <summary>
	/// Clears all elements from a concurrent queue.
	/// </summary>
	/// <typeparam name="T">The type of elements in the queue.</typeparam>
	/// <param name="queue">The concurrent queue to clear.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="queue"/> is null.</exception>
	// .NET Standard 2.0 doesn't has Clear
	public static void Clear<T>(this System.Collections.Concurrent.ConcurrentQueue<T> queue)
	{
		if (queue is null)
			throw new ArgumentNullException(nameof(queue));

		while (queue.TryDequeue(out _)) { }
	}
#endif

	/// <summary>
	/// Counts the number of elements in a non-generic sequence.
	/// </summary>
	/// <param name="source">The sequence to count.</param>
	/// <returns>The number of elements in the sequence.</returns>
	public static int Count2(this IEnumerable source)
	{
		if (source is IList list)
			return list.Count;
		else if (source is ICollection c)
			return c.Count;
		else
			return source.Cast<object>().Count();
	}

	/// <summary>
	/// Filters a sequence of nullable elements to exclude null values.
	/// </summary>
	/// <typeparam name="T">Type of element.</typeparam>
	/// <param name="enu">Source sequence.</param>
	/// <returns>The sequence of non-null elements.</returns>
	public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enu)
		where T : class
		=> enu.Where(x => x is not null);

	/// <summary>
	/// Filters a sequence of nullable elements to exclude null values.
	/// </summary>
	/// <typeparam name="T">Type of element.</typeparam>
	/// <param name="enu">Source sequence.</param>
	/// <returns>The sequence of non-null elements.</returns>
	public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enu)
		where T : struct
		=> enu.Where(x => x is not null).Select(x => x.Value);

	#region Duck Typing

	/// <summary>
	/// Creates a collection adapter that converts between <see cref="ICollection{TSource}"/> and <see cref="ICollection{TTarget}"/> using duck typing.
	/// </summary>
	/// <typeparam name="TSource">The source element type.</typeparam>
	/// <typeparam name="TTarget">The target element type.</typeparam>
	/// <param name="source">The source collection to wrap.</param>
	/// <param name="sourceToTarget">Function to convert from source type to target type.</param>
	/// <param name="targetToSource">Function to convert from target type to source type.</param>
	/// <returns>An <see cref="ICollection{TTarget}"/> adapter around the source collection.</returns>
	/// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
	/// <remarks>
	/// This method allows working with collections of different element types by providing conversion functions.
	/// The adapter performs conversions on-the-fly when accessing or modifying elements.
	/// </remarks>
	/// <example>
	/// <code>
	/// ICollection&lt;int&gt; numbers = new List&lt;int&gt; { 1, 2, 3 };
	/// ICollection&lt;string&gt; strings = numbers.AsDuckTypedCollection(
	///     n => n.ToString(),
	///     s => int.Parse(s)
	/// );
	/// </code>
	/// </example>
	public static ICollection<TTarget> AsDuckTypedCollection<TSource, TTarget>(
		this ICollection<TSource> source,
		Func<TSource, TTarget> sourceToTarget,
		Func<TTarget, TSource> targetToSource)
	{
		return new DuckTypingCollection<TSource, TTarget>(source, sourceToTarget, targetToSource);
	}

	#endregion
}