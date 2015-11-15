// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// StackedColumnsWrapper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    internal class StackedColumnsWrapper : StackedSeriesWrapperBase<IStackedColumnRenderableSeries>, IStackedColumnsWrapper
    {
        public double DataPointWidth { get; set; }

        /// <summary>
        /// Returns the data range of all the assosiated <see cref="IDataSeries"/> on X direction
        /// </summary>
        public IRange GetXRange(bool isLogarithmicAxis)
        {
            var range = GetCommonXRange().AsDoubleRange();

            if (!isLogarithmicAxis)
            {
                var count = SeriesCollection[0].DataSeries.Count;

                var additionalValue = count > 1
                    ? range.Diff / (count - 1) / 2 * GetDataPointWidthFraction()
                    : GetDataPointWidthFraction() / 2;

                range.Max += additionalValue;
                range.Min -= additionalValue;
            }

            return range;
        }

        private IRange GetCommonXRange()
        {
            IRange range = SeriesCollection[0].DataSeries.XRange;
            for (int i = 1; i < SeriesCollection.Count; i++)
            {
                range = range.Union(SeriesCollection[i].DataSeries.XRange);
            }
            
            return RangeFactory.NewRange(range.Min, range.Max);
        }

        public double GetDataPointWidthFraction()
        {
            return SeriesCollection[0].DataPointWidth;
        }

        /// <summary>
        /// Draws the <see cref="StackedColumnRenderableSeries"/> using the <see cref="IRenderContext2D"/> passed in
        /// </summary>
        /// <param name="series"></param>
        /// <param name="renderContext"></param>
        public override void DrawStackedSeries(IRenderContext2D renderContext)
        {
            // Checks whether all columns were passed to method, and then draw all one by one
            if (++Counter == SeriesCollection.Count(x => x.IsVisible))
            {
                Counter = 0;

                var seriesToDraw = SeriesCollection.Where(x => x.IsVisible).ToList();
                if (seriesToDraw.Any())
                {
                    var rpd = seriesToDraw[0].CurrentRenderPassData;
                    var fraction = GetDataPointWidthFraction();
                    var count = CalculateCount(rpd.PointSeries.XValues);

                    DataPointWidth = seriesToDraw[0].GetDatapointWidth(rpd.XCoordinateCalculator, rpd.PointSeries, count, fraction);

                    foreach (var stackedColumn in seriesToDraw)
                    {
                        DrawColumns(renderContext, (StackedColumnRenderableSeries)stackedColumn);
                    }
                }
            }
        }

        private double CalculateCount(IUltraList<double> xValues)
        {
            var minStep = double.MaxValue;

            for (int i = 1; i < xValues.Count; i++)
            {
                var tempDiff = xValues[i] - xValues[i - 1];
                if (tempDiff < minStep)
                {
                    minStep = tempDiff;
                }
            }

            return (xValues[xValues.Count - 1] - xValues[0] + minStep) / minStep;
        }

        /// <summary>
        /// Draws the <see cref="StackedColumnRenderableSeries"/> using the <see cref="IRenderContext2D"/>, <see cref="IRenderPassData"/> and renderable series itself passed in
        /// </summary>
        private void DrawColumns(IRenderContext2D renderContext, StackedColumnRenderableSeries series)
        {
            using (
                var penManager = new PenManager(renderContext, series.AntiAliasing, series.StrokeThickness,
                    series.Opacity))
            {
                var renderPassData = series.CurrentRenderPassData;

                var labelFormat = "{0:" + series.LabelTextFormatting + "}";

                var isOneHundredPercent = IsOneHundredPercentGroup(series.StackedGroupId);
                if (isOneHundredPercent)
                {
                    labelFormat += "%";
                }

                var isVerticalChart = renderPassData.IsVerticalChart;
                var gradientRotationAngle = series.GetChartRotationAngle();

                double spacing;
                var width = CalculateColumnWidth(series.StackedGroupId, out spacing);
                width = Math.Max(width, 0d);

                var fillBrush = renderContext.CreateBrush(series.FillBrush, series.Opacity, series.FillBrushMappingMode);
                var pen = penManager.GetPen(series.SeriesColor);

                var drawingHelper = SeriesDrawingHelpersFactory.GetSeriesDrawingHelper(renderContext, renderPassData);

                for (int i = 0; i < renderPassData.PointSeries.Count; i++)
                {
                    var drawPoint = renderPassData.PointSeries[i];

                    int groupIndex;
                    var count = GetGroupCountAtX(series, drawPoint.X, out groupIndex);

                    var xCenter = renderPassData.XCoordinateCalculator.GetCoordinate(drawPoint.X);
                    var center = CalculateShiftedColumnCenter(xCenter, groupIndex, count, width, spacing);

                    var accumulatedYValue = AccumulateYValueAtX(series, i, true);

                    var top = renderPassData.YCoordinateCalculator.GetCoordinate(accumulatedYValue.Item1);
                    var bottom = renderPassData.YCoordinateCalculator.GetCoordinate(accumulatedYValue.Item2);

                    var p1 = DrawingHelper.TransformPoint(new Point(center - width / 2d, top), isVerticalChart);
                    var p2 = DrawingHelper.TransformPoint(new Point(center + width / 2d, bottom), isVerticalChart);

                    var paletteProvider = series.PaletteProvider;
                    if (paletteProvider != null)
                    {
                        var overrideColor = paletteProvider.GetColor(series, drawPoint.X, drawPoint.Y);
                        if (overrideColor.HasValue)
                        {
                            using (var overriddenPen = penManager.GetPen(overrideColor.Value))
                            using (var overriddenFill = renderContext.CreateBrush(overrideColor.Value))
                            {
                                DrawColumn(drawingHelper, p1, p2, overriddenPen, overriddenFill,
                                    gradientRotationAngle);
                            }
                        }
                    }
                    else
                    {
                        DrawColumn(drawingHelper, p1, p2, pen, fillBrush,
                            gradientRotationAngle);
                    }

                    if (width > 0 && series.ShowLabel)
                    {
                        var labelValue = drawPoint.Y;

                        if (isOneHundredPercent)
                        {
                            var yRangeAtX = GetYRangeAtX(series, s => s.CurrentRenderPassData.PointSeries.YValues[i]);

                            labelValue /= (double)yRangeAtX.Diff*100d;
                        }

                        var labelText = string.Format(labelFormat, labelValue);

                        renderContext.DrawText(labelText, new Rect(p1, p2), AlignmentX.Center, AlignmentY.Center, series.LabelColor, series.LabelFontSize);
                    }

                }
            }
        }

        private void DrawColumn(ISeriesDrawingHelper drawingHelper, Point leftUpper, Point rightBottom, IPen2D stroke, IBrush2D fill, double gradientRotationAngle)
        {
            var drawAsLine = leftUpper.X.CompareTo(rightBottom.X) == 0;

            if (drawAsLine)
            {
                drawingHelper.DrawLine(leftUpper, rightBottom, stroke);
            }
            else
            {
                drawingHelper.DrawBox(leftUpper, rightBottom, fill, stroke, gradientRotationAngle);   
            }
        }

        private double CalculateColumnWidth(string stackedGroupId, out double spacingInPixels)
        {
            double result;
            var count = GetVisibleGroupsCount();

            var spacing = GetSpacing(stackedGroupId);
            if (GetSpacingMode(stackedGroupId) == SpacingMode.Absolute)
            {
                spacingInPixels = spacing;
                result = (DataPointWidth - spacing * (count - 1)) / count;
            }
            else
            {
                result = DataPointWidth / (count + spacing * count - spacing);
                spacingInPixels = result * spacing;
            }

            return result;
        }

        private int GetVisibleGroupsCount()
        {
            return SeriesGroups.Count(group => group.Item2.Any(x => x.IsVisible));
        }

        private double CalculateShiftedColumnCenter(double xValue, int groupIndex, int count, double width, double spacing)
        {
            return xValue - width * count / 2 - spacing * (count - 1) / 2 + groupIndex * (spacing + width) + 0.5 * width;
        }

        /// <summary>
        /// Returns Upper and Lower Bound of <see cref="IStackedColumnRenderableSeries"/> column
        /// </summary>
        public Tuple<double, double> GetSeriesVerticalBounds(IStackedColumnRenderableSeries series, int indexInDataSeries)
        {
            var val = AccumulateYValueAtX(series, indexInDataSeries);

            return new Tuple<double, double>(val.Item2, val.Item1);
        }

        /// <summary>
        /// Returns DataPointWith of <see cref="IStackedColumnRenderableSeries"/> considering spacing between groups
        /// </summary>
        public double GetSeriesBodyWidth(IStackedColumnRenderableSeries series, int dataSeriesIndex)
        {
            double spacing;
            return CalculateColumnWidth(series.StackedGroupId, out spacing);
        }

        internal int GetGroupCountAtX(IStackedColumnRenderableSeries series, double xValue, out int groupIndex)
        {
            groupIndex = -1;
            var count = 0;
            foreach (var seriesGroup in SeriesGroups)
            {
                var visibleSeries = seriesGroup.Item2.Where(x => x.IsVisible).ToList();
                foreach (var s in visibleSeries)
                {
                    var pointSeries = s.CurrentRenderPassData.PointSeries;
                    if (pointSeries != null)
                    {
                        var indexOfX = pointSeries.XValues.FindIndex(true, xValue, SearchMode.Exact);
                        if (indexOfX != -1 && !NumberUtil.IsNaN(pointSeries.YValues[indexOfX]))
                        {
                            count++;
                            break;
                        }
                    }
                }
                if (visibleSeries.Contains(series))
                {
                    groupIndex = count - 1;
                }
            }
            return count;
        }

        /// <summary>
        /// Gets mode of setting Spacing between stacked column groups
        /// </summary>
        public SpacingMode GetSpacingMode(string groupId)
        {
            var seriesFromSameGroup = GetStackedSeriesFromSameGroup(groupId);
            return seriesFromSameGroup[0].SpacingMode;
        }

        /// <summary>
        /// Gets spacing between stacked column groups
        /// </summary>
        public double GetSpacing(string groupId)
        {
            var seriesFromSameGroup = GetStackedSeriesFromSameGroup(groupId);
            return seriesFromSameGroup[0].Spacing;
        }

        /// <summary>
        /// Returns shifted <see cref="HitTestInfo"/> for horizontally / vertically stacked <see cref="StackedColumnRenderableSeries"/>
        /// </summary>
        public override HitTestInfo ShiftHitTestInfo(Point rawPoint, HitTestInfo nearestHitResult, double hitTestRadius, IStackedColumnRenderableSeries series)
        {
            nearestHitResult = base.ShiftHitTestInfo(rawPoint, nearestHitResult, hitTestRadius, series);

            if (!nearestHitResult.IsEmpty())
            {
                var isVerticalChart = series.CurrentRenderPassData.IsVerticalChart;

                double spacing;
                var width = CalculateColumnWidth(series.StackedGroupId, out spacing);

                int groupIndex;
                var count = GetGroupCountAtX(series, nearestHitResult.XValue.ToDouble(), out groupIndex);

                var hittestPoint = DrawingHelper.TransformPoint(nearestHitResult.HitTestPoint, isVerticalChart);

                var yCoord = hittestPoint.Y;
                var xCoord = CalculateShiftedColumnCenter(hittestPoint.X, groupIndex, count, width, spacing);

                var nearestPoint = new Point(xCoord, yCoord);
                nearestHitResult.HitTestPoint = DrawingHelper.TransformPoint(nearestPoint, isVerticalChart);

                nearestHitResult.IsHit = PointUtil.Distance(nearestPoint, rawPoint) < hitTestRadius;

                var distance = isVerticalChart ? Math.Abs(xCoord - rawPoint.Y) : Math.Abs(xCoord - rawPoint.X);
                nearestHitResult.IsWithinDataBounds = nearestHitResult.IsVerticalHit = distance < width / 2;
            }

            return nearestHitResult;
        }
    }
}