// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IPointSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Defines the interface to a collection of <see cref="IPoint"/>, a subset of X,Y data used to render points to the screen
    /// </summary>
    /// <seealso cref="Point2DSeries"/>
    /// <seealso cref="GenericPointSeriesBase{TY}"/>
    /// <seealso cref="ISeriesPoint{T}"/>
    /// <seealso cref="IPoint"/>    
    public interface IPointSeries 
    {
        /// <summary>
        /// Gets the Raw X-Values for the PointSeries
        /// </summary>
        IUltraList<double> XValues { get; }

        /// <summary>
        /// Gets the Raw Y-Values for the PointSeries
        /// </summary>
        IUltraList<double> YValues { get; }        

        /// <summary>
        /// Gets the count of the PointSeries
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the <see cref="IPoint"/> at the specified index, which merges the X,Y and higher order values into a single point
        /// </summary>
        /// <seealso cref="IGenericPointSeries{TY}"/>
        /// <seealso cref="ISeriesPoint{T}"/>
        IPoint this[int index] { get; }

        /// <summary>
        /// Gets the min, max range in the Y-Direction
        /// </summary>
        /// <returns>A <see cref="DoubleRange"/> defining the min, max in the Y-direction</returns>
        DoubleRange GetYRange();
    }
}