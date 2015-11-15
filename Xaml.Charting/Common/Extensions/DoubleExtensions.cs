// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DoubleExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
	internal static class DoubleExtensions
	{
		internal static bool IsRealNumber(this double number)
		{
			return (!(double.IsNaN(number) || double.IsInfinity(number) || double.MaxValue.Equals(number) || double.MinValue.Equals(number)));
		}

		internal static bool IsNaN(this double value)
		{
			// Fast NaN check. 
			// NOTE: Value != Value check is intentional
			// http://stackoverflow.com/questions/3286492/can-i-improve-the-double-isnan-x-function-call-on-embedded-c
			// 

			// ReSharper disable EqualExpressionComparison
			// ReSharper disable CompareOfFloatsByEqualityOperator
#pragma warning disable 1718
			return value != value;
#pragma warning restore 1718
			// ReSharper restore CompareOfFloatsByEqualityOperator
			// ReSharper restore EqualExpressionComparison
		}

		internal static double RoundOff(this double d)
		{
			return Math.Round(d);
		}

		internal static double Ceiling(this double d)
		{
			return Math.Ceiling(d);
		}

		internal static double Floor(this double d)
		{
			return Math.Floor(d);
		}

		internal static double RoundOff(this double d, MidpointRounding mode)
		{
			return d.RoundOff(0, mode);
		}

		/// <summary>
		/// Rounds using arithmetic (5 rounds up) symmetrical (up is away from zero) rounding
		/// </summary>
		/// <param name="d">A double number to be rounded.</param>
		/// <param name="decimals">The number of significant fractional digits (precision) in the return value.</param>
		/// <param name="mode">The midpoint rounding mode</param>
		/// <returns>The number nearest d with precision equal to decimals. If d is halfway between two numbers, then the nearest whole number away from zero is returned.</returns>
		internal static double RoundOff(this double d, int decimals, MidpointRounding mode)
		{
			if (mode == MidpointRounding.ToEven)
			{
				return Math.Round(d, decimals);
			}

			decimal factor = Convert.ToDecimal(Math.Pow(10, decimals));
			int sign = Math.Sign(d);
			return (double) (Decimal.Truncate((decimal) d*factor + 0.5m*sign)/factor);
		}

		internal static DateTime ToDateTime(this double ticks)
		{
			long localTicks = NumberUtil.Constrain((long) ticks, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks);                        
			return new DateTime(localTicks);
		}

		internal static double ClipToInt(this double d)
		{
			if (d > int.MaxValue)
				return int.MaxValue;
		  
			if (d < int.MinValue)
				return int.MinValue;

			return d;
		}

        internal static int ClipToIntValue(this double d)
        {
            if (d > int.MaxValue)
                return int.MaxValue;

            if (d < int.MinValue)
                return int.MinValue;

            return (int)d;
        }
	}
}