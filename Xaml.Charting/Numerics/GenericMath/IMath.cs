// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IMath.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Numerics.GenericMath
{
    /// <summary>
    /// Defines the interface to a generic math helper
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMath<T>
    {
		/// <summary>
		/// Gets the MinValue for T. for DateTime it returns DateTime.MinValue (it has .Ticks = 0)
		/// </summary>
        T MinValue { get; }

        /// <summary>
        /// Gets the MaxValue for T.
        /// </summary>
        T MaxValue { get; }

		/// <summary>
		/// Gets the ZeroValue for T. for DateTime it returns DateTime.MinValue (it has .Ticks = 0)
		/// </summary>
        T ZeroValue { get; }

        /// <summary>
        /// Returns the Max of A and B
        /// </summary>        
        T Max(T a, T b);

        /// <summary>
        /// Returns the Min of A and B
        /// </summary>        
        T Min(T a, T b);

        /// <summary>
        /// Returns the Min of A and B greater than a Floor
        /// </summary>        
        T MinGreaterThan(T floor, T a, T b);

        /// <summary>
        /// Returns if T is NaN. Only valid for Float, Double types. For all other types, always returns false
        /// </summary>        
        bool IsNaN(T value);

		/// <summary>
		/// Subtracts a - b. For DateTime it returns a new DateTime with .Ticks = a.Ticks - b.Ticks
		/// </summary>
        T Subtract(T a, T b);

        /// <summary>
        /// Get the Absolute value of (a)
        /// </summary>
        T Abs(T a);

        /// <summary>
        /// Converts to the equivalent value as a double
        /// </summary>        
        double ToDouble(T value);
            
        /// <summary>
        /// Multiplies lhs * rhs
        /// </summary>        
        T Mult(T lhs, T rhs);

        /// <summary>
        /// Multiplies lhs * rhs
        /// </summary>        
        T Mult(T lhs, double rhs);

		/// <summary>
		/// Adds lhs + rhs. for DateTime it returns a new DateTime with .Ticks = lhs.Ticks + rhs.Ticks
		/// </summary>
        T Add(T lhs, T rhs);

        /// <summary>
        ///     Returns T++
        /// for DateTime it increments .Ticks
        /// </summary>
        T Inc(ref T value);

        /// <summary>
        ///     Returns T--
		/// for DateTime it decrements .Ticks
        /// </summary>
        T Dec(ref T value);
    }
}