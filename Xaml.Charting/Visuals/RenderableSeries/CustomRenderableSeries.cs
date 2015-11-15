// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// CustomRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Defines a Custom Renderable Series - override Draw() to define what is drawn to the screen at render time
    /// </summary>
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    public class CustomRenderableSeries : BaseRenderableSeries
    {
        /// <summary>
        /// Draws the series using the <see cref="IRenderContext2D"/> and the <see cref="IRenderPassData"/> passed in
        /// </summary>
        /// <param name="renderContext">The render context. This is a graphics object which has methods to draw lines, quads and polygons to the screen</param>
        /// <param name="renderPassData">The render pass data. Contains a resampled <see cref="IPointSeries"/>, the <see cref="IndexRange"/> of points on the screen
        /// and the current YAxis and XAxis <see cref="ICoordinateCalculator{T}"/> to convert data-points to screen points</param>
        protected sealed override void InternalDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            this.Draw(renderContext, renderPassData);
        }

        /// <summary>
        /// Draws the series using the <see cref="IRenderContext2D"/> and the <see cref="IRenderPassData"/> passed in
        /// </summary>
        /// <param name="renderContext">The render context. This is a graphics object which has methods to draw lines, quads and polygons to the screen</param>
        /// <param name="renderPassData">The render pass data. Contains a resampled <see cref="IPointSeries"/>, the <see cref="IndexRange"/> of points on the screen
        /// and the current YAxis and XAxis <see cref="ICoordinateCalculator{T}"/> to convert data-points to screen points</param>
        protected virtual void Draw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
        }
    }
}
