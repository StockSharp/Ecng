// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TimeSpanDeltaCalculatorBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Numerics
{
    internal abstract class TimeSpanDeltaCalculatorBase : IDateDeltaCalculator
    {
        private const uint DefaultTicksCount = 8;

        private readonly IList<TimeSpan> _dateDeltas = new[]
                                                           {
                                                               TimeSpan.FromSeconds(1),
                                                               TimeSpan.FromSeconds(2),
                                                               TimeSpan.FromSeconds(5),
                                                               TimeSpan.FromSeconds(10),
                                                               TimeSpan.FromSeconds(30),
                                                               TimeSpan.FromMinutes(1),
                                                               TimeSpan.FromMinutes(2),
                                                               TimeSpan.FromMinutes(5),
                                                               TimeSpan.FromMinutes(10),
                                                               TimeSpan.FromMinutes(30),
                                                               TimeSpan.FromHours(1), 
                                                               TimeSpan.FromHours(2), 
                                                               TimeSpan.FromHours(4), 
                                                               TimeSpan.FromDays(1), 
                                                               TimeSpan.FromDays(2), 
                                                               TimeSpan.FromDays(7), 
                                                               TimeSpan.FromDays(14), 
                                                               TimeSpanExtensions.FromMonths(1), 
                                                               TimeSpanExtensions.FromMonths(3), 
                                                               TimeSpanExtensions.FromMonths(6), 
                                                               TimeSpanExtensions.FromYears(1),                                                                
                                                               TimeSpanExtensions.FromYears(2),      
                                                               TimeSpanExtensions.FromYears(5),      
                                                               TimeSpanExtensions.FromYears(10),      
                                                           };

        protected abstract long GetTicks(IComparable value);

        /// <summary>
        /// Given an absolute Axis Min and Max, returns a TickRange instance containing sensible MinorDelta and MajorDelta values
        /// </summary>
        public TimeSpanDelta GetDeltaFromRange(IComparable min, IComparable max, int minorsPerMajor, uint maxTicks = DefaultTicksCount)
        {
            return GetDeltaFromTickRange(GetTicks(min), GetTicks(max), minorsPerMajor, maxTicks);
        }

        private TimeSpanDelta GetDeltaFromTickRange(long min, long max, int minorsPerMajor, uint maxTicks)
        {
            if (min >= max)
            {
                return new TimeSpanDelta(TimeSpan.Zero, TimeSpan.Zero);
            }

            TimeSpanDelta deltaResult;

            // Get the first Major Delta that fits maxTicks deltas into the available range
            var range = (long)(max - min);
            var desiredSpan = new TimeSpan(range);
            var majorDelta = _dateDeltas.FirstOrDefault(x => new TimeSpan(x.Ticks * maxTicks) > desiredSpan);

            if (majorDelta.Equals(TimeSpan.Zero))
            {
                var years = range / TimeSpanExtensions.FromYears(1).Ticks;

                deltaResult = CalculateDeltas(0, years, minorsPerMajor, maxTicks, true);
            }
            else
            {
                deltaResult = GetDeltasForMajorDelta(majorDelta, range, minorsPerMajor, maxTicks);
            }

            return deltaResult;
        }

        private TimeSpanDelta CalculateDeltas(long min, long max, int minorsPerMajor, uint maxTicks, bool fromYears)
        {
            var tickSpacing = new NiceLongScale(min, max, minorsPerMajor, maxTicks).TickSpacing;

            var minorDelta = fromYears
                                 ? TimeSpanExtensions.FromYears((int) tickSpacing.Item1)
                                 : TimeSpan.FromTicks(tickSpacing.Item1);

            var majorDelta = fromYears
                                 ? TimeSpanExtensions.FromYears((int)tickSpacing.Item2)
                                 : TimeSpan.FromTicks(tickSpacing.Item2);

            return new TimeSpanDelta(minorDelta, majorDelta);
        }

        private TimeSpanDelta GetDeltasForMajorDelta(TimeSpan majorDelta, long range, int minorsPerMajor, uint maxTicks)
        {
            TimeSpanDelta deltaResult;

            // Case where both DateDeltas are in the list of predefined date deltas
            int i = _dateDeltas.IndexOf(majorDelta) - 2;
            if (i >= 0)
            {
                var minorDelta = _dateDeltas[i];
                deltaResult = new TimeSpanDelta(minorDelta, majorDelta);
            }
            else
            {
                // Case where the minor delta (or both) are less than the predefined list
                deltaResult = CalculateDeltas(0, range, minorsPerMajor, maxTicks, false);
            }

            return deltaResult;
        }

        
        IAxisDelta IDeltaCalculator.GetDeltaFromRange(IComparable min, IComparable max, int minorsPerMajor, uint maxTicks)
        {
            return GetDeltaFromRange(min, max, minorsPerMajor, maxTicks);
        }

    }
}
