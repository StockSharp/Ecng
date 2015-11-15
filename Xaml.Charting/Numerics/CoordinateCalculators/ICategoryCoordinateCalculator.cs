// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ICategoryCoordinateCalculator.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Numerics.CoordinateCalculators
{
    /// <summary>
    /// Defines the interface to a <see cref="CategoryDateTimeAxis"/> specific ICoordinateCalculator, to convert from pixel coordinate to index to data value and back
    /// </summary>
    public interface ICategoryCoordinateCalculator : ICoordinateCalculator<double>
    {
        int TransformDataToIndex(IComparable dataValue);

        /// <summary>
        /// Takes an integer index (or point number) to the underlying data and transforms to the data value on the axis. 
        /// 
        /// e.g. if the axis is a CategoryDateTimeAxis, accepts index, returns DateTime. 
        /// 
        /// If the index lies outside of the data-range, a projection is performed
        /// </summary>
        /// <param name="index">The index to the underlying data series</param>
        /// <returns>The data value</returns>
        DateTime TransformIndexToData(int index);

        /// <summary>
        /// Takes a DateTime data-value and transforms to integer index on the axis
        /// 
        /// e.g. if the axis is a CategoryDateTimeAxis, accepts DateTime, returns index. 
        /// 
        /// If the DateTime lies outside of the data-range, a projection is performed
        /// </summary>
        /// <param name="dataValue">The data value</param>
        /// <returns>The index to the underlying data series</returns>
        int TransformDataToIndex(DateTime dataValue);

        /// <summary>
        /// Takes a DateTime data-value and transforms to integer index on the axis
        /// 
        /// e.g. if the axis is a CategoryDateTimeAxis, accepts DateTime, returns index. 
        /// 
        /// If the DateTime lies outside of the data-range, a projection is performed
        /// </summary>
        /// <param name="dataValue">The data value</param>
        /// <param name="searchMode">Indicates a way in wich to look for the <paramref name="dataValue"/></param>
        /// <returns>The index to the underlying data series or -1 if <see cref="SearchMode.Exact"/> and the <paramref name="dataValue"/> doesn't exist.</returns>
        int TransformDataToIndex(DateTime dataValue, SearchMode searchMode);
    }
}