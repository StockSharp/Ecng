// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisParams.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections;
using Ecng.Xaml.Charting.Model.DataSeries;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Defines constants for the current axis setup for this render pass
    /// </summary>
    public struct AxisParams
    {
        private const double EPSILON = double.Epsilon;

        internal bool FlipCoordinates;
        internal double Size;
        internal double Offset;

        internal double VisibleMax;
        internal double VisibleMin;

        internal bool IsPolarAxis;
        internal bool IsCategoryAxis;

        internal bool IsLogarithmicAxis;
        internal double LogarithmicBase;

        internal bool IsXAxis;
        internal bool IsHorizontal;

        internal bool IsBaseXValuesSorted;
        internal IList BaseXValues;

        internal IPointSeries CategoryPointSeries;
        internal IndexRange PointRange;
        internal double DataPointPixelSize;
        internal double DataPointStep;


        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof (AxisParams)) return false;
            return Equals((AxisParams) obj);
        }

        /// <summary>
        /// Equalses the specified other.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns></returns>
        public bool Equals(AxisParams other)
        {
            return other.Size.Equals(Size) && other.VisibleMin.Equals(VisibleMin) &&
                   other.VisibleMax.Equals(VisibleMax) &&
                   other.IsCategoryAxis.Equals(IsCategoryAxis) &&
                   other.IsLogarithmicAxis.Equals(IsLogarithmicAxis) &&
                   Equals(other.CategoryPointSeries, CategoryPointSeries) &&
                   other.IsHorizontal.Equals(IsHorizontal) &&
                   other.FlipCoordinates.Equals(FlipCoordinates) &&
                   Equals(other.BaseXValues, BaseXValues) &&
                   Equals(other.PointRange, PointRange) &&
                   other.DataPointPixelSize.Equals(DataPointPixelSize) &&
                   other.DataPointStep.Equals(DataPointStep) &&
                   other.LogarithmicBase.Equals(LogarithmicBase) &&
                   other.IsBaseXValuesSorted.Equals(IsBaseXValuesSorted) &&
                   other.IsXAxis.Equals(IsXAxis);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = Size.GetHashCode();

                result = (result*397) ^ VisibleMin.GetHashCode();
                result = (result*397) ^ VisibleMax.GetHashCode();
                result = (result*397) ^ IsXAxis.GetHashCode();
                result = (result*397) ^ IsCategoryAxis.GetHashCode();
                result = (result*397) ^ (CategoryPointSeries != null ? CategoryPointSeries.GetHashCode() : 0);
                result = (result*397) ^ FlipCoordinates.GetHashCode();
                result = (result * 397) ^ IsHorizontal.GetHashCode();
                result = (result*397) ^ (BaseXValues != null ? BaseXValues.GetHashCode() : 0);
                result = (result*397) ^ (PointRange != null ? PointRange.GetHashCode() : 0);
                result = (result*397) ^ DataPointPixelSize.GetHashCode();
                result = (result*397) ^ DataPointStep.GetHashCode();
                result = (result*397) ^ IsBaseXValuesSorted.GetHashCode();
                result = (result * 397) ^ LogarithmicBase.GetHashCode();

                return result;
            }
        }
    }
}