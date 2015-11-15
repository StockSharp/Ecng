// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// Array2DSegment.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Represents part of 2D data for a specific X coordinate
    /// Provides list of vertical pixels selected from 2D data for a specific X index
    /// </summary>
    internal class Array2DSegment<TX, TY> : I2DArraySegment
        where TX : IComparable
        where TY : IComparable
    {
        private readonly int _xIndex;
        private readonly IHeatmap2DArrayDataSeriesInternal _dataSeries;
        private readonly Func<int, TX> _xMapping;
        private readonly Func<int, TY> _yMapping;
        private readonly int _arrayHeight;

        public Array2DSegment(IHeatmap2DArrayDataSeries dataSeries, Func<int, TX> xMapping, Func<int, TY> yMapping, int xIndex)
        {
            _arrayHeight = dataSeries.ArrayHeight;
            _xIndex = xIndex;
            _xMapping = xMapping;
            _yMapping = yMapping;
            _dataSeries = (IHeatmap2DArrayDataSeriesInternal)dataSeries;
        }

        public double X
        {
            get { return _xMapping(_xIndex).ToDouble(); }
        }

        public double Y
        {
            get { return _yMapping(_arrayHeight).ToDouble(); }
        }

        public double XValueAtLeft { get { return _xMapping(_xIndex).ToDouble(); } }

        public double XValueAtRight { get { return _xMapping(_xIndex + 1).ToDouble(); } }

        public double YValueAtBottom
        {
            get { return _yMapping(0).ToDouble(); }
        }
        public double YValueAtTop
        {
            get { return _yMapping(_arrayHeight).ToDouble(); }
        }

        public IList<int> GetVerticalPixelsArgb(DoubleToColorMappingSettings mappingSettings)
        {
            return new VerticalPixels(_arrayHeight, _dataSeries.GetArgbColorArray2D(mappingSettings), _xIndex);
        }

        public IList<double> GetVerticalPixelValues()
        {
            return new VerticalPixelValues(_arrayHeight, _dataSeries.GetArray2D(), _xIndex);
        }

        private class VerticalPixels: IList<int>
        {
            private readonly int _count, _xIndex;
            private readonly int[,] _argbColorArray2d;
            public VerticalPixels(int count, int[,] argbColorArray2d, int xIndex)
            {
                _xIndex = xIndex;
                _count = count;
                _argbColorArray2d = argbColorArray2d;
            }

            public int IndexOf(int item)
            {
                throw new NotImplementedException();
            }

            public void Insert(int index, int item)
            {
                throw new NotImplementedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotImplementedException();
            }

            public int this[int index]
            {
                get
                {
                    return _argbColorArray2d[index, _xIndex];
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public void Add(int item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(int item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(int[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public int Count
            {
                get { return _count; }
            }

            public bool IsReadOnly
            {
                get { throw new NotImplementedException(); }
            }

            public bool Remove(int item)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<int> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        private class VerticalPixelValues: IList<double>
        {
            private readonly int _count, _xIndex;
            private readonly double[,] _array2d;
            public VerticalPixelValues(int count, double[,] array2d, int xIndex)
            {
                _xIndex = xIndex;
                _count = count;
                _array2d = array2d;
            }

            public int IndexOf(double item)
            {
                throw new NotImplementedException();
            }

            public void Insert(int index, double item)
            {
                throw new NotImplementedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotImplementedException();
            }

            public double this[int index]
            {
                get
                {
                    return _array2d[index, _xIndex];
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public void Add(double item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(double item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(double[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public int Count
            {
                get { return _count; }
            }

            public bool IsReadOnly
            {
                get { throw new NotImplementedException(); }
            }

            public bool Remove(double item)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<double> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
    }
}