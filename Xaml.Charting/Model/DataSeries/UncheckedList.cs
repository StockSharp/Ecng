// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UncheckedList.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Text;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// A list wrapper that gives access to the underlying array. 
    /// TODO: Not fully implemented
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class UncheckedList<T> : IList<T>
    {
        private T[] array;
        private int baseIndex;
        private int count;

        internal UncheckedList(T[] array, int baseIndex, int count)
        {
            this.array = array;
            this.baseIndex = baseIndex;
            this.count = count;
        }

        internal UncheckedList(T[] array)
        {
            this.array = array;
            this.count = array.Length;
        }

        internal T[] Array { get { return this.array; } }

        internal int BaseIndex { get { return this.baseIndex; } }

        public int Count { get { return this.count; } }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public T this[int index]
        {
            get 
            { 
                return this.Array[index + this.BaseIndex]; 
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

}
