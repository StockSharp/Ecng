// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// CategoryTickCoordinatesProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Numerics.TickCoordinateProviders;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Numerics.CoordinateProviders
{
    /// <summary>
    /// Provides methods to convert arrays of major and minor ticks (data values) into pixel coordinates for the <see cref="CategoryDateTimeAxis"/>
    /// </summary>
    public class CategoryTickCoordinatesProvider: DefaultTickCoordinatesProvider
    {
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
            var result = new TickCoordinates(new double[0], new double[0], new float[0], new float[0]);

            var coordCalc = ParentAxis.GetCurrentCoordinateCalculator() as ICategoryCoordinateCalculator;
            if (coordCalc != null)
            {
                var tickCoords = base.GetTickCoordinates(minorTicks, majorTicks);

                var minorTickValues =
                    tickCoords.MinorTickCoordinates.Select(x => GetDataPointIndexAt(x, coordCalc)).ToArray();

                var majorTickValues =
                    tickCoords.MajorTickCoordinates.Select(x => GetDataPointIndexAt(x, coordCalc)).ToArray();

                result = new TickCoordinates(
                    minorTickValues,
                    majorTickValues,
                    tickCoords.MinorTickCoordinates,
                    tickCoords.MajorTickCoordinates);
            }

            return result;
        }

        private double GetDataPointIndexAt(double coord, ICategoryCoordinateCalculator coordCalc)
        {
            var dataValue = coordCalc.GetDataValue(coord);
            var index = (int)Math.Round(dataValue.ToDouble());

            return coordCalc.TransformIndexToData(index).ToDouble();
        }
    }
}
