// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// Range.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Numerics.GenericMath;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Abstract base implementation of <see cref="IRange"/>, used throughout Ultrachart for visible, data and index range calculations
    /// </summary>
    /// <typeparam name="T">The typeparameter of the range, e.g. <see cref="System.Double"/></typeparam>
    public abstract class Range<T> : BindableObject, IRange<T> where T:IComparable
    {
        private T _min;
        private T _max;

        private static IMath<T> _math = GenericMathFactory.New<T>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Range&lt;T&gt;"/> class.
        /// </summary>
        /// <remarks></remarks>
        protected Range()
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Range&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="min">The min value.</param>
        /// <param name="max">The max value.</param>
        /// <remarks></remarks>
        protected Range(T min, T max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Gets whether this Range is defined
        /// </summary>
        /// <example>Min and Max are not equal to double.NaN, or DateTime.MinValue or DateTime.MaxValue</example>
        /// <remarks></remarks>
        public virtual bool IsDefined { get { return Max.IsDefined() && Min.IsDefined(); } }

        /// <summary>
        /// Gets or sets the Min value of this range
        /// </summary>
        IComparable IRange.Min { get { return Min; } set { Min = (T)value; } }

        /// <summary>
        /// Gets or sets the Max value of this range
        /// </summary>
        IComparable IRange.Max { get { return Max; } set { Max = (T)value; } }

        /// <summary>
        /// Gets the difference (Max - Min) of this range
        /// </summary>
        IComparable IRange.Diff
        {
            get { return Diff; }
        }

        /// <summary>
        /// Gets whether the range is Zero, where Max equals Min
        /// </summary>
        /// <remarks></remarks>
        public abstract bool IsZero { get; }

        /// <summary>
        /// Gets or sets the Min value of this range
        /// </summary>
        public T Min
        {
            get { return _min; }
            set
            {
                var oldValue = _min;

                _min = value;

                OnPropertyChanged("Min", oldValue, value);
            }
        }

        /// <summary>
        /// Gets or sets the Max value of this range
        /// </summary>
        public T Max 
        {
            get { return _max; }
            set 
            {
                var oldValue = _max;

                _max = value;

                OnPropertyChanged("Max", oldValue, value);
            }
        }

        /// <summary>
        /// Gets the Diff (Max - Min) of this range
        /// </summary>
        public abstract T Diff { get; }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        /// <remarks></remarks>
        public abstract object Clone();

        /// <summary>
        /// Grows the current <see cref="IRange{T}"/> by the min and max fraction, returning this instance after modification
        /// </summary>
        /// <param name="minFraction">The Min fraction to grow by. For example, Min = -10 and minFraction = 0.1 will result in the new Min = -11</param>
        /// <param name="maxFraction">The Max fraction to grow by. For example, Max = 10 and minFraction = 0.2 will result in the new Max = 12</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public abstract IRange<T> GrowBy(double minFraction, double maxFraction);

        /// <summary>
        /// Clips the current <see cref="IRange{T}"/> to a maxmimum range 
        /// </summary>
        /// <param name="maximumRange">The Maximum Range</param>
        /// <returns>This instance, after the operation</returns>
        public abstract IRange<T> ClipTo(IRange<T> maximumRange);

        /// <summary>
        /// Converts this range to a <see cref="DoubleRange"/>, which are used internally for calculations
        /// </summary>
        /// <returns></returns>
        /// <example>For numeric ranges, the conversion is simple. For <see cref="DateRange"/> instances, returns a new <see cref="DoubleRange"/> with the Min and Max Ticks</example>
        /// <remarks></remarks>
        public abstract DoubleRange AsDoubleRange();

        /// <summary>
        /// Sets the Min, Max values on the <see cref="IRange{T}"/>, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public abstract IRange<T> SetMinMax(double min, double max);

        /// <summary>
        /// Sets the Min, Max values on the <see cref="IRange{T}"/> with a maximum range limit, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <param name="maxRange">The max range.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        public abstract IRange<T> SetMinMax(double min, double max, IRange<T> maxRange);

        /// <summary>
        /// Internal implementation: Sets the Min, Max values of the <see cref="Range{T}"/>
        /// </summary>
        /// <param name="min">The new Min value</param>
        /// <param name="max">The new Max value</param>
        protected void SetMinMaxInternal(T min, T max)
        {
            if (Max.CompareTo(min) < 0)
            {
                Max = max;
                Min = min;
            }
            else
            {
                Min = min;
                Max = max;
            }
        }

        /// <summary>
        /// Clips the current <see cref="IRange"/> to a maxmimum range with <see cref="RangeClipMode.MinMax"/> mode
        /// </summary>
        /// <param name="maximumRange">The Maximum Range</param>
        /// <returns>This instance, after the operation</returns>
        public IRange ClipTo(IRange maximumRange)
        {
            return ClipTo((IRange<T>)maximumRange);
        }

        /// <summary>
        /// Clips the current <see cref="IRange"/> to a maximum according to clip mode
        /// </summary>
        /// <param name="maximumRange">The maximum range</param>
        /// <param name="clipMode">clip mode which defines how to clip range</param>
        /// <returns>This instance, after the operation</returns>
        public IRange ClipTo(IRange maximumRange, RangeClipMode clipMode)
        {
            IRange newMaximumRange = null;

            if (clipMode == RangeClipMode.MinMax)
            {
                newMaximumRange = maximumRange;
            }
            else if (clipMode == RangeClipMode.Max)
            {
                var min = ComparableUtil.MinValue<T>();

                newMaximumRange = RangeFactory.NewWithMinMax(maximumRange, min, maximumRange.Max);
            }
            else if (clipMode == RangeClipMode.Min)
            {
                var max = ComparableUtil.MaxValue<T>();

                newMaximumRange = RangeFactory.NewWithMinMax(maximumRange, maximumRange.Min, max);
            }

            return ClipTo((IRange<T>) newMaximumRange);
        }

        /// <summary>
        /// Performs the Union of two <see cref="IRange"/> instances, returning a new <see cref="IRange"/>
        /// </summary>
        public IRange Union(IRange range)
        {
            return Union((IRange<T>)range);
        }

        /// <summary>
        /// Returns True if the value is within the Min and Max of the Range
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>
        /// True if the value is within the Min and Max of the Range
        /// </returns>
        public bool IsValueWithinRange(IComparable value)
        {
            return Min.CompareTo(value) <= 0 && Max.CompareTo(value) >= 0;
        }

        /// <summary>
        /// Performs a Union logical operation between two ranges. The returned <see cref="IRange{T}"/> has Min = Math.Min(range1.min, range2.min) 
        /// and Max = Math.Max(range1.Max, range2.Max)
        /// </summary>
        /// <param name="range">The input range to union with this range</param>
        /// <returns>The range result</returns>
        public IRange<T> Union(IRange<T> range)
        {
            return (IRange<T>) RangeFactory.NewRange(_math.Min(Min, range.Min), _math.Max(Max, range.Max));
        }

        /// <summary>
        /// Sets the Min, Max values on the <see cref="IRange"/>, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        IRange IRange.SetMinMax(double min, double max)
        {
            return SetMinMax(min, max);
        }

        /// <summary>
        /// Sets the Min, Max values on the <see cref="IRange"/> with a max range to clip values to, returning this instance after modification
        /// </summary>
        /// <param name="min">The new Min value.</param>
        /// <param name="max">The new Max value.</param>
        /// <param name="maxRange">The max range, which is used to clip values.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        IRange IRange.SetMinMaxWithLimit(double min, double max, IRange maxRange)
        {
            return SetMinMax(min, max, (IRange<T>)maxRange);
        }

        /// <summary>
        /// Grows the current <see cref="IRange"/> by the min and max fraction, returning this instance after modification
        /// </summary>
        /// <param name="minFraction">The Min fraction to grow by. For example, Min = -10 and minFraction = 0.1 will result in the new Min = -11</param>
        /// <param name="maxFraction">The max fraction.</param>
        /// <returns>This instance, after the operation</returns>
        /// <remarks></remarks>
        IRange IRange.GrowBy(double minFraction, double maxFraction)
        {
            return GrowBy(minFraction, maxFraction);
        }

        /// <summary>
        /// Returns the <see cref="System.String"/> that represents current <see cref="IRange"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return String.Format("{0} {{Min={1}, Max={2}}}", GetType(), Min, Max);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        /// <remarks></remarks>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Min.GetHashCode() * 397) ^ Max.GetHashCode();
            }
        }

        /// <summary>
        /// Compares Min and Max values to determine whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        /// <remarks></remarks>
        public override bool Equals(object obj)
        {
            var other = obj as IRange<T>;
            if (other == null)
            {
                return false;
            }
            return Equals((IRange<T>)obj);
        }

        /// <summary>
        /// Compares Min and Max values to determine whether the specified <see cref="IRange{T}"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="IRange{T}"/> to compare with the current <see cref="IRange{T}"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="IRange{T}"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        /// <remarks></remarks>
        public bool Equals(IRange<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Min.Equals(Min) && other.Max.Equals(Max);
        }
        
        internal void AssertMinLessOrEqualToThanMax()
        {
            Guard.Assert(Min, "Min").IsLessThanOrEqualTo(Max, "Max");
        }
    }
}