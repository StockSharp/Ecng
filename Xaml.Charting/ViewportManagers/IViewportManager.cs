// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IViewportManager.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Defines the interface to a ViewportManager, which may be used to intercept the X,Y axis ranging during render and invalidate the parent surface
    /// </summary>
    public interface IViewportManager : IUltrachartController
    {
        /// <summary>
        /// Gets the <see cref="ServiceContainer"/> which provides access to services throughout Ultrachart. 
        /// ServiceContainers are created one per <see cref="UltrachartSurface"/> instance, 
        /// and shared between peripheral components such as <see cref="AxisBase"/>, <see cref="BaseRenderableSeries"/>, <see cref="ChartModifierBase"/> instances.
        /// For a full list of available services, see the remarks on <see cref="ServiceContainer"/>
        /// </summary>
        IServiceContainer Services { get; set; }

        /// <summary>
        /// Returns <value>True</value> when a <see cref="ViewportManagerBase"/> has the <see cref="UltrachartSurface"/> attached to.
        /// </summary>
        bool IsAttached { get; }

        /// <summary>
        /// Called when the <see cref="IAxisParams.VisibleRange"/> changes for an axis. Override in derived types to get a notification of this occurring
        /// </summary>
        /// <param name="axis">The <see cref="IAxis"/>instance</param>
        void OnVisibleRangeChanged(IAxis axis);

        /// <summary>
        /// Called by the <see cref="UltrachartSurface"/> during render to calculate the new YAxis VisibleRange. Override in derived types to return a custom value
        /// </summary>
        /// <param name="yAxis">The YAxis to calculate for</param>
        /// <param name="renderPassInfo">The current <see cref="RenderPassInfo"/> containing render data</param>
        /// <returns>The new <see cref="IRange"/> VisibleRange for the axis</returns>
        IRange CalculateNewYAxisRange(IAxis yAxis, RenderPassInfo renderPassInfo);

        /// <summary>
        /// Called by the <see cref="UltrachartSurface"/> during render to calculate the new XAxis VisibleRange. Override in derived types to return a custom value
        /// </summary>
        /// <param name="xAxis">The XAxis to calculate for</param>
        /// <returns>The new <see cref="IRange"/> VisibleRange for the axis</returns>
        IRange CalculateNewXAxisRange(IAxis xAxis);

        /// <summary>
        /// Called by the <see cref="UltrachartSurface"/> during render to perform autoranging. Override in derived types to return a custom value
        /// </summary>
        /// <param name="axis">The axis  to calculate for</param>
        /// <returns>
        /// The new <see cref="IRange"/> VisibleRange for the axis
        /// </returns>
        IRange CalculateAutoRange(IAxis axis);

        /// <summary>
        /// Called when the <see cref="IUltrachartSurface"/> is rendered. 
        /// </summary>
        /// <param name="ultraChartSurface">The UltrachartSurface instance</param>
        void OnParentSurfaceRendered(IUltrachartSurface ultraChartSurface);

        /// <summary>
        /// May be called to trigger a redraw on the parent <see cref="UltrachartSurface"/>. See tne <see cref="RangeMode"/> for available options. 
        /// </summary>
        /// <param name="rangeMode">Tne <see cref="RangeMode"/> with options for the re-draw</param>
        void InvalidateParentSurface(RangeMode rangeMode);

        /// <summary>
        /// Called when the <see cref="UltrachartSurface"/> is attached to a <see cref="ViewportManagerBase"/>. May be overridden to get notification of attachment. 
        /// </summary>
        /// <param name="scs">The <see cref="UltrachartSurface"/> instance</param>
        void AttachUltrachartSurface(IUltrachartSurface scs);

        /// <summary>
        /// Called when the <see cref="UltrachartSurface"/> is detached from a <see cref="ViewportManagerBase"/>. May be overridden to get notification of detachment. 
        /// </summary>
        void DetachUltrachartSurface();
    }
}