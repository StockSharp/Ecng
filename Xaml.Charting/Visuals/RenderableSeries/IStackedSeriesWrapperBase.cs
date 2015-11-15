// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IStackedSeriesWrapperBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Rendering.Common;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Defines common properties to stacked series wrappers, such as <see cref="StackedColumnsWrapper"/> and <see cref="StackedMountainsWrapper"/>
    /// </summary>
    public interface IStackedSeriesWrapperBase<T> where T : IStackedRenderableSeries
    {
        /// <summary>
        /// Gets a value indicating whether all the series within the StackedGroup are 100% stacked.
        /// </summary>
        bool IsOneHundredPercentGroup(string groupId);

        /// <summary>
        /// Accumulate Y value at <see cref="index"/> for a stacked series,
        /// where Tuple.Item1 represents the upper series value at the <see cref="index"/> and Tuple.Item2 - the lower one.
        /// </summary>
        Tuple<double, double> AccumulateYValueAtX(T series, int index, bool isResampledSeries = false);

        /// <summary>
        /// Gets the YRange of the data (min, max of the <paramref name="series"/>) in the passed <param name="xIndexRange"/>, where indices are point-indices on the DataSeries columns.
        /// </summary>
        DoubleRange CalculateYRange(T series, IndexRange xIndexRange);

        /// <summary>
        /// Draws all the <see cref="IStackedRenderableSeries"/> being wrapped, using the <see cref="IRenderContext2D"/> passed in.
        /// </summary>
        void DrawStackedSeries(IRenderContext2D renderContext);

        /// <summary>
        /// Called internaly to correlate a hit-test result for the <param name="series"/>.
        /// </summary>
        HitTestInfo ShiftHitTestInfo(Point rawPoint, HitTestInfo nearestHitResult, double hitTestRadius, T series);

        /// <summary>
        /// Adds the <see cref="IStackedRenderableSeries"/> to the internal collection.
        /// </summary>
        void AddSeries(T series);

        /// <summary>
        /// Removes the <see cref="IStackedRenderableSeries"/> from the internal collection.
        /// </summary>
        void RemoveSeries(T series);

        /// <summary>
        /// Called internally to move series from one StackedGroup to another.
        /// </summary>
        void MoveSeriesToAnotherGroup(T rSeries, string oldGroupId, string newGroupId);

        /// <summary>
        /// Returns all the <see cref="IStackedRenderableSeries"/> from the same StackedGroup. 
        /// </summary>
        IList<T> GetStackedSeriesFromSameGroup(string groupId);

        /// <summary>
        /// Returns amount of the series being stacked.
        /// </summary>
        int GetStackedSeriesCount();
    }
}