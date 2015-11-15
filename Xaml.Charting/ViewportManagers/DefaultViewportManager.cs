// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DefaultViewportManager.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Diagnostics;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// The DefaultViewportManager performs a naive calculation for X and Y Axis VisibleRange. 
    /// On each render of the parent UltrachartSurface, either autorange to fit the data (depending on the Axis.AutoRange property value), 
    /// or return the original axis range (no change)
    /// </summary>
    public class DefaultViewportManager : ViewportManagerBase
    {
        /// <summary>
        /// Called when the <see cref="IAxisParams.VisibleRange"/> changes for an axis. Override in derived types to get a notification of this occurring
        /// </summary>
        /// <param name="axis">The <see cref="IAxis"/>instance</param>
        public override void OnVisibleRangeChanged(IAxis axis)
        {
            // Debug.WriteLine("{0}Axis VisibleRange: {1}, {2}", axis.IsXAxis ? "X" : "Y", axis.VisibleRange.Min, axis.VisibleRange.Max);
        }

        /// <summary>
        /// Called when the <see cref="IUltrachartSurface" /> is rendered.
        /// </summary>
        /// <param name="ultraChartSurface">The UltrachartSurface instance</param>
        public override void OnParentSurfaceRendered(IUltrachartSurface ultraChartSurface)
        {
            
        }

        /// <summary>
        /// Overridden by derived types, called when the parent <see cref="UltrachartSurface" /> requests the XAxis VisibleRange.
        /// The Range returned by this method will be applied to the chart on render
        /// </summary>
        /// <param name="xAxis">The XAxis</param>
        /// <returns>
        /// The new VisibleRange for the XAxis
        /// </returns>
        protected override IRange OnCalculateNewXRange(IAxis xAxis)
        {
            IRange newXRange = xAxis.VisibleRange;

            // Calculate the VisibleRange of X Axis, depending on AutoRange property
            if (xAxis.AutoRange == AutoRange.Always)
            {
                newXRange = CalculateAutoRange(xAxis);
            }

            return newXRange;
        }

        /// <summary>
        /// Overridden by derived types, called when the parent <see cref="UltrachartSurface" /> requests a YAxis VisibleRange.
        /// The Range returned by this method will be applied to the chart on render
        /// </summary>
        /// <param name="yAxis">The YAxis</param>
        /// <param name="renderPassInfo"></param>
        /// <returns>
        /// The new VisibleRange for the YAxis
        /// </returns>
        protected override IRange OnCalculateNewYRange(IAxis yAxis, RenderPassInfo renderPassInfo)
        {
            IRange newYRange = yAxis.VisibleRange;

            if ((yAxis.AutoRange == AutoRange.Always) &&
                renderPassInfo.PointSeries != null &&
                renderPassInfo.RenderableSeries != null)
            {
                newYRange = yAxis.CalculateYRange(renderPassInfo);
            }

            return newYRange;
        }
    }
}