// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FifoSeriesColumn.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    internal class FifoSeriesColumn<T> : BaseSeriesColumn<T>
    {
        private readonly T[] _doubleBuffer;
        private readonly int _fifoSize;

        public FifoSeriesColumn(int size)
        {
            _fifoSize = size;
            _innerList = new FifoBuffer<T>(size);
            _doubleBuffer = new T[size];
        }

        public override UncheckedList<T> ToUncheckedList(int baseIndex, int count)
        {
            int checkedBaseIndex = baseIndex > this.Count ? this.Count : baseIndex;
            int checkedCount = Math.Min(this.Count - checkedBaseIndex, count);

            if (_innerList.Count == _fifoSize)
            {
                ((FifoBuffer<T>)_innerList).CopyTo(checkedBaseIndex, _doubleBuffer, 0, checkedCount);
                return new UncheckedList<T>(_doubleBuffer, 0, checkedCount);
            }

            return new UncheckedList<T>(_innerList.ItemsArray, checkedBaseIndex, checkedCount);
        }

        public T[] ToArray()
        {
            if (_innerList.Count == _fifoSize)
            {
                _innerList.CopyTo(_doubleBuffer, 0);
                return _doubleBuffer;
            }

            return _innerList.ToArray();
        }

        public IList<T> ToUnorderedUncheckedList()
        {
            return _innerList.ItemsArray;
        }
    }
}