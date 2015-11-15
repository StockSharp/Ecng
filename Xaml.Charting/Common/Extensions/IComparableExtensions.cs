// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IComparableExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Globalization;
using System.Linq;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class IComparableExtensions
    {
        internal static bool IsDefined(this IComparable c)
        {
            return c != null && ComparableUtil.IsDefined(c);
        }

        internal static double ToDouble(this double c)
        {
            return c;
        }        

        internal static double ToDouble(this IComparable c)
        {
            if (c is double)
                return (double)c;

            return ComparableUtil.ToDouble(c);
        }

        internal static double[] ToDoubleArray<T>(this T[] inputArray) where T : IComparable
        {
            return inputArray.Select(x => x.ToDouble()).ToArray();
        }

        internal static DateTime ToDateTime(this IComparable c)
        {
            if (c is DateTime)
            {
                return (DateTime) c;
            }

            if (c is TimeSpan)
            {
                return new DateTime(((TimeSpan)c).Ticks);
            }

            if (c.IsDefined())
            {
                long localTicks = NumberUtil.Constrain((long) Convert.ChangeType(c, typeof (long), CultureInfo.InvariantCulture), DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks);
                return new DateTime(localTicks);
            }
            
            return new DateTime();
        }

        internal static TimeSpan ToTimeSpan(this IComparable c)
        {
            if (c is TimeSpan)
            {
                return (TimeSpan) c;
            }

            if (c is DateTime)
            {
                return new TimeSpan(((DateTime)c).Ticks);
            }

            if (c.IsDefined())
            {
                long localTicks = NumberUtil.Constrain((long) Convert.ChangeType(c, typeof (long), CultureInfo.InvariantCulture), TimeSpan.MinValue.Ticks, TimeSpan.MaxValue.Ticks);
                return new TimeSpan(localTicks);
            }

            return new TimeSpan();
        }
    }
}
