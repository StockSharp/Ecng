// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FloatRange.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// Defines a range of type <see cref="System.Single"/>
    /// </summary>
    public class FloatRange : Range<float>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FloatRange"/> class.
        /// </summary>
        /// <remarks></remarks>
        public FloatRange()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FloatRange"/> class.
        /// </summary>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <remarks></remarks>
        public FloatRange(float min, float max)
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
            return new FloatRange(Min, Max);
        }

        /// <summary>
        /// Gets the difference (Max - Min) of this range
        /// </summary>
        public override float Diff
        {
            get { return Max - Min; }
        }

        /// <summary>
        /// Gets whether the range is Zero, where Max equals Min
        /// </summary>
        /// <remarks></remarks>
        public override bool IsZero
        {
            get { return Math.Abs(Max - Min) < double.Epsilon; }
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
        /// Sets the Min, Max values on the <see cref="IRange"/>, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public override IRange<float> SetMinMax(double min, double max)
        {
            SetMinMaxInternal((float)min, (float)max);

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
        public override IRange<float> SetMinMax(double min, double max, IRange<float> maxRange)
        {
            Min = Math.Max((float)min, maxRange.Min);
            Max = Math.Min((float)max, maxRange.Max);

            return this;
        }

        /// <summary>
        /// Grows the current <see cref="IRange{T}"/> by the min and max fraction, returning this instance after modification
        /// </summary>
        /// <param name="minFraction">The Min fraction to grow by. For example, Min = -10 and minFraction = 0.1 will result in the new Min = -11</param>
        /// <param name="maxFraction">The Max fraction to grow by. For example, Max = 10 and minFraction = 0.2 will result in the new Max = 12</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public override IRange<float> GrowBy(double minFraction, double maxFraction)
        {
            float diff = Diff;

            if (Math.Abs(diff) < double.Epsilon)
            {
                Max += Max * (float)maxFraction;
                Min -= Min * (float)minFraction;
                return this;
            }

            Max += diff * (float)maxFraction;
            Min -= diff * (float)minFraction;

            return this;
        }

        /// <summary>
        /// Clips the current <see cref="IRange{T}"/> to a maxmimum range 
        /// </summary>
        /// <param name="maximumRange">The Maximum Range</param>
        /// <returns>This instance, after the operation</returns>
        public override IRange<float> ClipTo(IRange<float> maximumRange)
        {
            Max = Math.Min(Max, maximumRange.Max);
            Min = Math.Max(Min, maximumRange.Min);

            return this;
        }
    }
}
