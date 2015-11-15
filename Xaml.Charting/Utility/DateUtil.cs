// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DateUtil.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Globalization;
using System.Linq;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Utility
{
    internal static class DateUtil
    {
        private static readonly List<int> QuarterMonths = new List<int>(new[] {1, 4, 7, 10, 13});
        private static readonly List<int> HalfMonths = new List<int>(new[] { 1, 7, 13 });
        private static readonly List<int> BiMonths = new List<int>(new[] { 1, 3, 5, 7, 9, 11, 13 });

        private static readonly TimeSpan OneYear = TimeSpanExtensions.FromYears(1);
        private static readonly TimeSpan SixMonths = TimeSpanExtensions.FromMonths(6);
        private static readonly TimeSpan ThreeMonths = TimeSpanExtensions.FromMonths(3);
        private static readonly TimeSpan OneMonth = TimeSpanExtensions.FromMonths(1);

        internal static DateTime Max(DateTime a, DateTime b)
        {
            return a.Ticks > b.Ticks ? a : b;
        }

        internal static DateTime Min(DateTime a, DateTime b)
        {
            return a.Ticks < b.Ticks ? a : b;
        }

        internal static bool IsDivisibleBy(DateTime current, TimeSpan dateSpan)
        {
            // Special case for years - divisible by years if the day/month is 01
            if ((dateSpan.Ticks % OneYear.Ticks) == 0 &&
                current.Day == 01 && 
                current.Month == 01)
            {
                return true;
            }

            // Special case for years - divisible by years if the day is 01
            if ((dateSpan.Ticks % TimeSpanExtensions.FromMonths(1).Ticks) == 0 && 
                current.Day == 01)
            {
                return true;
            }

            return RoundUp(current, dateSpan).Equals(current);
        }

        internal static DateTime RoundUp(DateTime current, TimeSpan dateSpan)
        {
            if (dateSpan.IsDivisibleBy(OneYear))
            {
                long numYears = dateSpan.Ticks / OneYear.Ticks;
                
                // Check alignment. If already aligned to N years return current 
                if (current.Day == 01 && current.Month == 01 && NumberUtil.IsDivisibleBy((double)current.Year, (double)numYears))
                    return current;

                // Rounding up to N years is a simple numeric operation                
                int newYear = (int)NumberUtil.RoundUp((double)(current.Year + 1), (double)numYears);
                return new DateTime(newYear, 01, 01);
            }            
            if (dateSpan.IsDivisibleBy(SixMonths))
            {
                // Aligned to half-year already? Return input
                if (current.Day == 01 && HalfMonths.Contains(current.Month))
                    return current;

                int newMonth = current.Month < 7 ? 7 : 13;

                return NewAlignedDateTime(current.Year, newMonth);                
            }
            if (dateSpan.IsDivisibleBy(ThreeMonths))
            {
                // Aligned to quarter already? Return input
                if (current.Day == 01 && QuarterMonths.Contains(current.Month))
                    return current;

                int newMonth = current.Month < 4 ? 4 : current.Month < 7 ? 7 : current.Month < 10 ? 10 : 13;

                return NewAlignedDateTime(current.Year, newMonth);  
            }
            if (dateSpan.IsDivisibleBy(OneMonth))
            {
                // Aligned to month already? Return input
                if (current.Day == 01)
                    return current;

                // Numeric round up to 
                long numMonths =  (int)(dateSpan.Ticks / OneMonth.Ticks);
                int newMonth = (int)NumberUtil.RoundUp(current.Month + 1, (double)numMonths);
                int newYear = current.Year;

                return NewAlignedDateTime(newYear, newMonth);
            }

            double resultTicks = NumberUtil.RoundUp((double)current.Ticks, (double)dateSpan.Ticks);
            return new DateTime((long)resultTicks);
        }

        private static DateTime NewAlignedDateTime(int newYear, int newMonth)
        {
            while (newMonth > 12)
            {
                newMonth -= 12;
                newYear++;
            }

            return new DateTime(newYear, newMonth, 01);
        }

        private static DateTime AlignToYears(DateTime current, decimal amount)
        {
            int day = current.Day;
            int month = current.Month;
            int year = current.Year;

            if (day == 1 && month == 1)
            {
                year--;
            }

            // Find the remainder years to decide how much to round up by
            int remainder = year % (int)amount;
            year += (int)amount - remainder;

            return new DateTime(year, 01, 01);
        }

        private static DateTime AlignToMonths(DateTime current, decimal amount)
        {
            // Rounding up to 1 month = 1st of next month
            // Rounding up to 2 months = 1st of Feb, Apr, Jun, Aug, Oct, Dec depending on the input month
            // Rounding up to 3 months (quarterly) = 1st of Mar, Jun, Sep, Dec depending on input month
            // Rounding up to 6 months = 1st of Jun, Dec etc...

            int numMonths = (int) amount;
            int year = current.Year;
            int month = current.Month;
            int day = current.Day;

            if (numMonths == 1)
            {
                // No rounding necessary, already aligned to 1-month
                if (day == 1) return current;

                // Find the remainder months to round up
                int remainder = month % (int)amount;
                month += remainder + 1;
            }
            else
            {
                // Use the month selector arrays to find the next month
                var monthSelector = BiMonths;

                if (numMonths == 2) { monthSelector = BiMonths; }

                if (numMonths == 3) { monthSelector = QuarterMonths; }

                if (numMonths == 6) { monthSelector = HalfMonths; }

                // Round the month up
                month = monthSelector.First(x => x > current.Month);

                // Adjust back N months when input is 1st of the month
                if (day == 1) { month -= numMonths; }
            }

            // Wrap around when leaving the year boundary
            if (month > 12)
            {
                month -= 12;
                year++;
            }

            month = NumberUtil.Constrain(month, 1, 12);            

            // return the aligned date
            return new DateTime(year, month, 01);
        }

        private static DateTime AlignToWeeks(DateTime current, decimal amount)
        {
            // No alignment necessary
            if (current.DayOfWeek == DayOfWeek.Monday) { return current; }

            // DayOfWeek, Monday=1, Tuesday=2, Saturday=6, Sunday=0. 
            // Therefore Day - DayOfWeek turns all days into the Sunday before the Day
            // Finally add 8 to get next Monday, ie: the Monday after the day
            return current.AddDays(8 - (int) current.DayOfWeek).Date;
        }

        public static int WeekNumber(this DateTime dt) {
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(dt, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }

        private static DateTime AlignToDays(DateTime current, decimal amount)
        {
            var fraction = TimeSpan.FromDays((double)amount);
            return new DateTime((long)NumberUtil.RoundUp((double)current.Ticks, (double)fraction.Ticks));
        }

        private static DateTime AlignToSeconds(DateTime current, decimal amount)
        {
            var fraction = TimeSpan.FromSeconds((double)amount);
            return new DateTime((long)NumberUtil.RoundUp((double)current.Ticks, (double)fraction.Ticks));
        }
    }
}