// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FastErrorBarsRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals.PointMarkers;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Defines an Error Bars renderable series, supporting solid, stroked error bars and
    /// optional Point-markers <seealso cref="BaseRenderableSeries.PointMarker"/>. 
    /// </summary>
    /// <remarks>
    /// A RenderableSeries has a <see cref="IDataSeries"/> data-source, 
    /// may have a <see cref="BasePointMarker"/> point-marker, and draws onto a specific <see cref="RenderSurfaceBase"/> using the <see cref="IRenderContext2D"/>. 
    /// A given <see cref="UltrachartSurface"/> may have 0..N <see cref="BaseRenderableSeries"/>, each of which may map to, or share a <see cref="IDataSeries"/>
    /// </remarks>
    /// <seealso cref="IDataSeries"/>
    /// <seealso cref="BasePointMarker"/>
    /// <seealso cref="BaseRenderableSeries"/>
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
    public class FastErrorBarsRenderableSeries : BaseRenderableSeries
    {
        private int _errorBarWidth;

        /// <summary>
        /// Defines the DataPointWidth DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DataPointWidthProperty = DependencyProperty.Register("DataPointWidth", typeof(double), typeof(FastErrorBarsRenderableSeries), new PropertyMetadata(0.2, OnInvalidateParentSurface));

        /// <summary>
        /// Gets or sets the DataPointWidth, a value between 0.0 and 1.0 which defines the fraction of available space each column should occupy
        /// </summary>
        public virtual double DataPointWidth
        {
            get { return (double)GetValue(DataPointWidthProperty); }
            set { SetValue(DataPointWidthProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastErrorBarsRenderableSeries"/> class.
        /// </summary>
        /// <remarks></remarks>
        public FastErrorBarsRenderableSeries()
        {
            DefaultStyleKey = typeof(FastErrorBarsRenderableSeries);
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="BaseRenderableSeries"/> is valid for drawing.
        /// </summary>
        protected override bool GetIsValidForDrawing()
        {
            var isValid = base.GetIsValidForDrawing() && SeriesColor.A != 0 && StrokeThickness > 0;
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
            AssertDataPointType<HlcSeriesPoint>("ErrorDataSeries");

            _errorBarWidth = GetDatapointWidth(renderPassData.XCoordinateCalculator, CurrentRenderPassData.PointSeries, DataPointWidth);

            // Setup Constants...    
            var isVerticalChart = renderPassData.IsVerticalChart;
            var points = CurrentRenderPassData.PointSeries as HlcPointSeries;
            int setCount = points.Count;
            float xCoord, yTop, yBottom;

            int barWidthPixels = base.GetDatapointWidth(renderPassData.XCoordinateCalculator, points, DataPointWidth);

            using (var seriesPen = renderContext.CreatePen(SeriesColor, AntiAliasing, StrokeThickness, Opacity))
            {
                var drawingHelper = SeriesDrawingHelpersFactory.GetSeriesDrawingHelper(renderContext, CurrentRenderPassData);

                // Iterate over points collection and render error bars
                // Collate data into points (x1, y1, x2, y2 ...) and draw            
                for (int i = 0; i < setCount; i++)
                {
                    var pt = points[i] as GenericPoint2D<HlcSeriesPoint>;

                    var x = pt.X;
                    var ya = pt.YValues.YErrorHigh;
                    var yb = pt.YValues.YErrorLow;

                    if (double.IsNaN(ya) || double.IsNaN(yb))
                    {
                        continue;
                    }

                    xCoord = (float)renderPassData.XCoordinateCalculator.GetCoordinate(x);
                    yTop = (float)renderPassData.YCoordinateCalculator.GetCoordinate(ya);
                    yBottom = (float)renderPassData.YCoordinateCalculator.GetCoordinate(yb);

                    double columnWidth = barWidthPixels;

                    var halfRange = (int)(columnWidth * 0.5);
                    var xLeft = xCoord - halfRange;
                    var xRight = xCoord + halfRange;

                    drawingHelper.DrawLine(TransformPoint(xCoord, yTop, isVerticalChart), TransformPoint(xCoord, yBottom, isVerticalChart), seriesPen);
                    drawingHelper.DrawLine(TransformPoint(xLeft, yTop, isVerticalChart), TransformPoint(xRight, yTop, isVerticalChart), seriesPen);
                    drawingHelper.DrawLine(TransformPoint(xLeft, yBottom, isVerticalChart), TransformPoint(xRight, yBottom, isVerticalChart), seriesPen);
                }
            }
        }

        protected override HitTestInfo HitTestInternal(Point rawPoint, double hitTestRadius, bool interpolate)
        {
            var nearestHitPoint = base.HitTestInternal(rawPoint, GetHitTestRadiusConsideringPointMarkerSize(hitTestRadius), false);

            nearestHitPoint = HitTestSeriesWithBody(rawPoint, nearestHitPoint, hitTestRadius);

            var distance = CurrentRenderPassData.IsVerticalChart
                ? Math.Abs(nearestHitPoint.HitTestPoint.Y - rawPoint.Y)
                : Math.Abs(nearestHitPoint.HitTestPoint.X - rawPoint.X);

            if (!nearestHitPoint.IsWithinDataBounds)
            {
                var isVerticalHit = distance < GetSeriesBodyWidth(nearestHitPoint) / DataPointWidth / 2;
                nearestHitPoint.IsWithinDataBounds = nearestHitPoint.IsVerticalHit = isVerticalHit;
            }

            return nearestHitPoint;
        }

        protected override double GetSeriesBodyWidth(HitTestInfo nearestHitPoint)
        {
            return _errorBarWidth;
        }

        protected override double GetSeriesBodyLowerDataBound(HitTestInfo nearestHitPoint)
        {
            return nearestHitPoint.ErrorLow.ToDouble();
        }

        protected override double GetSeriesBodyUpperDataBound(HitTestInfo nearestHitPoint)
        {
            return nearestHitPoint.ErrorHigh.ToDouble();
        }

    }
}
