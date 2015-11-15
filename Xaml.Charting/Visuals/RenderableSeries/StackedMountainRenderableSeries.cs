// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// StackedMountainRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Visuals.PointMarkers;
using Ecng.Xaml.Licensing.Core;
using MatterHackers.Agg.VertexSource;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Defines a Stacked-Mountain renderable series, supporting rendering of mountain areas which have accumulated Y-values for multiple series in a group.
    /// </summary>
    /// <remarks>
    /// The StackedMountainRenderableSeries may render data from any a <see cref="IXyDataSeries{TX,TY}"/> derived data-source, 
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
    public class StackedMountainRenderableSeries : BaseMountainRenderableSeries, IStackedMountainRenderableSeries
    {
        /// <summary>
        /// Defines the StackedGroupId DependencyProperty
        /// </summary>
        public static readonly DependencyProperty StackedGroupIdProperty = DependencyProperty.Register("StackedGroupId", typeof(string), typeof(StackedMountainRenderableSeries), new PropertyMetadata("DefaultStackedGroupId", StackedGroupIdPropertyChanged));

        /// <summary>
        /// Defines the IsOneHundredPercent DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsOneHundredPercentProperty = DependencyProperty.Register("IsOneHundredPercent", typeof(bool), typeof(StackedMountainRenderableSeries), new PropertyMetadata(default(bool)));

        /// <summary>
        /// Initializes a new instance of the <see cref="StackedMountainRenderableSeries" /> class.
        /// </summary>
        public StackedMountainRenderableSeries()
        {
            DefaultStyleKey = typeof(StackedMountainRenderableSeries);
        }

        public IStackedMountainsWrapper Wrapper
        {
            get
            {
                var surface = (UltrachartSurface)GetParentSurface();
                return surface != null ? surface.StackedMountainsWrapper : null;
            }
        }

        /// <summary>
        /// Gets or sets a string stacked-group Id, used to ensure columns are stacked together
        /// </summary>
        public string StackedGroupId
        {
            get { return (string)GetValue(StackedGroupIdProperty); }
            set { SetValue(StackedGroupIdProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value whether all series with the same <see cref="StackedGroupId"/> will appear 100% stacked
        /// </summary>
        public bool IsOneHundredPercent
        {
            get { return (bool)GetValue(IsOneHundredPercentProperty); }
            set
            {
                SetValue(IsOneHundredPercentProperty, value);
                var ultraChartSurface = GetParentSurface();
                if (ultraChartSurface != null)
                {
                    ultraChartSurface.InvalidateElement();
                }
            }
        }

        /// <summary>
        /// Returns the data range of the assosiated <see cref="IDataSeries"/> on Y direction
        /// <param name="xRange">The X-Axis Range currently in view</param>
        /// <param name="getPositiveRange">Indicates whether to return positive YRange only</param>
        /// </summary>
        public override IRange GetYRange(IRange xRange, bool getPositiveRange)
        {
            if (xRange == null)
            {
                throw new ArgumentNullException("xRange");
            }

            var indicesRange = DataSeries.GetIndicesRange(xRange);

            var yRange = Wrapper.CalculateYRange(this, indicesRange);

            return yRange;
        }

        protected override void InternalDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            Wrapper.DrawStackedSeries(renderContext);
        }

        /// <summary>
        /// Draws the <see cref="StackedMountainRenderableSeries"/> using the <see cref="IRenderContext2D"/>, <see cref="IRenderPassData"/> and renderable series itself passed in
        /// </summary>
        void IStackedMountainRenderableSeries.DrawMountain(IRenderContext2D renderContext, bool isPreviousSeriesDigital)
        {
            var rpd = CurrentRenderPassData;
            var gradientRotationAngle = GetChartRotationAngle(CurrentRenderPassData);

            using (var areaBrush = renderContext.CreateBrush(AreaBrush, Opacity))
            using (var linePen = renderContext.CreatePen(SeriesColor, AntiAliasing, StrokeThickness, Opacity))
            {
                IPointSeries pointsForLine;
                var pointsForArea = FillWithPoint2DSeries(isPreviousSeriesDigital, out pointsForLine);

                var linesPathFactory = SeriesDrawingHelpersFactory.GetLinesPathFactory(renderContext, rpd);
                var mountainPathFactory = SeriesDrawingHelpersFactory.GetStackedMountainAreaPathFactory(renderContext, rpd, gradientRotationAngle);

                FastLinesHelper.IterateLines(mountainPathFactory, areaBrush, pointsForArea, rpd.XCoordinateCalculator, rpd.YCoordinateCalculator, false, false);
                FastLinesHelper.IterateLines(linesPathFactory, linePen, pointsForLine, rpd.XCoordinateCalculator, rpd.YCoordinateCalculator, IsDigitalLine, false);

                var pointMarker = GetPointMarker();
                if (pointMarker != null)
                {
                    // Iterate over points collection and render point markers
                    var pointMarkerPathFactory = SeriesDrawingHelpersFactory.GetPointMarkerPathFactory(renderContext, rpd, pointMarker);
                    FastPointsHelper.IteratePoints(pointMarkerPathFactory, pointsForLine, rpd.XCoordinateCalculator, rpd.YCoordinateCalculator);
                }
            }
        }

        private Point2DSeries FillWithPoint2DSeries(bool isPreviousSeriesDigital, out IPointSeries linePointSeries)
        {
            var rpd = CurrentRenderPassData;
            var count = rpd.PointSeries.Count;

            int topLineCapacity = IsDigitalLine ? count * 2 - 1 : count;
            int bottomLineCapacity = isPreviousSeriesDigital ? count * 2 - 1 : count;

            var capacity = topLineCapacity + bottomLineCapacity;

            var pointSeries = new Point2DSeries(capacity);
            pointSeries.XValues.SetCount(capacity);
            pointSeries.YValues.SetCount(capacity);

            var topPointSeries = new Point2DSeries(topLineCapacity);
            topPointSeries.XValues.SetCount(topLineCapacity);
            topPointSeries.YValues.SetCount(topLineCapacity);

            var topPosition = 0;
            var bottomPosition = capacity - 1;

            for (int i = 0; i < count; i++)
            {
                var drawPoint = rpd.PointSeries[i];

                var accumulatedYValue = Wrapper.AccumulateYValueAtX(this, i, true);

                if (IsDigitalLine && i != 0)
                {
                    topPointSeries.XValues[topPosition] = pointSeries.XValues[topPosition] = drawPoint.X;
                    topPointSeries.YValues[topPosition] = pointSeries.YValues[topPosition] = pointSeries.YValues[topPosition - 1];
                    topPosition++;
                }
                topPointSeries.XValues[topPosition] = pointSeries.XValues[topPosition] = drawPoint.X;
                topPointSeries.YValues[topPosition] = pointSeries.YValues[topPosition] = accumulatedYValue.Item1;
                topPosition++;

                if (isPreviousSeriesDigital && i != 0)
                {
                    pointSeries.XValues[bottomPosition] = drawPoint.X;
                    pointSeries.YValues[bottomPosition] = pointSeries.YValues[bottomPosition + 1];
                    bottomPosition--;
                }
                pointSeries.XValues[bottomPosition] = drawPoint.X;
                pointSeries.YValues[bottomPosition] = accumulatedYValue.Item2;
                bottomPosition--;
            }
            linePointSeries = topPointSeries;
            return pointSeries;
        }

        protected override HitTestInfo NearestHitResult(Point rawPoint, double hitTestRadius, SearchMode searchMode, bool considerYCoordinateForDistanceCalculation)
        {
            var nearestHitResult = HitTestInfo.Empty;

            if (IsVisible)
            {
                nearestHitResult = base.NearestHitResult(rawPoint, hitTestRadius, searchMode, considerYCoordinateForDistanceCalculation);

                nearestHitResult = Wrapper.ShiftHitTestInfo(rawPoint, nearestHitResult, hitTestRadius, this);
            }

            return nearestHitResult;
        }

        protected override HitTestInfo InterpolatePoint(Point rawPoint, HitTestInfo nearestHitResult, double hitTestRadius)
        {
            if (!nearestHitResult.IsEmpty())
            {
                var prevDataPointIndex = nearestHitResult.DataSeriesIndex;
                var nextDataPointIndex = nearestHitResult.DataSeriesIndex + 1;
                
                // Ensure the index isn't out of the bounds of the DataSeries.XValues
                if (nextDataPointIndex >= 0 && nextDataPointIndex < DataSeries.Count)
                {
                    var y1Values = GetPrevAndNextYValues(prevDataPointIndex, i => ((IComparable)DataSeries.YValues[i]).ToDouble());
                    var yValues = GetPrevAndNextYValues(prevDataPointIndex, i => Wrapper.AccumulateYValueAtX(this, i).Item1);

                    nearestHitResult = InterpolatePoint(rawPoint, nearestHitResult, hitTestRadius, yValues, y1Values);
                }
            }

            return nearestHitResult;
        }

        /// <summary>
        /// When overridden in derived classes, performs hit test on series using interpolated values
        /// </summary>
        protected override bool IsHitTest(Point rawPoint, HitTestInfo nearestHitResult, double hitTestRadius, Point previousDataPoint, Point nextDataPoint)
        {
            var isHit = base.IsHitTest(rawPoint, nearestHitResult, hitTestRadius, previousDataPoint, nextDataPoint);
            var hitDataValue = GetHitDataValue(rawPoint);

            if (!isHit)
            {
                isHit = Wrapper.IsHitTest(rawPoint, nearestHitResult, hitTestRadius, hitDataValue, this);
            }

            return isHit;
        }

        private static void StackedGroupIdPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var rSeries = d as StackedMountainRenderableSeries;
            if (rSeries != null && rSeries.Wrapper != null)
            {
                rSeries.Wrapper.MoveSeriesToAnotherGroup(rSeries, (string)e.OldValue, (string)e.NewValue);
            }
        }
    }
}