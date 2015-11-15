// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IXyDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Defines the interface to an Xy DataSeries, which contains columns of X-Values and Y-Values
    /// </summary>
    public interface IXyDataSeries : IDataSeries
    {
    }

    /// <summary>
    /// Defines the interface to a typed Xy DataSeries, which contains columns of X-Values and Y-Values. 
    /// </summary>    
    public interface IXyDataSeries<TX, TY> : IDataSeries<TX ,TY>, IXyDataSeries 
        where TX:IComparable
        where TY:IComparable
    {
        /// <summary>
        /// Appends an X, Y point to the series
        /// </summary>
        /// <param name="x">The X Value</param>
        /// <param name="y">The Y Value</param>
        void Append(TX x, TY y);

        /// <summary>
        /// Appends a list of X, Y points to the series
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception will be thrown if the count of x and y differ</exception>
        /// <param name="x">The list of X points</param>
        /// <param name="y">The list of Y points</param>
        void Append(IEnumerable<TX> x, IEnumerable<TY> y);

        /// <summary>
        /// Updates an X,Y point specified by the X-Value passed in. 
        /// </summary>
        /// <param name="x">The X Value to key on when updating</param>
        /// <param name="y">The new Y value</param>
        /// <exception cref="InvalidOperationException">Thrown if the x value is not in the DataSeries</exception>
        void Update(TX x, TY y);

        /// <summary>
        /// Inserts an X,Y point at the specified index
        /// </summary>
        /// <param name="index">The index to insert at</param>
        /// <param name="x">The X value</param>
        /// <param name="y">The Y value</param>
        void Insert(int index, TX x, TY y);        

        /// <summary>
        /// Inserts a list of X, Y points at the specified index
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception will be thrown if the count of x and y differ</exception>
        /// <param name="startIndex">The index to insert at</param>
        /// <param name="x">The list of X points</param>
        /// <param name="y">The list of Y points</param>
        void InsertRange(int startIndex, IEnumerable<TX> x, IEnumerable<TY> y);
    }
}