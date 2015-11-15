// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DoubleCoordinateCalculator.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Numerics.CoordinateCalculators
{
    internal sealed class FlippedDoubleCoordinateCalculator : CoordinateCalculatorBase
    {
        private readonly double _min;
        private readonly double _max;
        private double _dimensionOverMaxMinusMin;
        private readonly double _oneOverViewportDimension;
        private readonly double _viewportDimensionMinusOne;

        //Stub ctor
        public FlippedDoubleCoordinateCalculator(double viewportDimension, double min, double max, XyDirection xyDirection, bool flipCoordinates)
            : this(viewportDimension, min, max,
                    xyDirection == XyDirection.XDirection,
                    xyDirection == XyDirection.XDirection,
                    flipCoordinates)
        { }

        public FlippedDoubleCoordinateCalculator(double viewportDimension, double min, double max, bool isXAxis, bool isHorizontal, bool flipCoordinates)
        {
            IsXAxisCalculator = isXAxis;
            IsHorizontalAxisCalculator = isHorizontal;
            HasFlippedCoordinates = flipCoordinates;

            _min = min;
            _max = max;

            // We use Dimension-1 to constrain the Y pixels to 0...Dimension-1 
            _viewportDimensionMinusOne = viewportDimension - 1;
            _dimensionOverMaxMinusMin = (_viewportDimensionMinusOne) / (_max - _min);

            // Cache constant for inverse calculation
            _oneOverViewportDimension = 1.0 / (_viewportDimensionMinusOne);
        }

        public sealed override double GetCoordinate(DateTime dataValue)
        {
            return GetCoordinate(dataValue.Ticks);
        }

        public sealed override double GetCoordinate(double dataValue)
        {
            // Flipped implementation of GetCoordinate
            return _viewportDimensionMinusOne - ((_max - dataValue) * _dimensionOverMaxMinusMin) + CoordinatesOffset;
        }

        public sealed override double GetDataValue(double coordinate)
        {
            // Flipped implementation of GetDataValue
            var dataValue = (_max - _min) * (coordinate - CoordinatesOffset) * _oneOverViewportDimension + _min;

            return dataValue;
        }

        internal double CoordinateConstant
        {
            get { return _dimensionOverMaxMinusMin; }
            set { _dimensionOverMaxMinusMin = value; }
        }
    }

    internal sealed class DoubleCoordinateCalculator : CoordinateCalculatorBase
    {
        private readonly double _viewportDimension;
        private readonly double _min;
        private readonly double _max;
        private double _dimensionOverMaxMinusMin;
        private readonly double _oneOverViewportDimension;

        //Stub ctor
        public DoubleCoordinateCalculator(double viewportDimension, double min, double max, XyDirection xyDirection, bool flipCoordinates)
            : this(viewportDimension, min, max, 
                    xyDirection == XyDirection.XDirection, 
                    xyDirection == XyDirection.XDirection, 
                    flipCoordinates)
        {}

        public DoubleCoordinateCalculator(double viewportDimension, double min, double max, bool isXAxis, bool isHorizontal, bool flipCoordinates)
        {
            IsXAxisCalculator = isXAxis;
            IsHorizontalAxisCalculator = isHorizontal;
            HasFlippedCoordinates = flipCoordinates;

            _viewportDimension = viewportDimension;
            _min = min;
            _max = max;
            
            // We use Dimension-1 to constrain the Y pixels to 0...Dimension-1 
            _dimensionOverMaxMinusMin = (_viewportDimension - 1) / (_max - _min);

            // Cache constant for inverse calculation
            _oneOverViewportDimension = 1.0 / (_viewportDimension - 1);            
        }

        public sealed override double GetCoordinate(DateTime dataValue)
        {
            return GetCoordinate(dataValue.Ticks);
        }

        public sealed override double GetCoordinate(double dataValue)
        {
            // Unflipped implementation of GetCoordinate
            return ((_max - dataValue)*_dimensionOverMaxMinusMin) + CoordinatesOffset;
        }

        public sealed override double GetDataValue(double coordinate)
        {
            // Unflipped implementation of GetDataValue
            coordinate = _viewportDimension - (coordinate - CoordinatesOffset) - 1;
            
            var dataValue = (_max - _min) * coordinate * _oneOverViewportDimension + _min;

            return dataValue;
        }

        internal double CoordinateConstant
        {
            get { return _dimensionOverMaxMinusMin; }
            set { _dimensionOverMaxMinusMin = value; }
        }
    }
}