// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// XyySeriesPoint.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// A structure to contain Xyy series point values for the Y-Axis only
    /// </summary>
    public struct XyySeriesPoint : ISeriesPoint<double>, IComparable
    {
        private readonly double _y0;
        private readonly double _y1;

        /// <summary>
        /// Initializes a new instance of the <see cref="XyySeriesPoint" /> struct.
        /// </summary>
        /// <param name="y0">The y0 value.</param>
        /// <param name="y1">The y1 value.</param>
        public XyySeriesPoint(double y0, double y1) : this()
        {
            _y0 = y0;
            _y1 = y1;
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        /// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance is less than <paramref name="obj" />. Zero This instance is equal to <paramref name="obj" />. Greater than zero This instance is greater than <paramref name="obj" />.
        /// </returns>
        public int CompareTo(object obj)
        {
            var other = (XyySeriesPoint) obj;
            if (Y0 > other.Max || Y1 > other.Max) return 1;
            if (Y0 < other.Min || Y1 < other.Min) return -1;

            return 0;
        }

        /// <summary>
        /// Gets the Y0 value of the Xyy point
        /// </summary>
        public double Y0 { get { return _y0; } }

        /// <summary>
        /// Gets the Y1 value of the Xyy point
        /// </summary>
        public double Y1 { get { return _y1; } }

        /// <summary>
        /// Gets the maximum of this <see cref="ISeriesPoint{T}"/>. In the case of an <see cref="OhlcSeriesPoint"/> this would be the High value
        /// </summary>
        public double Max { get { return Math.Max(_y0, _y1); } }

        /// <summary>
        /// Gets the minimum of this <see cref="ISeriesPoint{T}"/>. In the case of an <see cref="OhlcSeriesPoint"/> this would be the Low value
        /// </summary>
        public double Min { get { return Math.Min(_y0, _y1); } }

        /// <summary>
        /// Gets the default Y-value of this <see cref="ISeriesPoint{T}"/>. In the case of an <see cref="XyySeriesPoint"/> this would be the Y0 value. 
        /// </summary>
        public double Y
        {
            get { return Y0; }
        }
    }
}