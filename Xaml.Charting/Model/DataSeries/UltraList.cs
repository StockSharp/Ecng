// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UltraList.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
// For full terms and conditions of the license, see http://www.ultrachart.com/ultrachart-eula/
// 
// This source code is protected by international copyright law. Unauthorized
// reproduction, reverse-engineering, or distribution of all or any portion of
// this source code is strictly prohibited.
// 
// This source code contains confidential and proprietary trade secrets of
// ulc software Services Ltd., and should at no time be copied, transferred, sold,
// distributed or made available without express written permission.
// *************************************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security;
using System.Threading;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics.GenericMath;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    public interface IUltraList<T> : IList<T>
    {
        /// <summary>
        /// Gets the maximum in the list
        /// </summary>
        T GetMaximum();

        /// <summary>
        /// Gets the minimum in the list
        /// </summary>
        T GetMinimum();

        /// <summary>
        /// Adds a range of items to the list
        /// </summary>
        void AddRange(IEnumerable<T> items);

        /// <summary>
        /// Inserts a range of items to the list
        /// </summary>
        void InsertRange(int index, IEnumerable<T> items);

        /// <summary>
        /// Removes a range of items from the list
        /// </summary>
        void RemoveRange(int index, int count);

        /// <summary>
        /// Gets the internal ItemsArray that this list wraps for direct unchecked access
        /// NOTE: The count of the ItemsArray may differ from the count of the List. Use the List.Count when iterating
        /// </summary>
        T[] ItemsArray { get; }

        /// <summary>
        /// Forces the count of the list, in operations where we know the capacity in advance
        /// </summary>
        /// <param name="setLength"></param>
        void SetCount(int setLength);
    }

    public interface IUltraReadOnlyList<T> : IUltraList<T> {
    }

    /// <summary>
    /// Implementation of generic list, same as .NET Framework version however we expose the inner array 
    /// for direct manipulation of the array. Tests show this to be around 4x faster than accessing via the indexed
    /// property
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class UltraList<T> : IUltraList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable
    {        
        internal class SynchronizedList : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable
        {
            private readonly List<T> _list;
            private readonly object _root;

            internal SynchronizedList(List<T> list)
            {
                _list = list;
                _root = ((ICollection) list).SyncRoot;
            }

            
            public int Count
            {
                get
                {
                    int count;
                    lock (_root)
                    {
                        count = _list.Count;
                    }
                    return count;
                }
            }

            public bool IsReadOnly
            {
                get { return ((ICollection<T>) _list).IsReadOnly; }
            }

            public T this[int index]
            {
                get
                {
                    T result;
                    lock (_root)
                    {
                        result = _list[index];
                    }
                    return result;
                }
                set
                {
                    lock (_root)
                    {
                        _list[index] = value;
                    }
                }
            }

            public void Add(T item)
            {
                lock (_root)
                {
                    _list.Add(item);
                }
            }

            public void Clear()
            {
                lock (_root)
                {
                    _list.Clear();
                }
            }

            public bool Contains(T item)
            {
                bool result;
                lock (_root)
                {
                    result = _list.Contains(item);
                }
                return result;
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                lock (_root)
                {
                    _list.CopyTo(array, arrayIndex);
                }
            }

            public bool Remove(T item)
            {
                bool result;
                lock (_root)
                {
                    result = _list.Remove(item);
                }
                return result;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                IEnumerator result;
                lock (_root)
                {
                    result = _list.GetEnumerator();
                }
                return result;
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                IEnumerator<T> enumerator;
                lock (_root)
                {
                    enumerator = ((IEnumerable<T>) _list).GetEnumerator();
                }
                return enumerator;
            }

            public int IndexOf(T item)
            {
                int result;
                lock (_root)
                {
                    result = _list.IndexOf(item);
                }
                return result;
            }

            public void Insert(int index, T item)
            {
                lock (_root)
                {
                    _list.Insert(index, item);
                }
            }

            public void RemoveAt(int index)
            {
                lock (_root)
                {
                    _list.RemoveAt(index);
                }
            }

        }

        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private readonly UltraList<T> _list;
            private readonly int _version;
            private T _current;
            private int _index;

            internal Enumerator(UltraList<T> list)
            {
                _list = list;
                _index = 0;
                _version = list._version;
                _current = default(T);
            }

            
            public T Current
            {
                get { return _current; }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _list._size + 1)
                    {
                        throw new InvalidOperationException("Enumerator Index out of range");
                    }
                    return Current;
                }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                UltraList<T> list = _list;
                if (_version == list._version && _index < list._size)
                {
                    _current = list._items[_index];
                    _index++;
                    return true;
                }
                return MoveNextRare();
            }

            void IEnumerator.Reset()
            {
                if (_version != _list._version)
                {
                    throw new InvalidOperationException("Enumerator version is invalid");
                }
                _index = 0;
                _current = default(T);
            }


            private bool MoveNextRare()
            {
                if (_version != _list._version)
                {
                    throw new InvalidOperationException("Enumerator version is invalid");
                }
                _index = _list._size + 1;
                _current = default(T);
                return false;
            }
        }


        private const int _defaultCapacity = 128;
        private static readonly T[] _emptyArray = new T[0];
        private T[] _items;
        private int _size;
        private object _syncRoot;
        private int _version;

        public UltraList() : this(_defaultCapacity)
        {
        }

        public UltraList(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }
            _items = new T[capacity];
        }

        public static UltraList<T> ForArray(T[] arr) {
            var list = new UltraList<T> {
                _items = arr,
                _size = arr.Length
            };

            return list;
        }

        public UltraList(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            var collection2 = collection as ICollection<T>;
            if (collection2 != null)
            {
                int count = collection2.Count;
                _items = new T[count];
                collection2.CopyTo(_items, 0);
                _size = count;
                return;
            }
            _size = 0;
            _items = new T[_defaultCapacity];
            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Add(enumerator.Current);
                }
            }
        }

        public T[] ItemsArray
        {
            get { return _items; }
        }

        internal int Capacity
        {
            get { return _items.Length; }
            set
            {
                if (value < _size)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        var array = new T[value];
                        if (_size > 0)
                        {
                            Array.Copy(_items, 0, array, 0, _size);
                        }
                        _items = array;
                        return;
                    }
                    _items = _emptyArray;
                }
            }
        }

        
        bool IList.IsFixedSize
        {
            get { return false; }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    Interlocked.CompareExchange<object>(ref _syncRoot, new object(), null);
                }
                return _syncRoot;
            }
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set
            {
                try
                {
                    this[index] = (T) value;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException(string.Format("Value type {0} was of the wrong type",
                                                              value.GetType().Name));
                }
            }
        }

        int IList.Add(object item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            try
            {
                Add((T) item);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(string.Format("Value type {0} was of the wrong type", item.GetType().Name));
            }
            return Count - 1;
        }

        [SecuritySafeCritical]
        bool IList.Contains(object item)
        {
            return IsCompatibleObject(item) && Contains((T) item);
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array != null && array.Rank != 1)
            {
                throw new ArgumentException("Array rank not supported", "array.Rank");
            }
            try
            {
                Array.Copy(_items, 0, array, arrayIndex, _size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("Invalid array type");
            }
        }

        int IList.IndexOf(object item)
        {
            if (IsCompatibleObject(item))
            {
                return IndexOf((T) item);
            }
            return -1;
        }

        void IList.Insert(int index, object item)
        {
            try
            {
                Insert(index, (T) item);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(string.Format("Value type {0} was of the wrong type", item.GetType().Name));
            }
        }

        [SecuritySafeCritical]
        void IList.Remove(object item)
        {
            if (IsCompatibleObject(item))
            {
                Remove((T) item);
            }
        }


        
        public int Count
        {
            get { return _size; }
            internal set { _size = value; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        public T this[int index]
        {
            get
            {
                if (index >= _size)
                {
                    // Quietly ignore out of bounds exception
                    return default(T);
                }
                return _items[index];
            }
            set
            {
                if (index >= _size)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                _items[index] = value;
                _version++;
            }
        }

        /// <summary>Adds an object to the end of the <see cref="T:System.Collections.Generic.List`1" />.</summary>
        /// <param name="item">The object to be added to the end of the <see cref="T:System.Collections.Generic.List`1" />. The value can be null for reference types.</param>
        public void Add(T item)
        {
            if (_size == _items.Length)
            {
                EnsureCapacity(_size + 1);
            }
            _items[_size++] = item;
            _version++;
        }

        public void Clear()
        {
            if (_size > 0)
            {
                Array.Clear(_items, 0, _size);
                _size = 0;
            }
            _version++;
        }

        public bool Contains(T item)
        {
            if (item == null)
            {
                for (int i = 0; i < _size; i++)
                {
                    if (_items[i] == null)
                    {
                        return true;
                    }
                }
                return false;
            }
            EqualityComparer<T> @default = EqualityComparer<T>.Default;
            for (int j = 0; j < _size; j++)
            {
                if (@default.Equals(_items[j], item))
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(_items, item, 0, _size);
        }

        public void Insert(int index, T item)
        {
            if (index > _size)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (_size == _items.Length)
            {
                EnsureCapacity(_size + 1);
            }
            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = item;
            _size++;
            _version++;
        }

        public bool Remove(T item)
        {
            int num = IndexOf(item);
            if (num >= 0)
            {
                RemoveAt(num);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            if (index >= _size)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            _size--;
            if (index < _size)
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = default(T);
            _version++;
        }


        private static bool IsCompatibleObject(object value)
        {
            return value is T || (value == null && default(T) == null);
        }

        public T GetMaximum()
        {
            return ArrayOperations.Maximum(_items, 0, Count);
        }

        public T GetMinimum()
        {
            return ArrayOperations.Minimum(_items, 0, Count);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            InsertRange(_size, collection);
        }

        public IUltraReadOnlyList<T> AsReadOnly() {
            return new UltraReadOnlyList<T>(this);
        }

        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (_size - index < count)
            {
                throw new ArgumentException("Invalid Offset or Length");
            }
            Array.Copy(_items, index, array, arrayIndex, count);
        }

        public bool EnsureMinSize(int minSize) {
            if(Count >= minSize)
                return false;

            EnsureCapacity(minSize);
            Count = minSize;
            return true;
        }

        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                int num = (_items.Length == 0) ? 4 : (_items.Length*2);
                if (num < min)
                {
                    num = min;
                }
                Capacity = num;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public int IndexOf(T item, int index)
        {
            if (index > _size)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return Array.IndexOf(_items, item, index, _size - index);
        }

        public int IndexOf(T item, int index, int count)
        {
            if (index > _size)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (count < 0 || index > _size - count)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            return Array.IndexOf(_items, item, index, count);
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentOutOfRangeException("collection");
            }
            if (index > _size)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            // Array case (Fastest)
            var array = collection as Array;
            if (array != null)
            {
                int count = array.Length;
                EnsureCapacity(_size + count);
                Array.Copy(_items, index, _items, index + count, _size - index);
                Array.Copy(array, 0, _items, index, count);
                _size += count;
                return;
            }

            // IList case (Fast)
            var iList = collection as IList<T>;
            if (iList != null)
            {
                int count = iList.Count;
                var iLstArray = iList.ToUncheckedList();
                EnsureCapacity(_size + count);
                Array.Copy(_items, index, _items, index + count, _size - index);
                Array.Copy(iLstArray, 0, _items, index, count);
                _size += count;
                _version++;
                return;
            }

            // IEnumerable case (slowest)
            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                var indexCopyLength = _size - index;
                var indexCopy = new T[indexCopyLength];
                Array.Copy(_items, index, indexCopy, 0, indexCopyLength);
                while (enumerator.MoveNext())
                {
                    EnsureCapacity(_size + 1);
                    _items[index] = enumerator.Current;
                    ++index;
                    ++_size;
                }
                Array.Copy(indexCopy, 0, _items, index, indexCopyLength);
            }
            _version++;

//            var collection2 = collection as ICollection<T>;
//            if (collection2 != null)
//            {
//                int count = collection2.Count;
//                if (count > 0)
//                {
//                    EnsureCapacity(_size + count);
//                    if (index < _size)
//                    {
//                        Array.Copy(_items, index, _items, index + count, _size - index);
//                    }
//                    if (this == collection2)
//                    {
//                        Array.Copy(_items, 0, _items, index, index);
//                        Array.Copy(_items, index + count, _items, index*2, _size - index);
//                    }
//                    else
//                    {
//                        var array = new T[count];
//                        collection2.CopyTo(array, 0);
//                        array.CopyTo(_items, index);
//                    }
//                    _size += count;
//                }
//            }
//            else
//            {
//                using (IEnumerator<T> enumerator = collection.GetEnumerator())
//                {
//                    while (enumerator.MoveNext())
//                    {
//                        Insert(index++, enumerator.Current);
//                    }
//                }
//            }
            _version++;
        }

        public int LastIndexOf(T item)
        {
            if (_size == 0)
            {
                return -1;
            }
            return LastIndexOf(item, _size - 1, _size);
        }

        public int LastIndexOf(T item, int index)
        {
            if (index >= _size)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return LastIndexOf(item, index, index + 1);
        }

        public int LastIndexOf(T item, int index, int count)
        {
            if (Count != 0 && index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (Count != 0 && count < 0)
            {
                throw new ArgumentOutOfRangeException("Count");
            }
            if (_size == 0)
            {
                return -1;
            }
            if (index >= _size)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (count > index + 1)
            {
                throw new ArgumentOutOfRangeException("Count");
            }
            return Array.LastIndexOf(_items, item, index, count);
        }

        public void RemoveRange(int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if (_size - index < count)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if (count > 0)
            {
                _size -= count;
                if (index < _size)
                {
                    Array.Copy(_items, index + count, _items, index, _size - index);
                }
                Array.Clear(_items, _size, count);
                _version++;
            }
        }

        public T[] ToArray()
        {
            var array = new T[_size];
            Array.Copy(_items, 0, array, 0, _size);
            return array;
        }

        public void TrimExcess()
        {
            var num = (int) (_items.Length*0.9);
            if (_size < num)
            {
                Capacity = _size;
            }
        }

        internal static IList<T> Synchronized(List<T> list)
        {
            return new SynchronizedList(list);
        }

        public void SetCount(int setLength)
        {
            _size = setLength;
        }
    }

    internal class UltraReadOnlyList<T> : IUltraReadOnlyList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable {
        readonly UltraList<T> _parent;

        public UltraReadOnlyList(UltraList<T> parent) {
            _parent = parent;
        }

        public UltraReadOnlyList(T[] arr) {
            _parent = UltraList<T>.ForArray(arr);
        }

        void ThrowReadOnly() { throw new InvalidOperationException("this list is read-only"); }

        public T[] ItemsArray => _parent.ItemsArray;

        internal int Capacity { get { return _parent.Capacity; } set { ThrowReadOnly(); }}

        bool IList.IsFixedSize => false;
        bool IList.IsReadOnly => true;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => ((ICollection)_parent).SyncRoot;
        object IList.this[int index] {get { return _parent[index]; } set { ThrowReadOnly(); }}
        int IList.Add(object item) { ThrowReadOnly(); return 0; }

        bool IList.Contains(object item) {
            return ((IList)_parent).Contains(item);
        }

        void ICollection.CopyTo(Array array, int arrayIndex) {
            ((ICollection)_parent).CopyTo(array, arrayIndex);
        }

        int IList.IndexOf(object item) {
            return ((IList)_parent).IndexOf(item);
        }

        void IList.Insert(int index, object item) {
            ThrowReadOnly();
        }

        void IList.Remove(object item) {
            ThrowReadOnly();
        }
        
        public int Count {get { return _parent.Count; } internal set { ThrowReadOnly(); }}

        bool ICollection<T>.IsReadOnly => true;

        public T this[int index] { get {return _parent[index];} set { ThrowReadOnly(); } }

        public void Add(T item) {
            ThrowReadOnly();
        }

        public void Clear() {
            ThrowReadOnly();
        }

        public bool Contains(T item) {
            return _parent.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex) {
            _parent.CopyTo(array, arrayIndex);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return _parent.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _parent.GetEnumerator();
        }

        public int IndexOf(T item) {
            return _parent.IndexOf(item);
        }

        public void Insert(int index, T item) {
            ThrowReadOnly();
        }

        public bool Remove(T item) {
            ThrowReadOnly();
            return false;
        }

        public void RemoveAt(int index) {
            ThrowReadOnly();
        }

        public T GetMaximum() { return _parent.GetMaximum(); }
        public T GetMinimum() { return _parent.GetMinimum(); }

        public void AddRange(IEnumerable<T> collection) {
            ThrowReadOnly();
        }

        public void CopyTo(T[] array) {
            _parent.CopyTo(array);
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count) {
            _parent.CopyTo(index, array, arrayIndex, count);
        }

        public bool EnsureMinSize(int minSize) {
            ThrowReadOnly();
            return false;
        }

        public int IndexOf(T item, int index) {
            return _parent.IndexOf(item, index);
        }

        public int IndexOf(T item, int index, int count) {
            return _parent.IndexOf(item, index, count);
        }

        public void InsertRange(int index, IEnumerable<T> collection) {
            ThrowReadOnly();
        }

        public int LastIndexOf(T item) {
            return _parent.LastIndexOf(item);
        }

        public int LastIndexOf(T item, int index) {
            return _parent.LastIndexOf(item, index);
        }

        public int LastIndexOf(T item, int index, int count) {
            return _parent.LastIndexOf(item, index, count);
        }

        public void RemoveRange(int index, int count) {
            ThrowReadOnly();
        }

        public T[] ToArray() {
            return _parent.ToArray();
        }

        public void TrimExcess() {
            ThrowReadOnly();
        }

        public void SetCount(int setLength) {
            ThrowReadOnly();
        }
    }

}