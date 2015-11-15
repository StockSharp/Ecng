// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// NumericTickProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Provides tick Coordinates for the <see cref="NumericAxis"/>
    /// </summary>
    public class NumericTickProvider : TickProvider<double>
    {
        private const double MinDeltaValue = 1E-13;

        /// <summary>
        /// Calls <see cref="NumericTickProvider.GetMinorTicks(IRange{double}, IAxisDelta{double})"/> to calcuate Minor Ticks, then returns a double representation of minor ticks array
        /// </summary>
        /// <param name="axis">The AxisParams for the axis</param>
        /// <returns>The array of ticks to display (data values converted to double)</returns>
        public override double[] GetMinorTicks(IAxisParams axis)
        {
            return GetMinorTicks(axis.VisibleRange.AsDoubleRange(), new DoubleAxisDelta(axis.MinorDelta.ToDouble(), axis.MajorDelta.ToDouble()));
        }

        /// <summary>
        /// Calls <see cref="NumericTickProvider.GetMajorTicks(IRange{double}, IAxisDelta{double})"/> to calcuate Minor Ticks, then returns a double representation of minor ticks array
        /// </summary>
        /// <param name="axis">The AxisParams for the axis</param>
        /// <returns>The array of ticks to display (data values converted to double)</returns>
        public override double[] GetMajorTicks(IAxisParams axis)
        {
            return GetMajorTicks(axis.VisibleRange.AsDoubleRange(), new DoubleAxisDelta(axis.MinorDelta.ToDouble(), axis.MajorDelta.ToDouble()));
        }

        /// <summary>
        /// Given a double tick range with Min, Max, MajorDelta and MinorDelta, return an array of absolute values for major ticks
        /// </summary>
        public double[] GetMajorTicks(IRange<double> tickRange, IAxisDelta<double> tickDelta)
        {
            return IsParamsValid(tickRange, tickDelta) ? CalculateMajorTicks(tickRange, tickDelta) : new double[0];
        }

        /// <summary>
        /// Determines whether the VisibleRange and Delta parameters are valid, e.g. are Real Numbers, and VisibleRange.Min &lt; Max
        /// </summary>
        /// <param name="tickRange">The VisibleRange</param>
        /// <param name="tickDelta">The Delta, e.g. MinorDelta, MajorDelta</param>
        /// <returns></returns>
        protected bool IsParamsValid(IRange<double> tickRange, IAxisDelta<double> tickDelta)
        {
            Guard.NotNull(tickRange, "tickRange");
            Guard.NotNull(tickDelta, "tickDelta");
            Guard.Assert(tickRange.Min, "tickRange.Min").IsLessThanOrEqualTo(tickRange.Max, "tickRange.Max");

            // Constraints to prevent infinite loop
            return tickDelta.MinorDelta.IsRealNumber()
                   && tickDelta.MinorDelta.CompareTo(MinDeltaValue) >= 0
                   && tickRange.Min.IsRealNumber()
                   && tickRange.Max.IsRealNumber();
        }

        /// <summary>
        /// Given a tickRange with Min, Max, MajorDelta and MinorDelta, return an array of absolute values for minor ticks
        /// </summary>
        public double[] GetMinorTicks(IRange<double> tickRange, IAxisDelta<double> tickDelta)
        {
            return IsParamsValid(tickRange, tickDelta) ? CalculateMinorTicks(tickRange, tickDelta) : new double[0];
        }

        /// <summary>
        /// Given a tickRange with Min, Max, MajorDelta and MinorDelta, return an array of absolute values for minor ticks
        /// </summary>
        public double[] GetMinorTicks(IRange<double> tickRange, IAxisDelta<double> tickDelta, double[] majorTicks)
        {
            return IsParamsValid(tickRange, tickDelta) ? CalculateMinorTicks(tickRange, tickDelta, majorTicks) : new double[0];
        }

        /// <summary>
        /// Calculates the Major Ticks for the axis given a VisibleRange and Delta
        /// </summary>
        /// <param name="tickRange">The VisibleRange</param>
        /// <param name="tickDelta">The Deltas, e.g. MinorDelta and MajorDelta</param>
        /// <returns>The Major ticks (data values) as double</returns>
        protected virtual double[] CalculateMajorTicks(IRange<double> tickRange, IAxisDelta<double> tickDelta)
        {
            return CalculateTicks(tickRange, tickDelta.MajorDelta, tickDelta.MajorDelta);
        }
        
        private double[] CalculateTicks(IRange<double> tickRange, double delta, double majorDelta)
        {
            var results = new List<double>();

            double min = tickRange.Min;
            double max = tickRange.Max;
            double current = min;

            bool calcMajorTicks = (delta.CompareTo(majorDelta) == 0);

            if (!NumberUtil.IsDivisibleBy(current, delta))
            {
                current = NumberUtil.RoundUp(current, delta);
            }

            double start = current;
            int tickCount = 0;
            while (current <= max)
            {
                // TRUE if major ticks are calculated && Current is divisible by MajorDelta
                // or if minor ticks are calculated && Current is NOT divisible by MajorDelta
                if (!(NumberUtil.IsDivisibleBy(current, majorDelta) ^ calcMajorTicks))
                {
                    results.Add(current);
                }
                current = start + ++tickCount * delta;
            }

            return results.ToArray();
        }

        /// <summary>
        /// Calculates the Minor Ticks for the axis given a VisibleRange and Delta
        /// </summary>
        /// <param name="tickRange">The VisibleRange</param>
        /// <param name="tickDelta">The Deltas, e.g. MinorDelta and MajorDelta</param>
        /// <param name="majorTicks">The previously calculated Major Ticks</param>
        /// <returns>The Minor Ticks (data values) as double</returns>
        protected virtual double[] CalculateMinorTicks(IRange<double> tickRange, IAxisDelta<double> tickDelta,
                                                    double[] majorTicks)
        {
            return CalculateMinorTicks(tickRange, tickDelta);
        }

        /// <summary>
        /// Calculates the Minor Ticks for the axis given a VisibleRange and Delta
        /// </summary>
        /// <param name="tickRange">The VisibleRange</param>
        /// <param name="tickDelta">The Deltas, e.g. MinorDelta and MajorDelta</param>
        /// <returns>The Major ticks (data values) as double</returns>
        protected virtual double[] CalculateMinorTicks(IRange<double> tickRange, IAxisDelta<double> tickDelta)
        {
            return CalculateTicks(tickRange, tickDelta.MinorDelta, tickDelta.MajorDelta);
        }
    }
}
