namespace Ecng.ComponentModel;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

// http://drwpf.com/blog/2007/09/16/can-i-bind-my-itemscontrol-to-a-dictionary/

/// <summary>
/// Represents an observable dictionary that raises notifications when items are added, removed, or refreshed.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
[Serializable]
[Obsolete]
public class ObservableDictionary<TKey, TValue> :
	IDictionary<TKey, TValue>,
	IDictionary,
	ISerializable,
	IDeserializationCallback,
	INotifyCollectionChanged,
	INotifyPropertyChanged
{
	#region constructors

	#region public

	/// <summary>
	/// Initializes a new instance of the ObservableDictionary class.
	/// </summary>
	public ObservableDictionary()
	{
		_keyedEntryCollection = new KeyedDictionaryEntryCollection();
	}

	/// <summary>
	/// Initializes a new instance of the ObservableDictionary class with the specified dictionary.
	/// </summary>
	/// <param name="dictionary">The dictionary whose elements are copied to the new ObservableDictionary.</param>
	public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
	{
		_keyedEntryCollection = new KeyedDictionaryEntryCollection();

		foreach (var entry in dictionary)
			DoAddEntry(entry.Key, entry.Value);
	}

	/// <summary>
	/// Initializes a new instance of the ObservableDictionary class that uses the specified key comparer.
	/// </summary>
	/// <param name="comparer">The comparer to use when comparing keys.</param>
	public ObservableDictionary(IEqualityComparer<TKey> comparer)
	{
		_keyedEntryCollection = new KeyedDictionaryEntryCollection(comparer);
	}

	/// <summary>
	/// Initializes a new instance of the ObservableDictionary class with the specified dictionary and key comparer.
	/// </summary>
	/// <param name="dictionary">The dictionary whose elements are copied to the new ObservableDictionary.</param>
	/// <param name="comparer">The comparer to use when comparing keys.</param>
	public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
	{
		_keyedEntryCollection = new KeyedDictionaryEntryCollection(comparer);

		foreach (var entry in dictionary)
			DoAddEntry(entry.Key, entry.Value);
	}

	#endregion public

	#region protected

	/// <summary>
	/// Initializes a new instance of the ObservableDictionary class from serialization data.
	/// </summary>
	/// <param name="info">The SerializationInfo to populate with data.</param>
	/// <param name="context">The destination for this serialization.</param>
	protected ObservableDictionary(SerializationInfo info, StreamingContext context)
	{
		_siInfo = info;
	}

	#endregion protected

	#endregion constructors

	#region properties

	#region public

	/// <summary>
	/// Gets the comparer used to compare keys in the dictionary.
	/// </summary>
	public IEqualityComparer<TKey> Comparer => _keyedEntryCollection.Comparer;

	/// <summary>
	/// Gets the number of key-value pairs contained in the dictionary.
	/// </summary>
	public int Count => _keyedEntryCollection.Count;

	/// <summary>
	/// Gets the collection of keys in the dictionary.
	/// </summary>
	public Dictionary<TKey, TValue>.KeyCollection Keys => TrueDictionary.Keys;

	/// <summary>
	/// Gets or sets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key of the value to get or set.</param>
	public TValue this[TKey key]
	{
		get => (TValue)_keyedEntryCollection[key].Value;
		set => DoSetEntry(key, value);
	}

	/// <summary>
	/// Gets the collection of values in the dictionary.
	/// </summary>
	public Dictionary<TKey, TValue>.ValueCollection Values => TrueDictionary.Values;

	#endregion public

	#region private

	private Dictionary<TKey, TValue> TrueDictionary
	{
		get
		{
			if (_dictionaryCacheVersion != _version)
			{
				_dictionaryCache.Clear();
				foreach (DictionaryEntry entry in _keyedEntryCollection)
					_dictionaryCache.Add((TKey)entry.Key, (TValue)entry.Value);
				_dictionaryCacheVersion = _version;
			}
			return _dictionaryCache;
		}
	}

	#endregion private

	#endregion properties

	#region methods

	#region public

	/// <summary>
	/// Adds the specified key and value to the dictionary.
	/// </summary>
	/// <param name="key">The key of the element to add.</param>
	/// <param name="value">The value of the element to add.</param>
	public void Add(TKey key, TValue value)
	{
		DoAddEntry(key, value);
	}

	/// <summary>
	/// Removes all keys and values from the dictionary.
	/// </summary>
	public void Clear()
	{
		DoClearEntries();
	}

	/// <summary>
	/// Determines whether the dictionary contains the specified key.
	/// </summary>
	/// <param name="key">The key to locate in the dictionary.</param>
	public bool ContainsKey(TKey key)
	{
		return _keyedEntryCollection.Contains(key);
	}

	/// <summary>
	/// Determines whether the dictionary contains a specific value.
	/// </summary>
	/// <param name="value">The value to locate in the dictionary.</param>
	public bool ContainsValue(TValue value)
	{
		return TrueDictionary.ContainsValue(value);
	}

	/// <summary>
	/// Returns an enumerator that iterates through the dictionary.
	/// </summary>
	public IEnumerator GetEnumerator()
	{
		return new Enumerator(this, false);
	}

	/// <summary>
	/// Removes the value with the specified key from the dictionary.
	/// </summary>
	/// <param name="key">The key of the element to remove.</param>
	public bool Remove(TKey key)
	{
		return DoRemoveEntry(key);
	}

	/// <summary>
	/// Gets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key of the element to locate.</param>
	/// <param name="value">When this method returns, the value associated with the specified key, if found; otherwise, the default value for the type.</param>
	public bool TryGetValue(TKey key, out TValue value)
	{
		var result = _keyedEntryCollection.Contains(key);
		value = result ? (TValue)_keyedEntryCollection[key].Value : default;
		return result;
	}

	#endregion public

	#region protected

	/// <summary>
	/// Called before adding an entry to the dictionary.
	/// </summary>
	/// <param name="key">The key for the entry to add.</param>
	/// <param name="value">The value for the entry to add.</param>
	/// <returns>True if the entry is added; otherwise, false.</returns>
	protected virtual bool AddEntry(TKey key, TValue value)
	{
		_keyedEntryCollection.Add(new DictionaryEntry(key, value));
		return true;
	}

	/// <summary>
	/// Called to clear all entries from the dictionary.
	/// </summary>
	/// <returns>True if there were entries to clear; otherwise, false.</returns>
	protected virtual bool ClearEntries()
	{
		bool result = (Count > 0);
		if (result)
		{
			_keyedEntryCollection.Clear();
		}
		return result;
	}

	/// <summary>
	/// Retrieves the index and entry associated with the specified key.
	/// </summary>
	/// <param name="key">The key to locate in the dictionary.</param>
	/// <param name="entry">When this method returns, contains the DictionaryEntry for the specified key, if found.</param>
	/// <returns>The index of the entry if found; otherwise, -1.</returns>
	protected int GetIndexAndEntryForKey(TKey key, out DictionaryEntry entry)
	{
		entry = new DictionaryEntry();
		int index = -1;
		if (_keyedEntryCollection.Contains(key))
		{
			entry = _keyedEntryCollection[key];
			index = _keyedEntryCollection.IndexOf(entry);
		}
		return index;
	}

	/// <summary>
	/// Raises the CollectionChanged event with the provided arguments.
	/// </summary>
	/// <param name="args">Details of the change.</param>
	protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
	{
		_collectionChanged?.Invoke(this, args);
	}

	/// <summary>
	/// Raises the PropertyChanged event for the specified property name.
	/// </summary>
	/// <param name="name">The name of the property that changed.</param>
	protected virtual void OnPropertyChanged(string name)
	{
		_propertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	/// <summary>
	/// Called to remove an entry with the specified key from the dictionary.
	/// </summary>
	/// <param name="key">The key of the entry to remove.</param>
	/// <returns>True if the entry was removed; otherwise, false.</returns>
	protected virtual bool RemoveEntry(TKey key)
	{
		return _keyedEntryCollection.Remove(key);
	}

	/// <summary>
	/// Called to set the value for an entry with the specified key in the dictionary.
	/// </summary>
	/// <param name="key">The key of the entry to update.</param>
	/// <param name="value">The new value to set.</param>
	/// <returns>True if the value was changed; otherwise, false.</returns>
	protected virtual bool SetEntry(TKey key, TValue value)
	{
		var keyExists = _keyedEntryCollection.Contains(key);

		// if identical key/value pair already exists, nothing to do
		if (keyExists && value.Equals((TValue)_keyedEntryCollection[key].Value))
			return false;

		// otherwise, remove the existing entry
		if (keyExists)
			_keyedEntryCollection.Remove(key);

		// add the new entry
		_keyedEntryCollection.Add(new DictionaryEntry(key, value));

		return true;
	}

	#endregion protected

	#region private

	private void DoAddEntry(TKey key, TValue value)
	{
		if (AddEntry(key, value))
		{
			_version++;

			var index = GetIndexAndEntryForKey(key, out var entry);
			FireEntryAddedNotifications(entry, index);
		}
	}

	private void DoClearEntries()
	{
		if (ClearEntries())
		{
			_version++;
			FireResetNotifications();
		}
	}

	private bool DoRemoveEntry(TKey key)
	{
		var index = GetIndexAndEntryForKey(key, out var entry);

		var result = RemoveEntry(key);
		if (result)
		{
			_version++;
			if (index > -1)
				FireEntryRemovedNotifications(entry, index);
		}

		return result;
	}

	private void DoSetEntry(TKey key, TValue value)
	{
		var index = GetIndexAndEntryForKey(key, out var entry);

		if (SetEntry(key, value))
		{
			_version++;

			// if prior entry existed for this key, fire the removed notifications
			if (index > -1)
			{
				FireEntryRemovedNotifications(entry, index);

				// force the property change notifications to fire for the modified entry
				_countCache--;
			}

			// then fire the added notifications
			index = GetIndexAndEntryForKey(key, out entry);
			FireEntryAddedNotifications(entry, index);
		}
	}

	private void FireEntryAddedNotifications(DictionaryEntry entry, int index)
	{
		// fire the relevant PropertyChanged notifications
		FirePropertyChangedNotifications();

		// fire CollectionChanged notification
		OnCollectionChanged(index > -1
			? new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>((TKey)entry.Key, (TValue)entry.Value), index)
			: new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	}

	private void FireEntryRemovedNotifications(DictionaryEntry entry, int index)
	{
		// fire the relevant PropertyChanged notifications
		FirePropertyChangedNotifications();

		// fire CollectionChanged notification
		OnCollectionChanged(index > -1
			? new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>((TKey)entry.Key, (TValue)entry.Value), index)
			: new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	}

	private void FirePropertyChangedNotifications()
	{
		if (Count != _countCache)
		{
			_countCache = Count;
			OnPropertyChanged("Count");
			OnPropertyChanged("Item[]");
			OnPropertyChanged("Keys");
			OnPropertyChanged("Values");
		}
	}

	private void FireResetNotifications()
	{
		// fire the relevant PropertyChanged notifications
		FirePropertyChangedNotifications();

		// fire CollectionChanged notification
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	}

	#endregion private

	#endregion methods

	#region interfaces

	#region IDictionary<TKey, TValue>

	/// <summary>
	/// Adds an element with the provided key and value to the dictionary.
	/// </summary>
	void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
	{
		DoAddEntry(key, value);
	}

	/// <summary>
	/// Removes the element with the specified key from the dictionary.
	/// </summary>
	bool IDictionary<TKey, TValue>.Remove(TKey key)
	{
		return DoRemoveEntry(key);
	}

	/// <summary>
	/// Determines whether the dictionary contains the specified key.
	/// </summary>
	bool IDictionary<TKey, TValue>.ContainsKey(TKey key)
	{
		return _keyedEntryCollection.Contains(key);
	}

	/// <summary>
	/// Gets the value associated with the specified key.
	/// </summary>
	bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
	{
		return TryGetValue(key, out value);
	}

	/// <summary>
	/// Gets a collection containing the keys in the dictionary.
	/// </summary>
	ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

	/// <summary>
	/// Gets a collection containing the values in the dictionary.
	/// </summary>
	ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

	/// <summary>
	/// Gets or sets the element with the specified key.
	/// </summary>
	TValue IDictionary<TKey, TValue>.this[TKey key]
	{
		get => (TValue)_keyedEntryCollection[key].Value;
		set => DoSetEntry(key, value);
	}

	#endregion IDictionary<TKey, TValue>

	#region IDictionary

	/// <summary>
	/// Adds an element with the provided key and value to the dictionary.
	/// </summary>
	void IDictionary.Add(object key, object value)
	{
		DoAddEntry((TKey)key, (TValue)value);
	}

	/// <summary>
	/// Removes all elements from the dictionary.
	/// </summary>
	void IDictionary.Clear()
	{
		DoClearEntries();
	}

	/// <summary>
	/// Determines whether the dictionary contains an element with the specified key.
	/// </summary>
	bool IDictionary.Contains(object key)
	{
		return _keyedEntryCollection.Contains((TKey)key);
	}

	/// <summary>
	/// Returns an IDictionaryEnumerator for the dictionary.
	/// </summary>
	IDictionaryEnumerator IDictionary.GetEnumerator()
	{
		return new Enumerator(this, true);
	}

	/// <summary>
	/// Gets a value indicating whether the dictionary has a fixed size.
	/// </summary>
	bool IDictionary.IsFixedSize => false;

	/// <summary>
	/// Gets a value indicating whether the dictionary is read-only.
	/// </summary>
	bool IDictionary.IsReadOnly => false;

	/// <summary>
	/// Gets or sets the element with the specified key.
	/// </summary>
	object IDictionary.this[object key]
	{
		get => _keyedEntryCollection[(TKey)key].Value;
		set => DoSetEntry((TKey)key, (TValue)value);
	}

	/// <summary>
	/// Gets a collection containing the keys in the dictionary.
	/// </summary>
	ICollection IDictionary.Keys => Keys;

	/// <summary>
	/// Removes the element with the specified key from the dictionary.
	/// </summary>
	void IDictionary.Remove(object key)
	{
		DoRemoveEntry((TKey)key);
	}

	/// <summary>
	/// Gets a collection containing the values in the dictionary.
	/// </summary>
	ICollection IDictionary.Values => Values;

	#endregion IDictionary

	#region ICollection<KeyValuePair<TKey, TValue>>

	/// <summary>
	/// Adds the specified key-value pair to the dictionary.
	/// </summary>
	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> kvp)
	{
		DoAddEntry(kvp.Key, kvp.Value);
	}

	/// <summary>
	/// Removes all key-value pairs from the dictionary.
	/// </summary>
	void ICollection<KeyValuePair<TKey, TValue>>.Clear()
	{
		DoClearEntries();
	}

	/// <summary>
	/// Determines whether the dictionary contains the specified key-value pair.
	/// </summary>
	bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> kvp)
	{
		return _keyedEntryCollection.Contains(kvp.Key);
	}

	/// <summary>
	/// Copies the key-value pairs of the dictionary to the specified array starting at the given index.
	/// </summary>
	void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException(nameof(array), "CopyTo() failed:  array parameter was null");
		}
		if ((index < 0) || (index > array.Length))
		{
			throw new ArgumentOutOfRangeException(nameof(index), "CopyTo() failed:  index parameter was outside the bounds of the supplied array");
		}
		if ((array.Length - index) < _keyedEntryCollection.Count)
		{
			throw new ArgumentException("CopyTo() failed:  supplied array was too small", nameof(array));
		}

		foreach (DictionaryEntry entry in _keyedEntryCollection)
			array[index++] = new KeyValuePair<TKey, TValue>((TKey)entry.Key, (TValue)entry.Value);
	}

	/// <summary>
	/// Gets the number of key-value pairs contained in the dictionary.
	/// </summary>
	int ICollection<KeyValuePair<TKey, TValue>>.Count => _keyedEntryCollection.Count;

	/// <summary>
	/// Gets a value indicating whether the dictionary is read-only.
	/// </summary>
	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

	/// <summary>
	/// Removes the specified key-value pair from the dictionary.
	/// </summary>
	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> kvp)
	{
		return DoRemoveEntry(kvp.Key);
	}

	#endregion ICollection<KeyValuePair<TKey, TValue>>

	#region ICollection

	/// <summary>
	/// Copies the elements of the dictionary to an Array, starting at a particular Array index.
	/// </summary>
	void ICollection.CopyTo(Array array, int index)
	{
		((ICollection)_keyedEntryCollection).CopyTo(array, index);
	}

	/// <summary>
	/// Gets the number of key-value pairs in the dictionary.
	/// </summary>
	int ICollection.Count => _keyedEntryCollection.Count;

	/// <summary>
	/// Gets a value indicating whether access to the dictionary is synchronized (thread safe).
	/// </summary>
	bool ICollection.IsSynchronized => ((ICollection)_keyedEntryCollection).IsSynchronized;

	/// <summary>
	/// Gets an object that can be used to synchronize access to the dictionary.
	/// </summary>
	object ICollection.SyncRoot => ((ICollection)_keyedEntryCollection).SyncRoot;

	#endregion ICollection

	#region IEnumerable<KeyValuePair<TKey, TValue>>

	/// <summary>
	/// Returns an enumerator that iterates through the dictionary.
	/// </summary>
	IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
	{
		return new Enumerator(this, false);
	}

	#endregion IEnumerable<KeyValuePair<TKey, TValue>>

	#region IEnumerable

	/// <summary>
	/// Returns an enumerator that iterates through the dictionary.
	/// </summary>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion IEnumerable

	#region ISerializable

	/// <summary>
	/// Populates a SerializationInfo with the data needed to serialize the dictionary.
	/// </summary>
	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException(nameof(info));
		}

		var entries = new Collection<DictionaryEntry>();
		foreach (var entry in _keyedEntryCollection)
			entries.Add(entry);
		info.AddValue("entries", entries);
	}

	#endregion ISerializable

	#region IDeserializationCallback

	/// <summary>
	/// Runs when the entire object graph has been deserialized.
	/// </summary>
	public virtual void OnDeserialization(object sender)
	{
		if (_siInfo != null)
		{
			var entries = (Collection<DictionaryEntry>)
				_siInfo.GetValue("entries", typeof(Collection<DictionaryEntry>));
			foreach (var entry in entries)
				AddEntry((TKey)entry.Key, (TValue)entry.Value);
		}
	}

	#endregion IDeserializationCallback

	#region INotifyCollectionChanged

	/// <summary>
	/// Occurs when the collection changes.
	/// </summary>
	event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
	{
		add => _collectionChanged += value;
		remove => _collectionChanged -= value;
	}

	private NotifyCollectionChangedEventHandler _collectionChanged;

	#endregion INotifyCollectionChanged

	#region INotifyPropertyChanged

	/// <summary>
	/// Occurs when a property value changes.
	/// </summary>
	event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
	{
		add => _propertyChanged += value;
		remove => _propertyChanged -= value;
	}

	private PropertyChangedEventHandler _propertyChanged;

	#endregion INotifyPropertyChanged

	#endregion interfaces

	#region protected classes

	#region KeyedDictionaryEntryCollection<TKey>

	/// <summary>
	/// Represents a keyed collection of DictionaryEntry items using TKey as the key.
	/// </summary>
	protected class KeyedDictionaryEntryCollection : KeyedCollection<TKey, DictionaryEntry>
	{
		#region constructors

		#region public

		/// <summary>
		/// Initializes a new instance of the KeyedDictionaryEntryCollection class.
		/// </summary>
		public KeyedDictionaryEntryCollection() { }

		/// <summary>
		/// Initializes a new instance of the KeyedDictionaryEntryCollection class with the specified comparer.
		/// </summary>
		/// <param name="comparer">The comparer to use for key comparisons.</param>
		public KeyedDictionaryEntryCollection(IEqualityComparer<TKey> comparer) : base(comparer) { }

		#endregion public

		#endregion constructors

		#region methods

		#region protected

		/// <summary>
		/// Extracts the key from the DictionaryEntry.
		/// </summary>
		/// <param name="entry">The DictionaryEntry for which to get the key.</param>
		/// <returns>The key associated with the entry.</returns>
		protected override TKey GetKeyForItem(DictionaryEntry entry)
		{
			return (TKey)entry.Key;
		}

		#endregion protected

		#endregion methods
	}

	#endregion KeyedDictionaryEntryCollection<TKey>

	#endregion protected classes

	#region public structures

	#region Enumerator

	[Serializable, StructLayout(LayoutKind.Sequential)]
	private struct Enumerator(ObservableDictionary<TKey, TValue> dictionary, bool isDictionaryEntryEnumerator) : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
	{
		#region properties

		public readonly KeyValuePair<TKey, TValue> Current
		{
			get
			{
				ValidateCurrent();
				return _current;
			}
		}

		#endregion properties

		#region methods

		public readonly void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		public bool MoveNext()
		{
			ValidateVersion();
			_index++;
			if (_index < dictionary._keyedEntryCollection.Count)
			{
				_current = new KeyValuePair<TKey, TValue>((TKey)dictionary._keyedEntryCollection[_index].Key, (TValue)dictionary._keyedEntryCollection[_index].Value);
				return true;
			}
			_index = -2;
			_current = new KeyValuePair<TKey, TValue>();
			return false;
		}

		private readonly void ValidateCurrent()
		{
			if (_index == -1)
			{
				throw new InvalidOperationException("The enumerator has not been started.");
			}
			else if (_index == -2)
			{
				throw new InvalidOperationException("The enumerator has reached the end of the collection.");
			}
		}

		private readonly void ValidateVersion()
		{
			if (_version != dictionary._version)
			{
				throw new InvalidOperationException("The enumerator is not valid because the dictionary changed.");
			}
		}

		#endregion methods

		#region IEnumerator implementation

		readonly object IEnumerator.Current
		{
			get
			{
				ValidateCurrent();
				if (isDictionaryEntryEnumerator)
				{
					return new DictionaryEntry(_current.Key, _current.Value);
				}
				return new KeyValuePair<TKey, TValue>(_current.Key, _current.Value);
			}
		}

		void IEnumerator.Reset()
		{
			ValidateVersion();
			_index = -1;
			_current = new KeyValuePair<TKey, TValue>();
		}

		#endregion IEnumerator implementation

		#region IDictionaryEnumerator implementation

		readonly DictionaryEntry IDictionaryEnumerator.Entry
		{
			get
			{
				ValidateCurrent();
				return new DictionaryEntry(_current.Key, _current.Value);
			}
		}
		readonly object IDictionaryEnumerator.Key
		{
			get
			{
				ValidateCurrent();
				return _current.Key;
			}
		}
		readonly object IDictionaryEnumerator.Value
		{
			get
			{
				ValidateCurrent();
				return _current.Value;
			}
		}

		#endregion
		#region fields

		private readonly int _version = dictionary._version;
		private int _index = -1;
		private KeyValuePair<TKey, TValue> _current = new();

		#endregion fields
	}

	#endregion Enumerator

	#endregion public structures

	#region fields

	private readonly KeyedDictionaryEntryCollection _keyedEntryCollection;

	private int _countCache;
	private readonly Dictionary<TKey, TValue> _dictionaryCache = [];
	private int _dictionaryCacheVersion;
	private int _version;

	[NonSerialized]
	private readonly SerializationInfo _siInfo;

	#endregion fields
}