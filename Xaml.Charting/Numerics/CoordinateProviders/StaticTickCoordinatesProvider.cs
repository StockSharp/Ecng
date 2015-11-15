// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// StaticTickCoordinatesProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Numerics.TickCoordinateProviders
{
    /// <summary>
    /// Provides methods to convert arrays of major and minor ticks (data values) into pixel coordinates.
    /// Used when the <see cref="IAxis"/> is in static mode(<see cref="IAxis.IsStaticAxis"/>==<value>true</value>).
    /// </summary>
    public class StaticTickCoordinatesProvider: DefaultTickCoordinatesProvider
    {
        private TickCoordinates _tickCoords;

        private IRange _prevRange;
        private double _prevSize;

        /// <summary>
        /// Converts arrays of major and minor ticks (data values) into <see cref="TickCoordinates" /> structure containing pixel coordinates.
        /// </summary>
        /// <param name="minorTicks">The minor ticks, cast to double.</param>
        /// <param name="majorTicks">The major ticks, cast to double.</param>
        /// <returns>
        /// The <see cref="TickCoordinates" /> structure containing pixel coordinates.
        /// </returns>
        public override TickCoordinates GetTickCoordinates(double[] minorTicks, double[] majorTicks)
        {
            var visibleRangeChanged = !ParentAxis.VisibleRange.Equals(_prevRange);
            var axisSizeChanged = ParentAxis.ActualWidth.CompareTo(_prevSize) != 0;

            if (_tickCoords.IsEmpty || axisSizeChanged)
            {
                _tickCoords = base.GetTickCoordinates(minorTicks, majorTicks);
            }
            else if (visibleRangeChanged)
            {
                OverrideTickValues(_tickCoords);
            }
            else
            {
                OverrideTickCoordinates(_tickCoords);
            }

            _prevRange = ParentAxis.VisibleRange;
            _prevSize = ParentAxis.ActualWidth;

            return _tickCoords;
        }

        private void OverrideTickValues(TickCoordinates tickCoords)
        {
            var coordinateCalculator = ParentAxis.GetCurrentCoordinateCalculator();

            if (coordinateCalculator != null)
            {
                IComparable dataValue = null;
                for (int i = 0; i < tickCoords.MinorTickCoordinates.Length; i++)
                {
                    dataValue = ParentAxis.GetDataValue(tickCoords.MinorTickCoordinates[i]);
                    tickCoords.MinorTicks[i] = dataValue.ToDouble();
                }

                for (int i = 0; i < tickCoords.MajorTickCoordinates.Length; i++)
                {
                    dataValue = ParentAxis.GetDataValue(tickCoords.MajorTickCoordinates[i]);
                    tickCoords.MajorTicks[i] = dataValue.ToDouble();
                }
            }
        }

        private void OverrideTickCoordinates(TickCoordinates tickCoords)
        {
            var coordinateCalculator = ParentAxis.GetCurrentCoordinateCalculator();

            if (coordinateCalculator != null)
            {
                float coord;
                for (int i = 0; i < tickCoords.MinorTickCoordinates.Length; i++)
                {
                    coord = (float)ParentAxis.GetCoordinate(tickCoords.MinorTicks[i]);
                    tickCoords.MinorTickCoordinates[i] = coord;
                }

                for (int i = 0; i < tickCoords.MajorTickCoordinates.Length; i++)
                {
                    coord = (float)ParentAxis.GetCoordinate(tickCoords.MajorTicks[i]);
                    tickCoords.MajorTickCoordinates[i] = coord;
                }
            }
        }
    }
}
