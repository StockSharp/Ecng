namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using MoreLinq;

	using Wintellect.PowerCollections;

	using Ecng.Common;

	public static class CollectionHelper
	{
		#region PowerCollections Algorithms Methods

		public static ICollection UnCast<T>(this ICollection<T> collection)
		{
			return Algorithms.Untyped(collection);
		}

		public static IList UnCast<T>(this IList<T> list)
		{
			return Algorithms.Untyped(list);
		}

		#endregion

		private sealed class EqualityComparer<T> : IEqualityComparer<T>
		{
			private readonly Func<T, T, bool> _comparer;

			public EqualityComparer(Func<T, T, bool> comparer)
			{
				if (comparer == null)
					throw new ArgumentNullException(nameof(comparer));

				_comparer = comparer;
			}

			public bool Equals(T x, T y)
			{
				return _comparer(x, y);
			}

			public int GetHashCode(T obj)
			{
				return obj.GetHashCode();
			}
		}

		private sealed class Comparer<T> : IComparer<T>
		{
			private readonly Func<T, T, int> _comparer;

			public Comparer(Func<T, T, int> comparer)
			{
				if (comparer == null)
					throw new ArgumentNullException(nameof(comparer));

				_comparer = comparer;
			}

			public int Compare(T x, T y)
			{
				return _comparer(x, y);
			}
		}

		public static IEqualityComparer<T> ToComparer<T>(this Func<T, T, bool> comparer)
		{
			return new EqualityComparer<T>(comparer);
		}

		public static IComparer<T> ToComparer<T>(this Func<T, T, int> comparer)
		{
			return new Comparer<T>(comparer);
		}

		public static IComparer<T> ToComparer<T>(this Comparison<T> comparer)
		{
			return comparer.ToFunc().ToComparer();
		}

		public static Func<T, T, int> ToFunc<T>(this Comparison<T> comparer)
		{
			return (t1, t2) => comparer(t1, t2);
		}

		public static bool SequenceEqual<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, T, bool> comparer)
		{
			return first.SequenceEqual(second, comparer.ToComparer());
		}

		public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> collection, Comparison<T> comparison)
		{
			return collection.OrderBy(item => item, comparison.ToComparer());
		}

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

		public static void TryAdd<T>(this ICollection<T> source, IEnumerable<T> values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));

			foreach (var value in values)
				source.TryAdd(value);
		}

		public static bool TryAdd<T>(this ICollection<T> source, T value)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (!source.Contains(value))
			{
				source.Add(value);
				return true;
			}

			return false;
		}

		public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
		{
			if (dict == null)
				throw new ArgumentNullException(nameof(dict));

			if (!dict.ContainsKey(key))
			{
				dict.Add(key, value);
				return true;
			}

			return false;
		}

		public static T ConcatEx<T, TItem>(this T first, T second)
			where T : ICollection<TItem>, new()
		{
			var retVal = new T();
			retVal.AddRange(first.Concat(second));
			return retVal;
		}

		public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> items)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (items == null)
				throw new ArgumentNullException(nameof(items));

			if (source is List<T>)
				((List<T>)source).AddRange(items);
			else if (source is ICollectionEx<T>)
				((ICollectionEx<T>)source).AddRange(items);
			else
			{
				foreach (var item in items)
					source.Add(item);
			}
		}

		public static IEnumerable<T> RemoveRange<T>(this ICollection<T> source, IEnumerable<T> items)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			return items.Where(source.Remove).ToArray();
		}

		public static IEnumerable<T> RemoveWhere<T>(this ICollection<T> collection, Func<T, bool> filter)
		{
			var removingItems = collection.Where(filter).ToArray();

			foreach (var t in removingItems)
				collection.Remove(t);

			return removingItems;
		}

		public static IEnumerable<T> SelectMany<T>(this IEnumerable<IEnumerable<T>> values)
		{
			return values.SelectMany(value => value);
		}

		public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> values)
		{
			return values.OrderBy(value => value);
		}

		public static IEnumerable<T> OrderByDescending<T>(this IEnumerable<T> values)
		{
			return values.OrderByDescending(value => value);
		}

		public static int GetHashCodeEx<T>(this IEnumerable<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			var hash = 0;

			var index = 0;
			foreach (var item in collection)
				hash ^= (31 ^ index++) * item.GetHashCode();

			hash %= 2 ^ 32;
			return hash;
		}

		public static bool HasNullItem<T>(this IEnumerable<T> items)
			where T : class
		{
			return items.Contains(null);
		}

		public static T[] CopyAndClear<T>(this ICollection<T> items)
		{
			var retVal = items.ToArray();
			items.Clear();
			return retVal;
		}

		public static T[] CopyAndClear<T>(this HashSet<T> items)
		{
			var retVal = items.ToArray();
			items.Clear();
			return retVal;
		}

		public static TValue GetAndRemove<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
		{
			var value = dict[key];
			dict.Remove(key);
			return value;
		}

		public static T ElementAtFromEnd<T>(this IEnumerable<T> source, int index)
		{
			return source.ElementAt(source.GetIndexFromEnd(index));
		}

		public static T ElementAtFromEndOrDefault<T>(this IEnumerable<T> source, int index)
		{
			return source.ElementAtOrDefault(source.GetIndexFromEnd(index));
		}

		private static int GetIndexFromEnd<T>(this IEnumerable<T> source, int index)
		{
			return source.Count() - 1 - index;
		}

		public static T ElementAtFromEnd<T>(this LinkedList<T> list, int index)
		{
			if (list == null)
				throw new ArgumentNullException(nameof(list));

			if (list.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(list));

			var curr = list.Last;

			while (index > 0)
			{
				curr = curr.Previous;
				index--;

				if (curr == null)
					throw new ArgumentOutOfRangeException(nameof(list));
			}

			return curr.Value;
		}

		public static T ElementAtFromEnd<T>(this SynchronizedLinkedList<T> list, int index)
		{
			if (list == null)
				throw new ArgumentNullException(nameof(list));

			if (list.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(list));

			var curr = list.Last;

			while (index > 0)
			{
				curr = curr.Previous;
				index--;

				if (curr == null)
					throw new ArgumentOutOfRangeException(nameof(list));
			}

			return curr.Value;
		}

		public static T ElementAtFromEndOrDefault<T>(this LinkedList<T> list, int index)
		{
			if (list == null)
				throw new ArgumentNullException(nameof(list));

			var curr = list.Last;

			while (index > 0 && curr != null)
			{
				curr = curr.Previous;
				index--;
			}

			return curr == null ? default(T) : curr.Value;
		}

		public static T ElementAtFromEndOrDefault<T>(this SynchronizedLinkedList<T> list, int index)
		{
			if (list == null)
				throw new ArgumentNullException(nameof(list));

			var curr = list.Last;

			while (index > 0 && curr != null)
			{
				curr = curr.Previous;
				index--;
			}

			return curr == null ? default(T) : curr.Value;
		}

		#region Dictionary Methods

		public static void CopyTo<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IDictionary<TKey, TValue> destination)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			foreach (var pair in source)
				destination.Add(pair);
		}

		public static IDictionary<TKey, TValue> TypedAs<TKey, TValue>(this IDictionary dictionary)
		{
			if (dictionary == null)
				throw new ArgumentNullException(nameof(dictionary));

			return dictionary.Cast<DictionaryEntry>().ToDictionary(item => item.Key.To<TKey>(), item => item.Value.To<TValue>());
		}

		public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
		{
			return source.ToDictionary(pair => pair.Key, pair => pair.Value);
		}

		public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> comparer)
		{
			return source.ToDictionary(pair => pair.Key, pair => pair.Value, comparer);
		}

		public static IDictionary<TKey, TValue> ToDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, int, TKey> keySelector, Func<TSource, int, TValue> valueSelector)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (keySelector == null)
				throw new ArgumentNullException(nameof(keySelector));

			if (valueSelector == null)
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

		public static IDictionary<TKey, IEnumerable<TValue>> ToDictionary<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> grouping)
		{
			if (grouping == null)
				throw new ArgumentNullException(nameof(grouping));

			return grouping.ToDictionary(g => g.Key, g => (IEnumerable<TValue>)g);
		}

		public static MultiDictionary<TKey, TValue> ToMultiDictionary<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> grouping)
		{
			if (grouping == null)
				throw new ArgumentNullException(nameof(grouping));

			var retVal = new MultiDictionary<TKey, TValue>(false);

			foreach (var group in grouping)
				retVal.AddMany(group.Key, group);

			return retVal;
		}

		public static MultiDictionary<TKey, TValue> ToMultiDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>> pairs)
		{
			if (pairs == null)
				throw new ArgumentNullException(nameof(pairs));

			var retVal = new MultiDictionary<TKey, TValue>(false);

			foreach (var pair in pairs)
				retVal.AddMany(pair.Key, pair.Value);

			return retVal;
		}

		public static IEnumerable<TKey> GetKeys<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TValue value)
		{
			return from pair in dictionary where pair.Value.Equals(value) select pair.Key;
		}

		public static IEnumerable<TKey> GetKeys<TKey, TValue>(this MultiDictionaryBase<TKey, TValue> dictionary, TValue value)
		{
			return from pair in dictionary where pair.Value.Contains(value) select pair.Key;
		}

		public static TValue SafeAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
			where TValue : new()
		{
			bool isNew;
			return dictionary.SafeAdd(key, out isNew);
		}

        private static class FastActivatorCache<TKey, TValue> where TValue : new()
        {
            public readonly static Func<TKey, TValue> Activator; 
            static FastActivatorCache()
            {
                Activator = k => FastActivator<TValue>.CreateObject();
            } 
        }

        public static TValue SafeAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out bool isNew)
			where TValue : new()
		{
			return dictionary.SafeAdd(key, FastActivatorCache<TKey,TValue>.Activator, out isNew);
		}

		public static TValue SafeAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> handler)
		{
			bool isNew;
			return dictionary.SafeAdd(key, handler, out isNew);
		}

		public static TValue SafeAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> handler, out bool isNew)
		{
			if (dictionary == null)
				throw new ArgumentNullException(nameof(dictionary));

			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			isNew = false;

			TValue value;

			if (!dictionary.TryGetValue(key, out value))
			{
				var syncObj = dictionary is ISynchronizedCollection<KeyValuePair<TKey, TValue>> ? ((ISynchronizedCollection<KeyValuePair<TKey, TValue>>)dictionary).SyncRoot : (object)dictionary;

				lock (syncObj)
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

		public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
			//where V : class
		{
			if (dict == null)
				throw new ArgumentNullException(nameof(dict));

			TValue value;
			dict.TryGetValue(key, out value);
			return value;
		}

		public static TValue? TryGetValue2<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
			where TValue : struct
		{
			if (dict == null)
				throw new ArgumentNullException(nameof(dict));

			TValue value;
			if (dict.TryGetValue(key, out value))
				return value;
			else
				return null;
		}

		#endregion

		public static TResult SyncGet<TCollection, TResult>(this TCollection collection, Func<TCollection, TResult> func)
			where TCollection : class, ISynchronizedCollection
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			if (func == null)
				throw new ArgumentNullException(nameof(func));

			lock (collection.SyncRoot)
				return func(collection);
		}

		public static void SyncDo<TCollection>(this TCollection collection, Action<TCollection> action)
			where TCollection : class, ISynchronizedCollection
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			if (action == null)
				throw new ArgumentNullException(nameof(action));

			lock (collection.SyncRoot)
				action(collection);
		}

		//public static TValue TryGetValue<TKey, TValue>(this SynchronizedDictionary<TKey, TValue> dict, TKey key)
		//{
		//    if (dict == null)
		//        throw new ArgumentNullException("dict");

		//    lock (dict.SyncRoot)
		//        return ((IDictionary<TKey, TValue>)dict).TryGetValue(key);
		//}

		//public static TValue? TryGetValue2<TKey, TValue>(this SynchronizedDictionary<TKey, TValue> dict, TKey key)
		//    where TValue : struct
		//{
		//    if (dict == null)
		//        throw new ArgumentNullException("dict");

		//    lock (dict.SyncRoot)
		//        return ((IDictionary<TKey, TValue>)dict).TryGetValue2(key);
		//}

		public static ICollection<TValue> TryGetValue<TKey, TValue>(this SynchronizedMultiDictionary<TKey, TValue> dict, TKey key)
		{
			if (dict == null)
				throw new ArgumentNullException(nameof(dict));

			lock (dict.SyncRoot)
			{
				var retVal = ((MultiDictionaryBase<TKey, TValue>)dict).TryGetValue(key);
				return retVal != null ? retVal.ToArray() : null;
			}
		}

		public static IEnumerable<TKey> GetKeys<TKey, TValue>(this SynchronizedDictionary<TKey, TValue> dictionary, TValue value)
		{
			lock (dictionary.SyncRoot)
				return ((IDictionary<TKey, TValue>)dictionary).GetKeys(value);
		}

		public static IEnumerable<TKey> GetKeys<TKey, TValue>(this SynchronizedMultiDictionary<TKey, TValue> dictionary, TValue value)
		{
			lock (dictionary.SyncRoot)
				return ((MultiDictionaryBase<TKey, TValue>)dictionary).GetKeys(value).ToArray();
		}

		public static T TryDequeue<T>(this Queue<T> queue)
			where T : class
		{
			return queue.IsEmpty() ? null : queue.Dequeue();
		}

		public static T? TryDequeue2<T>(this Queue<T> queue)
			where T : struct
		{
			return queue.IsEmpty() ? (T?)null : queue.Dequeue();
		}

		public static T TryDequeue<T>(this SynchronizedQueue<T> queue)
			where T : class
		{
			lock (queue.SyncRoot)
				return queue.IsEmpty() ? null : queue.Dequeue();
		}

		public static T? TryDequeue2<T>(this SynchronizedQueue<T> queue)
			where T : struct
		{
			lock (queue.SyncRoot)
				return queue.IsEmpty() ? (T?)null : queue.Dequeue();
		}

		public static T FirstOr<T>(this IEnumerable<T> source, T alternate)
		{
			foreach (var t in source)
				return t;

			return alternate;
		}

		public static T? FirstOr<T>(this IEnumerable<T> source)
			where T : struct
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			foreach (var t in source)
				return t;

			return null;
		}

		public static T? LastOr<T>(this IEnumerable<T> source)
			where T : struct
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			var list = source as IList<T>;
			if (list != null)
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

		public static bool IsEmpty<T>(this IEnumerable<T> source)
		{
			var col = source as ICollection<T>;
			if (col != null)
				return col.Count == 0;

			var col2 = source as ICollection;
			if (col2 != null)
				return col2.Count == 0;

			return !source.Any();
		}

		public static bool IsEmpty<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		{
			return !source.Any(predicate);
		}

		public static bool IsEmpty<T>(this ICollection<T> source)
		{
			return source.Count == 0;
		}

		public static bool IsEmpty<T>(this T[] source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return source.Length == 0;
		}

		#region BitArray methods

		public static bool[] ToBits(this double value)
		{
			return value.ToBits(64);
		}

		public static bool[] ToBits(this double value, int count)
		{
			return value.ToBits(0, count);
		}

		public static bool[] ToBits(this double value, int startBit, int bitCount)
		{
			return value.AsRaw().ToBits(startBit, bitCount);
		}

		public static bool[] ToBits(this float value)
		{
			return value.ToBits(32);
		}

		public static bool[] ToBits(this float value, int bitCount)
		{
			return value.ToBits(0, bitCount);
		}

		public static bool[] ToBits(this float value, int startBit, int bitCount)
		{
			return value.AsRaw().ToBits(startBit, bitCount);
		}

		public static bool[] ToBits(this long value)
		{
			return value.ToBits(64);
		}

		public static bool[] ToBits(this long value, int bitCount)
		{
			//if (value > 2.Pow(bitCount - 1))
			//	throw new ArgumentOutOfRangeException("value");

			return value.ToBits(0, bitCount);
		}

		public static bool[] ToBits(this long value, int startBit, int bitCount)
		{
			var ints = value.GetParts();

			var bits = new List<bool>();

			if (startBit < 32)
				bits.AddRange(ints[0].ToBits(startBit, bitCount.Min(32 - startBit)));

			if ((startBit + bitCount) > 32)
				bits.AddRange(ints[1].ToBits((startBit - 32).Max(0), (bitCount - 32)));

			return bits.ToArray();
		}

		public static bool[] ToBits(this int value)
		{
			return value.ToBits(32);
		}

		public static bool[] ToBits(this int value, int bitCount)
		{
			//if (value > 2.Pow(bitCount - 1))
			//	throw new ArgumentOutOfRangeException("value");

			return value.ToBits(0, bitCount);
		}

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

		public static int FromBits(this bool[] bits)
		{
			return bits.FromBits(0);
		}

		public static int FromBits(this bool[] bits, int startBit)
		{
			return (int)bits.FromBits2(startBit);
		}

		public static long FromBits2(this bool[] bits)
		{
			return bits.FromBits(0);
		}

		public static long FromBits2(this bool[] bits, int startBit)
		{
			if (bits == null)
				throw new ArgumentNullException(nameof(bits));

			if (startBit >= bits.Length)
				throw new ArgumentOutOfRangeException(nameof(startBit));

			var value = 0L;

			for (var i = 0; i < bits.Length; i++)
				value = value.SetBit(i + startBit, bits[i]);

			return value;
		}

		public static void AddRange(this BitArray array, params bool[] bits)
		{
			if (array == null)
				throw new ArgumentNullException(nameof(array));

			if (bits == null)
				throw new ArgumentNullException(nameof(bits));

			var arrayLength = array.Length;

			array.Length += bits.Length;

			for (var i = 0; i < bits.Length; i++)
				array[arrayLength + i] = bits[i];
		}

		#endregion

		public static void Shrink<TCollection, TItem>(this TCollection collection, int bufferSize)
			where TCollection : class, ISynchronizedCollection<TItem>
		{
			if (collection.Count > bufferSize * 1.5)
			{
				collection.SyncDo(c =>
				{
					var elapsed = c.Skip(bufferSize / 2).ToList();
					c.Clear();
					c.AddRange(elapsed);
				});
			}
		}

		private sealed class EnumerableEx<T> : SimpleEnumerable<T>, IEnumerableEx<T>
		{
			private readonly int _count;

			public EnumerableEx(IEnumerable<T> enumerable, int count)
				: base(enumerable.GetEnumerator)
			{
				if (count < 0)
					throw new ArgumentOutOfRangeException(nameof(count));

				_count = count;
			}

			int IEnumerableEx.Count => _count;
		}

		public static IEnumerableEx<T> ToEx<T>(this IEnumerable<T> values)
		{
			return values.ToEx(values.Count());
		}

		public static IEnumerableEx<T> ToEx<T>(this IEnumerable<T> values, int count)
		{
			return new EnumerableEx<T>(values, count);
		}

		public static SynchronizedList<T> Sync<T>(this IList<T> list)
		{
			if (list == null)
				return null;

			var syncList = list as SynchronizedList<T>;

			if (syncList == null)
			{
				syncList = new SynchronizedList<T>();
				syncList.AddRange(list);
			}

			return syncList;
		}

		public static SynchronizedDictionary<TKey, TValue> Sync<TKey, TValue>(this IDictionary<TKey, TValue> dict)
		{
			if (dict == null)
				return null;

			var syncDict = dict as SynchronizedDictionary<TKey, TValue>;

			if (syncDict == null)
			{
				var typedDict = dict as Dictionary<TKey, TValue>;
				syncDict = new SynchronizedDictionary<TKey, TValue>(typedDict == null ? null : typedDict.Comparer);
				syncDict.AddRange(dict);
			}

			return syncDict;
		}

		public static SynchronizedSet<T> Sync<T>(this HashSet<T> list)
		{
			if (list == null)
				return null;

			var syncList = new SynchronizedSet<T>();
			syncList.AddRange(list);
			return syncList;
		}

		//// http://stackoverflow.com/questions/3683105/calculate-difference-from-previous-item-with-linq
		//public static IEnumerable<TResult> SelectWithPrevious<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TSource, TResult> projection)
		//{
		//	if (source == null)
		//		throw new ArgumentNullException("source");

		//	if (projection == null)
		//		throw new ArgumentNullException("projection");

		//	using (var iterator = source.GetEnumerator())
		//	{
		//		if (!iterator.MoveNext())
		//			yield break;

		//		var previous = iterator.Current;

		//		while (iterator.MoveNext())
		//		{
		//			yield return projection(previous, iterator.Current);
		//			previous = iterator.Current;
		//		}
		//	}
		//}

		public static IEnumerable<TSource> WhereWithPrevious<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, bool> predicate)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			using (var iterator = source.GetEnumerator())
			{
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
		}

		public static void Bind<T>(this INotifyList<T> source, IList<T> destination)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			source.Added += destination.Add;
			source.Removed += item => destination.Remove(item);
			source.Inserted += destination.Insert;
			source.Cleared += destination.Clear;

			source.ForEach(destination.Add);
		}
	}
}