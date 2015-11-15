// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TimeSpanRange.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Defines a Range of Type TimeSpan
    /// </summary>
    /// <remarks></remarks>
    public class TimeSpanRange : Range<TimeSpan>
    {
        private static readonly string FormattingString = @"hh\:mm\:ss";

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpanRange"/> class.
        /// </summary>
        /// <remarks></remarks>
        public TimeSpanRange()
        {
            Min = TimeSpan.MaxValue;
            Max = TimeSpan.MaxValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpanRange"/> class.
        /// </summary>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <remarks></remarks>
        public TimeSpanRange(TimeSpan min, TimeSpan max)
            : base(min, max)
        {
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        /// <remarks></remarks>
        public override string ToString()
        {
            return string.Format("{0} {{Min={1}, Max={2}}}", GetType(), Min.ToString(FormattingString),
                                 Max.ToString(FormattingString));
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public override object Clone()
        {
            return new TimeSpanRange(Min, Max);
        }

        /// <summary>
        /// Gets the difference (Max - Min) of this range
        /// </summary>
        public override TimeSpan Diff
        {
            get { return new TimeSpan((Max - Min).Ticks); }
        }

        /// <summary>
        /// Gets whether the range is Zero, where Max equals Min
        /// </summary>
        /// <remarks></remarks>
        public override bool IsZero
        {
            get { return (Max - Min).Ticks <= TimeSpan.MinValue.Ticks; }
        }

        /// <summary>
        /// Converts this range to a <see cref="DoubleRange"/>, which are used internally for calculations
        /// </summary>
        /// <returns></returns>
        /// <example>For numeric ranges, the conversion is simple. For <see cref="TimeSpanRange"/> instances, returns a new <see cref="DoubleRange"/> with the Min and Max Ticks</example>
        /// <remarks></remarks>
        public override DoubleRange AsDoubleRange()
        {
            return new DoubleRange(Min.Ticks, Max.Ticks);
        }

        /// <summary>
        /// Sets the Min, Max values on the <see cref="IRange{T}"/>, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public override IRange<TimeSpan> SetMinMax(double min, double max)
        {
            var minTicks = NumberUtil.Constrain((long)min.RoundOff(), TimeSpan.MinValue.Ticks, TimeSpan.MaxValue.Ticks);
            var maxTicks = NumberUtil.Constrain((long)max.RoundOff(), TimeSpan.MinValue.Ticks, TimeSpan.MaxValue.Ticks);

            SetMinMaxInternal(new TimeSpan(minTicks), new TimeSpan(maxTicks));

            return this;
        }

        /// <summary>
        /// Sets the Min, Max values on the <see cref="IRange{T}"/> with a maximum range limit, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <param name="maxRange">The max range.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public override IRange<TimeSpan> SetMinMax(double min, double max, IRange<TimeSpan> maxRange)
        {
            var minTimeSpan = new TimeSpan((long)min.RoundOff());
            var maxTimeSpan = new TimeSpan((long)max.RoundOff());

            if (maxRange != null)
            {
                minTimeSpan = ComparableUtil.Max(minTimeSpan, maxRange.Min);
                maxTimeSpan = ComparableUtil.Min(maxTimeSpan, maxRange.Max);
            }

            Min = minTimeSpan;
            Max = maxTimeSpan;

            return this;
        }

        /// <summary>
        /// Grows the current <see cref="IRange{T}"/> by the min and max fraction, returning this instance after modification
        /// </summary>
        /// <param name="minFraction">The Min fraction to grow by. For example, Min = -10 and minFraction = 0.1 will result in the new Min = -11</param>
        /// <param name="maxFraction">The Max fraction to grow by. For example, Max = 10 and minFraction = 0.2 will result in the new Max = 12</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public override IRange<TimeSpan> GrowBy(double minFraction, double maxFraction)
        {
            if (!Min.IsDefined() || !Max.IsDefined())
            {
                return this;
            }

            var range = (Max - Min).Ticks;

            if (range == 0)
            {
                Max = new TimeSpan((long)(Max.Ticks + Max.Ticks * maxFraction));
                Min = new TimeSpan((long)(Min.Ticks - Min.Ticks * minFraction));

                return this;
            }

            long newMaxTicks = (long)(Max.Ticks + range * maxFraction);
            long newMinTicks = (long)(Min.Ticks - range * minFraction);

            if (newMaxTicks > TimeSpan.MaxValue.Ticks || newMinTicks < TimeSpan.MinValue.Ticks)
            {
                return this;
            }

            Max = new TimeSpan(newMaxTicks);
            Min = new TimeSpan(newMinTicks);

            return this;
        }

        /// <summary>
        /// Clips the current <see cref="IRange{T}"/> to a maxmimum range 
        /// </summary>
        /// <param name="maximumRange">The Maximum Range</param>
        /// <returns>This instance, after the operation</returns>
        public override IRange<TimeSpan> ClipTo(IRange<TimeSpan> maximumRange)
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