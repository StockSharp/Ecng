// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TimeSpanExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class TimeSpanExtensions
    {
        internal const double DaysInYear = 365.2425;
        internal const double DaysInMonth = 365.2425 / 12.0;

        internal static bool IsZero(this TimeSpan timeSpan)
        {
            return timeSpan == TimeSpan.Zero;
        }

        internal static TimeSpan FromMonths(int numberMonths)
        {
            return TimeSpan.FromDays(numberMonths * DaysInMonth);
        }

        internal static TimeSpan FromWeeks(int numberWeeks)
        {
            return TimeSpan.FromDays(numberWeeks * 7);
        }

        public static TimeSpan FromYears(int numberYears)
        {
            return TimeSpan.FromDays(numberYears*DaysInYear);
        }

        public static bool IsDivisibleBy(this TimeSpan current, TimeSpan other)
        {
            return NumberUtil.IsDivisibleBy((double)current.Ticks, (double)other.Ticks);
        }

        internal static bool IsAdditionValid(this TimeSpan current, TimeSpan delta)
        {
            var result = false;

            var year = current + delta;
            if (year < TimeSpan.MaxValue)
            {
                result = true;
            }

            return result;
        }
    }
}