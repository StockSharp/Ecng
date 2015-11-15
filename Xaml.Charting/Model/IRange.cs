// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IRange.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Defines the base interface to a Range (Min, Max), used throughout Ultrachart for visible, data and index range calculations
    /// </summary>
    public interface IRange : ICloneable, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the Min value of this range
        /// </summary>
        IComparable Min { get; set; }

        /// <summary>
        /// Gets or sets the Max value of this range
        /// </summary>
        IComparable Max { get; set; }

        /// <summary>
        /// Gets whether this Range is defined
        /// </summary>
        /// <example>Min and Max are not equal to double.NaN, or DateTime.MinValue or DateTime.MaxValue</example>
        bool IsDefined { get; }

        /// <summary>
        /// Gets the difference (Max - Min) of this range
        /// </summary>
        IComparable Diff { get; }

        /// <summary>
        /// Gets whether the range is Zero, where Max equals Min
        /// </summary>
        bool IsZero { get; }

        /// <summary>
        /// Converts this range to a <see cref="DoubleRange"/>, which are used internally for calculations
        /// </summary>
        /// <example>For numeric ranges, the conversion is simple. For <see cref="DateRange"/> instances, returns a new <see cref="DoubleRange"/> with the Min and Max Ticks</example>
        /// <returns></returns>
        DoubleRange AsDoubleRange();

        /// <summary>
        /// Grows the current <see cref="IRange"/> by the min and max fraction, returning this instance after modification
        /// </summary>
        /// <param name="minFraction">The Min fraction to grow by. For example, Min = -10 and minFraction = 0.1 will result in the new Min = -11</param>
        /// <param name="maxFraction">The Max fraction to grow by. For example, Max = 10 and minFraction = 0.2 will result in the new Max = 12</param>
        /// <returns>This instance, after the operation</returns>
        IRange GrowBy(double minFraction, double maxFraction);

        /// <summary>
        /// Sets the Min, Max values on the <see cref="IRange"/>, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        IRange SetMinMax(double min, double max);

        /// <summary>
        /// Sets the Min, Max values on the <see cref="IRange"/> with a max range to clip values to, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <param name="maxRange">The max range, which is used to clip values.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        IRange SetMinMaxWithLimit(double min, double max, IRange maxRange);

        /// <summary>
        /// Clips the current <see cref="IRange"/> to a maxmimum range with <see cref="RangeClipMode.MinMax"/> mode
        /// </summary>
        /// <param name="maximumRange">The Maximum Range</param>
        /// <returns>This instance, after the operation</returns>
        IRange ClipTo(IRange maximumRange);

        /// <summary>
        /// Clips the current <see cref="IRange"/> to a maximum according to clip mode
        /// </summary>
        /// <param name="maximumRange">The maximum range</param>
        /// <param name="clipMode">clip mode which defines how to clip range</param>
        /// <returns>This instance, after the operation</returns>
        IRange ClipTo(IRange maximumRange, RangeClipMode clipMode);

        /// <summary>
        /// Performs the Union of two <see cref="IRange"/> instances, returning a new <see cref="IRange"/>
        /// </summary>
        IRange Union(IRange range);

        /// <summary>
        /// Returns True if the value is within the Min and Max of the Range
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if the value is within the Min and Max of the Range</returns>
        bool IsValueWithinRange(IComparable value);
    }

    /// <summary>
    /// Defines the generic interface to a Range (Min, Max), used throughout Ultrachart for visible, data and index range calculations
    /// </summary>
    /// <typeparam name="T">The Type Parameter, expected types are Double, DateTime etc... </typeparam>
    /// <remarks></remarks>    
    public interface IRange<T> : IRange where T:IComparable
    {
        /// <summary>
        /// Gets or sets the Min value of this range
        /// </summary>
        new T Min { get; set; }

        /// <summary>
        /// Gets or sets the Max value of this range
        /// </summary>
        new T Max { get; set; }

        /// <summary>
        /// Gets the Diff (Max - Min) of this range
        /// </summary>
        new T Diff { get; }

        /// <summary>
        /// Grows the current <see cref="IRange{T}"/> by the min and max fraction, returning this instance after modification
        /// </summary>
        /// <param name="minFraction">The Min fraction to grow by. For example, Min = -10 and minFraction = 0.1 will result in the new Min = -11</param>
        /// <param name="maxFraction">The Max fraction to grow by. For example, Max = 10 and maxFraction = 0.2 will result in the new Max = 12</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        new IRange<T> GrowBy(double minFraction, double maxFraction);

        /// <summary>
        /// Sets the Min, Max values on the <see cref="IRange{T}"/>, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        new IRange<T> SetMinMax(double min, double max);

        /// <summary>
        /// Performs the Union of two <see cref="IRange"/> instances, returning a new <see cref="IRange"/>
        /// </summary>
        /// <example>
        /// <code>
        /// var firstRange = new DoubleRange(1, 2);
        /// var secondRange = new DoubleRange(1.5, 2.5)
        /// var unionRange = firstRange.Union(secondRange); 
        /// // unionRange result should be new DoubleRange(1, 2.5)
        /// </code>
        /// </example>
        /// <param name="range"></param>
        /// <returns></returns>
        IRange<T> Union(IRange<T> range);
    }

    /// <summary>
    /// Provide values which define how to perform clipping of <see cref="IRange"/>
    /// </summary>
    public enum RangeClipMode
    {
        /// <summary>
        /// Allow clipping at Min and Max
        /// </summary>
        MinMax,
        /// <summary>
        /// Allow clipping only at Max
        /// </summary>
        Max,
        /// <summary>
        /// Allow clipping only at Min
        /// </summary>
        Min
    }
}