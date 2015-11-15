// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// LogarithmicDoubleCoordinateCalculator.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Numerics.CoordinateCalculators
{
    internal sealed class LogarithmicDoubleCoordinateCalculator : CoordinateCalculatorBase, ILogarithmicCoordinateCalculator
    {
        private readonly double _dimensionOverMaxMinusMin;
        private readonly double _logMax;
        private readonly double _logMin;
        private readonly double _mult;
        private readonly double _oneOverViewportDimension;
        private readonly double _viewportDimension;
        private readonly double _logBase;

        //Stub ctor
        public LogarithmicDoubleCoordinateCalculator(double viewportDimension, double min, double max, XyDirection xyDirection, bool flipCoordinates)
            : this(viewportDimension, min, max, 10d, xyDirection == XyDirection.XDirection, true, flipCoordinates)
        {
        }

        public LogarithmicDoubleCoordinateCalculator(double viewportDimension, double min, double max, double logBase, bool isXAxis, bool isHorizontal, bool flipCoordinates)
        {
            IsXAxisCalculator = isXAxis;
            IsHorizontalAxisCalculator = isHorizontal;
            HasFlippedCoordinates = flipCoordinates;

            IsLogarithmicAxisCalculator = true;
            _logBase = logBase;

            _viewportDimension = viewportDimension;

            _mult = min < 0 ? -1.0 : 1.0;

            _logMax = Math.Log(_mult * max, _logBase);
            _logMin = Math.Log(_mult * min, _logBase);

            // We use Dimension-1 to constrain the Y pixels to 0...Dimension-1 
            _dimensionOverMaxMinusMin = ((viewportDimension) - 1)/(_logMax - _logMin);

            // Cache constant for inverse calculation
            _oneOverViewportDimension = 1.0/(_viewportDimension - 1);
        }

        public double LogarithmicBase { get { return _logBase; }}

        public sealed override double GetCoordinate(DateTime dataValue)
        {
            return GetCoordinate(dataValue.Ticks);
        }        

        public sealed override double GetCoordinate(double dataValue)
        {
            double coord = ((_logMax - Math.Log(_mult * dataValue, _logBase)) * _dimensionOverMaxMinusMin);

            return Flip(IsXAxisCalculator ^ HasFlippedCoordinates, coord, _viewportDimension) + CoordinatesOffset;
        }

        public sealed override double GetDataValue(double coordinate)
        {
            coordinate = Flip(!(IsXAxisCalculator ^ HasFlippedCoordinates), coordinate - CoordinatesOffset, _viewportDimension);
            double exponent = (_logMax - _logMin) * coordinate * _oneOverViewportDimension + _logMin;

            return Math.Pow(_logBase, exponent) * _mult;
        }

        public override DoubleRange TranslateBy(double minFraction, double maxFraction, IRange inputRange)
        {
            var newRange = ((IRange)inputRange.Clone()).GrowBy(minFraction, maxFraction, true, _logBase);

            return newRange.AsDoubleRange();
        }

        public override DoubleRange TranslateBy(double pixels, DoubleRange inputRange)
        {
            double zeroPoint = GetDataValue(0);
            double toScrollPoint = GetDataValue(pixels);

            double deltaPoint = Math.Log(toScrollPoint, _logBase) - Math.Log(zeroPoint, _logBase);

            if (IsXAxisCalculator)
                deltaPoint = -deltaPoint;

            double newMin = Math.Pow(_logBase, _logMin + deltaPoint);
            double newMax = Math.Pow(_logBase, _logMax + deltaPoint);

            return new DoubleRange(newMin, newMax);
        }
    }
}