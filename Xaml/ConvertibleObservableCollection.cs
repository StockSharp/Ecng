namespace Ecng.Xaml
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	public class ConvertibleObservableCollection<TItem, TDisplay> : BaseObservableCollection, IListEx<TItem>
			where TDisplay : class
	{
		private readonly ICollection<TDisplay> _collection;
		private readonly Func<TItem, TDisplay> _converter;
		private readonly Dictionary<TItem, TDisplay> _convertedValues = new Dictionary<TItem, TDisplay>();

		public ConvertibleObservableCollection(ICollection<TDisplay> collection, Func<TItem, TDisplay> converter)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			if (converter == null)
				throw new ArgumentNullException(nameof(converter));

			_collection = collection;
			_converter = converter;
		}

		private object SyncRoot => ((ICollection)_collection).SyncRoot;

		public TItem[] Items
		{
			get
			{
				lock (SyncRoot)
					return _convertedValues.Keys.ToArray();
			}
		}

		public TDisplay TryGet(TItem item)
		{
			lock (SyncRoot)
				return _convertedValues.TryGetValue(item);
		}

		public event Action<IEnumerable<TItem>> AddedRange
		{
			add { throw new NotSupportedException(); }
			remove { throw new NotSupportedException(); }
		}

		public event Action<IEnumerable<TItem>> RemovedRange
		{
			add { throw new NotSupportedException(); }
			remove { throw new NotSupportedException(); }
		}

		public void AddRange(IEnumerable<TItem> items)
		{
			lock (SyncRoot)
			{
				var converted = new List<TDisplay>();

				foreach (var item in items)
				{
					var display = _converter(item);
					_convertedValues.Add(item, display);
					converted.Add(display);
				}

				_collection.AddRange(converted);
			}

			CheckCount();
		}

		public void RemoveRange(IEnumerable<TItem> items)
		{
			lock (SyncRoot)
			{
				var converted = new List<TDisplay>();

				foreach (var item in items)
				{
					var display = TryGet(item);

					if (display == null)
						continue;

					_convertedValues.Remove(item);
					converted.Add(display);
				}

				_collection.RemoveRange(converted);
			}
		}

		public override int RemoveRange(int index, int count)
		{
			lock (SyncRoot)
			{
				var items = _convertedValues.Keys.Skip(index).Take(count).ToArray();
				RemoveRange(items);
				return items.Length;
			}
		}

		public void Clear()
		{
			lock (SyncRoot)
			{
				_convertedValues.Clear();
				_collection.Clear();
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<TItem> GetEnumerator()
		{
			lock (SyncRoot)
				return _convertedValues.Keys.GetEnumerator();
		}

		/// <summary>
		/// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
		public void Add(TItem item)
		{
			lock (SyncRoot)
			{
				var display = _converter(item);
				_convertedValues.Add(item, display);
				_collection.Add(display);
			}

			CheckCount();
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <returns>
		/// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </returns>
		/// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
		public bool Remove(TItem item)
		{
			lock (SyncRoot)
			{
				var display = TryGet(item);

				if (display == null)
					return false;

				_convertedValues.Remove(item);
				_collection.Remove(display);
				return true;
			}
		}

		/// <summary>
		/// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <returns>
		/// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </returns>
		public override int Count
		{
			get
			{
				lock (SyncRoot)
					return _convertedValues.Count;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
		/// </summary>
		/// <returns>
		/// true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.
		/// </returns>
		bool ICollection<TItem>.IsReadOnly => false;

		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
		/// </summary>
		/// <returns>
		/// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
		/// </returns>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
		public bool Contains(TItem item)
		{
			lock (SyncRoot)
				return _convertedValues.ContainsKey(item);
		}

		/// <summary>
		/// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception><exception cref="T:System.ArgumentException">The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
		public void CopyTo(TItem[] array, int arrayIndex)
		{
			lock (SyncRoot)
				_convertedValues.Keys.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
		/// </summary>
		/// <returns>
		/// The index of <paramref name="item"/> if found in the list; otherwise, -1.
		/// </returns>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
		public int IndexOf(TItem item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1"/> at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param><param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1"/>.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
		public void Insert(int index, TItem item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Removes the <see cref="T:System.Collections.Generic.IList`1"/> item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets or sets the element at the specified index.
		/// </summary>
		/// <returns>
		/// The element at the specified index.
		/// </returns>
		/// <param name="index">The zero-based index of the element to get or set.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
		public TItem this[int index]
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}