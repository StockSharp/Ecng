// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TimeSpanTickProviderBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// A Common base class for TimeSpan and DateTimeTickProviders, which provide tick coordinates for the <see cref="DateTimeAxis"/> and <see cref="TimeSpanAxis"/>
    /// </summary>
    public abstract class TimeSpanTickProviderBase : TickProvider<IComparable>
    {
        /// <summary>
        /// Returns Generic-typed representation of major ticks array
        /// </summary>
        /// <param name="axis">The AxisParams for the axis</param>
        /// <returns>The array of ticks to display (data values converted to T)</returns>
        public override IComparable[] GetMajorTicks(IAxisParams axis)
        {
            return GetMajorTicks(axis.VisibleRange, new TimeSpanDelta(axis.MinorDelta.ToTimeSpan(), axis.MajorDelta.ToTimeSpan()));
        }

        /// <summary>
        /// Returns Generic-typed representation of minor ticks array
        /// </summary>
        /// <param name="axis">The AxisParams for the axis</param>
        /// <returns>The array of ticks to display (data values converted to T)</returns>
        public override IComparable[] GetMinorTicks(IAxisParams axis)
        {
            return GetMinorTicks(axis.VisibleRange, new TimeSpanDelta(axis.MinorDelta.ToTimeSpan(), axis.MajorDelta.ToTimeSpan()));
        }

        /// <summary>
        /// Converts ticks in generic format to Double, e.g. cast to double for numeric types, or cast DateTime.Ticks to double for DateTime types
        /// </summary>
        /// <param name="ticks"></param>
        /// <returns></returns>
        protected override double[] ConvertTicks(IComparable[] ticks)
        {
            return ticks.Select(GetTicks).ToArray();
        }

        /// <summary>
        /// Returns <see cref="DateTime.Ticks"/> or <see cref="TimeSpan.Ticks"/> depending on derived type
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected abstract double GetTicks(IComparable value);

        /// <summary>
        /// Given a date tick range with Min, Max, MajorDelta and MinorDelta, return an array of absolute values for major ticks
        /// </summary>
        private IComparable[] GetMajorTicks(IRange tickRange, IAxisDelta<TimeSpan> tickDelta)
        {
            var results = new List<IComparable>();

            if (AssertRangesValid(tickRange, tickDelta.MajorDelta))
            {
                var min = tickRange.Min;
                var max = tickRange.Max;

                var current = min;

                current = RoundUp(current, tickDelta.MajorDelta);

                while (current.CompareTo(max) <= 0 && IsAdditionValid(current, tickDelta.MajorDelta))
                {
                    if (!results.Contains(current))
                    {
                        results.Add(current);
                    }

                    current = AddDelta(current, tickDelta.MajorDelta);
                }
            }

            return results.ToArray();
        }

        private bool AssertRangesValid(IRange tickRange, TimeSpan tickDelta)
        {
            Guard.NotNull(tickRange, "tickRange");
            Guard.NotNull(tickDelta, "tickDelta");
            Guard.Assert(tickRange.Min, "tickRange.Min").IsLessThanOrEqualTo(tickRange.Max, "tickRange.Max");

            return !tickDelta.IsZero() && !tickRange.IsZero &&
                   tickRange.Min.IsDefined() && tickRange.Max.IsDefined();
        }

        /// <summary>
        /// When overriden in a derived class, Rounds up the <see cref="IComparable" /> to the nearest TimeSpan specified by <paramref name="delta" />
        /// </summary>
        /// <param name="current">The current value.</param>
        /// <param name="delta">The delta.</param>
        /// <returns>The rounded value</returns>
        protected abstract IComparable RoundUp(IComparable current, TimeSpan delta);

        /// <summary>
        /// Determines whether addition is valid between the current <see cref="IComparable"/> and the TimeSpan specified by <paramref name="delta" />
        /// </summary>
        /// <param name="current">The current.</param>
        /// <param name="delta">The delta.</param>
        /// <returns>If True, addition is valid</returns>
        protected abstract bool IsAdditionValid(IComparable current, TimeSpan delta);

        /// <summary>
        /// When overriden in a derived class, Adds the <see cref="IComparable" /> to the nearest TimeSpan specified by <paramref name="delta" />
        /// </summary>
        /// <param name="current">The current value.</param>
        /// <param name="delta">The delta.</param>
        /// <returns>The addition result</returns>
        protected abstract IComparable AddDelta(IComparable current, TimeSpan delta);

        /// <summary>
        /// When overriden in a derived class, Determines whether the <see cref="IComparable" /> is divisible by the TimeSpan specified by <paramref name="delta" />
        /// </summary>
        /// <param name="current">The current value.</param>
        /// <param name="delta">The delta.</param>
        /// <returns>If True, IsDivisibleBy</returns>
        protected abstract bool IsDivisibleBy(IComparable current, TimeSpan delta);

        /// <summary>
        /// Given a date tick range with Min, Max, MajorDelta and MinorDelta, return an array of absolute values for major ticks
        /// </summary>
        private IComparable[] GetMinorTicks(IRange tickRange, IAxisDelta<TimeSpan> tickDelta)
        {
            var results = new List<IComparable>();

            if (AssertRangesValid(tickRange, tickDelta.MinorDelta))
            {
                var min = tickRange.Min;
                var max = tickRange.Max;

                var current = min;

                if (!IsDivisibleBy(current, tickDelta.MinorDelta))
                {
                    current = RoundUp(current, tickDelta.MinorDelta);
                }

                while (current.CompareTo(max) < 0 && IsAdditionValid(current, tickDelta.MinorDelta))
                {
                    if (!IsDivisibleBy(current, tickDelta.MajorDelta) && current.CompareTo(max) != 0 &&
                        current.CompareTo(min) != 0)
                    {
                        results.Add(current);
                    }

                    current = AddDelta(current, tickDelta.MinorDelta);
                }
            }

            return results.ToArray();
        }
    }
}
