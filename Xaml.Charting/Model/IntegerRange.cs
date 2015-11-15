// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IntegerRange.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// Defines a Range of type Integer
    /// </summary>
    public class IntegerRange : Range<int>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegerRange"/> class.
        /// </summary>
        /// <remarks></remarks>
        public IntegerRange()
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegerRange"/> class.
        /// </summary>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <remarks></remarks>
        public IntegerRange(int min, int max)
            : base(min, max)
        {
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public override object Clone()
        {
            return new IntegerRange(Min, Max);
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
            return new DoubleRange(Min, Max);
        }

        /// <summary>
        /// Sets the Min, Max values on the <see cref="IRange{T}"/>, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public override IRange<int> SetMinMax(double min, double max)
        {
            SetMinMaxInternal((int)min, (int)max);

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
            throw new NotImplementedException();
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
            var range = Max - Min;

            if (range == 0)
            {
                Max += (int)(Max * maxFraction);
                Min -= (int)(Min * minFraction);
                return this;
            }

            int max = Max + (int) (range*maxFraction);
            int min = Min - (int) (range*minFraction);

            return new IntegerRange(min, max);
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