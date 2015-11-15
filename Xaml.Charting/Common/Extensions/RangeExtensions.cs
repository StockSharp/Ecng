// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RangeExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class RangeExtensions
    {
        /// <summary>
        /// Grows current <see cref="IRange"/> instance by <paramref name="minFraction"/>, <paramref name="maxFraction"/>
        /// </summary>
        /// <param name="range"></param>
        /// <param name="minFraction"></param>
        /// <param name="maxFraction"></param>
        /// <param>Indicates whether perform calculations in logarithmic space</param>
        /// <param name="isLogarithmic">Indicates whether perform calculations in logarithmic space</param>
        /// <param name="logBase">If <paramref name="isLogarithmic"/>, use this value as a base for logarithmic calculations</param>
        /// <returns></returns>
        public static IRange GrowBy(this IRange range, double minFraction, double maxFraction, bool isLogarithmic, double logBase)
        {
            if (isLogarithmic)
            {
                var doubleRange = range.AsDoubleRange();

                var min = doubleRange.Min <= 0 ? double.Epsilon : doubleRange.Min;
                var max = doubleRange.Max <= 0 ? double.Epsilon : doubleRange.Max;

                var logMin = Math.Log(min, logBase);
                var logMax = Math.Log(max, logBase);

                double diff = logMax - logMin;

                double minDelta = diff*minFraction;
                double maxDelta = diff*maxFraction;

                double newMin = Math.Pow(logBase, logMin - minDelta);
                double newMax = Math.Pow(logBase, logMax + maxDelta);

                if (newMin > newMax)
                {
                    NumberUtil.Swap(ref newMin, ref newMax);
                }

                var newRange = new DoubleRange(newMin, newMax);
                return RangeFactory.NewWithMinMax(range, newRange.Min, newRange.Max);
            }

            return range.GrowBy(minFraction, maxFraction);
        }

        public static IRange Union(this IEnumerable<IRange> ranges)
        {
            IRange result = null;
            foreach (var range in ranges)
            {
                if (result == null)
                    result = range;
                else
                    result = result.Union(range);
            }
            return result;
        }
    }
}
