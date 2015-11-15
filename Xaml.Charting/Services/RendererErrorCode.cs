// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RendererErrorCode.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

using Ecng.Xaml.Charting.Common;

namespace Ecng.Xaml.Charting.Services
{
    /// <summary>
    /// Error code returned by the Renderer in 2D or 3D
    /// </summary>
    public class RendererErrorCode : StringlyTyped
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RendererErrorCode"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public RendererErrorCode(string value) : base(value)
        {
        }
    }

    internal class RendererErrorCodes
    {
        internal static readonly RendererErrorCode BecauseThereAreNoDataSeries = new RendererErrorCode("Because none of the UltrachartSurface.RenderableSeries has a DataSeries assigned");
        internal static readonly RendererErrorCode BecauseThereAreNoRenderableSeries = new RendererErrorCode("Because the UltrachartSurface.RenderableSeries collection is null or empty. Please ensure that you have set some RenderableSeries with RenderableSeries.DataSeries assigned, or you have set Axis.VisibleRange for all axes in order to view a blank chart.");
        internal static readonly RendererErrorCode BecauseRenderSurfaceIsNull = new RendererErrorCode("Because the UltrachartSurface.RenderSurface is null. Please ensure either the default RenderSurface is set, or a custom one has been assigned to UltrachartSurface.RenderSurface");
        internal static readonly RendererErrorCode BecauseXAxesOrYAxesIsNull = new RendererErrorCode("Because the UltrachartSurface.XAxes or UltrachartSurface.YAxes property is null. Please ensure that the UltrachartSurface.XAxes and UltrachartSurface.YAxes properties have been set, e.g. check for binding errors if using MVVM");
        internal static readonly RendererErrorCode BecauseThereAreNoYAxes = new RendererErrorCode("Because the UltrachartSurface has no YAxes. Please ensure UltrachartSurface.YAxis is set, or UltrachartSurface.YAxes has at least one axis");
        internal static readonly RendererErrorCode BecauseThereAreNoXAxes = new RendererErrorCode("Because the UltrachartSurface has no XAxes. Please ensure UltrachartSurface.XAxis is set, or UltrachartSurface.XAxes has at least one axis");        
        internal static readonly RendererErrorCode BecauseViewportSizeIsNotValid = new RendererErrorCode("Because the UltrachartSurface Viewport Size is not valid (e.g. 0 sized)");
        internal static readonly RendererErrorCode BecauseVisibleRangeIsNullOrZeroOnOneOrMoreXOrYAxes = new RendererErrorCode("Because the VisibleRange on one or more X or Y Axes is null, zero or undefined.");
        internal static readonly RendererErrorCode ToEnsureVisibleRangeAtStartupHaveDataSeries = new RendererErrorCode("To ensure Auto-Range on Startup works, please check all your RenderableSeries have DataSeries assigned, or, just set a VisibleRange on all your axes");
        internal static readonly RendererErrorCode Success = new RendererErrorCode("");
        internal static readonly RendererErrorCode BecauseOneOrMoreWarningsOccurred = new RendererErrorCode("Because one or more Warnings occurred during rendering");
        internal static readonly RendererErrorCode ToDisableThisMessage = new RendererErrorCode(". To disable this message set UltrachartSurface.DebugWhyDoesntUltrachartRender=False");
        internal static readonly RendererErrorCode BecauseUsingCategoryDateTimeAxisAndNoRenderableSeries = new RendererErrorCode("Because the RenderableSeries Collection is null or empty and you are using CategoryDateTimeAxis, which requires at least one RenderableSeries to draw");
        internal static readonly RendererErrorCode BecauseTickProviderIsNull = new RendererErrorCode("Because the TickProvider on one or more X or Y Axes is null. Please ensure that the TickProvider is not null");
    }
}