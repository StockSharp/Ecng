// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DateTimeExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class DateTimeExtensions
    {
        internal static bool IsDefined(this DateTime current)
        {
            return current != DateTime.MaxValue && current != DateTime.MinValue;
        }

        internal static bool IsAdditionValid(this DateTime current, TimeSpan delta)
        {
            const int DaysInYear = 365;

            var result = false;

            var year = current.Year + delta.TotalDays/DaysInYear;
            if(year < DateTime.MaxValue.Year)
            {
                result = true;
            }

            return result;
        }

        internal static DateTime AddDelta(this DateTime current, TimeSpan delta)
        {
            if (delta.IsDivisibleBy(TimeSpanExtensions.FromYears(1)))
            {
                return current.AddYears((int)(delta.Ticks / TimeSpanExtensions.FromYears(1).Ticks));
            }
            if (delta.IsDivisibleBy(TimeSpanExtensions.FromMonths(1)))
            {
                return current.AddMonths((int) (delta.Ticks/TimeSpanExtensions.FromMonths(1).Ticks));
            }

            return current.Add(delta);
        }

        internal static DateTime AddMonths(this DateTime current, int months)
        {
            int yearAdd = 0;
            int monthAdd = months;
            while(monthAdd > 12)
            {
                yearAdd++;
                monthAdd -= 12;
            }

            return new DateTime(current.Year + yearAdd, current.Month + monthAdd, current.Day, current.Hour, current.Minute, current.Second, current.Millisecond);
        }

        internal static DateTime AddYears(this DateTime current, int years)
        {
            return new DateTime(current.Year + years, current.Month, current.Day, current.Hour, current.Minute, current.Second, current.Millisecond);
        }
    }
}
