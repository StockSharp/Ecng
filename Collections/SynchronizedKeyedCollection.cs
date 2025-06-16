namespace Ecng.Collections;

using System;
using System.Collections.Generic;

using Ecng.Common;

/// <summary>
/// Represents a thread-safe keyed collection that provides synchronization for its operations.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TValue">The type of values in the collection.</typeparam>
[Serializable]
public abstract class SynchronizedKeyedCollection<TKey, TValue> : KeyedCollection<TKey, TValue>, ISynchronizedCollection<KeyValuePair<TKey, TValue>>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedKeyedCollection{TKey, TValue}"/> class.
	/// </summary>
	protected SynchronizedKeyedCollection()
		: base(new SynchronizedDictionary<TKey, TValue>())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedKeyedCollection{TKey, TValue}"/> class with the specified equality comparer.
	/// </summary>
	/// <param name="comparer">The equality comparer to use when comparing keys.</param>
	protected SynchronizedKeyedCollection(IEqualityComparer<TKey> comparer)
		: base(new SynchronizedDictionary<TKey, TValue>(comparer))
	{
	}

	/// <summary>
	/// Gets the synchronized dictionary used internally by the collection.
	/// </summary>
	private SynchronizedDictionary<TKey, TValue> SyncDict => (SynchronizedDictionary<TKey, TValue>)InnerDictionary;

	/// <summary>
	/// Gets the synchronization root object used to synchronize access to the collection.
	/// </summary>
	public SyncObject SyncRoot => SyncDict.SyncRoot;

	/// <summary>
	/// Gets or sets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key of the value to get or set.</param>
	/// <returns>The value associated with the specified key.</returns>
	public override TValue this[TKey key]
	{
		set
		{
			lock (SyncRoot)
				base[key] = value;
		}
	}

	/// <summary>
	/// Removes all keys and values from the collection.
	/// </summary>
	public override void Clear()
	{
		lock (SyncRoot)
			base.Clear();
	}

	/// <summary>
	/// Removes the value with the specified key from the collection.
	/// </summary>
	/// <param name="key">The key of the element to remove.</param>
	/// <returns><c>true</c> if the element is successfully found and removed; otherwise, <c>false</c>.</returns>
	public override bool Remove(TKey key)
	{
		lock (SyncRoot)
			return base.Remove(key);
	}
}