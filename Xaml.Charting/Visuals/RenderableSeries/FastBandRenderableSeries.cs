// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FastBandRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common.Databinding;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
	/// <summary>
	/// A raster RenderableSeries type which displays two lines and shaded bands between them, where band-colors depend on whether one line is greater than the other
	/// For usage, bind to an <see cref="XyyDataSeries{TX,TY}"/> and set the <see cref="BaseRenderableSeries.SeriesColor"/>, <see cref="FastBandRenderableSeries.Series1Color"/>, 
	/// <see cref="FastBandRenderableSeries.BandUpColor"/> and <see cref="FastBandRenderableSeries.BandDownColor"/> properties
	/// </summary>
	[UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
	public class FastBandRenderableSeries : BaseRenderableSeries
	{
		/// <summary>
		/// Defines the IsDigitalLine DependencyProperty
		/// </summary>
		public static readonly DependencyProperty IsDigitalLineProperty = DependencyProperty.Register("IsDigitalLine", typeof(bool), typeof(FastBandRenderableSeries), new PropertyMetadata(false, OnInvalidateParentSurface));

		/// <summary>
		/// Defines the Series1Color DependencyProperty
		/// </summary>
		public static readonly DependencyProperty Series1ColorProperty = DependencyProperty.Register("Series1Color", typeof(Color), typeof(FastBandRenderableSeries), new PropertyMetadata(default(Color), OnInvalidateParentSurface));

		/// <summary>
		/// Defines the BandUpColor DependencyProperty
		/// </summary>
		public static readonly DependencyProperty BandUpColorProperty = DependencyProperty.Register("BandUpColor", typeof(Color), typeof(FastBandRenderableSeries), new PropertyMetadata(default(Color), OnInvalidateParentSurface));

		/// <summary>
		/// Defines the BandDownColor DependencyProperty
		/// </summary>
		public static readonly DependencyProperty BandDownColorProperty = DependencyProperty.Register("BandDownColor", typeof(Color), typeof(FastBandRenderableSeries), new PropertyMetadata(default(Color), OnInvalidateParentSurface));        

		/// <summary>
		/// Defines the StrokeDashArray DependencyProperty
		/// </summary>
		public static readonly DependencyProperty Series0StrokeDashArrayProperty = DependencyProperty.Register("Series0StrokeDashArray", typeof(double[]), typeof(FastBandRenderableSeries),
			new PropertyMetadata(null, OnInvalidateParentSurface));

		/// <summary>
		/// Defines the StrokeDashArray DependencyProperty
		/// </summary>
		public static readonly DependencyProperty Series1StrokeDashArrayProperty = DependencyProperty.Register("Series1StrokeDashArray", typeof(double[]), typeof(FastBandRenderableSeries),
			new PropertyMetadata(null, OnInvalidateParentSurface));

		private FrameworkElement _rolloverMarker1Cache;

		/// <summary>
		/// Initializes a new instance of the <see cref="FastBandRenderableSeries" /> class.
		/// </summary>
		public FastBandRenderableSeries()
		{
			DefaultStyleKey = typeof(FastBandRenderableSeries);
		}

		/// <summary>
		/// Gets or sets a value indicating whether this line series is a digital (step) line
		/// </summary>
		public bool IsDigitalLine
		{
			get { return (bool)GetValue(IsDigitalLineProperty); }
			set { SetValue(IsDigitalLineProperty, value); }
		}

		/// <summary>
		/// Gets or sets a StrokeDashArray property, used to define a dashed line. See the MSDN Documentation for 
		/// <see cref="Shape.StrokeDashArray"/> as this property attempts to mimic the same behaviour
		/// </summary>
		[TypeConverter(typeof(StringToDoubleArrayTypeConverter))]
		public double[] Series0StrokeDashArray
		{
			get { return (double[])GetValue(Series0StrokeDashArrayProperty); }
			set { SetValue(Series0StrokeDashArrayProperty, value); }
		}

		/// <summary>
		/// Gets or sets a StrokeDashArray property, used to define a dashed line. See the MSDN Documentation for 
		/// <see cref="Shape.StrokeDashArray"/> as this property attempts to mimic the same behaviour
		/// </summary>
		[TypeConverter(typeof(StringToDoubleArrayTypeConverter))]
		public double[] Series1StrokeDashArray
		{
			get { return (double[])GetValue(Series1StrokeDashArrayProperty); }
			set { SetValue(Series1StrokeDashArrayProperty, value); }
		}

		/// <summary>
		/// Gets or sets the SeriesColor of the Y1 line. For the Y0 line, use SeriesColor
		/// </summary>
		public Color Series1Color
		{
			get { return (Color)GetValue(Series1ColorProperty); }
			set { SetValue(Series1ColorProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Color of the shaded area when Y1 is less than Y0
		/// </summary>
		public Color BandDownColor
		{
			get { return (Color)GetValue(BandDownColorProperty); }
			set { SetValue(BandDownColorProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Color of the shaded area when Y1 is greater than Y0
		/// </summary>
		public Color BandUpColor
		{
			get { return (Color)GetValue(BandUpColorProperty); }
			set { SetValue(BandUpColorProperty, value); }
		}

		/// <summary>
		/// Gets or sets the RolloverMarker for one of the series
		/// </summary>
		public FrameworkElement RolloverMarker1
		{
			get { return _rolloverMarker1Cache; }
			private set { _rolloverMarker1Cache = value; }
		}

		/// <summary>
		/// Creates a RolloverMarker from the RolloverMarkerTemplate property
		/// </summary>
		protected override void CreateRolloverMarker()
		{
			base.CreateRolloverMarker();

			RolloverMarker1 = RenderableSeries.PointMarker.CreateFromTemplate(RolloverMarkerTemplate, this);
		}

		protected override HitTestInfo HitTestInternal(Point rawPoint, double hitTestRadius, bool interpolate)
		{
			var hitTestResult = base.HitTestInternal(rawPoint, GetHitTestRadiusConsideringPointMarkerSize(hitTestRadius), interpolate);

			hitTestResult.DataSeriesType = DataSeriesType.Xyy;

			if (interpolate && !hitTestResult.IsHit)
			{
				var hitDataPoint = GetHitDataValue(rawPoint);

				var leftIndex = DataSeries.XValues.FindIndex(DataSeries.IsSorted, hitDataPoint.Item1,
															 SearchMode.RoundDown);
				var rightIndex = DataSeries.XValues.FindIndex(DataSeries.IsSorted, hitDataPoint.Item1,
															  SearchMode.RoundUp);

				// Get a triangle to check hit-test
				var leftX = ((IComparable)DataSeries.XValues[leftIndex]).ToDouble();
				var rightX = ((IComparable)DataSeries.XValues[rightIndex]).ToDouble();

				var dataPointToLeft = new Point(leftX, ((IComparable)DataSeries.YValues[leftIndex]).ToDouble());
				var dataPointToRight = new Point(rightX, ((IComparable)DataSeries.YValues[rightIndex]).ToDouble());
				var dataPointHasNans = Double.IsNaN(dataPointToLeft.Y) || Double.IsNaN(dataPointToRight.Y);

				var dataPointToLeft2 = new Point(leftX,
												 ((IComparable)((IXyyDataSeries)DataSeries).Y1Values[leftIndex]).
													 ToDouble());
				var dataPointToRight2 = new Point(rightX,
												  ((IComparable)((IXyyDataSeries)DataSeries).Y1Values[rightIndex]).
													  ToDouble());
				var dataPoint2HasNans = Double.IsNaN(dataPointToLeft2.Y) || Double.IsNaN(dataPointToRight2.Y);

                // if there are NaNs, it does not hit
				if (dataPointHasNans || dataPoint2HasNans)
					return hitTestResult;
                
				var checkPoint = new Point(hitDataPoint.Item1.ToDouble(), hitDataPoint.Item2.ToDouble());

				var firstSeries = new PointUtil.Line(dataPointToLeft, dataPointToRight);
				var secondSeries = new PointUtil.Line(dataPointToLeft2, dataPointToRight2);

				Point intersection;
				var hasIntersection = PointUtil.LineSegmentsIntersection2D(firstSeries, secondSeries, out intersection);

				if (hasIntersection)
				{
					hitTestResult.IsHit =
						PointUtil.IsPointInTriangle(checkPoint, dataPointToLeft, dataPointToLeft2,
													intersection) ||
						PointUtil.IsPointInTriangle(checkPoint, dataPointToRight, dataPointToRight2,
													intersection);
				}
				else
				{
					hitTestResult.IsHit =
						PointUtil.IsPointInTriangle(checkPoint, dataPointToLeft, dataPointToRight,
													dataPointToLeft2) ||
						PointUtil.IsPointInTriangle(checkPoint, dataPointToLeft2, dataPointToRight2,
													dataPointToRight);
				}
			}

			return hitTestResult;
		}

		/// <summary>
		/// Called by <see cref="BaseRenderableSeries.HitTest(Point,bool)" /> to get the nearest (non-interpolated) <see cref="HitTestInfo" /> to the mouse point
		/// </summary>
		/// <param name="rawPoint">The mouse point</param>
		/// <param name="hitTestRadius">The radius (in pixels) to use when determining if the <paramref name="rawPoint" /> is over a data-point</param>
		/// <param name="searchMode">The search mode.</param>
		/// <param name="considerYCoordinateForDistanceCalculation">if set to <c>true</c> then perform a true euclidean distance to find the nearest hit result.</param>
		/// <returns>
		/// The <see cref="HitTestInfo" /> result
		/// </returns>
		protected override HitTestInfo NearestHitResult(Point rawPoint, double hitTestRadius, SearchMode searchMode, bool considerYCoordinateForDistanceCalculation)
		{
			var nearestHitResult = base.NearestHitResult(rawPoint, hitTestRadius, searchMode, considerYCoordinateForDistanceCalculation);

			if (!nearestHitResult.IsEmpty())
			{
				var isVerticalChart = CurrentRenderPassData.IsVerticalChart;

				var coordinateCalculator = CurrentRenderPassData.YCoordinateCalculator;

				double xCoord = isVerticalChart ? nearestHitResult.HitTestPoint.Y : nearestHitResult.HitTestPoint.X;
				double y1Coord = coordinateCalculator.GetCoordinate(nearestHitResult.Y1Value.ToDouble());

				var y1HitPoint = TransformPoint(new Point(xCoord, y1Coord), isVerticalChart);

				nearestHitResult.Y1HitTestPoint = y1HitPoint;

				nearestHitResult.IsHit = nearestHitResult.IsHit ||
                                         PointUtil.Distance(y1HitPoint, rawPoint) < hitTestRadius;
			}

			return nearestHitResult;
		}

		protected override HitTestInfo InterpolatePoint(Point rawPoint, HitTestInfo nearestHitResult, double hitTestRadius)
		{
			var secondLineHitResult = new HitTestInfo
			{
				XValue = nearestHitResult.XValue,
				YValue = nearestHitResult.Y1Value,
				HitTestPoint = nearestHitResult.Y1HitTestPoint,
				DataSeriesIndex = nearestHitResult.DataSeriesIndex

			};

            var yValues = GetPrevAndNextYValues(nearestHitResult.DataSeriesIndex, i => ((IComparable)DataSeries.YValues[i]).ToDouble());
			var hitResult = InterpolatePoint(rawPoint, nearestHitResult, hitTestRadius, yValues);

		    yValues = GetPrevAndNextYValues(secondLineHitResult.DataSeriesIndex, i => ((IComparable) ((IXyyDataSeries) DataSeries).Y1Values[i]).ToDouble());
            secondLineHitResult = InterpolatePoint(rawPoint, secondLineHitResult, hitTestRadius, yValues);

			hitResult.Y1Value = secondLineHitResult.YValue;
			hitResult.Y1HitTestPoint = secondLineHitResult.HitTestPoint;

			hitResult.IsHit = hitResult.IsHit || secondLineHitResult.IsHit;

			return hitResult;
		}

        /// <summary>
        /// Gets a value indicating whether this <see cref="BaseRenderableSeries"/> is valid for drawing.
        /// </summary>
        protected override bool GetIsValidForDrawing()
        {
            var isValid = base.GetIsValidForDrawing() &&
                          ((SeriesColor.A != 0 && StrokeThickness > 0) ||
                           (Series1Color.A != 0 && StrokeThickness > 0) ||
                           BandDownColor.A != 0 || BandUpColor.A != 0 ||
                           PointMarker != null);
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
			AssertDataPointType<XyySeriesPoint>("XyyDataSeries");

			using (var series1Pen = renderContext.CreatePen(SeriesColor, AntiAliasing, StrokeThickness, Opacity))
			using (var series2Pen = renderContext.CreatePen(Series1Color, AntiAliasing, StrokeThickness, Opacity))
			{
				DrawBands(series1Pen, series2Pen, renderContext, renderPassData);
			}
		}

		private void DrawBands(IPen2D series1Pen, IPen2D series2Pen, IRenderContext2D renderContext, IRenderPassData renderPassData)
		{
			using (var bandUpBrush = renderContext.CreateBrush(BandUpColor, Opacity))
			using (var bandDownBrush = renderContext.CreateBrush(BandDownColor, Opacity))
			{
				var pointSeries = (XyyPointSeries)CurrentRenderPassData.PointSeries;
				int setCount = pointSeries.Count;

				/*
				 * Two series, by XyyPoint
				 * Y0, Y1
				 * coordinates are 
				 * X0 X1
				 * Y0a Y1a
				 * Y1b Y1b
				 * 
				 * While Y0 > Y1, shade polygon in BandColor
				 * While Y1 < Y0, shade polygon in BandColor1
				 * 
				 * Draw lines from X0 Y0a to X1 Y1a in SeriesColor
				 * Draw lines from X0 Y0b to X2 Y1b in SeriesColor1
				 * 
				 */

				//var stopwatch = Stopwatch.StartNew();

				// Collate data into points (x1, y1, x2, y2 ...)
				// and render lines 
                var drawingHelper = SeriesDrawingHelpersFactory.GetSeriesDrawingHelper(renderContext, CurrentRenderPassData);
                
				var polygons = CreateBandPolygons(renderContext, renderPassData, pointSeries.YPoints, bandUpBrush,
												  pointSeries.Y1Points, bandDownBrush);

				foreach (var polygon in polygons)
				{
					// Draw polygons
                    var points = PointUtil.ClipPolygon(polygon.Points, renderContext.ViewportSize).ToArray();
                    drawingHelper.FillPolygon(polygon.Brush, points);
				}
               
                // Draw Series0
                using (var pen = renderContext.CreatePen(SeriesColor, AntiAliasing, StrokeThickness, Opacity, Series0StrokeDashArray))
                {
                    var linesPathContextFactory = SeriesDrawingHelpersFactory.GetLinesPathFactory(renderContext, CurrentRenderPassData);

                    FastLinesHelper.IterateLines(linesPathContextFactory, pen, pointSeries.YPoints,
                        CurrentRenderPassData.XCoordinateCalculator,
                        CurrentRenderPassData.YCoordinateCalculator,
                        IsDigitalLine,
                        DrawNaNAs == LineDrawMode.ClosedLines);
                }

                // Draw Series1
                using (var pen = renderContext.CreatePen(Series1Color, AntiAliasing, StrokeThickness, Opacity, Series1StrokeDashArray))
                {
                    var linesPathContextFactory = SeriesDrawingHelpersFactory.GetLinesPathFactory(renderContext, CurrentRenderPassData);

                    FastLinesHelper.IterateLines(linesPathContextFactory, pen, pointSeries.Y1Points,
                        CurrentRenderPassData.XCoordinateCalculator,
                        CurrentRenderPassData.YCoordinateCalculator,
                        IsDigitalLine,
                        DrawNaNAs == LineDrawMode.ClosedLines);
                }

				var pm = GetPointMarker();
				if (pm != null)
				{
                    // by default point marker colors bounded to series color
                    var hasDefaultPen = pm.Stroke == SeriesColor;
                    var hasDefaultBrush = pm.Fill == SeriesColor;

				    var pen1 = hasDefaultPen ? series1Pen : null;
				    var pen2 = hasDefaultPen ? series2Pen : null;

				    var brush1 = hasDefaultBrush ? bandUpBrush : null;
				    var brush2 = hasDefaultBrush ? bandDownBrush : null;

					// Iterate over points collection and render point markers
					for (int i = 0; i < setCount; i++)
					{
						var point = pointSeries[i] as GenericPoint2D<XyySeriesPoint>;

						double x = point.X;
						double ya = point.YValues.Y0;
						double yb = point.YValues.Y1;

// ReSharper disable EqualExpressionComparison
// ReSharper disable CompareOfFloatsByEqualityOperator
                        // If either Ya or Yb are NaN
                        if (ya != ya || yb != yb)
// ReSharper restore CompareOfFloatsByEqualityOperator
// ReSharper restore EqualExpressionComparison
						{
							continue;
						}

						var x1 = (float) renderPassData.XCoordinateCalculator.GetCoordinate(x);
						var y1a = (float) renderPassData.YCoordinateCalculator.GetCoordinate(ya);
						var y1b = (float) renderPassData.YCoordinateCalculator.GetCoordinate(yb);

					    drawingHelper.DrawPoint(pm, new Point(x1, y1a), brush1, pen1);
                        drawingHelper.DrawPoint(pm, new Point(x1, y1b), brush2, pen2);
					}
				}
			}
		}

		/// <summary>
		/// Creates the band polygons given two <see cref="IPointSeries"/> inputs 
		/// </summary>
		/// <param name="renderContext">The render context.</param>
		/// <param name="renderPassData">The render pass data.</param>
		/// <param name="yPointSeries">The y point series.</param>
		/// <param name="yBrush">The y brush.</param>
		/// <param name="y1PointSeries">The y1 point series.</param>
		/// <param name="y1Brush">The y1 brush.</param>
		/// <returns></returns>
		protected virtual IList<Polygon> CreateBandPolygons(IRenderContext2D renderContext, IRenderPassData renderPassData, IPointSeries yPointSeries, IBrush2D yBrush, IPointSeries y1PointSeries, IBrush2D y1Brush)
		{
			Guard.NotNull(yPointSeries, "yPointSeries");
			Guard.NotNull(y1PointSeries, "y1PointSeries");
			Guard.ArrayLengthsSame(yPointSeries.Count, "yPointSeries", y1PointSeries.Count, "y1PointSeries");

			/*
			 * Logic for band creation
			 * 
			 * At start, is Y>Y1? Yes, start up band, is Y<Y1 start down band
			 * iterate until you find crossover point, keep intersection and create band polygon
			 * 
			 * Flip band polygon (up -> down and down -> up)
			 * iterate until you find a crossover point, keep intersection and create band polygon
			 * 
			 */

            var polygons = new List<Polygon>();
            var pointSeriesCount = yPointSeries.Count;

            var pointsForNewPolygon = new List<Point>(32);

            var polygonStartIndex = 0;
            polygonStartIndex = SkipNans(yPointSeries, y1PointSeries, polygonStartIndex, pointSeriesCount);

            for (var currentPointIndex = polygonStartIndex + 1; currentPointIndex < pointSeriesCount; currentPointIndex++)
            {
                // Start creating band geometry. Add X,Y of one edge of the polygon
                var prevX = yPointSeries[currentPointIndex - 1].X;
                var prevY0 = yPointSeries[currentPointIndex - 1].Y;
                var prevY1 = y1PointSeries[currentPointIndex - 1].Y;

                var x = yPointSeries[currentPointIndex].X;
                var y0 = yPointSeries[currentPointIndex].Y;
                var y1 = y1PointSeries[currentPointIndex].Y;

                var hasNans = y0.IsNaN() || y1.IsNaN();
                if (hasNans)
                {
                    StartPolygon(polygonStartIndex, currentPointIndex, yPointSeries, renderPassData, pointsForNewPolygon);

                    FinishPolygon(polygonStartIndex, currentPointIndex, y1PointSeries, renderPassData, pointsForNewPolygon);

                    polygons.Add(new Polygon(pointsForNewPolygon.ToArray(), prevY0 > prevY1 ? yBrush : y1Brush));

                    // skip all NaNs
                    currentPointIndex++;
                    currentPointIndex = SkipNans(yPointSeries, y1PointSeries, currentPointIndex, pointSeriesCount);

                    if (currentPointIndex >= pointSeriesCount) break;

                    // Start a new polygon
                    polygonStartIndex = currentPointIndex;
                    pointsForNewPolygon = new List<Point>(32);
                }
                else
                {
                    var lineA = new PointUtil.Line(prevX, prevY0, x, y0);
                    var lineB = new PointUtil.Line(prevX, prevY1, x, y1);

                    Point intersection;
                    var hasIntersection = PointUtil.LineSegmentsIntersection2D(lineA, lineB, out intersection);

                    if (hasIntersection)
                    {
                        var xCoordCalculator = renderPassData.XCoordinateCalculator;
                        var yCoordCalculator = renderPassData.YCoordinateCalculator;
                        var isVerticalChart = renderPassData.IsVerticalChart;

                        var xCoord = (float)xCoordCalculator.GetCoordinate(x);
                        var xCoordPrev = (float)xCoordCalculator.GetCoordinate(prevX);

                        // Need to handle the case of Category XAxis,
                        // because its CoordCalculator rounds coords to the nearest integer value
                        var xIntersectionCoordinates = xCoordCalculator.IsCategoryAxisCalculator
                            ? xCoordPrev + (xCoord - xCoordPrev) * (intersection.X - prevX)
                            : xCoordCalculator.GetCoordinate(intersection.X);

                        var yIntersectionCoordinates = yCoordCalculator.GetCoordinate(intersection.Y);

                        var intersectionCoordinates = new Point(xIntersectionCoordinates, yIntersectionCoordinates);

                        StartPolygon(polygonStartIndex, currentPointIndex, yPointSeries, renderPassData, pointsForNewPolygon);

                        if (IsDigitalLine)
                        {
                            pointsForNewPolygon.Add(TransformPoint(new Point(xCoord, (float)yCoordCalculator.GetCoordinate(prevY0)), isVerticalChart));
                            pointsForNewPolygon.Add(TransformPoint(new Point(xCoord, (float)yCoordCalculator.GetCoordinate(prevY1)), isVerticalChart));
                        }
                        else
                        {
                            pointsForNewPolygon.Add(TransformPoint(intersectionCoordinates, isVerticalChart));
                        }

                        FinishPolygon(polygonStartIndex, currentPointIndex, y1PointSeries, renderPassData, pointsForNewPolygon);

                        polygons.Add(new Polygon(pointsForNewPolygon.ToArray(), prevY0 > prevY1 ? yBrush : y1Brush));

                        // Start a new polygon
                        polygonStartIndex = currentPointIndex;
                        pointsForNewPolygon = new List<Point>(32);
                        

                        //First point of a new polygon
                        if (!IsDigitalLine)
                        {
                            // Start a new polygon with the intersection point
                            pointsForNewPolygon.Add(TransformPoint(intersectionCoordinates, isVerticalChart));
                        }
                    }
                }
            }

            // Add last polygon
            if (pointSeriesCount > 0)
            {
                var lastY0Point = yPointSeries[pointSeriesCount - 1];
                var lastY1Point = y1PointSeries[pointSeriesCount - 1];

                if (!lastY0Point.Y.IsNaN() && !lastY1Point.Y.IsNaN())
                {
                    StartPolygon(polygonStartIndex, pointSeriesCount, yPointSeries, renderPassData, pointsForNewPolygon);
                    FinishPolygon(polygonStartIndex, pointSeriesCount, y1PointSeries, renderPassData, pointsForNewPolygon);

                    var fillBrush = lastY0Point.Y > lastY1Point.Y ? yBrush : y1Brush;
                    polygons.Add(new Polygon(pointsForNewPolygon.ToArray(), fillBrush));
                }
            }

            return polygons;
        }

        private void StartPolygon(int startIndex, int endIndex, IPointSeries y0PointSeries, IRenderPassData renderPassData, List<Point> polygonPoints)
        {
            for (var i = startIndex; i < endIndex; i++)
            {
                AddPointToPolygon(y0PointSeries[i], polygonPoints, false, renderPassData);
            }
        }

        private void FinishPolygon(int startIndex, int endIndex, IPointSeries y1PointSeries, IRenderPassData renderPassData, List<Point> polygonPoints)
        {
            for (var i = endIndex - 1; i >= startIndex; i--)
            {
                AddPointToPolygon(y1PointSeries[i], polygonPoints, true, renderPassData);
            }

            polygonPoints.Add(polygonPoints[0]);
        }

        private void AddPointToPolygon(IPoint point, List<Point> polygonPoints, bool needToInvert, IRenderPassData renderPassData)
        {
            var xCoordCalculator = renderPassData.XCoordinateCalculator;
            var yCoordCalculator = renderPassData.YCoordinateCalculator;
            var isVerticalChart = renderPassData.IsVerticalChart;

            var yCoord = (float)yCoordCalculator.GetCoordinate(point.Y);
            var xCoord = (float)xCoordCalculator.GetCoordinate(point.X);

            var transformPoint = TransformPoint(new Point(xCoord, yCoord), isVerticalChart);

            if (IsDigitalLine && polygonPoints.Count > 0)
            {
                var lastPoint = polygonPoints[polygonPoints.Count - 1];

                var isInverted = needToInvert ^ isVerticalChart;

                polygonPoints.Add(isInverted
                    ? new Point(lastPoint.X, transformPoint.Y)
                    : new Point(transformPoint.X, lastPoint.Y));
            }

            polygonPoints.Add(transformPoint);
        }

        private static int SkipNans(IPointSeries yPoints, IPointSeries y1Points, int index, int pointCount)
        {
            while (index < pointCount && (double.IsNaN(yPoints[index].Y) || double.IsNaN(y1Points[index].Y)))
                index++;

            return index;
        }

		/// <summary>
		/// Called when the <see cref="BaseRenderableSeries.DataSeries" /> property changes - i.e. a new <see cref="IDataSeries" /> has been set
		/// </summary>
		/// <param name="oldDataSeries">The old <see cref="IDataSeries" /></param>
		/// <param name="newDataSeries">The new <see cref="IDataSeries" /></param>
		/// <exception cref="System.InvalidOperationException"></exception>
		protected override void OnDataSeriesDependencyPropertyChanged(IDataSeries oldDataSeries, IDataSeries newDataSeries)
		{
			if (newDataSeries != null && !(newDataSeries is IXyyDataSeries))
			{
				throw new InvalidOperationException(string.Format("{0} expects a DataSeries of type {1}. Please ensure the correct data has been bound to the Renderable Series", GetType().Name, typeof(IXyyDataSeries)));
			}

			base.OnDataSeriesDependencyPropertyChanged(oldDataSeries, newDataSeries);
		}
		
		/// <summary>
		/// A struct to hold information about a Polygon drawn on a <see cref="FastBandRenderableSeries"/>
		/// </summary>
		protected struct Polygon
		{
			private readonly Point[] _points;
			private readonly IBrush2D _brush2D;

			/// <summary>
			/// Initializes a new instance of the <see cref="Polygon"/> struct.
			/// </summary>
			/// <param name="points">The points.</param>
			/// <param name="brush2D">The brush2 d.</param>
			public Polygon(Point[] points, IBrush2D brush2D)
				: this()
			{
				_points = points;
				_brush2D = brush2D;
			}

			/// <summary>
			/// Gets the brush for filling the polygon
			/// </summary>            
			public IBrush2D Brush { get { return _brush2D; } }

			/// <summary>
			/// Gets the points that form the outline of the polygon
			/// </summary>
			public Point[] Points { get { return _points; } }
		}
	}
}
