// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FastMountainRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.PointMarkers;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Provides Fast Mountain (Area) series rendering, however makes the assumption that all X-Data is evenly spaced. Gaps in the data are collapsed
    /// </summary>
    /// <remarks>
    /// A RenderableSeries has a <see cref="IDataSeries"/> data-source, 
    /// may have a <see cref="BasePointMarker"/> point-marker, and draws onto a specific <see cref="RenderSurfaceBase"/> using the <see cref="IRenderContext2D"/>. 
    /// A given <see cref="UltrachartSurface"/> may have 0..N <see cref="BaseRenderableSeries"/>, each of which may map to, or share a <see cref="IDataSeries"/>
    /// </remarks>
    /// <seealso cref="IDataSeries"/>
    /// <seealso cref="BasePointMarker"/>
    /// <seealso cref="IRenderContext2D"/>
    /// <seealso cref="FastLineRenderableSeries"/>
    /// <seealso cref="FastMountainRenderableSeries"/>
    /// <seealso cref="FastColumnRenderableSeries"/>
    /// <seealso cref="FastOhlcRenderableSeries"/>
    /// <seealso cref="XyScatterRenderableSeries"/>
    /// <seealso cref="FastCandlestickRenderableSeries"/>
    /// <seealso cref="FastBandRenderableSeries"/>
    /// <seealso cref="FastErrorBarsRenderableSeries"/>
    /// <seealso cref="FastBoxPlotRenderableSeries"/>
    /// <seealso cref="FastBubbleRenderableSeries"/>
    /// <seealso cref="FastHeatMapRenderableSeries"/>
    /// <seealso cref="StackedColumnRenderableSeries"/>
    /// <seealso cref="StackedMountainRenderableSeries"/>
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    public class FastMountainRenderableSeries : BaseMountainRenderableSeries
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FastMountainRenderableSeries" /> class.
        /// </summary>
        public FastMountainRenderableSeries()
        {
            DefaultStyleKey = typeof(FastMountainRenderableSeries);
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="BaseRenderableSeries"/> is valid for drawing.
        /// </summary>
        protected override bool GetIsValidForDrawing()
        {
            var isValid = base.GetIsValidForDrawing() &&
                          ((SeriesColor.A != 0 && StrokeThickness > 0) || AreaBrush != null);
            return isValid;
        }

        /// <summary>
        /// Draws the series using the <see cref="IRenderContext2D" /> and the <see cref="IRenderPassData" /> passed in
        /// </summary>
        /// <param name="renderContext">The render context. This is a graphics object which has methods to draw lines, quads and polygons to the screen</param>
        /// <param name="renderPassData">The render pass data. Contains a resampled 
        /// <see cref="IPointSeries" />, the 
        /// <see cref="IndexRange" /> of points on the screen
        /// and the current YAxis and XAxis 
        /// <see cref="ICoordinateCalculator{T}" /> to convert data-points to screen points</param>
        protected override void InternalDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            var gradientRotationAngle = GetChartRotationAngle(renderPassData);

            // End Y-point or X-point(depends on chart orientation) is either the height (e.g. the bottom of the chart pane), or the zero line (e.g. if the chart has negative numbers)
            var zeroCoord = (float)GetYZeroCoord();
            var pointSeries = CurrentRenderPassData.PointSeries;
            var lineColor = SeriesColor;
            
            using (var areaBrush = renderContext.CreateBrush(AreaBrush, Opacity))
            using (var linePen = renderContext.CreatePen(SeriesColor, AntiAliasing, StrokeThickness, Opacity))
            {
                var linesPathFactory =  SeriesDrawingHelpersFactory.GetLinesPathFactory(renderContext, CurrentRenderPassData);
                var mountainPathFactory = SeriesDrawingHelpersFactory.GetMountainAreaPathFactory(renderContext,CurrentRenderPassData,zeroCoord, gradientRotationAngle);
                
                if (PaletteProvider != null)
                {
                    // If the line is paletted, use the penned DrawLines technique
                    using (var penManager = new PenManager(renderContext, AntiAliasing, StrokeThickness, Opacity))
                    {
                        // NOTE: No disposed closure here as IterateLines function is synchronous
                        Func<double, double, IPen2D> createPenFunc = (x, y) =>
                        {
                            var color = PaletteProvider.GetColor(this, x, y) ?? lineColor;
                            // ReSharper disable once AccessToDisposedClosure
                            return penManager.GetPen(color);
                        };

                        Func<double, double, IBrush2D> createBrushFunc = (x, y) =>
                        {
                            var color = PaletteProvider.GetColor(this, x, y);
                            // ReSharper disable AccessToDisposedClosure
                            return color != null ? penManager.GetBrush(color.Value) : areaBrush;
                            // ReSharper restore AccessToDisposedClosure
                        };

                        FastLinesHelper.IterateLines(
                            mountainPathFactory,
                            createBrushFunc,
                            pointSeries,
                            CurrentRenderPassData.XCoordinateCalculator,
                            CurrentRenderPassData.YCoordinateCalculator,
                            IsDigitalLine,
                            false);

                        FastLinesHelper.IterateLines(
                            linesPathFactory, 
                            createPenFunc,
                            pointSeries,
                            CurrentRenderPassData.XCoordinateCalculator,
                            CurrentRenderPassData.YCoordinateCalculator,
                            IsDigitalLine,
                            DrawNaNAs == LineDrawMode.ClosedLines);
                    }
                }
                else
                {
                    FastLinesHelper.IterateLines(
                        mountainPathFactory,
                        areaBrush, pointSeries,
                        CurrentRenderPassData.XCoordinateCalculator,
                        CurrentRenderPassData.YCoordinateCalculator,
                        IsDigitalLine,
                        false);

                    FastLinesHelper.IterateLines(linesPathFactory, linePen, pointSeries,
                        CurrentRenderPassData.XCoordinateCalculator,
                        CurrentRenderPassData.YCoordinateCalculator,
                        IsDigitalLine,
                        false);
                }

                var pointMarker = GetPointMarker();
                if (pointMarker != null)
                {
                    var pointMarkerPathFactory =
                    SeriesDrawingHelpersFactory.GetPointMarkerPathFactory(renderContext, CurrentRenderPassData,
                        pointMarker);

                    FastPointsHelper.IteratePoints(pointMarkerPathFactory,
                        pointSeries,
                        CurrentRenderPassData.XCoordinateCalculator,
                        CurrentRenderPassData.YCoordinateCalculator);

                }
            }
        }

        /// <summary>
        /// When overridden in derived classes, performs hit test on series using interpolated values
        /// </summary>
        /// <param name="rawPoint"></param>
        /// <param name="nearestHitResult"></param>
        /// <param name="hitTestRadius"></param>
        /// <param name="yValues"></param>
        /// <param name="previousDataPoint"> </param>
        /// <param name="nextDataPoint"></param>
        /// <returns></returns>
        protected override bool IsHitTest(Point rawPoint, HitTestInfo nearestHitResult, double hitTestRadius, Point previousDataPoint, Point nextDataPoint)
        {
            var isHit = base.IsHitTest(rawPoint, nearestHitResult, hitTestRadius, previousDataPoint, nextDataPoint);

            var hitDataValue = GetHitDataValue(rawPoint);

            // Check if is in X range
            if (!isHit && hitDataValue.Item1.ToDouble() >= previousDataPoint.X && hitDataValue.Item1.ToDouble() <= nextDataPoint.X)
            {
                var interpolatedY = nearestHitResult.YValue.ToDouble();
                if (!double.IsNaN(interpolatedY))
                {
                    var topLine = new PointUtil.Line(previousDataPoint, nextDataPoint);
                    var perpendicular = new PointUtil.Line(new Point(hitDataValue.Item1.ToDouble(), ZeroLineY), 
                                                           new Point(hitDataValue.Item1.ToDouble(), Math.Max(previousDataPoint.Y, nextDataPoint.Y)));
                    Point topIntersect;
                    PointUtil.LineIntersection2D(topLine, perpendicular, out topIntersect);
                    isHit = hitDataValue.Item2.ToDouble().CompareTo(ZeroLineY) >= 0 && hitDataValue.Item2.ToDouble().CompareTo(topIntersect.Y) <= 0;
                }
            }

            return isHit;
        }
    }
}