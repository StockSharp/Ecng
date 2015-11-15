// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IndexRange.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Defines a range used to specify array indices to another series
    /// </summary>
    public class IndexRange : Range<int>
    {
        double _dMin, _dMax;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexRange"/> class.
        /// </summary>
        /// <remarks></remarks>
        public IndexRange() {
            Init(Min, Max);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexRange"/> class.
        /// </summary>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <remarks></remarks>
        public IndexRange(int min, int max) : base(min, max) {
            Init(Min, Max);
        }

        void Init(int min, int max) {
            ((INotifyPropertyChanged)this).PropertyChanged += OnPropertyChanged;

            _dMin = min;
            _dMax = max;
        }

        void OnPropertyChanged(object sender, PropertyChangedEventArgs args) {
            if(args.PropertyName != "Min" && args.PropertyName != "Max")
                return;

            if((int)_dMin.RoundOff() != Min)
                _dMin = Min;

            if((int)_dMax.RoundOff() != Max)
                _dMax = Max;
        }

        /// <summary>
        /// Gets whether this Range is defined
        /// </summary>
        /// <example>Min and Max are not equal to double.NaN and are greater or equal to zero</example>
        /// <remarks></remarks>
        public override bool IsDefined
        {
            get
            {
                return base.IsDefined && Min <= Max;
            }
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public override object Clone()
        {
            return new IndexRange(Min, Max) {
                _dMin = _dMin,
                _dMax = _dMax
            };
        }

        /// <summary>
        /// Gets the Diff (Max - Min) of this range
        /// </summary>
        public override int Diff
        {
            get { return Max - Min; }
        }

        /// <summary>
        /// Gets whether the range is Zero, where Max equals Min
        /// </summary>
        /// <remarks></remarks>
        public override bool IsZero
        {
            get { return Diff == 0; }
        }

        /// <summary>
        /// Converts this range to a <see cref="DoubleRange"/>, which are used internally for calculations
        /// </summary>
        /// <example>For numeric ranges, the conversion is simple. For <see cref="DateRange"/> instances, returns a new <see cref="DoubleRange"/> with the Min and Max Ticks</example>
        /// <returns></returns>
        public override DoubleRange AsDoubleRange()
        {
            return new DoubleRange(_dMin, _dMax);
        }

        /// <summary>
        /// Sets the Min, Max values on the <see cref="IRange{T}"/>, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public override IRange<int> SetMinMax(double min, double max) {
            var imin = (int)min.RoundOff();
            var imax = (int)max.RoundOff();

            SetMinMaxInternal(imin, imax);

            if(imin == Min) _dMin = min;
            if(imax == Max) _dMax = max;

            return this;
        }

        /// <summary>
        /// Sets the Min, Max values on the <see cref="IRange{T}"/> with a max range to clip values to, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <param name="maxRange">The max range, which is used to clip values.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public override IRange<int> SetMinMax(double min, double max, IRange<int> maxRange)
        {
            Min = (int) Math.Max(min, maxRange.Min);
            Max = (int) Math.Min(max, maxRange.Max);

            return this;
        }

        /// <summary>
        /// Grows the current <see cref="IRange{T}"/> by the min and max fraction, returning this instance after modification
        /// </summary>
        /// <param name="minFraction">The Min fraction to grow by. For example, Min = -10 and minFraction = 0.1 will result in the new Min = -11</param>
        /// <param name="maxFraction">The Max fraction to grow by. For example, Max = 10 and minFraction = 0.2 will result in the new Max = 12</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public override IRange<int> GrowBy(double minFraction, double maxFraction)
        {
            //UltrachartDebugLogger.Instance.WriteLine("GrowBy({0}, {1})  min={2}, max={3}, dmin={4}, dmax={5}", minFraction, maxFraction, Min, Max, _dMin, _dMax);

            var rangeDiff = Diff;

            var isZeroRange = (rangeDiff == 0);

            _dMax += maxFraction * (isZeroRange ? _dMax : rangeDiff);
            _dMin -= minFraction * (isZeroRange ? _dMin : rangeDiff);

            if(_dMax < _dMin)
                NumberUtil.Swap(ref _dMin, ref _dMax);

            var max = (int)_dMax.RoundOff();
            var min = (int)_dMin.RoundOff();

            if(max < min)
                NumberUtil.Swap(ref min, ref max);

            var tmpMin = _dMin;
            var tmpMax = _dMax;

            Min = min;
            Max = max;

            _dMin = tmpMin;
            _dMax = tmpMax;

            //UltrachartDebugLogger.Instance.WriteLine("GrowBy: min={0}, max={1}, dmin={2}, dmax={3}", Min, Max, _dMin, _dMax);

            return new IndexRange(min, max) {
                _dMin = _dMin,
                _dMax = _dMax,
            };
        }

        public override string ToString() {
            return string.Format("min={0}, max={1}, dmin={2:F3}, dmax={3:F3}", Min, Max, _dMin, _dMax);
        }

        /// <summary>
        /// Clips the current <see cref="IRange{T}"/> to a maxmimum range 
        /// </summary>
        /// <param name="maximumRange">The Maximum Range</param>
        /// <returns>This instance, after the operation</returns>
        public override IRange<int> ClipTo(IRange<int> maximumRange)
        {
            var oldMax = Max;
            var oldMin = Min;

            var max = Max > maximumRange.Max ? maximumRange.Max : Max;
            var min = Min < maximumRange.Min ? maximumRange.Min : Min;

            if (min > maximumRange.Max)
            {
                min = maximumRange.Min;
            }
            if (max < oldMin)
            {
                max = maximumRange.Max;
            }
            if (min > max)
            {
                min = maximumRange.Min;
                max = maximumRange.Max;
            }

            SetMinMaxInternal(min, max);

            return this;
        }
    }
}