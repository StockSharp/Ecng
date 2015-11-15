// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TraderViewportManager.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting
{
    /*
    /// <summary>
    /// The TraderViewportManager calculates XAxis and YAxis ranges in an intuitive way for trading platforms. It expects the
    /// XAxis to be of type CategoryDateTimeAxis. If the extents ofthe data are outside of the viewport, no changes are applied to the
    /// range, but if the latest candle (value) is in the viewport, or the YMin, YMax are inside the viewport, it performs intuitive
    /// scaling on the XAxis and YAxis ranges 
    /// </summary>
    public class TraderViewportManager : ViewportManagerBase
    {
        public static readonly DependencyProperty PointsInViewportProperty = DependencyProperty.Register("PointsInViewport", typeof(int), typeof(TraderViewportManager), new PropertyMetadata(30, (s, e) => ((TraderViewportManager)s).OnPointsInViewportChanged(e)));
        public static readonly DependencyProperty YRatioProperty = DependencyProperty.Register("YRatio", typeof(DoubleRange), typeof(TraderViewportManager), new PropertyMetadata(null, (s, e) => ((TraderViewportManager)s).OnYRatioChanged(e)));

        private const int MinimumPointsInViewport = 10;
        private int _lastLatestXIndex = int.MinValue;
        private DoubleRange _lastYAutoRange;

        public int PointsInViewport
        {
            get { return (int)GetValue(PointsInViewportProperty); }
            set { SetValue(PointsInViewportProperty, value); }
        }

        public DoubleRange YRatio
        {
            get { return (DoubleRange)GetValue(YRatioProperty); }
            set { SetValue(YRatioProperty, value); }
        }

        public override void OnVisibleRangeChanged(IAxis axis)
        {
            Guard.ArgumentNotNull(axis, "axis");

            if (axis.IsXAxis)
            {
                ComputeDesiredPointsInViewport(axis);
            }
            else
            {
                ComputeDesiredYRatio(axis);
            }
        }

        private void ComputeDesiredYRatio(IAxis axis)
        {
            if (_lastYAutoRange == null)
                return;

            var yRange = axis.VisibleRange.AsDoubleRange();

            // This the ratio before the data update
            double ratioMax = (_lastYAutoRange.Max - yRange.Min) / (yRange.Max - yRange.Min);

            // This the ratio before the data update
            double ratioMin = (_lastYAutoRange.Min - yRange.Min) / (yRange.Max - yRange.Min);

            YRatio = new DoubleRange(ratioMin, ratioMax);
        }

        private void ComputeDesiredPointsInViewport(IAxis axis)
        {
            var categoryXAxis = axis as ICategoryAxis;
            if (categoryXAxis == null)
            {
                throw new ArgumentNullException("xAxis", "The parameter XAxis must not be null and must be of type CategoryDateTimeAxis");
            }

            // User has set a new visible range on the X-Axis. Calculate the points in viewport
            var dataset = categoryXAxis.DataSet;
            var newRange = categoryXAxis.VisibleRange;

            // Case 1: Latest point is out of the viewport and range has changed
            if (!newRange.IsValueWithinRange(_lastLatestXIndex))
            {
                int minIndex = (int)newRange.Min;
                int maxIndex = (int)newRange.Max;
                PointsInViewport = Math.Max(maxIndex - minIndex, MinimumPointsInViewport);
                return;
            }

            // Case 2: Latest point is in the viewport and range has changed
            int latestXIndex = dataset.BaseXValues.Count - 1;
            int minXIndex = (int)newRange.Min;
            PointsInViewport = Math.Max(latestXIndex - minXIndex, MinimumPointsInViewport);
        }

        protected override IRange OnCalculateNewYRange(IAxis yAxis, RenderPassInfo renderPassInfo)
        {
            var yRange = yAxis.VisibleRange.AsDoubleRange();
            var autoYRange = yAxis.CalculateYRange(renderPassInfo).AsDoubleRange();

            // Default case: YAxis is zoomed so that the max, min of the data (autoYRange) are outside of the 
            // axis range. In this case, just use the existing yMin, yMax values 
            double newYMin = yRange.Min;
            double newYMax = yRange.Max;

            // Case 1: There is spacing above the data, recompute the ratio and new YMax
            if (_lastYAutoRange != null && YRatio != null && autoYRange.Max > yRange.Max)
            {
                // Use this ratio to compute the new YRange max
                newYMax = ((autoYRange.Max - yRange.Min) / YRatio.Max) + yRange.Min;
            }

            // Case 2: There is spacing below the data, recompute the ratio and new yMin
            if (_lastYAutoRange != null && YRatio != null && autoYRange.Min < yRange.Min)
            {
                // Use this ratio to compute the new YRange Min
                newYMin = ((autoYRange.Min - yRange.Min) / YRatio.Min) + yRange.Min;
            }              

            _lastYAutoRange = autoYRange;
            return RangeFactory.NewWithMinMax(yRange, newYMin, newYMax);
        }

        protected override IRange OnCalculateNewXRange(IAxis xAxis)
        {
            var categoryXAxis = xAxis as ICategoryAxis;
            if (categoryXAxis == null)
            {
                throw new ArgumentNullException("xAxis", "The parameter XAxis must not be null and must be of type CategoryDateTimeAxis");
            }

            using (var s = xAxis.SuspendUpdates())
            {
                s.ResumeTargetOnDispose = false;
                var existingRange = categoryXAxis.VisibleRange;
                var dataset = categoryXAxis.DataSet;

                if (existingRange == null || !existingRange.IsDefined || dataset == null ||
                    dataset.BaseXValues.Count == 0)
                {
                    // Set default XAxis range if range is undefined
                    return existingRange;
                }

                var baseXValues = dataset.BaseXValues;
                int latestXIndex = baseXValues.Count - 1;

                // If the updated point is not in the viewport or has not changed
                if (latestXIndex == _lastLatestXIndex || !existingRange.IsValueWithinRange(_lastLatestXIndex))
                {
                    // Set the default range
                    _lastLatestXIndex = latestXIndex;
                    return existingRange;                    
                }

                int diff = latestXIndex - _lastLatestXIndex;

                var rangeMin = latestXIndex - PointsInViewport;
                var rangeMax = (((int) categoryXAxis.VisibleRange.Max) + diff);

                _lastLatestXIndex = latestXIndex;

                var newRange = new IndexRange(rangeMin, rangeMax);
                return newRange;
            }
        }

        private void OnPointsInViewportChanged(DependencyPropertyChangedEventArgs e)
        {
            int newValue = (int)e.NewValue;
            if (newValue < 10)
            {
                PointsInViewport = 10;
                return;
            }

            OnInvalidateParentSurface(e);
        }

        private void OnYRatioChanged(DependencyPropertyChangedEventArgs e)
        {
            var newValue = (DoubleRange) e.NewValue;
            if (newValue != null)
            {
                if (newValue.Min > newValue.Max)
                    throw new InvalidOperationException("YRatio.Min must be less than YRatio.Max");
            }

            OnInvalidateParentSurface(e);
        }
    }*/
}