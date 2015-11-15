// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisInteractivityHelper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Provides implementation for interactivity methods from <see cref="IAxisInteractivityHelper"/>
    /// </summary>
    internal class AxisInteractivityHelper : IAxisInteractivityHelper
    {
        private readonly ICoordinateCalculator<double> _coordCalculator;

        public AxisInteractivityHelper(ICoordinateCalculator<double> coordinateCalculator)
        {
            _coordCalculator = coordinateCalculator;
        }

        public IRange Zoom(IRange initialRange, double fromCoord, double toCoord)
        {
            double min = _coordCalculator.GetDataValue(fromCoord);
            double max = _coordCalculator.GetDataValue(toCoord);

            if (min >= max)
            {
                NumberUtil.Swap(ref min, ref max);
            }

            return RangeFactory.NewWithMinMax(initialRange, min, max);
        }

        public IRange ZoomBy(IRange initialRange, double minFraction, double maxFraction)
        {
            var translatedRange = _coordCalculator.TranslateBy(minFraction, maxFraction, initialRange);

            return RangeFactory.NewWithMinMax(initialRange, translatedRange.Min, translatedRange.Max);
        }

        public IRange ScrollInMinDirection(IRange rangeToScroll, double pixels)
        {
            var scrolledRange = Scroll(rangeToScroll, pixels);

            return ConstrainScrolledRange(rangeToScroll, scrolledRange.Min, rangeToScroll.Max);
        }

        private IRange ConstrainScrolledRange(IRange rangeToScroll, IComparable min, IComparable max)
        {
            var newRange = rangeToScroll;

            if (min.CompareTo(max) < 0)
            {
                newRange = RangeFactory.NewWithMinMax(rangeToScroll, min, max);
            }

            return newRange;
        }

        public IRange ScrollInMaxDirection(IRange rangeToScroll, double pixels)
        {
            var scrolledRange = Scroll(rangeToScroll, pixels);

            return ConstrainScrolledRange(rangeToScroll, rangeToScroll.Min, scrolledRange.Max);
        }

        public IRange Scroll(IRange rangeToScroll, double pixels)
        {
            var translatedRange = _coordCalculator.TranslateBy(pixels, rangeToScroll.AsDoubleRange());

            return RangeFactory.NewWithMinMax(rangeToScroll, translatedRange.Min, translatedRange.Max);
        }

        [Obsolete("The ScrollBy method is Obsolete as it is only really possible to implement on Category Axis. For this axis type just update the IndexRange (visibleRange) by N to scroll the axis", true)]
        public IRange ScrollBy(IRange rangeToScroll, int pointAmount)
        {
            throw new NotImplementedException();
        }

        public IRange ClipRange(IRange rangeToClip, IRange maximumRange, ClipMode clipMode)
        {
            /*
                 - ClipMode.None means you can pan right off the edge of the data into uncharted space. 
                 - ClipMode.StretchAtExtents causes a zooming (stretch) action when you reach the edge of the data. 
                 - ClipAtExtents forces the panning operation to stop suddenly at the extents of the data
                 - ClipAtMin forces the panning operation to stop suddenly at the minimum of the data, but expand at the maximum
             */

            IRange newRange = rangeToClip;

            if (clipMode != ClipMode.None)
            {
                var clippedRange = ((IRange) newRange.Clone()).ClipTo(maximumRange);

                var clipAtMin = (clippedRange.Min.CompareTo(newRange.Min) != 0);
                var clipAtMax = (clippedRange.Max.CompareTo(newRange.Max) != 0);

                var rangeToClipAsDouble = rangeToClip.AsDoubleRange();
                var clippedAsDouble = clippedRange.AsDoubleRange();

                double offset = clipAtMax
                                    ? rangeToClipAsDouble.Max - clippedAsDouble.Max
                                    : rangeToClipAsDouble.Min - clippedAsDouble.Min;

                var newMin = rangeToClipAsDouble.Min - offset;
                var newMax = newMin + rangeToClipAsDouble.Diff;

                if (_coordCalculator.IsLogarithmicAxisCalculator)
                {
                    var logCoordCalc = (ILogarithmicCoordinateCalculator) _coordCalculator;

                    var logMin = Math.Log(rangeToClipAsDouble.Min, logCoordCalc.LogarithmicBase);
                    var logMax = Math.Log(rangeToClipAsDouble.Max, logCoordCalc.LogarithmicBase);

                    double deltaPoint = clipAtMax
                                            ? logMax - Math.Log(clippedAsDouble.Max, logCoordCalc.LogarithmicBase)
                                            : logMin - Math.Log(clippedAsDouble.Min, logCoordCalc.LogarithmicBase);

                    newMin = Math.Pow(logCoordCalc.LogarithmicBase, logMin - deltaPoint);

                    var diff = logMax - logMin;
                    newMax = Math.Pow(logCoordCalc.LogarithmicBase, logMin - deltaPoint + diff);
                }

                switch (clipMode)
                {
                    case ClipMode.ClipAtExtents:
                        if (clipAtMin && clipAtMax)
                        {
                            newRange = clippedRange;
                        }
                        else if (clipAtMin || clipAtMax)
                        {
                            //find range shifted by offset and constraint it by maximumRange
                            newRange = RangeFactory.NewWithMinMax(maximumRange, newMin, newMax, maximumRange);
                        }
                        break;
                    case ClipMode.ClipAtMin:
                        newRange = clipAtMin
                                       ? RangeFactory.NewWithMinMax(maximumRange, newMin,
                                                                    newMax, maximumRange)
                                       : RangeFactory.NewWithMinMax(maximumRange, clippedRange.Min, newRange.Max);
                        break;
                    case ClipMode.StretchAtExtents:
                        newRange = clippedRange;
                        break;
                }
            }

            if (newRange.Min.CompareTo(newRange.Max) > 0)
            {
                newRange = RangeFactory.NewRange(newRange.Max, newRange.Min);
            }

            Guard.Assert(newRange.Min, "min").IsLessThanOrEqualTo(newRange.Max, "max");

            return newRange;
        }
    }
}
