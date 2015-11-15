// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// OhlcSeriesPoint.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// A structure to contain OHLC series point values for the Y-Axis only
    /// </summary>
    public struct OhlcSeriesPoint : ISeriesPoint<double>, IComparable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OhlcSeriesPoint" /> struct.
        /// </summary>
        /// <param name="open">The open value.</param>
        /// <param name="high">The high value.</param>
        /// <param name="low">The low value.</param>
        /// <param name="close">The close value.</param>
        public OhlcSeriesPoint(double open, double high, double low, double close) : this()
        {
            Open = open;
            High = high;
            Low = low;
            Close = close;
        }

        /// <summary>
        /// Gets the open value
        /// </summary>
        public double Open { get; private set; }

        /// <summary>
        /// Gets the high value
        /// </summary>
        public double High { get; private set; }

        /// <summary>
        /// Gets the low value
        /// </summary>
        public double Low { get; private set; }

        /// <summary>
        /// Gets the close value
        /// </summary>
        public double Close { get; private set; }

        /// <summary>
        /// Gets the maximum of this <see cref="ISeriesPoint{T}"/>. In the case of an <see cref="OhlcSeriesPoint"/> this would be the High value
        /// </summary>
        public double Max { get { return High; } }

        /// <summary>
        /// Gets the minimum of this <see cref="ISeriesPoint{T}"/>. In the case of an <see cref="OhlcSeriesPoint"/> this would be the Low value
        /// </summary>
        public double Min { get { return Low; } }

        /// <summary>
        /// Gets the default Y-value of this <see cref="ISeriesPoint{T}"/>. In the case of an <see cref="OhlcSeriesPoint"/> this would be the Close value
        /// </summary>
        public double Y
        {
            get { return Close; }
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
            var other = (OhlcSeriesPoint) obj;
            return Max > other.Max ? 1 :
                Min < other.Min ? -1 :
                0;
        }
    }
}