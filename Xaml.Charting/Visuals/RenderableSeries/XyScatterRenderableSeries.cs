// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// XyScatterRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Provides Scatter series rendering via the <see cref="BaseRenderableSeries.PointMarker"/> property. 
    /// </summary>
    /// <remarks><see cref="XyScatterRenderableSeries"/> does not support resampling and so ignores the <see cref="BaseRenderableSeries.ResamplingMode"/> property</remarks>
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    public class XyScatterRenderableSeries : BaseRenderableSeries
    {
        /// <summary>
        /// The DoClusterResampling property
        /// </summary>
        public static readonly DependencyProperty DoClusterResamplingProperty = DependencyProperty.Register("DoClusterResampling", typeof(bool), typeof(BaseRenderableSeries), new PropertyMetadata(false));

        // todo adjust it according to screen size
        private const int _binsWidth = 400;
        private const int _binsHeight = 300;
        private static byte[] _bins;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="XyScatterRenderableSeries"/> should use Cluster Resampling
        /// </summary>
        public bool DoClusterResampling
        {
            get { return (bool)GetValue(DoClusterResamplingProperty); }
            set { SetValue(DoClusterResamplingProperty, value); }
        }

        /// <summary>
        /// If true, the data is displayed as XY, e.g. like a Scatter plot, not a line (time) series
        /// </summary>
        public override bool DisplaysDataAsXy
        {
            get { return true; }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="XyScatterRenderableSeries"/> class.
        /// </summary>
        /// <remarks></remarks>
        public XyScatterRenderableSeries()
        {
            DefaultStyleKey = typeof(XyScatterRenderableSeries);

            this.SetCurrentValue(ResamplingModeProperty, ResamplingMode.None);
        }

        /// <summary>
        /// Draws the series using the <see cref="IRenderContext2D"/> and the <see cref="IRenderPassData"/> passed in
        /// </summary>
        /// <param name="renderContext">The render context. This is a graphics object which has methods to draw lines, quads and polygons to the screen</param>
        /// <param name="renderPassData">The render pass data. Contains a resampled <see cref="IPointSeries"/>, the <see cref="IndexRange"/> of points on the screen
        /// and the current YAxis and XAxis <see cref="ICoordinateCalculator{T}"/> to convert data-points to screen points</param>
        protected override void InternalDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            var pointSeries = CurrentRenderPassData.PointSeries;
            var rpd = CurrentRenderPassData;
            var pm = GetPointMarker();

            if (pm != null)
            {
                var paletteProvider = PaletteProvider;
                var seriesColor = SeriesColor;

                var pointMarkerPathFactory = SeriesDrawingHelpersFactory.GetPointMarkerPathFactory(renderContext, CurrentRenderPassData,pm);
                if (paletteProvider != null)
                {
                    // If the line is paletted, use the penned DrawLines technique
                    using (var penManager = new PenManager(renderContext, AntiAliasing, StrokeThickness, Opacity))
                    {
                        // NOTE: No disposed closure here as IterateLines function is synchronous
                        Func<double, double, IPen2D> createPenFunc = (x, y) =>
                        {
                            var color = PaletteProvider.GetColor(this, x, y) ?? seriesColor;
                            return penManager.GetPen(color);
                        };

                        FastPointsHelper.IteratePoints(pointMarkerPathFactory, createPenFunc, pointSeries, rpd.XCoordinateCalculator, rpd.YCoordinateCalculator);
                    }
                }
                else
                {
                    // Re-use the same code above to DrawLines, but this time passing the PointMarker wrapped in a PointMarkerLineContextFactory, 
                    // so the PointMarkerLineContextFactory will receive the Begin(), MoveTo() calls, which will be used to draw PointMarkers
                    // 
                    // NOTE: This code is syntactically equivalent to a for-loop over POintSeries XValues, YValues, calling pointMarker.Draw at each point. We just choose to re-use highly optimized code inside FastLinesHelper
                    FastPointsHelper.IteratePoints(pointMarkerPathFactory, pointSeries, rpd.XCoordinateCalculator, rpd.YCoordinateCalculator);
                }
            }
        }

        protected override HitTestInfo HitTestInternal(Point rawPoint, double hitTestRadius, bool interpolate)
        {
            return base.HitTestInternal(rawPoint, GetHitTestRadiusConsideringPointMarkerSize(hitTestRadius), false);
        }
    }
}