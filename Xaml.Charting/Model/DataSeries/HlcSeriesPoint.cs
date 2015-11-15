// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// HlcSeriesPoint.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// An HlcSeriesPoint is an internally used structure which contains transformed points to render Y-values on the <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/>. 
    /// Used in conjuction with the <see cref="FastErrorBarsRenderableSeries"/> and <see cref="HlcDataSeries{TX,TY}"/>
    /// </summary>
    public struct HlcSeriesPoint : ISeriesPoint<double>, IComparable
    {
        /// <summary>
        /// Gets the maximum of this <see cref="ISeriesPoint{T}"/>. 
        /// </summary>
        public double Max { get { return YErrorHigh; } }
        /// <summary>
        /// Gets the minimum of this <see cref="ISeriesPoint{T}"/>.
        /// </summary>
        public double Min { get { return YErrorLow; } }
        /// <summary>
        /// Gets the default Y-value of this <see cref="ISeriesPoint{T}"/>. 
        /// </summary>
        public double Y { get; private set; }
        /// <summary>
        /// Gets the Y-High value of this <see cref="ISeriesPoint{T}"/>. 
        /// </summary>
        public double YErrorHigh { get; private set; }
        /// <summary>
        /// Gets the Y-Low value of this <see cref="ISeriesPoint{T}"/>. 
        /// </summary>
        public double YErrorLow { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HlcSeriesPoint" /> struct.
        /// </summary>
        /// <param name="y">The y.</param>
        /// <param name="yErrorHigh">The y error high.</param>
        /// <param name="yErrorLow">The y error low.</param>
        public HlcSeriesPoint(double y, double yErrorHigh, double yErrorLow)
            : this()
        {
            Y = y;
            YErrorHigh = yErrorHigh;
            YErrorLow = yErrorLow;
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        /// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="obj" /> in the sort order. Zero This instance occurs in the same position in the sort order as <paramref name="obj" />.Greater than zero This instance follows <paramref name="obj" /> in the sort order.
        /// </returns>
        public int CompareTo(object obj)
        {
            var other = (HlcSeriesPoint)obj;
            return Max > other.Max ? 1 :
                       Min < other.Min ? -1 :
                           0;
        }
    }
}