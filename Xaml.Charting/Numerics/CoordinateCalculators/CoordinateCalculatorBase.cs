// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// CoordinateCalculatorBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Numerics.CoordinateCalculators
{
    [Flags]
    enum CoordinateCalculatorOptions
    {
        XAxisCalculator = 1,
        YAxisCalculator = 2,
        CategoryAxisCalculator = 4,
        LogarithmicAxisCalculator = 8,
        HorizontalAxisCalculator = 16,
        WithFlippedCoordinates = 32
    }

//    internal static class CoordinateCalculatorOptionHelper
//    {
//        internal static CoordinateCalculatorOptions GetOptions(bool isXAxis, bool isHorizontal, bool hasFlippedCoords)
//        {
//            var options = isXAxis
//                ? CoordinateCalculatorOptions.XAxisCalculator
//                : CoordinateCalculatorOptions.YAxisCalculator;
//
//            options = isHorizontal ? options | CoordinateCalculatorOptions.HorizontalAxisCalculator : options;
//            return hasFlippedCoords ? options | CoordinateCalculatorOptions.WithFlippedCoordinates : options;
//        }
//
//        private static CoordinateCalculatorOptions GetCategoryCalculatorOptions(bool isHorizontal)
//        {
//            return GetOptions(true, isHorizontal, false) | CoordinateCalculatorOptions.CategoryAxisCalculator;
//        }
//
//        private static CoordinateCalculatorOptions GetLogarithmicCalculatorOptions(bool isXAxis, bool isHorizontal, bool hasFlippedCoords)
//        {
//            return GetOptions(isXAxis, isHorizontal, hasFlippedCoords) | CoordinateCalculatorOptions.LogarithmicAxisCalculator;
//        }
//    }

    internal abstract class CoordinateCalculatorBase : ICoordinateCalculator<double>
    {
        public bool IsCategoryAxisCalculator { get; internal set; }
        public bool IsLogarithmicAxisCalculator { get; internal set; }
        public bool IsHorizontalAxisCalculator { get; internal set; }
        public bool IsXAxisCalculator { get; internal set; }
        public double CoordinatesOffset { get; internal set; }
        public bool HasFlippedCoordinates { get; internal set; }

        public bool IsPolarAxisCalculator { get; internal set; }

        public abstract double GetCoordinate(DateTime dataValue);

        public abstract double GetCoordinate(double dataValue);        

        public virtual DoubleRange TranslateBy(double pixels, DoubleRange inputRange)
        {
            double zeroPoint = GetDataValue(0);
            double toScrollPoint = GetDataValue(pixels);

            double deltaPoint = toScrollPoint - zeroPoint;

            if (IsXAxisCalculator)
                deltaPoint = -deltaPoint;

            double newMin = inputRange.Min.ToDouble() + deltaPoint;
            double newMax = inputRange.Max.ToDouble() + deltaPoint;

            return new DoubleRange(newMin, newMax);
        }

        public abstract double GetDataValue(double pixelCoordinate);

        public virtual DoubleRange TranslateBy(double minFraction, double maxFraction, IRange inputRange)
        {
            var newRange = ((IRange)inputRange.Clone()).GrowBy(minFraction, maxFraction, false, 0);

            return newRange.AsDoubleRange();
        }

        protected static double Flip(bool flipCoords, double coord, double viewportDimension)
        {
            return flipCoords ? viewportDimension - coord - 1 : coord;
        }
    }
}
