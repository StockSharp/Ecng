// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FifoBuffer.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Linq;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics.GenericMath;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    internal class FifoBuffer<T> : IUltraList<T>
    {
        private T[] _innerList;
        private int _startIndex = -1;

        private readonly int _size;
        private int _usedSize;

        public int Size { get { return _size; } }
        public int Count { get { return _usedSize; } internal set { _usedSize = value; } }

        public FifoBuffer(int size)
        {
            _size = size;
            _innerList = new T[size];
        }

        public T Add(T item)
        {
            var index = NextIndex();

            _innerList[index] = item;
            _usedSize = Math.Min(_usedSize + 1, _size);

            return item;
        }

        public T GetMaximum()
        {
            return ArrayOperations.Maximum(_innerList, 0, _usedSize);
        }

        public T GetMinimum()
        {
            return ArrayOperations.Minimum(_innerList, 0, _usedSize);
        }

        public void AddRange(IEnumerable<T> items)
        {
            int start = NextIndex();
            var innerArray = _innerList;

            int rem = Size - start;

            // Array copy impl (fastest)
            var array = items as Array;
            if (array != null)
            {
                // If items count is greater than size, set startIndex to 0 and 
                // copy last N items into local array
                ArrayAddRange(start, array, array.Length, rem, innerArray);
                return;
            }

            // IList impl (fast)
            var iList = items as IList<T>;
            if (iList != null)
            {

                var srcInnerArray = iList.ToUncheckedList();
                ArrayAddRange(start, srcInnerArray, iList.Count, rem, innerArray);
                return;
            }

            // IEnumerable impl (slow)
            var enumerableArray = items.ToArray();
            ArrayAddRange(start, enumerableArray, enumerableArray.Length, rem, innerArray);
        }

        public void InsertRange(int index, IEnumerable<T> items)
        {
            throw new NotSupportedException("Insert is not a supported operation on a Fifo Buffer");
        }

        public void RemoveRange(int index, int count)
        {
            throw new NotSupportedException("Remove is not a supported operation on a Fifo Buffer");
        }

        private void ArrayAddRange(int start, Array array, int srcCount, int rem, T[] innerArray)
        {
            if (srcCount > Size)
            {
                Array.Copy(array, srcCount - Size, _innerList, 0, Size);
                _startIndex = -1;
                _usedSize = Size;
                return;
            }

            // If Items Count is less than remaining size then copy from start-Index to end
            // of input array without wrap-around
            if (srcCount < rem)
            {
                Array.Copy(array, 0, innerArray, start, srcCount);
                _usedSize = Math.Min(_usedSize + srcCount, _size);
                _startIndex = start + srcCount - 1;
                return;
            }

            // Case where remaining to end of internal buffer is less than array length, 
            // we must copy in two blocks
            int firstBlockCount = rem;
            int secondBlockCount = srcCount - rem;
            Array.Copy(array, 0, _innerList, start, firstBlockCount);
            Array.Copy(array, rem, _innerList, 0, secondBlockCount);

            _usedSize = Math.Min(_usedSize + srcCount, _size);
            _startIndex = secondBlockCount - 1;
        }

        public int IndexOf(T item)
        {
            int rawIndex = Array.IndexOf(_innerList, item, 0, _size);
            return rawIndex != -1 ? ReverseResolveIndex(rawIndex) : -1;
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException("Insert is not a supported operation on a Fifo Buffer");
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException("Remove is not a supported operation on a Fifo Buffer");
        }

        T IList<T>.this[int index]
        {
            get
            {
                return GetItemAt(index);
            }
            set
            {
                SetItemAt(index, value);
            }
        }

        public T this[int index]
        {
            get
            {
                return GetItemAt(index);
            }
        }

        private int NextIndex()
        {
            if (_startIndex >= 0 || _usedSize == Size) //Now rotating
            {
                _startIndex = (_startIndex + 1) % Size;
                if (_startIndex > _usedSize)
                    _startIndex = _usedSize;

                return _startIndex;
            }
            else
            {
                return _usedSize;
            }
        }

        internal int ReverseResolveIndex(int index)
        {
            if (_startIndex < 0)
                return index;
            else
                return (index - _startIndex + _usedSize - 1) % _usedSize;
        }

        internal int ResolveIndex(int index)
        {
            if (_startIndex < 0)
                return index;
            else
                return (_startIndex + 1 + index) % _usedSize;
        }

        private void ValidateIndex(int index)
        {
            if (index < 0 || index >= _usedSize)
                throw new IndexOutOfRangeException();
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public void Clear()
        {
            _innerList = new T[_size];
            _startIndex = 0;
            _usedSize = 0;
        }

        public bool Contains(T item)
        {
            return Array.IndexOf(_innerList, item, 0, _size) != -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            int start = _startIndex + 1;
            int rem = array.Length - start;
            Array.Copy(_innerList, start, array, arrayIndex, rem);
            int secondBlockSize = array.Length - rem;
            if (secondBlockSize > 0)
                Array.Copy(_innerList, 0, array, rem + arrayIndex, secondBlockSize);
        }

        internal void CopyTo(int sourceIndex, T[] array, int destinationIndex, int count)
        {
            int start = _startIndex + 1 + sourceIndex;
            if (start < array.Length)
            {
                int rem = Math.Min(array.Length - start, count);
                Array.Copy(_innerList, start, array, destinationIndex, rem);
                int secondBlockSize = count - rem;
                if (secondBlockSize > 0)
                    Array.Copy(_innerList, 0, array, destinationIndex + rem, secondBlockSize);
            }
            else
            {
                start -= array.Length;
                Array.Copy(_innerList, start, array, destinationIndex, count);
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            int rawIndex = Array.IndexOf(_innerList, item, 0, _size);
            if (rawIndex >= 0)
            {
                _usedSize--;
                // 6 7 3 4 5, rawIndex = 4 (value=5)
                // 6 7 3 4 5, rawIndex = 2 (value=3)
                if (rawIndex < Size-1)
                {
                    Array.Copy(_innerList, rawIndex+1, _innerList, rawIndex, Size-rawIndex-1);
                }

                _innerList[_usedSize] = default(T);
                if (rawIndex <= _startIndex)
                    _startIndex--;

                return true;
            }
            else
                return false;
        }

        public T GetItemAt(int index)
        {
            ValidateIndex(index);
            int i = ResolveIndex(index);
            return _innerList[i];
        }

        public void SetItemAt(int index, T value)
        {
            ValidateIndex(index);
            int i = ResolveIndex(index);
            _innerList[i] = value;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_usedSize == 0) yield break;

            int i = _startIndex + 1;
            for (; i < _usedSize; i++)
                yield return _innerList[i];

            for (i = 0; i <= _startIndex; i++)
                yield return _innerList[i];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal int StartIndex
        {
            get { return _startIndex; }
            set { _startIndex = value; }
        }

        public T[] ItemsArray
        {
            get { return _innerList; }
        }

        public void SetCount(int setLength)
        {
            throw new InvalidOperationException("SetCount is not valid on Circular-Buffer list types");
        }
    }
}
