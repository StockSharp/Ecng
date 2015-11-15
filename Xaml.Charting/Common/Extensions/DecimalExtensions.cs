// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DecimalExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    internal static class DecimalExtensions
    {
        internal static decimal RoundOff(this decimal d)
        {
            return decimal.Round(d, 0);
        }

        internal static decimal RoundOff(this decimal d, MidpointRounding mode)
        {
            return RoundOff(d, 0, mode);
        }

        /// <summary>
        /// Rounds using arithmetic (5 rounds up) symmetrical (up is away from zero) rounding
        /// </summary>
        /// <param name="d">A Decimal number to be rounded.</param>
        /// <param name="decimals">The number of significant fractional digits (precision) in the return value.</param>
        /// <param name="mode">The midpoint rounding mode</param>
        /// <returns>The number nearest d with precision equal to decimals. If d is halfway between two numbers, then the nearest whole number away from zero is returned.</returns>
        internal static decimal RoundOff(this decimal d, int decimals, MidpointRounding mode)
        {
            if (mode == MidpointRounding.ToEven)
            {
                return decimal.Round(d, decimals);
            }

            decimal factor = Convert.ToDecimal(Math.Pow(10, decimals));
            int sign = Math.Sign(d);
            return Decimal.Truncate(d * factor + 0.5m * sign) / factor;
        }

        internal static DateTime ToDateTime(this decimal d)
        {
            return new DateTime((long)d.RoundOff(MidpointRounding.AwayFromZero));
        }
    }
}