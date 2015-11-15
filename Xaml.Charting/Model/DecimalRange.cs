// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DecimalRange.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Defines a range of type <see cref="System.Decimal"/>
    /// </summary>
    public class DecimalRange : Range<decimal>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DecimalRange"/> class.
        /// </summary>
        /// <remarks></remarks>
        public DecimalRange()
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DecimalRange"/> class.
        /// </summary>
        /// <param name="min">The min value.</param>
        /// <param name="max">The max value.</param>
        /// <remarks></remarks>
        public DecimalRange(decimal min, decimal max)
            : base(min, max)
        {            
        }

        /// <summary>
        /// Gets the difference (Max - Min) of this range
        /// </summary>
        public override decimal Diff
        {
            get { return Max - Min; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is zero.
        /// </summary>
        /// <remarks></remarks>
        public override bool IsZero
        {
            get { return Diff == decimal.Zero; }
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public override object Clone()
        {
            return new DecimalRange(Min, Max);
        }

        /// <summary>
        /// Converts this range to a <see cref="DoubleRange"/>, which are used internally for calculations
        /// </summary>
        /// <returns></returns>
        /// <example>For numeric ranges, the conversion is simple. For <see cref="DateRange"/> instances, returns a new <see cref="DoubleRange"/> with the Min and Max Ticks</example>
        /// <remarks></remarks>
        public override DoubleRange AsDoubleRange()
        {
            return new DoubleRange((double)Min, (double)Max);
        }

        /// <summary>
        /// Sets the Min, Max values on the <see cref="IRange"/>, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public override IRange<decimal> SetMinMax(double min, double max)
        {
            SetMinMaxInternal((decimal)min, (decimal)max);

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
        public override IRange<decimal> SetMinMax(double min, double max, IRange<decimal> maxRange)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Grows the current <see cref="IRange{T}"/> by the min and max fraction, returning this instance after modification
        /// </summary>
        /// <param name="minFraction">The Min fraction to grow by. For example, Min = -10 and minFraction = 0.1 will result in the new Min = -11</param>
        /// <param name="maxFraction">The Max fraction to grow by. For example, Max = 10 and minFraction = 0.2 will result in the new Max = 12</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public override IRange<decimal> GrowBy(double minFraction, double maxFraction)
        {
            var range = Diff;

            if (range == 0.0M)
            {
                Max += Max * (decimal)maxFraction;
                Min -= Min * (decimal)minFraction;
                return this;
            }

            Max += range * (decimal)maxFraction;
            Min -= range * (decimal)minFraction;

            return this;
        }

        /// <summary>
        /// Clips the current <see cref="IRange{T}"/> to a maxmimum range 
        /// </summary>
        /// <param name="maximumRange">The Maximum Range</param>
        /// <returns>This instance, after the operation</returns>
        public override IRange<decimal> ClipTo(IRange<decimal> maximumRange)
        {
            Max = Math.Min(Max, maximumRange.Max);
            Min = Math.Max(Min, maximumRange.Min);

            return this;
        }
    }
}