// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FastImpulseRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Diagnostics;
using System.Windows;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Provides Impulse series rendering, which draws a vertical line from zero to with an optional point-marker at the end of the line
    /// </summary>
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    public class FastImpulseRenderableSeries : BaseRenderableSeries
    {
        private IPen2D _linePen;

        /// <summary>
        /// Initializes a new instance of the <see cref="FastImpulseRenderableSeries" /> class.
        /// </summary>
        public FastImpulseRenderableSeries()
        {
            DefaultStyleKey = typeof (FastImpulseRenderableSeries);
        }

        /// <summary>
        /// Returns the data range of the assosiated <see cref="IDataSeries"/> on Y direction
        /// <param name="xRange">The X-Axis Range currently in view</param>
        /// <param name="getPositiveRange">Indicates whether to return positive YRange only</param>
        /// </summary>
        public override IRange GetYRange(IRange xRange, bool getPositiveRange)
        {
            var yRange = base.GetYRange(xRange, getPositiveRange);
            double zeroLineY = ZeroLineY;

            // In case of log axis
            if (getPositiveRange && zeroLineY <= 0d)
            {
                // Do not allow ZeroLineY 
                zeroLineY = yRange.Min.ToDouble();
            }

            return RangeFactory.NewRange(Math.Min(yRange.Min.ToDouble(), zeroLineY), Math.Max(yRange.Max.ToDouble(), zeroLineY));
        }

        protected override HitTestInfo HitTestInternal(Point rawPoint, double hitTestRadius, bool interpolate)
        {
            var headHitRadius = GetHitTestRadiusConsideringPointMarkerSize(hitTestRadius - StrokeThickness/2d);

            // Looking for the nearest DP
            var nearestHitPoint = base.NearestHitResult(rawPoint, headHitRadius, SearchMode.Nearest, true);
            if (!nearestHitPoint.IsHit)
            {
                // Looking for the nearest DP by X
                var nearestHitPointX = base.NearestHitResult(rawPoint, hitTestRadius, SearchMode.Nearest, false);
                if (!nearestHitPointX.IsHit)
                {
                    if (nearestHitPointX.DataSeriesIndex != -1 && DataSeries.YValues.Count != 0)
                    {
                        // Check if the click was on a column body
                        var yValue = ((IComparable) DataSeries.YValues[nearestHitPointX.DataSeriesIndex]).ToDouble();
                        var yCoordinateCalculator = CurrentRenderPassData.YCoordinateCalculator;

                        var yValueUnderMouse =
                            yCoordinateCalculator.GetDataValue(
                                TransformPoint(rawPoint, CurrentRenderPassData.IsVerticalChart).Y);

                        var dataPointCoord = CurrentRenderPassData.IsVerticalChart
                            ? nearestHitPointX.HitTestPoint.Y
                            : nearestHitPointX.HitTestPoint.X;

                        double yLowBound, yUpBound;
                        if (yValue.CompareTo(ZeroLineY) > 0)
                        {
                            yLowBound = ZeroLineY;
                            yUpBound = yValue;
                        }
                        else
                        {
                            yLowBound = yValue;
                            yUpBound = ZeroLineY;
                        }

                        nearestHitPointX.IsHit =
                            CheckIsInBounds(rawPoint.X, dataPointCoord - hitTestRadius, dataPointCoord + hitTestRadius) &&
                            CheckIsInBounds(yValueUnderMouse, yLowBound, yUpBound);
                    }
                }

                if (nearestHitPointX.IsHit) return nearestHitPointX;
            }

            return nearestHitPoint;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="BaseRenderableSeries"/> is valid for drawing.
        /// </summary>
        protected override bool GetIsValidForDrawing()
        {
            var isValid = base.GetIsValidForDrawing() &&
                          ((SeriesColor.A != 0 && StrokeThickness > 0) || PointMarker != null);
            return isValid;
        }

        /// <summary>
        /// Draws the series using the <see cref="IRenderContext2D"/> and the <see cref="IRenderPassData"/> passed in
        /// </summary>
        /// <param name="renderContext">The render context. This is a graphics object which has methods to draw lines, quads and polygons to the screen</param>
        /// <param name="renderPassData">The render pass data. Contains a resampled <see cref="IPointSeries"/>, the <see cref="IndexRange"/> of points on the screen
        /// and the current YAxis and XAxis <see cref="ICoordinateCalculator{T}"/> to convert data-points to screen points</param>
        protected override void InternalDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            var isVerticalChart = renderPassData.IsVerticalChart;
            var pointSeries = CurrentRenderPassData.PointSeries;

            // Setup Constants...            
            int setCount = pointSeries.Count;
            var paletteProvider = PaletteProvider;

            using (var penManager = new PenManager(renderContext, AntiAliasing, StrokeThickness, Opacity))
            {
                _linePen = penManager.GetPen(SeriesColor);
                var zeroY = (float) GetYZeroCoord();
               
                var drawingHelper = SeriesDrawingHelpersFactory.GetSeriesDrawingHelper(renderContext,CurrentRenderPassData);

                // Collate data into points (x1, y1, x2, y2 ...) and draw            
                for (int i = 0; i < setCount; i++)
                {
                    var point = pointSeries[i];

                    if (double.IsNaN(point.Y))
                        continue;

                    var x1 = (float) renderPassData.XCoordinateCalculator.GetCoordinate(point.X);
                    var y1 = (float) renderPassData.YCoordinateCalculator.GetCoordinate(point.Y);

                    var pt1 = TransformPoint(new Point(x1, y1), isVerticalChart);
                    var pt2 = TransformPoint(new Point(x1, zeroY), isVerticalChart);
                    if (paletteProvider != null)
                    {
                        var overrideColor = paletteProvider.GetColor(this, point.X, point.Y);
                        if (overrideColor.HasValue)
                        {
                            var overridePen = penManager.GetPen(overrideColor.Value);
                            drawingHelper.DrawLine(pt1, pt2, overridePen);
                            continue;
                        }
                    }
                    drawingHelper.DrawLine(pt1, pt2, _linePen);
                }
            }

            var pm = GetPointMarker();
            if (pm != null)
            {
                var pointMarkerPathFactory = SeriesDrawingHelpersFactory.GetPointMarkerPathFactory(renderContext, CurrentRenderPassData, pm);

                FastPointsHelper.IteratePoints(pointMarkerPathFactory, pointSeries,
                    CurrentRenderPassData.XCoordinateCalculator,
                    CurrentRenderPassData.YCoordinateCalculator);
            }
        }
    }
}
