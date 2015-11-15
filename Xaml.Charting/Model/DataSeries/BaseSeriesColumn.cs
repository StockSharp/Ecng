// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// BaseSeriesColumn.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    internal abstract class BaseSeriesColumn<T> : ISeriesColumn<T>
    {
        protected IUltraList<T> _innerList = new UltraList<T>(128);

        public virtual UncheckedList<T> ToUncheckedList(int baseIndex, int count)
        {
            int checkedBaseIndex = baseIndex > this.Count ? this.Count : baseIndex;
            int checkedCount = Math.Min(this.Count - checkedBaseIndex, count);
            return new UncheckedList<T>(_innerList.ItemsArray, checkedBaseIndex, checkedCount);
        }

        public T[] UncheckedArray()
        {
            return _innerList.ItemsArray;
        }

        public void Add(T item)
        {
            _innerList.Add(item);
        }

        public int Add(object value)
        {
            return ((IList)_innerList).Add(value);
        }

        public bool Contains(object value)
        {
            return ((IList) _innerList).Contains(value);
        }

        public void Clear()
        {
            _innerList.Clear();
        }

        public int IndexOf(object value)
        {
            if (Count == 0)
                return -1;

            T converted = (T)Convert.ChangeType(value, typeof (T), null);
            return IndexOf(converted);
        }

        public void Insert(int index, object value)
        {
            _innerList.Insert(index, (T)value);
        }

        public void Remove(object value)
        {
            _innerList.Remove((T)value);
        }

        public bool Contains(T item)
        {
            return _innerList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _innerList.CopyTo(array, arrayIndex);
        }

        bool ICollection<T>.Remove(T item)
        {
            return _innerList.Remove(item);
        }

        public T GetMinimum()
        {
            return _innerList.GetMinimum();
        }

        public T GetMaximum()
        {
            return _innerList.GetMaximum();
        }

        public void AddRange(IEnumerable<T> items)
        {
            _innerList.AddRange(items);
        }

        public void InsertRange(int startIndex, IEnumerable<T> values)
        {
            _innerList.InsertRange(startIndex, values);
        }

        public void RemoveRange(int startIndex, int count)
        {
            _innerList.RemoveRange(startIndex, count);
        }

        public void Remove(T item)
        {
            _innerList.Remove(item);
        }

        public int IndexOf(T item)
        {
            return _innerList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _innerList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _innerList.RemoveAt(index);
        }

        object IList.this[int index]
        {
            get { return _innerList[index]; }
            set { _innerList[index] = (T)value; }
        }

        public T this[int index]
        {
            get { return _innerList[index]; }
            set { _innerList[index] = value; }
        }

        public void CopyTo(Array array, int index)
        {
            ((IList)_innerList).CopyTo(array, index);            
        }

        public int Count
        {
            get { return _innerList.Count; }
        }

        public object SyncRoot
        {
            get { return _innerList; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool HasValues
        {
            get { return _innerList.Count != 0; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}