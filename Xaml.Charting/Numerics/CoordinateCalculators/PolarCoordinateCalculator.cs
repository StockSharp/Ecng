using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ecng.Xaml.Charting.Numerics.CoordinateCalculators
{
    internal class PolarCoordinateCalculator : CoordinateCalculatorBase
    {
        private const double MaxDegree = 360;

        private readonly double _min;
        private readonly double _max;
        private readonly double _dataPerDegree;

        public PolarCoordinateCalculator(double rangeMin, double rangeMax, bool isXAxis, bool isHorizontal, bool flipCoordinates)
        {
            _min = rangeMin;
            _max = rangeMax;

            _dataPerDegree = (_max - _min) / MaxDegree;

            IsXAxisCalculator = isXAxis;
            IsHorizontalAxisCalculator = isHorizontal;
            HasFlippedCoordinates = flipCoordinates;
        }

        public override double GetCoordinate(DateTime dataValue)
        {
            return GetCoordinate(dataValue.Ticks);
        }

        public override double GetCoordinate(double dataValue)
        {
            dataValue = (dataValue - _min) / _dataPerDegree;

            return Flip(HasFlippedCoordinates, dataValue, MaxDegree + 1);
        }

        public override double GetDataValue(double pixelCoordinate)
        {
            pixelCoordinate = Flip(HasFlippedCoordinates, pixelCoordinate, MaxDegree + 1);

            return _min + pixelCoordinate * _dataPerDegree;
        }
    }
}
