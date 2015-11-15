// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TimespanDelta.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Defines a TimeSpan Delta, which provides Major and Minor deltas as used in <see cref="DateTimeAxis"/>
    /// </summary>
    public class TimeSpanDelta : IAxisDelta<TimeSpan>, IEquatable<TimeSpanDelta>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpanDelta"/> class.
        /// </summary>
        /// <remarks></remarks>
        public TimeSpanDelta()
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpanDelta"/> class.
        /// </summary>
        /// <param name="minorDelta">The minor delta.</param>
        /// <param name="majorDelta">The major delta.</param>
        /// <remarks></remarks>
        public TimeSpanDelta(TimeSpan minorDelta, TimeSpan majorDelta)
        {
            MinorDelta = minorDelta;
            MajorDelta = majorDelta;            
        }

        IComparable IAxisDelta.MajorDelta { get { return MajorDelta; } }

        IComparable IAxisDelta.MinorDelta { get { return MinorDelta; } }      

        /// <summary>
        /// Gets or sets the major delta.
        /// </summary>
        /// <value>The major delta.</value>
        /// <remarks></remarks>
        public TimeSpan MajorDelta { get; set; }

        /// <summary>
        /// Gets or sets the minor delta.
        /// </summary>
        /// <value>The minor delta.</value>
        /// <remarks></remarks>
        public TimeSpan MinorDelta { get; set; }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public object Clone()
        {
            return new TimeSpanDelta(MinorDelta, MajorDelta);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        /// <remarks></remarks>
        public override bool Equals(object obj)
        {
            return Equals(obj as TimeSpanDelta);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
        /// <remarks></remarks>
        public bool Equals(TimeSpanDelta other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.MajorDelta.Equals(MajorDelta) && other.MinorDelta.Equals(MinorDelta);
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
                return (MajorDelta.GetHashCode() * 397) ^ MinorDelta.GetHashCode();
            }
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        /// <remarks></remarks>
        public static bool operator ==(TimeSpanDelta left, TimeSpanDelta right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        /// <remarks></remarks>
        public static bool operator !=(TimeSpanDelta left, TimeSpanDelta right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        /// <remarks></remarks>
        public override string ToString()
        {
            return string.Format("MinorDelta: {0}, MajorDelta, {1}", MinorDelta, MajorDelta);
        }
    }
}
