// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ICoordinateCalculator.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Numerics.CoordinateCalculators
{
    /// <summary>
    /// Using pre-computed constants, types which implement ICoordinateCalculator can convert from pixel coordinate to data value and back
    /// </summary>
    /// <typeparam name="T">The type of the underlying data to convert</typeparam>
    public interface ICoordinateCalculator<T> where T : IComparable
    {
        /// <summary>
        /// Gets a value indicating whether this is a category axis coordinate calculator
        /// </summary>
        bool IsCategoryAxisCalculator { get; }

        /// <summary>
        /// Gets a value indicating whether this is a logarithmic axis coordinate calculator
        /// </summary>
        bool IsLogarithmicAxisCalculator { get; }

        /// <summary>
        /// Gets a value indicating whether this is a horizontal axis coordinate calculator
        /// </summary>
        bool IsHorizontalAxisCalculator { get; }

        /// <summary>
        /// Gets a value indicating whether this is coordinate calculator belongs by X axis
        /// </summary>
        bool IsXAxisCalculator { get; }

        /// <summary>
        /// Gets a value indicating whether coordinates are flipped
        /// </summary>
        bool HasFlippedCoordinates { get; }

        bool IsPolarAxisCalculator { get; }

        double CoordinatesOffset { get; }

        /// <summary>
        /// Transforms the DateTime data value into a pixel coordinate
        /// </summary>
        /// <param name="dataValue">The DateTime data value</param>
        /// <returns>The pixel coordinate</returns>
        double GetCoordinate(DateTime dataValue);

        /// <summary>
        /// Transforms a data value into a pixel coordinate
        /// </summary>
        /// <param name="dataValue">The data value</param>
        /// <returns>The pixel coordinate</returns>
        double GetCoordinate(T dataValue);

        /// <summary>
        /// Transforms a pixel coordinate into a data value
        /// </summary>
        /// <param name="pixelCoordinate">The pixel coordinate</param>
        /// <returns>The data value</returns>
        T GetDataValue(double pixelCoordinate);

        /// <summary>
        /// Translates the min and max of the input range by the specified data value. Specific implementations of <see cref="ICoordinateCalculator{T}"/> such as
        /// <see cref="DoubleCoordinateCalculator"/>, <see cref="LogarithmicDoubleCoordinateCalculator"/> and <see cref="CategoryCoordinateCalculator"/> will treat this differently
        /// </summary>
        /// <param name="pixels">The number of pixels to translate by. InputRange min and max will be translated by this positive or negative amount</param>
        /// <param name="inputRange">The input <see cref="DoubleRange"/> to translate</param>
        /// <returns>A new instance of <see cref="CategoryCoordinateCalculator"/> with the translation applied</returns>
        DoubleRange TranslateBy(double pixels, DoubleRange inputRange);

        /// <summary>
        /// Translates the min and max of the input range, multiplies them by the specified <paramref name="minFraction"/>, <paramref name="maxFraction"/>. Specific implementations of <see cref="ICoordinateCalculator{T}"/> such as
        /// <see cref="DoubleCoordinateCalculator"/>, <see cref="LogarithmicDoubleCoordinateCalculator"/> and <see cref="CategoryCoordinateCalculator"/> will treat this differently
        /// </summary>
        /// <param name="inputRange">The input <see cref="IRange"/> to translate</param>
        /// <param name="minFraction">The multiplier of range start</param>
        /// <param name="maxFraction">The multiplier of range end</param>
        DoubleRange TranslateBy(double minFraction, double maxFraction, IRange inputRange);
    }
}