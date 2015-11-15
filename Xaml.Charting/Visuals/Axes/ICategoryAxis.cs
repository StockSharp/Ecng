// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ICategoryAxis.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Model.DataSeries;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Defines the interface to a category axis, which treats X-data as linearly increasing regardless of value
    /// </summary>
    public interface ICategoryAxis : IAxis
    {
        /// <summary>
        /// Converts the CategoryDateTimeAxis's <see cref="AxisBase.VisibleRange"/> of type <see cref="IndexRange"/> to a <see cref="DateRange"/> of concrete date-values.
        /// Note: If either index is outside of the range of data on the axis, the date values will be inteporlated.
        /// </summary>
        /// <param name="visibleRange">The input <see cref="IndexRange"/></param>
        /// <returns>The <see cref="DateRange"/> with transformed dates that correspond to input indices</returns>
        DateRange ToDateRange(IndexRange visibleRange);
    }
}