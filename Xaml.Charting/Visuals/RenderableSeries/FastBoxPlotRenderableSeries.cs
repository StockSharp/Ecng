// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FastBoxPlotRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections;
using System.Windows;
using System.Windows.Media;
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
    /// Defines a Box Plot renderable series, supporting rendering of Lowest, Lower Quartile, Median, Upper Quartile, Maximum 
    /// statistical data onto a wicked (stroked-outline) box with solid color or gradient filled body. 
    /// </summary>
    /// <remarks>
    /// The FastBoxPlotRenderableSeries requires a <see cref="BoxPlotDataSeries{TX,TY}"/> data-source, 
    /// may have a <see cref="BasePointMarker"/> point-marker, and draws onto a specific <see cref="RenderSurfaceBase"/> using the <see cref="IRenderContext2D"/>. 
    /// A given <see cref="UltrachartSurface"/> may have 0..N <see cref="IDataSeries"/>, each of which may map to, or share a <see cref="IDataSeries"/>
    /// </remarks>
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
    /// <seealso cref="BaseRenderableSeries"/>
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    public class FastBoxPlotRenderableSeries : BaseRenderableSeries
    {
        /// <summary>
        /// Defines the DataPointWidth DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DataPointWidthProperty = DependencyProperty.Register("DataPointWidth", typeof(double), typeof(FastBoxPlotRenderableSeries), new PropertyMetadata(0.2, OnInvalidateParentSurface));

        /// <summary>
        /// Defines the UpBodyBrush DependencyProperty
        /// </summary>
        public static readonly DependencyProperty BodyBrushProperty = DependencyProperty.Register("BodyBrush", typeof(Brush), typeof(FastBoxPlotRenderableSeries), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)), OnInvalidateParentSurface));

        private int _barWidth;

        /// <summary>
        /// Initializes a new instance of the <seealso cref="FastBoxPlotRenderableSeries"/> class.
        /// </summary>
        /// <remarks></remarks>
        public FastBoxPlotRenderableSeries()
        {
            DefaultStyleKey = typeof(FastBoxPlotRenderableSeries);
        }

        /// <summary>
        /// Gets or sets the DataPointWidth, a value between 0.0 and 1.0 which defines the fraction of available space each column should occupy
        /// </summary>
        public virtual double DataPointWidth
        {
            get { return (double)GetValue(DataPointWidthProperty); }
            set { SetValue(DataPointWidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Brush used for box-plot body 
        /// </summary>
        public Brush BodyBrush
        {
            get { return (Brush)GetValue(BodyBrushProperty); }
            set { SetValue(BodyBrushProperty, value); }
        }

        
        protected override HitTestInfo HitTestInternal(Point rawPoint, double hitTestRadius, bool interpolate)
        {
            var nearestHitPoint = base.HitTestInternal(rawPoint, hitTestRadius, false);

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

        /// <summary>
        /// Converts the result of a Hit-Test operation (<see cref="HitTestInfo"/>) to a <see cref="SeriesInfo"/> struct, which may be used as a
        /// ViewModel when outputting series values as bindings. <see cref="SeriesInfo"/> is used by the <see cref="Ecng.Xaml.Charting.ChartModifiers.RolloverModifier"/>, <see cref="Ecng.Xaml.Charting.ChartModifiers.CursorModifier"/>
        /// and <see cref="UltrachartLegend"/> classes
        /// </summary>
        /// <param name="hitTestInfo"></param>
        /// <returns></returns>
        public override SeriesInfo GetSeriesInfo(HitTestInfo hitTestInfo)
        {
            return new BoxPlotSeriesInfo(this, hitTestInfo);
        }

        protected override double GetSeriesBodyWidth(HitTestInfo nearestHitPoint)
        {
            return _barWidth;
        }

        protected override double GetSeriesBodyLowerDataBound(HitTestInfo nearestHitPoint)
        {
            return nearestHitPoint.Minimum.ToDouble();
        }

        protected override double GetSeriesBodyUpperDataBound(HitTestInfo nearestHitPoint)
        {
            return nearestHitPoint.Maximum.ToDouble();
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="BaseRenderableSeries"/> is valid for drawing.
        /// </summary>
        protected override bool GetIsValidForDrawing()
        {
            var isValid = base.GetIsValidForDrawing() && 
                          ((SeriesColor.A != 0 && StrokeThickness > 0) || BodyBrush != null);
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
            double gradientRotationAngle = 0;

            AssertDataPointType<BoxSeriesPoint>("BoxDataSeries");

            // Setup Constants...    
            var points = CurrentRenderPassData.PointSeries as BoxPointSeries;
            int setCount = points.Count;

            _barWidth = GetDatapointWidth(renderPassData.XCoordinateCalculator, points, DataPointWidth);

            using (var seriesPen = renderContext.CreatePen(SeriesColor, AntiAliasing, StrokeThickness, Opacity))
            using (var medianPen = renderContext.CreatePen(SeriesColor, AntiAliasing, StrokeThickness + 1, Opacity))
            using (var bodyBrush = renderContext.CreateBrush(BodyBrush, Opacity, TextureMappingMode.PerPrimitive))
            {
                var drawingHelper = SeriesDrawingHelpersFactory.GetSeriesDrawingHelper(renderContext, CurrentRenderPassData);

                // Iterate over points collection and render error bars
                // Collate data into points (x1, y1, x2, y2 ...) and draw            
                for (int i = 0; i < setCount; i++)
                {
                    var pt = points[i] as GenericPoint2D<BoxSeriesPoint>;

                    double x = pt.X;
                    double yMin = pt.YValues.Min;
                    double yLower = pt.YValues.LowerQuartile;
                    double yMed = pt.YValues.Y;
                    double yUpper = pt.YValues.UpperQuartile;
                    double yMax = pt.YValues.Max;


                    if (double.IsNaN(yMin) || double.IsNaN(yLower) || double.IsNaN(yMed) || double.IsNaN(yUpper) || double.IsNaN(yMax))
                    {
                        continue;
                    }

                    var xCoord = renderPassData.XCoordinateCalculator.GetCoordinate(x);
                    var yMaxCoord = renderPassData.YCoordinateCalculator.GetCoordinate(yMax);
                    var yUpperCoord = renderPassData.YCoordinateCalculator.GetCoordinate(yUpper);
                    var yMedCoord = renderPassData.YCoordinateCalculator.GetCoordinate(yMed);
                    var yLowerCoord = renderPassData.YCoordinateCalculator.GetCoordinate(yLower);
                    var yMinCoord = renderPassData.YCoordinateCalculator.GetCoordinate(yMin);

                    double columnWidth = _barWidth;

                    var halfRange = (columnWidth * 0.5);
                    var xLeft = xCoord - halfRange;
                    var xRight = xCoord + halfRange;

                    // Draw error bar behind Box
                    drawingHelper.DrawLine(TransformPoint(new Point(xLeft, yMaxCoord), isVerticalChart), TransformPoint(new Point(xRight, yMaxCoord), isVerticalChart), seriesPen);
                    drawingHelper.DrawLine(TransformPoint(new Point(xCoord, yMaxCoord), isVerticalChart), TransformPoint(new Point(xCoord, yUpperCoord), isVerticalChart), seriesPen);
                    drawingHelper.DrawLine(TransformPoint(new Point(xCoord, yLowerCoord), isVerticalChart), TransformPoint(new Point(xCoord, yMinCoord), isVerticalChart), seriesPen);
                    drawingHelper.DrawLine(TransformPoint(new Point(xLeft, yMinCoord), isVerticalChart), TransformPoint(new Point(xRight, yMinCoord), isVerticalChart), seriesPen);

                    // Draw box overlay
                    drawingHelper.DrawBox(TransformPoint(new Point(xLeft, yLowerCoord), isVerticalChart), TransformPoint(new Point(xRight, yUpperCoord), isVerticalChart), bodyBrush, seriesPen,gradientRotationAngle);
                  
                    // Draw Median bar
                    // Requires checking because of medianPen strokeThickness is larger by 1
                    if (StrokeThickness > 0)
                    {
                        drawingHelper.DrawLine(TransformPoint(new Point(xLeft + 1, yMedCoord), isVerticalChart), TransformPoint(new Point(xRight, yMedCoord), isVerticalChart), medianPen);
                    }
                }
            }
        }
    }
}
