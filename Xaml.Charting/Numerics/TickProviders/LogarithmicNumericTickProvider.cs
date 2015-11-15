// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// LogarithmicNumericTickProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Text;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    public class LogarithmicNumericTickProvider : NumericTickProvider
    {
        /// <summary>
        /// Gets or sets the value which determines the base used for the logarithm.
        /// </summary>
        public double LogarithmicBase { get; set; }

        /// <summary>
        /// Calculates the Major Ticks for the axis given a VisibleRange and Delta
        /// </summary>
        /// <param name="tickRange">The VisibleRange</param>
        /// <param name="tickDelta">The Deltas, e.g. MinorDelta and MajorDelta</param>
        /// <returns>
        /// The Major ticks (data values) as double
        /// </returns>
        protected override double[] CalculateMajorTicks(IRange<double> tickRange, IAxisDelta<double> tickDelta)
        {
            var results = new List<double>();
            double min = tickRange.Min;
            double max = tickRange.Max;
            double current = min;
            double majorDelta = tickDelta.MajorDelta;

            if (!NumberUtil.IsPowerOf(current, LogarithmicBase, LogarithmicBase))
            {
                current = NumberUtil.RoundDownPower(current, LogarithmicBase, LogarithmicBase);
            }

            double start = Math.Log(current, LogarithmicBase);
            start = Math.Round(start, 10);

            if (!NumberUtil.IsDivisibleBy(start, majorDelta))
            {
                start = NumberUtil.RoundUp(start, majorDelta);
            }

            var exp = start;
            current = Math.Pow(LogarithmicBase, exp);

            int tickCount = 0;
            while (current <= max)
            {
                // If major ticks are calculated, the exponent of Current should be divisible by MajorDelta
                if (NumberUtil.IsDivisibleBy(exp, majorDelta))
                {
                    results.Add(current);
                }

                exp = start + ++tickCount*majorDelta;

                current = Math.Pow(LogarithmicBase, exp);
            }

            return results.ToArray();
        }

        /// <summary>
        /// Calculates the Minor Ticks for the axis given a VisibleRange and Delta
        /// </summary>
        /// <param name="tickRange">The VisibleRange</param>
        /// <param name="tickDelta">The Deltas, e.g. MinorDelta and MajorDelta</param>
        /// <returns>
        /// The Major ticks (data values) as double
        /// </returns>
        protected override double[] CalculateMinorTicks(IRange<double> tickRange, IAxisDelta<double> tickDelta)
        {
            var majorTicks = CalculateMajorTicks(tickRange, tickDelta);
            return CalculateMinorTicks(tickRange, tickDelta, majorTicks);
        }

        /// <summary>
        /// Calculates the Minor Ticks for the axis given a VisibleRange and Delta
        /// </summary>
        /// <param name="tickRange">The VisibleRange</param>
        /// <param name="tickDelta">The Deltas, e.g. MinorDelta and MajorDelta</param>
        /// <param name="majorTicks"></param>
        /// <returns>
        /// The Major ticks (data values) as double
        /// </returns>
        protected override double[] CalculateMinorTicks(IRange<double> tickRange, IAxisDelta<double> tickDelta,
            double[] majorTicks)
        {
            var results = new List<double>();

            var minorDelta = tickDelta.MinorDelta;
           var majorDelta = tickDelta.MajorDelta;

           var count = majorTicks.Length;

            // Generate minor ticks between all the major ones
            // plus before the first one and after the last one.
           while (count >= 0)
           {
               var logDiff = Math.Pow(LogarithmicBase, majorDelta);
               var upper = count < majorTicks.Length ? majorTicks[count] : majorTicks[count - 1] * logDiff;

               var prev = upper/logDiff;
               var increment = prev * minorDelta;

               var current = prev + increment;

               while (current < upper)
               {
                   results.Add(current);
                   current += increment;
               }

               --count;
           }

            results.Reverse();
            return results.ToArray();
        }
    }
}
