// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TradeChartTickCalculator.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Numerics.TickCoordinateProviders;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Numerics
{
    internal class TradeChartParams : IAxisParams
    {
        public IRange VisibleRange { get; set; }
        public IRange<double> GrowBy { get; set; }
        public IComparable MinorDelta { get; set; }
        public IComparable MajorDelta { get; set; }

        public IRange GetMaximumRange()
        {
            throw new NotImplementedException();
        }
    }

    internal class TradeChartTickCalculator
    {
        private static readonly IList<TimeSpan> _dateDeltas = new[]
                                                           {
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

        internal static TickCoordinates GetTickCoordinates(IndexRange indexRange, DateRange dateRange, ICategoryCoordinateCalculator coordCalc, int minorsPerMajor, int maxAutoTicks)
        {            
//            if (coordCalc == null || indexRange.Min >= indexRange.Max)
//                return default(AxisBase.TickCoordinates);

            TimeSpan range = dateRange.Max - dateRange.Min;

            // Get the first Major Delta that fits 5 deltas into the available range
            var desiredSpan = new TimeSpan(range.Ticks);

            var majorDelta = _dateDeltas.FirstOrDefault(x => new TimeSpan(x.Ticks * maxAutoTicks) > desiredSpan);

            TimeSpanDelta delta;
            if (majorDelta.Equals(TimeSpan.Zero))
            {
                var scale = new NiceDoubleScale(0, range.Ticks/(double) TimeSpanExtensions.FromYears(1).Ticks,
                    minorsPerMajor, (uint) maxAutoTicks);
                scale.CalculateDelta();

                var yearSpacing = scale.TickSpacing;

                delta = new TimeSpanDelta(TimeSpanExtensions.FromYears((int)yearSpacing.MinorDelta), TimeSpanExtensions.FromYears((int)yearSpacing.MajorDelta));
            }

            // Case where both DateDeltas are in the list of predefined date deltas
            int i = _dateDeltas.IndexOf(majorDelta) - 2;
            if (i >= 0)
            {
                var minorDelta = _dateDeltas[i];
                delta = new TimeSpanDelta(minorDelta, majorDelta);
            }
            else
            {
                // Case where the minor delta (or both) are less than the predefined list
                var scale = new NiceDoubleScale(0, range.Ticks, minorsPerMajor, (uint)maxAutoTicks);
                scale.CalculateDelta();

                var tickSpacing = scale.TickSpacing;

                delta = new TimeSpanDelta(TimeSpan.FromTicks((long)tickSpacing.MinorDelta), TimeSpan.FromTicks((long)tickSpacing.MajorDelta));   
            }

            var dateTimeTickProvider = new DateTimeTickProvider();
            var @params = new TradeChartParams
                          {
                              VisibleRange = dateRange,
                              MajorDelta = delta.MajorDelta,
                              MinorDelta = delta.MinorDelta
                          };

            var majorTicks = dateTimeTickProvider.GetMajorTicks(@params).ToDoubleArray();
            var majorTickIndices = majorTicks.Select(x => coordCalc.TransformDataToIndex(x.ToDateTime())).ToArray();
            var majorTickCoords = majorTicks.Select(x => (float)coordCalc.GetCoordinate(coordCalc.TransformDataToIndex(x.ToDateTime()))).ToArray();

            var minorTickIndices = GetMinorTickIndices(indexRange, majorTickIndices, minorsPerMajor);
            var minorTicks = minorTickIndices.Select(x => coordCalc.TransformIndexToData(x).ToDouble()).ToArray();
            var minorTickCoords = minorTickIndices.Select(x => (float)coordCalc.GetCoordinate(x)).ToArray();

            var tickCoordinates = new TickCoordinates(
                minorTicks,
                majorTicks,
                minorTickCoords,
                majorTickCoords);

            return tickCoordinates;
        }

        private static int[] GetMinorTickIndices(IndexRange indexRange, int[] majorTickIndices, int minorsPerMajor)
        {
            List<int> minorTickIndices = new List<int>();
            int aveDelta = 0;
            int count = 0;
            for(int i = 1; i < majorTickIndices.Length; i++, count++)
            {
                aveDelta += majorTickIndices[i] - majorTickIndices[i - 1];
            }
            aveDelta = (int) Math.Ceiling((double)aveDelta / (double)(count * minorsPerMajor));
            aveDelta = Math.Max(aveDelta, 1);
            int offset = aveDelta - indexRange.Min % aveDelta;

            for (int i = indexRange.Min + offset; i <= indexRange.Max; i += aveDelta)
            {
                minorTickIndices.Add(i);
            }
            return minorTickIndices.ToArray();
        }
    }
}