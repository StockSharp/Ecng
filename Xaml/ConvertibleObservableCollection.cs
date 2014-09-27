namespace Ecng.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	public class ConvertibleObservableCollection<TItem, TDisplay> : ThreadSafeObservableCollection<TDisplay>, IList<TItem>, ICollectionEx<TItem>
		where TDisplay : class
	{
		private readonly Func<TItem, TDisplay> _converter;
		private readonly Dictionary<TItem, TDisplay> _convertedValues = new Dictionary<TItem, TDisplay>();

		public ConvertibleObservableCollection(Func<TItem, TDisplay> converter)
		{
			if (converter == null)
				throw new ArgumentNullException("converter");

			_converter = converter;
		}

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

				AddRange(converted);
			}
		}

		public IEnumerable<TItem> RemoveRange(IEnumerable<TItem> items)
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

				RemoveRange(converted);
			}

			// TODO
			return items;
		}

		public override void RemoveRange(int index, int count)
		{
			lock (SyncRoot)
			{
				RemoveRange(_convertedValues.Keys.Skip(index).Take(count).ToArray());
			}
		}

		public override void Clear()
		{
			lock (SyncRoot)
			{
				_convertedValues.Clear();
				base.Clear();
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
		/// </returns>
		public new IEnumerator<TItem> GetEnumerator()
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
				Add(display);
			}
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

				Remove(display);
				return true;	
			}
		}

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
		/// Gets or sets the element at the specified index.
		/// </summary>
		/// <returns>
		/// The element at the specified index.
		/// </returns>
		/// <param name="index">The zero-based index of the element to get or set.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
		public new TItem this[int index]
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		//protected override IEnumerable<TDisplay> GetRange(int index, int count)
		//{
		//	lock (SyncRoot)
		//		return _convertedValues.Values.Skip(index).Take(count);
		//}
	}
}