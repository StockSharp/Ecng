﻿namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Nito.AsyncEx;

	using Ecng.Common;

	public static class CollectionHelper
	{
		private sealed class EqualityComparer<T>(Func<T, T, bool> comparer) : IEqualityComparer<T>
		{
			private readonly Func<T, T, bool> _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));

			public bool Equals(T x, T y)
			{
				return _comparer(x, y);
			}

			public int GetHashCode(T obj)
			{
				return obj.GetHashCode();
			}
		}

		private sealed class Comparer<T>(Func<T, T, int> comparer) : IComparer<T>
		{
			private readonly Func<T, T, int> _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));

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
				lock (sync.SyncRoot)
					InternalTryAdd();
			}
			else
			{
				InternalTryAdd();
			}
		}

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
				lock (sync.SyncRoot)
					return InternalTryAdd();
			}

			return InternalTryAdd();
		}

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
				lock (sync.SyncRoot)
					return InternalTryAdd();
			}

			return InternalTryAdd();
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

		public static void RemoveRange<T>(this ICollection<T> source, IEnumerable<T> items)
		{
			if (items is null)
				throw new ArgumentNullException(nameof(items));

			if (source is ICollectionEx<T> ex)
				ex.RemoveRange(items);
			else
				items.ForEach(i => source.Remove(i));
		}

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
			if (collection is null)
				throw new ArgumentNullException(nameof(collection));

			unchecked
			{
				var hash = 0;

				var index = 0;
				foreach (var item in collection)
					hash ^= (31 ^ index++) * (item?.GetHashCode() ?? 0);

				hash %= 2 ^ 32;
				return hash;
			}
		}

		public static bool HasNullItem<T>(this IEnumerable<T> items)
			where T : class
		{
			return items.Contains(null);
		}

		public static T[] CopyAndClear<T>(this ICollection<T> items)
		{
			T[] InternalCopyAndClear()
			{
				var retVal = items.ToArray();
				items.Clear();
				return retVal;
			}

			if (items is not ISynchronizedCollection sync) return InternalCopyAndClear();

			lock (sync.SyncRoot)
				return InternalCopyAndClear();
		}

		public static TValue GetAndRemove<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
		{
			TValue InternalGetAndRemove()
			{
				var value = dict[key];
				dict.Remove(key);
				return value;
			}

			if (dict is not ISynchronizedCollection sync) return InternalGetAndRemove();

			lock (sync.SyncRoot)
				return InternalGetAndRemove();
		}

		public static TValue? TryGetAndRemove2<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
			where TValue : struct
		{
			return dict.TryGetAndRemove(key, out var value) ? value : (TValue?)null;
		}

		public static TValue TryGetAndRemove<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
		{
			return dict.TryGetAndRemove(key, out var value) ? value : default;
		}

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
				lock (sync.SyncRoot)
					return InternalTryGetAndRemove(out value);
			}

			return InternalTryGetAndRemove(out value);
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

		public static PairSet<TKey, TValue> ToPairSet<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
		{
			return source.ToPairSet(System.Collections.Generic.EqualityComparer<TKey>.Default);
		}

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

		public static void CopyTo<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IDictionary<TKey, TValue> destination)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			if (destination is null)
				throw new ArgumentNullException(nameof(destination));

			foreach (var pair in source)
				destination.Add(pair);
		}

		public static IDictionary<TKey, TValue> TypedAs<TKey, TValue>(this IDictionary dictionary)
		{
			if (dictionary is null)
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

		public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<Tuple<TKey, TValue>> source)
		{
			return source.ToDictionary(pair => pair.Item1, pair => pair.Item2);
		}

		public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<Tuple<TKey, TValue>> source, IEqualityComparer<TKey> comparer)
		{
			return source.ToDictionary(pair => pair.Item1, pair => pair.Item2, comparer);
		}

		public static Tuple<TKey, TValue> ToTuple<TKey, TValue>(this KeyValuePair<TKey, TValue> pair)
		{
			return Tuple.Create(pair.Key, pair.Value);
		}

		public static KeyValuePair<TKey, TValue> ToPair<TKey, TValue>(this Tuple<TKey, TValue> pair)
		{
			if (pair is null)
				throw new ArgumentNullException(nameof(pair));

			return new KeyValuePair<TKey, TValue>(pair.Item1, pair.Item2);
		}

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

		public static IDictionary<TKey, IEnumerable<TValue>> ToDictionary<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> grouping)
		{
			if (grouping is null)
				throw new ArgumentNullException(nameof(grouping));

			return grouping.ToDictionary(g => g.Key, g => (IEnumerable<TValue>)g);
		}

		public static IEnumerable<TKey> GetKeys<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TValue value)
		{
			return from pair in dictionary where pair.Value.Equals(value) select pair.Key;
		}

		public static TValue SafeAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
		{
			return dictionary.SafeAdd(key, out _);
		}

        private static class FastActivatorCache<TKey, TValue>
        {
            public static readonly Func<TKey, TValue> Activator;

            static FastActivatorCache()
            {
                Activator = k => FastActivator<TValue>.CreateObject();
            }
        }

        public static TValue SafeAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out bool isNew)
		{
			return dictionary.SafeAdd(key, FastActivatorCache<TKey,TValue>.Activator, out isNew);
		}

		public static TValue SafeAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> handler)
		{
			return dictionary.SafeAdd(key, handler, out _);
		}

		public static TValue SafeAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> handler, out bool isNew)
		{
			if (dictionary is null)
				throw new ArgumentNullException(nameof(dictionary));

			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			isNew = false;

			if (!dictionary.TryGetValue(key, out var value))
			{
				var syncObj = (dictionary as ISynchronizedCollection)?.SyncRoot ?? (object)dictionary;

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

		public static Task<TValue> SafeAddAsync<TKey, TValue>(this IDictionary<TKey, TaskCompletionSource<TValue>> dictionary, TKey key, Func<TKey, CancellationToken, Task<TValue>> handler, CancellationToken cancellationToken)
		{
			if (dictionary is null)
				throw new ArgumentNullException(nameof(dictionary));

			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			var syncObj = (dictionary as ISynchronizedCollection)?.SyncRoot ?? (object)dictionary;

			TaskCompletionSource<TValue> source;

			lock (syncObj)
			{
				if (dictionary.TryGetValue(key, out source))
					return source.Task;

				source = new();
				dictionary.Add(key, source);
			}

			void remove()
			{
				lock (syncObj)
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

		public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
			//where V : class
		{
			if (dict is null)
				throw new ArgumentNullException(nameof(dict));

			dict.TryGetValue(key, out var value);
			return value;
		}

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

		public static TKey TryGetKey<TKey, TValue>(this PairSet<TKey, TValue> pairSet, TValue value)
		{
			pairSet.TryGetKey(value, out var key);
			return key;
		}

		public static TKey? TryGetKey2<TKey, TValue>(this PairSet<TKey, TValue> pairSet, TValue value)
			where TKey : struct
		{
			if (pairSet.TryGetKey(value, out var key))
				return key;
			else
				return null;
		}

		#endregion

		public static TResult SyncGet<TCollection, TResult>(this TCollection collection, Func<TCollection, TResult> func)
			where TCollection : class, ISynchronizedCollection
		{
			if (collection is null)
				throw new ArgumentNullException(nameof(collection));

			if (func is null)
				throw new ArgumentNullException(nameof(func));

			lock (collection.SyncRoot)
				return func(collection);
		}

		public static void SyncDo<TCollection>(this TCollection collection, Action<TCollection> action)
			where TCollection : class, ISynchronizedCollection
		{
			if (collection is null)
				throw new ArgumentNullException(nameof(collection));

			if (action is null)
				throw new ArgumentNullException(nameof(action));

			lock (collection.SyncRoot)
				action(collection);
		}

		public static IEnumerable<TKey> GetKeys<TKey, TValue>(this SynchronizedDictionary<TKey, TValue> dictionary, TValue value)
		{
			lock (dictionary.SyncRoot)
				return ((IDictionary<TKey, TValue>)dictionary).GetKeys(value);
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

		public static T TryPeek<T>(this Queue<T> queue)
			where T : class
		{
			return queue.IsEmpty() ? null : queue.Peek();
		}

		public static T? TryPeek2<T>(this Queue<T> queue)
			where T : struct
		{
			return queue.IsEmpty() ? (T?)null : queue.Peek();
		}

		public static T TryPeek<T>(this SynchronizedQueue<T> queue)
			where T : class
		{
			lock (queue.SyncRoot)
				return queue.IsEmpty() ? null : queue.Peek();
		}

		public static T? TryPeek2<T>(this SynchronizedQueue<T> queue)
			where T : struct
		{
			lock (queue.SyncRoot)
				return queue.IsEmpty() ? (T?)null : queue.Peek();
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
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			foreach (var t in source)
				return t;

			return null;
		}

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

		[Obsolete("Use StringHelper.IsEmpty.")]
		public static bool IsEmpty(this IEnumerable<char> source)
			=> source is null || !source.Any();

		public static bool IsEmpty<T>(this IEnumerable<T> source)
		{
			if (source is ICollection<T> col)
				return col.Count == 0;

			if (source is ICollection col2)
				return col2.Count == 0;

			if (source is IEnumerableEx ex)
				return ex.Count == 0;

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
			if (source is null)
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
			//	throw new ArgumentOutOfRangeException(nameof(value));

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

			return [.. bits];
		}

		public static bool[] ToBits(this int value)
		{
			return value.ToBits(32);
		}

		public static bool[] ToBits(this int value, int bitCount)
		{
			//if (value > 2.Pow(bitCount - 1))
			//	throw new ArgumentOutOfRangeException(nameof(value));

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
			if (bits is null)
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
			if (list is null)
				return null;

			if (list is not SynchronizedList<T> syncList)
			{
				syncList = [.. list];
			}

			return syncList;
		}

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

		public static SynchronizedSet<T> Sync<T>(this HashSet<T> list)
		{
			if (list is null)
				return null;

			var syncList = new SynchronizedSet<T>();
			syncList.AddRange(list);
			return syncList;
		}

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

			// Return trivial case - difference in string lengths exceeds threshhold
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

					//Fastest execution for min value of 3 integers
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

		public static ISet<T> ToSet<T>(this IEnumerable<T> values)
		{
#if NETSTANDARD2_0
			return new HashSet<T>(values);
#else
			return values.ToHashSet();
#endif
		}

		public static ISet<string> ToIgnoreCaseSet(this IEnumerable<string> values)
		{
#if NETSTANDARD2_0
			return new HashSet<string>(values, StringComparer.InvariantCultureIgnoreCase);
#else
			return values.ToHashSet(StringComparer.InvariantCultureIgnoreCase);
#endif
		}

		public static IEnumerable<T[]> Batch<T>(this IEnumerable<T> source, int size)
		{
#if NETSTANDARD2_0
			return Batch<T, T[]>(source, size, source => source.ToArray(), () => false);
#else
			return source.Chunk(size);
#endif
		}

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

		public static IEnumerable<T> Append2<T>(this IEnumerable<T> values, T value)
			=> Enumerable.Append(values, value);

		// https://stackoverflow.com/a/35874937
		public static async Task<IEnumerable<T1>> SelectManyAsync<T, T1>(this IEnumerable<T> enumeration, Func<T, Task<IEnumerable<T1>>> func)
			=> (await Task.WhenAll(enumeration.Select(func))).SelectMany(s => s);

		public static KeyValuePair<TKey, TValue> ToPair<TKey, TValue>(this (TKey key, TValue value) _)
			=> new(_.key, _.value);

		public static void Add<TKey, TValue>(this IDictionary<TKey, TValue> dict, (TKey key, TValue value) tuple)
			=> dict.Add(tuple.ToPair());

		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			if (action is null)
				throw new ArgumentNullException(nameof(action));

			foreach (var item in source)
				action(item);
		}

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

		public static T SingleWhenOnly<T>(this IEnumerable<T> source)
		{
			if (source is ICollection<T> coll)
				return coll.Count == 1 ? coll.First() : default;
			else
				return source.Count() == 1 ? source.First() : default;
		}

#if NETSTANDARD2_0
		public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count)
			=> source.Take(source.Count() - count);

		// .NET Standard 2.0 doesn't has Clear
		public static void Clear<T>(this System.Collections.Concurrent.ConcurrentQueue<T> queue)
		{
			if (queue is null)
				throw new ArgumentNullException(nameof(queue));

			while (queue.TryDequeue(out _)) { }
		}
#endif

		public static int Count2(this IEnumerable source)
		{
			if (source is IList list)
				return list.Count;
			else if (source is ICollection c)
				return c.Count;
			else
				return source.Cast<object>().Count();
		}
	}
}
