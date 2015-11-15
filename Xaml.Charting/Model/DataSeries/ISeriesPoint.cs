// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ISeriesPoint.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// Defines the interface to a Series Point, an internally used structure which contains transformed points to render Y-values on the <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/>
    /// </summary>
    /// <typeparam name="T">The Type of the Y-Values</typeparam>
    public interface ISeriesPoint<T> : IComparable where T:IComparable
    {
        /// <summary>
        /// Gets the maximum of this <see cref="ISeriesPoint{T}"/>. In the case of an <see cref="OhlcSeriesPoint"/> this would be the High value
        /// </summary>
        T Max { get; }

        /// <summary>
        /// Gets the minimum of this <see cref="ISeriesPoint{T}"/>. In the case of an <see cref="OhlcSeriesPoint"/> this would be the Low value
        /// </summary>
        T Min { get; }

        /// <summary>
        /// Gets the default Y-value of this <see cref="ISeriesPoint{T}"/>. In the case of an <see cref="OhlcSeriesPoint"/> this would be the Close value
        /// </summary>
        T Y { get; }
    }
}