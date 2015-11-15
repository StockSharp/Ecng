// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// Point2DSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics.GenericMath;


namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Defines a strongly typed PointSeries of Point2D items, a subset of X,Y data used to render points to the screen
    /// </summary>
    public class Point2DSeries : IPoint2DListSeries
    {
        private UltraList<double> xValues;
        private UltraList<double> yValues;
        private double[] _xItems;
        private double[] _yItems;
        private bool _isFrozen;

        /// <summary>
        /// Initializes a new instance of the <see cref="PointSeries"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public Point2DSeries(int capacity)
        {
            xValues = new UltraList<double>(capacity);
            yValues = new UltraList<double>(capacity);            
        }

        /// <summary>
        /// Gets the min, max range in the Y-Direction
        /// </summary>
        /// <returns>
        /// A <see cref="DoubleRange"/> defining the min, max in the Y-direction
        /// </returns>
        public DoubleRange GetYRange()
        {
            var uncheckedList = yValues.ItemsArray;
            int count = this.yValues.Count;

            double min;
            double max;
            ArrayOperations.MinMax(uncheckedList, 0, count, out min, out max);

            return new DoubleRange(min, max);
        }

        public int XBaseIndex
        {
            get { return 0; }
        }

        public void Add(Point2D value)
        {
            this.xValues.Add(value.X);
            this.yValues.Add(value.Y);
        }

        IPoint IPointSeries.this[int index]
        {
            get { return new Point2D(_xItems[index], _yItems[index]); }
        }

        public IUltraList<double> XValues
        {
            get { return xValues; }
        }

        public IUltraList<double> YValues
        {
            get { return this.yValues; }
        }

        UncheckedList<double> IPoint2DListSeries.YValues
        {
            get { return new UncheckedList<double>(this.yValues.ItemsArray); }
        }

        UncheckedList<double> IPoint2DListSeries.XValues
        {
            get { return new UncheckedList<double>(this.xValues.ItemsArray, 0, this.xValues.Count); }
        }

        /// <summary>
        /// Gets the count of the PointSeries
        /// </summary>
        public int Count
        {
            get { return this.yValues.Count; }
        }

        /// <summary>
        /// Freezes this instance, enables caching of inner arrays
        /// </summary>
        public void Freeze()
        {
            _xItems = xValues.ToUncheckedList();
            _yItems = yValues.ToUncheckedList();
        }
    }
}
