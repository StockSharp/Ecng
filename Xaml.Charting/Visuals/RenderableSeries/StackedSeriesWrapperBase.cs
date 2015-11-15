// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// StackedSeriesWrapperBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Windows;
using Ecng.Xaml.Charting.Common.Databinding;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    internal abstract class StackedSeriesWrapperBase<T> : IStackedSeriesWrapperBase<T> where T : IStackedRenderableSeries
    {
        protected readonly List<Tuple<string, List<T>>> SeriesGroups;
        protected readonly List<T> SeriesCollection;

        // Uses internally to avoid drawing series separatelly
        protected int Counter;

        protected StackedSeriesWrapperBase()
        {
            SeriesCollection = new List<T>();
            SeriesGroups = new List<Tuple<string, List<T>>>();
        }

        /// <summary>
        /// Gets count if StackedSeriesCollection count
        /// </summary>
        public int GetStackedSeriesCount()
        {
            return SeriesCollection.Count;
        }

        /// <summary>
        /// Calculates YRange for passed in <see cref="series"/>
        /// </summary>
        public DoubleRange CalculateYRange(T series, IndexRange xIndexRange)
        {
            var yMax = series.ZeroLineY;
            var yMin = series.ZeroLineY;

            int iMin = Math.Max(xIndexRange.Min, 0);
            int iMax = Math.Min(xIndexRange.Max, series.DataSeries.XValues.Count - 1);

            for (int i = iMin; i <= iMax; i++)
            {
                var accumulatedValue = AccumulateYValueAtX(series, i);
                if (accumulatedValue.Item1 > 0 && accumulatedValue.Item1 > yMax)
                {
                    yMax = accumulatedValue.Item1;
                }
                if (accumulatedValue.Item1 < 0 && accumulatedValue.Item1 < yMin)
                {
                    yMin = accumulatedValue.Item1;
                }
            }
            return RangeFactory.NewRange(yMin, yMax).AsDoubleRange();
        }

        /// <summary>
        /// Accumulate Y value at <see cref="index"/> for stacked series in PointSeries or in DataSeries accordint to <see cref="inPointSeries"/>
        /// Item1 - represents Top Accumulated value
        /// Item2 - represents Bottom Accumulated value
        /// </summary>
        public Tuple<double, double> AccumulateYValueAtX(T series, int index, bool isResampledSeries = false)
        {
            double top = series.ZeroLineY;
            double bottom = series.ZeroLineY;
            var positive = 0d;
            var negative = 0d;

            var seriesFromSameGroup = GetStackedSeriesFromSameGroup(series.StackedGroupId);
            var isOneHundredPercent = IsOneHundredPercentGroup(series.StackedGroupId);

            foreach (var s in seriesFromSameGroup.Where(x => x.IsVisible))
            {
                var value = isResampledSeries
                    ? s.CurrentRenderPassData.PointSeries.YValues[index].ToDouble()
                    : ((IComparable)s.DataSeries.YValues[index]).ToDouble();

                if (NumberUtil.IsNaN(value))
                {
                    continue;
                }
                if (value >= 0)
                {
                    positive += value;
                }
                else if (value < 0)
                {
                    negative += value;
                }

                if (ReferenceEquals(s, series))
                {
                    top = value >= 0 ? positive : negative;
                    top += series.ZeroLineY;
                    bottom = top - value;

                    // skip further calculations of negative/positive if not 100% series
                    if (!isOneHundredPercent)
                        break;
                }
            }

            if (isOneHundredPercent)
            {
                var rangeDiff = positive - negative;

                // Don't expose a constant, because there will
                // be computation errors which cause hit test to fail at edges
                top = top * 100d / rangeDiff;
                bottom = bottom * 100d / rangeDiff;
            }

            return new Tuple<double, double>(top, bottom);
        }

        /// <summary>
        /// Gets a value whether all stacked series with the same StackedGroupId will appear 100% stacked
        /// </summary>
        public bool IsOneHundredPercentGroup(string groupId)
        {
            var seriesFromSameGroup = GetStackedSeriesFromSameGroup(groupId);

            return seriesFromSameGroup[0].IsOneHundredPercent;
        }

        public IRange GetYRangeAtX(T series, Func<T, double> getYValue)
        {
            var positive = 0.0;
            var negative = 0.0;

            var seriesFromSameGroup = GetStackedSeriesFromSameGroup(series.StackedGroupId);
            foreach (var s in seriesFromSameGroup.Where(x => x.IsVisible && x.DataSeries != null))
            {
                var value = getYValue(s);
                if (NumberUtil.IsNaN(value))
                {
                    continue;
                }
                if (value > 0)
                {
                    positive += value;
                }
                else
                {
                    negative += value;
                }
            }
            return RangeFactory.NewRange(negative, positive);
        }

        public abstract void DrawStackedSeries(IRenderContext2D renderContext);

        /// <summary>
        /// Returns shifted <see cref="HitTestInfo"/> for horizontally / vertically stacked <see cref="StackedColumnRenderableSeries"/>
        /// </summary>
        public virtual HitTestInfo ShiftHitTestInfo(Point rawPoint, HitTestInfo nearestHitResult, double hitTestRadius, T series)
        {
            if (series.DataSeries != null && series.CurrentRenderPassData != null && !nearestHitResult.IsEmpty())
            {
                var isVerticalChart = series.CurrentRenderPassData.IsVerticalChart;

                var hitTestPoint = DrawingHelper.TransformPoint(nearestHitResult.HitTestPoint, isVerticalChart);

                var xCoord = hitTestPoint.X;
                var yCoord = hitTestPoint.Y;

                var isOneHundredPercent = IsOneHundredPercentGroup(series.StackedGroupId);

                var seriesFromSameGroup = GetStackedSeriesFromSameGroup(series.StackedGroupId);
                if (seriesFromSameGroup.Count(x => x.IsVisible) > 1 || isOneHundredPercent)
                {
                    var index = nearestHitResult.DataSeriesIndex;
                    var yValue = AccumulateYValueAtX(series, index);

                    var yCoordinateCalculator = series.CurrentRenderPassData.YCoordinateCalculator;
                    yCoord = yCoordinateCalculator.GetCoordinate(yValue.Item1);

                    if (isOneHundredPercent)
                    {
                        var yRangeAtX = GetYRangeAtX(series, s => ((IComparable)s.DataSeries.YValues[index]).ToDouble());

                        nearestHitResult.Persentage = (double)nearestHitResult.YValue / (double)yRangeAtX.Diff * 100;
                        nearestHitResult.DataSeriesType = DataSeriesType.OneHundredPercentStackedXy;
                    }
                    nearestHitResult.Y1Value = yValue.Item1;
                }

                var nearestPoint = new Point(xCoord, yCoord);
                nearestHitResult.HitTestPoint = DrawingHelper.TransformPoint(nearestPoint, isVerticalChart);
            }

            return nearestHitResult;
        }

        #region Manipulations With Series Collections

        /// <summary>
        /// Add <see cref="IStackedRenderableSeries"/> to wrappers internal collection
        /// </summary>
        public void AddSeries(T series)
        {
            if (!SeriesCollection.Contains(series))
            {
                SeriesCollection.Add(series);

                string groupId = series.StackedGroupId;
                AddSeriesToGroup(series, groupId);
            }
        }

        /// <summary>
        /// Uses internally to add <see cref="IStackedRenderableSeries"/> to internal _seriesGroups dictionary
        /// </summary>
        private void AddSeriesToGroup(T series, string groupId)
        {
            if (ContainsGroup(groupId))
            {
                var seriesFromSameGroup = GetStackedSeriesFromSameGroup(series.StackedGroupId);
                seriesFromSameGroup.Add(series);
            }
            else
            {
                SeriesGroups.Add(new Tuple<string, List<T>>(groupId, new List<T> { series }));
            }
        }

        private bool ContainsGroup(string groupId)
        {
            return SeriesGroups.Any(group => group.Item1 == groupId);
        }

        /// <summary>
        /// Remove <see cref="IStackedRenderableSeries"/> from wrappers internal collection
        /// </summary>
        public void RemoveSeries(T series)
        {
            if (SeriesCollection.Contains(series))
            {
                SeriesCollection.Remove(series);

                string groupId = series.StackedGroupId;
                RemoveSeriesFromGroup(series, groupId);
            }
        }

        /// <summary>
        /// Uses internally to remove <see cref="IStackedRenderableSeries"/> from internal _seriesGroups dictionary
        /// </summary>
        private void RemoveSeriesFromGroup(T series, string groupId)
        {
            if (ContainsGroup(groupId))
            {
                var index = FindIndexOfGroup(groupId);

                SeriesGroups[index].Item2.Remove(series);

                if (SeriesGroups[index].Item2.Count == 0)
                {
                    SeriesGroups.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Used internally to move a stacked series to a new group
        /// </summary>
        public void MoveSeriesToAnotherGroup(T rSeries, string oldGroupId, string newGroupId)
        {
            if (ContainsGroup(oldGroupId))
            {
                RemoveSeriesFromGroup(rSeries, oldGroupId);
                AddSeriesToGroup(rSeries, newGroupId);
            }
        }

        /// <summary>
        /// Returns all <see cref="IStackedRenderableSeries"/> with the same groupId 
        /// </summary>
        public IList<T> GetStackedSeriesFromSameGroup(string groupId)
        {
            var i = FindIndexOfGroup(groupId);
            return SeriesGroups[i].Item2;
        }

        protected int FindIndexOfGroup(string groupId)
        {
            var index = -1;
            foreach (var group in SeriesGroups)
            {
                index++;
                if (group.Item1 == groupId)
                {
                    break;
                }
            }
            return index;
        }

        #endregion

        #region Internal access for testing

        internal List<T> StackedSeriesCollection { get { return SeriesCollection; } }

        internal List<Tuple<string, List<T>>> StackedSeriesGroups
        {
            get { return SeriesGroups; }
        }

        internal int SeriesToDrawCounter
        {
            get { return Counter; }
        }

        #endregion
    }
}