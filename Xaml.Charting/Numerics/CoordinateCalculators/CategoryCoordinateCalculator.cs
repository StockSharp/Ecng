// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// CategoryCoordinateCalculator.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Numerics.CoordinateCalculators
{
    internal sealed class CategoryCoordinateCalculator : CoordinateCalculatorBase, ICategoryCoordinateCalculator
    {
        public bool IsBaseXValuesSorted { get; private set; }
        protected IList BaseXValues { get; private set; }

        private readonly double _barTimeFrame;
        private readonly IndexRange _visibleRange;
        private readonly double _pixelsPerBar;
        private readonly double _viewportDimension;

        public CategoryCoordinateCalculator(double barTimeFrame, double pixelsPerBar, double viewportDimension, IPointSeries categoryPointSeries, IndexRange visibleRange, bool isHorizontal, IList baseXValues, bool isBaseXValuesSorted)
        {
            BaseXValues = baseXValues;
            IsBaseXValuesSorted = isBaseXValuesSorted;
            IsXAxisCalculator = true;
            IsHorizontalAxisCalculator = isHorizontal;
            IsCategoryAxisCalculator = true;
            
            Guard.Assert(barTimeFrame, "barTimeFrame").IsGreaterThan(0.0);
            Guard.NotNull(categoryPointSeries, "categoryPointSeries");

            _barTimeFrame = barTimeFrame;
            _visibleRange = visibleRange;

            _pixelsPerBar = pixelsPerBar;
            _viewportDimension = viewportDimension;
        }

        public sealed override double GetCoordinate(DateTime dataValue)
        {
            return GetCoordinate(dataValue.Ticks);
        }

        public sealed override double GetCoordinate(double dataValue)
        {
            return _viewportDimension*(dataValue.RoundOff() - _visibleRange.Min)/(_visibleRange.Max - _visibleRange.Min) + CoordinatesOffset;
        }

        public sealed override double GetDataValue(double coordinate)
        {
            int numBarsInView = _visibleRange.Max - _visibleRange.Min;
            double localCoordinate = numBarsInView*(coordinate - CoordinatesOffset)/_viewportDimension;

            return (localCoordinate + _visibleRange.Min).RoundOff();
        }

        public override DoubleRange TranslateBy(double pixels, DoubleRange inputRange)
        {
            // Translate the minimum range by finding the index to the underlying dataset
            int pointsTraversed = (int) (-pixels/_pixelsPerBar);

            double newMin = inputRange.Min + pointsTraversed;
            double newMax = inputRange.Max + pointsTraversed;

            return new DoubleRange(newMin, newMax);
        }

        public DateTime TransformIndexToData(int index) {
            if(index == int.MinValue) return DateTime.MinValue;

            var baseSetCount = BaseXValues.Count;

            IComparable result = DateTime.MinValue;

            if (!BaseXValues.IsNullOrEmptyList())
            {
                // If datavalue is outside of the known data, linearly interpolate 
                // using the default point spacing (which is the estimated or actual 
                // datavalue between points passed to the coordinate calculator)
                if(index < 0) {
                    result = index * _barTimeFrame + TransformIndexToDataInternal(0).ToDateTime().Ticks;
                } else if(index >= baseSetCount) {
                    result = (index - baseSetCount + 1) * _barTimeFrame + TransformIndexToDataInternal(baseSetCount - 1).ToDateTime().Ticks;
                } else {
                    result = TransformIndexToDataInternal(index);
                }
            }

            return result.ToDateTime();
        }

        public int TransformDataToIndex(IComparable dataValue)
        {
            return TransformDataToIndex(dataValue.ToDateTime());
        }

        public int TransformDataToIndex(DateTime dataValue)
        {
            return TransformDataToIndex(dataValue, SearchMode.Nearest);
        }

        public int TransformDataToIndex(DateTime dataValue, SearchMode mode)
        {
            var baseSetCount = BaseXValues.Count;

            var result = TransformDataToIndexInternal(dataValue, mode);

            if (!BaseXValues.IsNullOrEmptyList())
            {
                var last = ((IComparable) BaseXValues[baseSetCount - 1]).ToDateTime();
                var first = ((IComparable) BaseXValues[0]).ToDateTime();

                // If datavalue is outside of the known data, linearly interpolate into the future/past
                if (dataValue > last)
                {
                    result = (int) ((dataValue.Ticks - last.Ticks)/_barTimeFrame + baseSetCount - 1);
                }
                else if(dataValue < first)
                {
                    result = (int) ((dataValue.Ticks - first.Ticks)/_barTimeFrame);
                }
            }

            return result;
        }


        private IComparable TransformIndexToDataInternal(int index)
        {
            index = NumberUtil.Constrain(index, 0, BaseXValues.Count - 1);

            return (IComparable)BaseXValues[index];
        }

        /// <summary>
        /// Finds index of the data-value in the point-series using corresponding <see cref="SearchMode"/>
        /// </summary>
        /// <param name="dataValue"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        private int TransformDataToIndexInternal(IComparable dataValue, SearchMode mode)
        {
            // Cast given dataValue to DataSeries' type
            if (!BaseXValues.IsNullOrEmptyList())
            {
                dataValue = ComparableUtil.FromDouble(dataValue.ToDouble(), BaseXValues[0].GetType());
            }

            return BaseXValues.FindIndex(IsBaseXValuesSorted, dataValue, mode);
        }
    }
}