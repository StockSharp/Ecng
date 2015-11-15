// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// StackedColumnsWrapper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Model.DataSeries;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Defines the mode of setting spacing between <see cref="StackedColumnRenderableSeries"/> groups
    /// </summary>
    public enum SpacingMode
    {
        /// <summary>
        /// For setting spacing in pixels
        /// </summary>
        Absolute,
        /// <summary>
        /// For setting spacing in fraction of column group width
        /// </summary>
        Relative
    }

    /// <summary>
    /// Defines interface for <see cref="StackedColumnsWrapper"/>
    /// </summary>
    public interface IStackedColumnsWrapper : IStackedSeriesWrapperBase<IStackedColumnRenderableSeries>
    {
        /// <summary>
        /// Gets fraction of the data point width
        /// </summary>
        double GetDataPointWidthFraction();

        /// <summary>
        /// Returns DataPointWith of <see cref="IStackedColumnRenderableSeries"/> considering spacing between groups
        /// </summary>
        double GetSeriesBodyWidth(IStackedColumnRenderableSeries series, int dataSeriesIndex);

        /// <summary>
        /// Returns Upper and Lower Bound of <see cref="IStackedColumnRenderableSeries"/> column
        /// </summary>
        Tuple<double, double> GetSeriesVerticalBounds(IStackedColumnRenderableSeries series, int indexInDataSeries);

        /// <summary>
        /// Returns the data range of all the assosiated <see cref="IDataSeries"/> on X direction
        /// </summary>
        IRange GetXRange(bool isLogarithmicAxis);
    }
}