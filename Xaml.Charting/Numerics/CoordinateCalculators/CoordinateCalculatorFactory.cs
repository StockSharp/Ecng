// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// CoordinateCalculatorFactory.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Numerics.CoordinateCalculators
{
    /// <summary>
    /// Used internally by Ultrachart. Defines the interface to the coordinate calculator factor, which creates an appropriate coordinate calculator for the <see cref="AxisParams"/> passed in
    /// </summary>
    public interface ICoordinateCalculatorFactory
    {
        /// <summary>
        /// Creates a new <see cref="ICoordinateCalculator{T}"/>
        /// </summary>
        /// <param name="arg">The <see cref="AxisParams"/> instance containing axis data</param>
        /// <returns>The Coordinate calculator instance</returns>
        ICoordinateCalculator<double> New(AxisParams arg);
    }

    internal sealed class CoordinateCalculatorFactory : ICoordinateCalculatorFactory
    {
        /// <summary>
        /// Creates a new <see cref="ICoordinateCalculator{T}" />
        /// </summary>
        /// <param name="arg">The <see cref="AxisParams" /> instance containing axis data</param>
        /// <returns>
        /// The Coordinate calculator instance
        /// </returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public ICoordinateCalculator<double> New(AxisParams arg)
        {
            ICoordinateCalculator<double> coordinateCalculator;

            if (arg.IsCategoryAxis && arg.CategoryPointSeries != null)
            {
                coordinateCalculator = new CategoryCoordinateCalculator(arg.DataPointStep,
                                                                        arg.DataPointPixelSize,
                                                                        arg.Size,
                                                                        arg.CategoryPointSeries,
                                                                        new IndexRange((int) arg.VisibleMin.RoundOff(), (int) arg.VisibleMax.RoundOff()),
                                                                        arg.IsHorizontal,
                                                                        arg.BaseXValues,
                                                                        arg.IsBaseXValuesSorted);
            }
            else if (arg.IsLogarithmicAxis)
            {
                coordinateCalculator = new LogarithmicDoubleCoordinateCalculator(
                    arg.Size, 
                    arg.VisibleMin, 
                    arg.VisibleMax,
                    arg.LogarithmicBase,
                    arg.IsXAxis, 
                    arg.IsHorizontal, 
                    arg.FlipCoordinates);
            }
            else if (arg.IsPolarAxis)
            {
                if (arg.IsXAxis)
                {
                    coordinateCalculator = new PolarCoordinateCalculator(arg.VisibleMin, arg.VisibleMax, arg.IsXAxis, arg.IsHorizontal, arg.FlipCoordinates);
                }
                else
                {
                    var shouldFlip = !arg.FlipCoordinates;
                    coordinateCalculator = GetDoulbeCoordinateCalculator(arg, shouldFlip);
                }
                
            }
            else
            {
                bool shouldFlip = arg.IsXAxis ^ arg.FlipCoordinates;
                coordinateCalculator = GetDoulbeCoordinateCalculator(arg, shouldFlip);
            }

            if(coordinateCalculator == null)
            {
                throw new InvalidOperationException(string.Format("Unable to create a tick calculator instance."));
            }

            ((CoordinateCalculatorBase) coordinateCalculator).CoordinatesOffset = arg.Offset;
            ((CoordinateCalculatorBase) coordinateCalculator).IsPolarAxisCalculator = arg.IsPolarAxis;

            return coordinateCalculator;
        }

        private static ICoordinateCalculator<double> GetDoulbeCoordinateCalculator(AxisParams arg, bool shouldFlip)
        {
            if (shouldFlip)
            {
                return new FlippedDoubleCoordinateCalculator(arg.Size, arg.VisibleMin, arg.VisibleMax, arg.IsXAxis,
                    arg.IsHorizontal, arg.FlipCoordinates);
            }
            else
            {
                return new DoubleCoordinateCalculator(arg.Size, arg.VisibleMin, arg.VisibleMax, arg.IsXAxis,
                    arg.IsHorizontal, arg.FlipCoordinates);
            }
        }
    }
}