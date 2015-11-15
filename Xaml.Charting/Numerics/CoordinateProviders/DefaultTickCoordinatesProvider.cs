// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DefaultTickCoordinatesProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Text;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Numerics.CoordinateProviders;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Numerics.TickCoordinateProviders
{
    /// <summary>
    /// Provides methods to convert arrays of major and minor ticks (data values) into pixel coordinates.
    /// </summary>
    public class DefaultTickCoordinatesProvider : ProviderBase, ITickCoordinatesProvider
    {
        // Precision constant to deal with calculation precision problems in coord calculators
        // Prevents cliping of edge tick labels
        protected const double Precision = -0.00000001;

        protected const double MinTickDistance = double.Epsilon;

        protected readonly List<double> _minorTicks = new List<double>();
        protected readonly List<double> _majorTicks = new List<double>();
        protected readonly List<float> _minorTickCoords = new List<float>();
        protected readonly List<float> _majorTickCoords = new List<float>();

        private double _currOffset, _currAxisSize;

        /// <summary>
        /// Converts arrays of major and minor ticks (data values) into <see cref="TickCoordinates"/> structure containing pixel coordinates.
        /// </summary>
        /// <param name="minorTicks">The minor ticks, cast to double.</param>
        /// <param name="majorTicks">The major ticks, cast to double.</param>
        /// <returns>The <see cref="TickCoordinates"/> structure containing pixel coordinates.</returns>
        public virtual TickCoordinates GetTickCoordinates(double[] minorTicks, double[] majorTicks)
        {
            _minorTicks.Clear();
            _majorTicks.Clear();
            _minorTickCoords.Clear();
            _majorTickCoords.Clear();

            _currAxisSize = GetAxisSize();
            var coordinateCalculator = ParentAxis.GetCurrentCoordinateCalculator();

            if (coordinateCalculator != null && Math.Abs(_currAxisSize) >= double.Epsilon)
            {
                _currOffset = ParentAxis.GetAxisOffset();

                if (!minorTicks.IsNullOrEmpty())
                {
                    CalculateTickCoords(minorTicks, coordinateCalculator, false);
                }

                if (!majorTicks.IsNullOrEmpty())
                {
                    CalculateTickCoords(majorTicks, coordinateCalculator, true);
                }
            }

            return new TickCoordinates(_minorTicks.ToArray(), _majorTicks.ToArray(), _minorTickCoords.ToArray(), _majorTickCoords.ToArray());
        }

        private double GetAxisSize()
        {
            var axisSize = ParentAxis.IsHorizontalAxis ? ParentAxis.ActualWidth : ParentAxis.ActualHeight;

            if (Math.Abs(axisSize) < double.Epsilon && ParentAxis.ParentSurface != null)
            {
                var renderSurface = ParentAxis.ParentSurface.RenderSurface;
                if (renderSurface != null)
                {
                    axisSize = ParentAxis.IsHorizontalAxis ? renderSurface.ActualWidth : renderSurface.ActualHeight;
                }
            }

            return axisSize;
        }

        protected virtual void CalculateTickCoords(double[] ticks, ICoordinateCalculator<double> coordinateCalculator, bool isMajor)
        {
            var tickCollection = isMajor ? _majorTicks : _minorTicks;
            var tickCoordsCollection = isMajor ? _majorTickCoords : _minorTickCoords;

            double prevTick = ticks[0], currTick = prevTick;
            double prevTickCoord = coordinateCalculator.GetCoordinate(currTick), currTickCoord = prevTickCoord;

            if (IsInBounds(currTickCoord))
            {
                tickCoordsCollection.Add((float) currTickCoord);
                tickCollection.Add(currTick);
            }

            for (int i = 1; i < ticks.Length; i++)
            {
                currTick = ticks[i];
                if (Math.Abs(currTick - prevTick) > MinTickDistance)
                {
                    currTickCoord = coordinateCalculator.GetCoordinate(ticks[i]);

                    if (Math.Abs(currTickCoord - prevTickCoord) > MinTickDistance &&
                        IsInBounds(currTickCoord))
                    {
                        tickCollection.Add(currTick);
                        tickCoordsCollection.Add((float) currTickCoord);

                        prevTick = currTick;
                        prevTickCoord = currTickCoord;
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether GridLinesPanel contains passed coordinate
        /// </summary>
        /// <remarks></remarks>
        protected virtual bool IsInBounds(double coord)
        {
            coord -= _currOffset;

            // TODO: Fix the problem with calculations in CoordinateCalculators and remove the Precision const
            return coord >= Precision && coord < _currAxisSize- Precision;
        }
    }
}
