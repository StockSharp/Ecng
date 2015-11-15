// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// BaseColumnRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using System;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// An abstract base class which factors out many properties from the <seealso cref="FastColumnRenderableSeries"/>
    /// and <see cref="StackedColumnRenderableSeries"/> types. 
    /// </summary>
    /// <seealso cref="FastColumnRenderableSeries"/>
    /// <seealso cref="StackedColumnRenderableSeries"/>
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    public abstract class BaseColumnRenderableSeries : BaseRenderableSeries
    {
        /// <summary>
        /// contains precalculated resampled X and Y coordinated for bars, also column widths
        /// [columnCenterX][Y1][Y2=zeroLine][width].....
        /// </summary>
        private double[] _precalculatedPoints;

        /// <summary>
        /// Defines the FillColor DependencyProperty
        /// </summary>
        [Obsolete("We're sorry! FastColumnRenderableSeries.FillColor is obsolete, please use FillBrush instead", true)]
        public static readonly DependencyProperty FillColorProperty = DependencyProperty.Register("FillColor", typeof(Color), typeof(BaseColumnRenderableSeries), new PropertyMetadata(ColorConstants.White, OnRenderablePropertyChangedStatic));

        /// <summary>
        /// Defines the FillBrush DependencyProperty
        /// </summary>        
        public static readonly DependencyProperty FillBrushProperty = DependencyProperty.Register("FillBrush", typeof(Brush), typeof(BaseColumnRenderableSeries), new PropertyMetadata(new SolidColorBrush(ColorConstants.White), OnRenderablePropertyChangedStatic));
        
        /// <summary>
        /// Defines the FillBrushMappingMode DependencyProperty
        /// </summary>     
        public static readonly DependencyProperty FillBrushMappingModeProperty = DependencyProperty.Register("FillBrushMappingMode", typeof(TextureMappingMode), typeof(BaseColumnRenderableSeries), new PropertyMetadata(TextureMappingMode.PerPrimitive, OnRenderablePropertyChangedStatic));
        
        /// <summary>
        /// Defines the DataPointWidth DependencyProperty
        /// </summary>
        public static readonly DependencyProperty UniformWidthProperty = DependencyProperty.Register("UseUniformWidth", typeof(bool), typeof(BaseColumnRenderableSeries), new PropertyMetadata(false, OnRenderablePropertyChangedStatic));

        /// <summary>
        /// Defines the UniformWidth DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DataPointWidthProperty = DependencyProperty.Register("DataPointWidth", typeof(double), typeof(BaseColumnRenderableSeries), new PropertyMetadata(0.4, OnRenderablePropertyChangedStatic));

        /// <summary>
        /// Minimum column width, used when UseUniformWidth is set
        /// </summary>
        protected double _minColumnWidth;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseColumnRenderableSeries" /> class.
        /// </summary>
        protected BaseColumnRenderableSeries()
        {
            this.SetCurrentValue(ResamplingModeProperty, ResamplingMode.Max);
        }

        /// <summary>
        /// Gets or sets the Fill Color for columns. The column outline is specified by <see cref="BaseRenderableSeries.SeriesColor"/>
        /// </summary>
        [Obsolete("We're sorry! FastColumnRenderableSeries.FillColor is obsolete, please use FillBrush instead", true)]
        public Color FillColor
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets or sets the Fill Brush for columns. The column outline is specified by <see cref="BaseRenderableSeries.SeriesColor"/>
        /// </summary>
        public Brush FillBrush
        {
            get { return (Brush)GetValue(FillBrushProperty); }
            set { SetValue(FillBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets a <see cref="TextureMappingMode"/> which defines how columns are filled when a gradient <see cref="BaseColumnRenderableSeries.FillBrush"/> is used.
        /// 
        ///   If <see cref="TextureMappingMode.PerScreen"/>, then a single texture is shared across multiple columns
        ///   If <see cref="TextureMappingMode.PerPrimitive"/>, then a texture is created and scaled per-column fill area
        /// </summary>
        public TextureMappingMode FillBrushMappingMode
        {
            get
            {
                return (TextureMappingMode)GetValue(FillBrushMappingModeProperty);
            }
            set
            {
                SetValue(FillBrushMappingModeProperty, value);
            }
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
        /// Gets or sets the DataPointWidth, a value between 0.0 and 1.0 which defines the fraction of available space each column should occupy
        /// </summary>
        public bool UseUniformWidth
        {
            get { return (bool)GetValue(UniformWidthProperty); }
            set { SetValue(UniformWidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value which determines the zero line in Y direction.
        /// Used to set the bottom of an area
        /// </summary>
        public double ZeroLineY
        {
            get { return (double)GetValue(ZeroLineYProperty); }
            set { SetValue(ZeroLineYProperty, value); }
        }

        /// <summary>
        /// Performs a hit-test at the specific mouse point (X,Y coordinate on the parent <see cref="UltrachartSurface" />),
        /// returning a <see cref="HitTestInfo" /> struct with the results
        /// </summary>
        /// <param name="rawPoint">The mouse point on the parent <see cref="UltrachartSurface" /></param>
        /// <param name="hitTestRadius">The radius in pixels to determine whether a mouse is over a data-point</param>
        /// <param name="interpolate">If true, use interpolation to perform a hit-test between data-points, or on the area if a <seealso cref="FastMountainRenderableSeries" />, <seealso cref="FastColumnRenderableSeries" /> or <seealso cref="FastCandlestickRenderableSeries" /></param>
        /// <returns>
        /// A <see cref="HitTestInfo" /> struct with the results
        /// </returns>
        public override HitTestInfo HitTest(Point rawPoint, double hitTestRadius, bool interpolate = false)
        {
            var nearestHitResult = HitTestInfo.Empty;

            if (IsVisible && CurrentRenderPassData != null)
            {
                nearestHitResult = base.HitTest(rawPoint, hitTestRadius, interpolate);

                var transformationStrategy = CurrentRenderPassData.TransformationStrategy;
                rawPoint = transformationStrategy.Transform(rawPoint);
                nearestHitResult.HitTestPoint = transformationStrategy.Transform(nearestHitResult.HitTestPoint);
                nearestHitResult.Y1HitTestPoint = transformationStrategy.Transform(nearestHitResult.Y1HitTestPoint);

                nearestHitResult = HitTestSeriesWithBody(rawPoint, nearestHitResult, hitTestRadius);

                nearestHitResult.HitTestPoint = transformationStrategy.ReverseTransform(nearestHitResult.HitTestPoint);
                nearestHitResult.Y1HitTestPoint = transformationStrategy.ReverseTransform(nearestHitResult.Y1HitTestPoint);
            }

            return nearestHitResult;
        }

        protected override HitTestInfo HitTestInternal(Point rawPoint, double hitTestRadius, bool interpolate)
        {
            var nearestHitResult = HitTestInfo.Empty;

            if (IsVisible)
            {
                nearestHitResult = NearestHitResult(rawPoint, GetHitTestRadiusConsideringPointMarkerSize(hitTestRadius),
                    SearchMode.Nearest, false);
            }
            var distance = CurrentRenderPassData.IsVerticalChart
                ? Math.Abs(nearestHitResult.HitTestPoint.Y - rawPoint.Y)
                : Math.Abs(nearestHitResult.HitTestPoint.X - rawPoint.X);

            if (!nearestHitResult.IsWithinDataBounds)
            {
                var isVerticalHit = distance < GetSeriesBodyWidth(nearestHitResult)/DataPointWidth/2;   
                nearestHitResult.IsWithinDataBounds = nearestHitResult.IsVerticalHit = isVerticalHit;
            }

            return nearestHitResult;
        }

        protected override double GetSeriesBodyWidth(HitTestInfo nearestHitPoint)
        {
            var columnCenterCoord = CurrentRenderPassData.IsVerticalChart ? nearestHitPoint.HitTestPoint.Y : nearestHitPoint.HitTestPoint.X;
            var columnWidth = GetColumnWidth(nearestHitPoint.DataSeriesIndex, columnCenterCoord);

            return columnWidth;
        }

        private int GetColumnWidth(int dataPointIndex, double dataPointXCoord)
        {
            var columnWidth = _minColumnWidth;

            var neighbourIndex = dataPointIndex - 1;
            if (neighbourIndex > 0 && !UseUniformWidth)
            {
                var yValue = (IComparable)DataSeries.YValues[neighbourIndex];
                var yCoord = CurrentRenderPassData.YCoordinateCalculator.GetCoordinate(yValue.ToDouble());

                if (!double.IsNaN(yCoord))
                {
                    IComparable xValue = XAxis.IsCategoryAxis ? neighbourIndex : (IComparable)DataSeries.XValues[neighbourIndex];

                    var prevXCoord = CurrentRenderPassData.XCoordinateCalculator.GetCoordinate(xValue.ToDouble());

                    columnWidth = Math.Abs((dataPointXCoord - prevXCoord) * DataPointWidth);
                }
            }

            return (int)columnWidth;
        }

        /// <summary>
        /// Draws the series using the <see cref="IRenderContext2D"/> and the <see cref="IRenderPassData"/> passed in
        /// </summary>
        /// <param name="renderContext">The render context. This is a graphics object which has methods to draw lines, quads and polygons to the screen</param>
        /// <param name="renderPassData">The render pass data. Contains a resampled <see cref="IPointSeries"/>, the <see cref="IndexRange"/> of points on the screen
        /// and the current YAxis and XAxis <see cref="ICoordinateCalculator{T}"/> to convert data-points to screen points</param>
        protected override void InternalDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            var points = CurrentRenderPassData.PointSeries;
            
            int pointsCount = points.Count;
            var columnWidth = GetColumnWidth(points, renderPassData);

            // End Y-point or X-point(depends on chart orientation) is either the height (e.g. the bottom of the chart pane), or the zero line (e.g. if the chart has negative numbers)
            var zeroCoord = (int)GetYZeroCoord();

            using (var penManager = new PenManager(renderContext, AntiAliasing, StrokeThickness, Opacity))            
            {
                var fillBrush = CreateBrush(renderContext, FillBrush, Opacity, FillBrushMappingMode);
                renderContext.DisposeResourceAfterDraw(fillBrush);

                renderContext.SetPrimitvesCachingEnabled(true);
                if (columnWidth > 1)
                {
                    DrawAsColumns(
                        renderContext,
                        points,
                        pointsCount,
                        zeroCoord,
                        renderPassData,
                        columnWidth,
                        fillBrush,
                        PaletteProvider,
                        penManager);
                }
                else
                {
                    if (StrokeThickness > 0 && SeriesColor.A != 0)
                    {
                        DrawAsLines(renderContext, points, pointsCount, zeroCoord, renderPassData, PaletteProvider, penManager);
                    }
                }
                //      renderContext.SetPrimitvesCachingEnabled(false);
            }
        }

        private IBrush2D CreateBrush(IRenderContext2D renderContext, Brush fillBrush, double opacity, TextureMappingMode fillBrushMappingMode)
        {
            if (FillBrush is SolidColorBrush)
                return renderContext.CreateBrush((fillBrush as SolidColorBrush).Color, opacity);
            else
                return renderContext.CreateBrush(fillBrush, opacity, fillBrushMappingMode);
        }

        /// <summary>
        /// When overriden in a derived class, computes the width of the columns, which depends on the input data, 
        /// any spacing and the current viewport dimensions
        /// </summary>
        /// <param name="points">The <see cref="IPointSeries"/> containing resampled data to render</param>
        /// <param name="renderPassData">The <see cref="IRenderPassData"/> containing information about the current render pass</param>
        /// <returns>The width of the column</returns>
        protected virtual double GetColumnWidth(IPointSeries points, IRenderPassData renderPassData)
        {
            return GetDatapointWidth(renderPassData.XCoordinateCalculator, points, DataPointWidth);
        }

        protected virtual double GetNonUniformColumnWidth(IPointSeries points, IRenderPassData renderPassData, double prevCoord, double xCenter, int pointsIndex)
        {
            double columnWidth = double.NaN;
            if (double.IsNaN(prevCoord))
            {
                var nextIndex = pointsIndex + 1;

                //Calculate columnWidth based on next DataPoint coordinate
                if (nextIndex < points.Count && !double.IsNaN(points[nextIndex].Y))
                {
                    var nextPointCoord = renderPassData.XCoordinateCalculator.GetCoordinate(points[nextIndex].X);
                    columnWidth = (nextPointCoord - xCenter) * DataPointWidth;
                }
            }
            else
            {
                //Calculate columnWidth based on previous DataPoint coordinate
                columnWidth = (xCenter - prevCoord) * DataPointWidth;
            }
            return columnWidth;
        }

        private void DrawAsColumns(
            IRenderContext2D renderContext,
            IPointSeries points,
            int setCount,
            int zeroY,
            IRenderPassData renderPassData,
            double columnWidthPixels,
            IBrush2D fillBrush,
            IPaletteProvider paletteProvider,
            IPenManager penManager)
        {
            var linePen = penManager.GetPen(SeriesColor);
            var isVerticalChart = renderPassData.IsVerticalChart;
            var gradientRotationAngle = GetChartRotationAngle(renderPassData);

            var drawingHelper = SeriesDrawingHelpersFactory.GetSeriesDrawingHelper(renderContext,
                CurrentRenderPassData);

            double xCenter, xLeft, xRight, yBottom, yTop, halfColWidth;

            // Bottom, top, center coords, half column width and data point index
            var precalculatedPointsCount = points.Count * 5;
            var actualCoordCount = 0;

            if (_precalculatedPoints == null || _precalculatedPoints.Length != precalculatedPointsCount)
            {
                _precalculatedPoints = new double[precalculatedPointsCount];
            }

            var pointPen = linePen;
            var pointFill = fillBrush;

            //double dataPointWidth = DataPointWidth;
            _minColumnWidth = columnWidthPixels;

            // Collate data into points (x1, y1, x2, y2 ...)            
            for (int pointsIndex = 0; pointsIndex < setCount; pointsIndex++)
            {
                if (!GetColumnCenterTopAndBottom(pointsIndex, renderPassData, zeroY, out xCenter, out yBottom, out yTop))
                {
                    continue;
                }

                halfColWidth = _minColumnWidth * 0.5;
                xLeft = xCenter + halfColWidth;

                //Check for an overflow
                if (xLeft > int.MaxValue || yBottom > int.MaxValue)
                {
                    continue;
                }

                _precalculatedPoints[actualCoordCount++] = pointsIndex;
                _precalculatedPoints[actualCoordCount++] = xCenter;
                _precalculatedPoints[actualCoordCount++] = yBottom;
                _precalculatedPoints[actualCoordCount++] = yTop; 
                _precalculatedPoints[actualCoordCount++] = halfColWidth;
            }

            halfColWidth = _minColumnWidth / 2;

            //Draw column series using precalculated coords
            for (int precalculatedPointsIndex = 0; precalculatedPointsIndex < actualCoordCount;)
            {
                var pointIndex = (int)_precalculatedPoints[precalculatedPointsIndex++];
                xCenter = _precalculatedPoints[precalculatedPointsIndex++];
                yBottom = _precalculatedPoints[precalculatedPointsIndex++];
                yTop = _precalculatedPoints[precalculatedPointsIndex++];

                var halfColWidthIndex = precalculatedPointsIndex++;
                if (!UseUniformWidth)
                {
                    halfColWidth = _precalculatedPoints[halfColWidthIndex];
                }

                xLeft = xCenter + halfColWidth;
                xRight = xCenter - halfColWidth;

                var x1 = xRight;
                var x2 = xLeft;
                var y1 = yBottom;
                var y2 = yTop;

                var pt1 = TransformPoint(new Point(x1, y1), isVerticalChart);
                var pt2 = TransformPoint(new Point(x2, y2), isVerticalChart);
                
                if (paletteProvider != null)
                {
                    var point = points[pointIndex];

                    var overrideColor = paletteProvider.GetColor(this, point.X, point.Y);
                    if (overrideColor.HasValue)
                    {
                        var overriddenPen = penManager.GetPen(overrideColor.Value);
                        using (var overriddenFill = renderContext.CreateBrush(overrideColor.Value))
                        {
                            drawingHelper.DrawBox(pt1, pt2, overriddenFill, overriddenPen, gradientRotationAngle);
                            continue;
                        }
                    }
                }

                drawingHelper.DrawBox(pt1, pt2, pointFill, pointPen, gradientRotationAngle);
            }
        }

        /// <summary>
        /// When overrided in a derived class, returns the extents of a column as pixel coordinates
        /// </summary>
        /// <param name="dataPointIndex">The index to the <see cref="IPointSeries"/> for this column</param>
        /// <param name="renderPassData">The <see cref="IRenderPassData"/> valid for the current render pass</param>
        /// <param name="zeroY">The pixel coordinate of zero in the Y-direction</param>
        /// <param name="xCenter">[out] The X-Axis pixel coordinate</param>
        /// <param name="yTop">[out] The Left-edge Y-Axis pixel coordinate</param>
        /// <param name="yBottom">[out] The Right-edge Y-Axis pixel coordinate</param>
        /// <returns></returns>
        protected virtual bool GetColumnCenterTopAndBottom(int dataPointIndex, IRenderPassData renderPassData, int zeroY,
            out double xCenter, out double yTop, out double yBottom)
        {
            xCenter = 0; yTop = 0; yBottom = 0;

            var point = renderPassData.PointSeries[dataPointIndex];

            var isRendered = !double.IsNaN(point.Y) &&
                             Math.Abs(point.Y - ZeroLineY) >= double.Epsilon;

            if (isRendered)
            {
                xCenter = renderPassData.XCoordinateCalculator.GetCoordinate(point.X);
                yTop = renderPassData.YCoordinateCalculator.GetCoordinate(point.Y);
                yBottom = zeroY;

                if (yTop < yBottom)
                {
                    NumberUtil.Swap(ref yBottom, ref yTop);
                }
            }

            return isRendered;
        }

        private void DrawAsLines(
            IRenderContext2D renderContext,
            IPointSeries points,
            int setCount,
            int zeroY,
            IRenderPassData renderPassData,
            IPaletteProvider paletteProvider,
            IPenManager penManager)
        {
            var linePen = penManager.GetPen(SeriesColor);
            _minColumnWidth = linePen.StrokeThickness;

            var isVerticalChart = renderPassData.IsVerticalChart;

            var pointPen = linePen;

            var drawingHelper = SeriesDrawingHelpersFactory.GetSeriesDrawingHelper(renderContext,
                CurrentRenderPassData);

            // Collate data into points (x1, y1, x2, y2 ...) and draw            
            for (int i = 0; i < setCount; i++)
            {
                var point = points[i];

                double xCoord, yCoord1, yCoord2;
                if (!GetColumnCenterTopAndBottom(i, renderPassData, zeroY, out xCoord, out yCoord1, out yCoord2)) continue;

                var x1 = (int)xCoord;
                var y1 = (int)yCoord1;
                var y2 = (int)yCoord2;

                var pt1 = TransformPoint(new Point(x1, y1), isVerticalChart);
                var pt2 = TransformPoint(new Point(x1, y2), isVerticalChart);

                if (paletteProvider != null)
                {
                    var overrideColor = paletteProvider.GetColor(this, point.X, point.Y);
                    if (overrideColor.HasValue)
                    {
                        using (var overridePen = penManager.GetPen(overrideColor.Value))
                        {
                            drawingHelper.DrawLine(pt1, pt2, overridePen);
                            continue;
                        }
                    }
                }

                drawingHelper.DrawLine(pt1, pt2, pointPen);
            }
        }

        private static void OnRenderablePropertyChangedStatic(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OnInvalidateParentSurface(d, e);
        }
    }
}
