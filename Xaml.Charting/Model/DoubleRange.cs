// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DoubleRange.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Defines a range of type <see cref="System.Double"/>
    /// </summary>
    public class DoubleRange : Range<double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleRange"/> class.
        /// </summary>
        /// <remarks></remarks>
        public DoubleRange()
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleRange"/> class.
        /// </summary>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <remarks></remarks>
        public DoubleRange(double min, double max) : base(min, max)
        {
        }

        /// <summary>
        /// Returns a new Undefined range
        /// </summary>        
        public static DoubleRange UndefinedRange { get {  return new DoubleRange(double.NaN, double.NaN);} }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public override object Clone()
        {
            return new DoubleRange(Min, Max);
        }

        /// <summary>
        /// Gets the difference (Max - Min) of this range
        /// </summary>
        public override double Diff
        {
            get { return Max - Min; }
        }

        /// <summary>
        /// Gets whether the range is Zero, where Max equals Min
        /// </summary>
        /// <remarks></remarks>
        public override bool IsZero
        {
            get { return Math.Abs(Diff) <= double.Epsilon; }
        }

        /// <summary>
        /// Converts this range to a <see cref="DoubleRange"/>, which are used internally for calculations
        /// </summary>
        /// <example>For numeric ranges, the conversion is simple. For <see cref="DateRange"/> instances, returns a new <see cref="DoubleRange"/> with the Min and Max Ticks</example>
        /// <returns></returns>
        public override DoubleRange AsDoubleRange()
        {
            return this;
        }

        /// <summary>
        /// Sets the Min, Max values on the <see cref="IRange"/>, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public override IRange<double> SetMinMax(double min, double max)
        {
            SetMinMaxInternal(min, max);

            return this;
        }

        /// <summary>
        /// Sets the Min, Max values on the <see cref="IRange"/> with a max range to clip values to, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <param name="maxRange">The max range, which is used to clip values.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public override IRange<double> SetMinMax(double min, double max, IRange<double> maxRange)
        {
            Min = Math.Max(min, maxRange.Min);
            Max = Math.Min(max, maxRange.Max);

            return this;
        }

        /// <summary>
        /// Grows the current <see cref="IRange{T}"/> by the min and max fraction, returning this instance after modification
        /// </summary>
        /// <param name="minFraction">The Min fraction to grow by. For example, Min = -10 and minFraction = 0.1 will result in the new Min = -11</param>
        /// <param name="maxFraction">The Max fraction to grow by. For example, Max = 10 and minFraction = 0.2 will result in the new Max = 12</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public override IRange<double> GrowBy(double minFraction, double maxFraction)
        {            
            double diff =  Diff;

            // If min == max, expand around the mid line
            double min = Min - minFraction * (IsZero ? Min : diff);
            double max = Max + maxFraction * (IsZero ? Max : diff);

            // Swap if min > max (occurs when mid line is negative)
            if (min > max)
            {
                NumberUtil.Swap(ref min, ref max);
            }

            // If still zero, then expand around the zero line
            if (Math.Abs(min - max) <= double.Epsilon && Math.Abs(min) <= double.Epsilon)
            {
                min = -1.0;
                max = 1.0;
            }

            Min = min;
            Max = max;

            return this;
        }

        /// <summary>
        /// Clips the current <see cref="IRange{T}"/> to a maxmimum range 
        /// </summary>
        /// <param name="maximumRange">The Maximum Range</param>
        /// <returns>This instance, after the operation</returns>
        public override IRange<double> ClipTo(IRange<double> maximumRange)
        {
            var oldMax = Max;
            var oldMin = Min;

            var max = Math.Min(Max, maximumRange.Max);
            var min = Math.Max(Min, maximumRange.Min);

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
