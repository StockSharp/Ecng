// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ISeriesColumn.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Defines the interface to a generically typed series column in a <see cref="DataSeries{TX,TY}"/>
    /// </summary>
    /// <typeparam name="T">The underlying type of this <see cref="ISeriesColumn{T}"/></typeparam>
    /// <remarks></remarks>
    public interface ISeriesColumn<T> : IList<T>, IList, ISeriesColumn
    {
        /// <summary>
        /// Gets the minimum value of the <see cref="ISeriesColumn{T}"/>
        /// </summary>
        /// <returns></returns>
        T GetMinimum();

        /// <summary>
        /// Gets the maximum value of the <see cref="ISeriesColumn{T}"/>
        /// </summary>
        /// <returns></returns>
        T GetMaximum();

        /// <summary>
        /// Adds a range of items to the column
        /// </summary>
        /// <param name="values">The values.</param>
        /// <remarks></remarks>
        void AddRange(IEnumerable<T> values);

        /// <summary>
        /// Insert a range of items at the specified index
        /// </summary>
        /// <param name="startIndex">The index to insert at.</param>
        /// <param name="values">The values.</param>
        /// <remarks></remarks>
        void InsertRange(int startIndex, IEnumerable<T> values);
        
        /// <summary>
        /// Remove a range of items starting from the specified index
        /// </summary>
        /// <param name="startIndex">The index to start removing from</param>
        /// <param name="count">Numbers of point to remove</param>
        /// <remarks></remarks>
        void RemoveRange(int startIndex, int count);        
    }

    /// <summary>
    /// Defines the interface to a series column in a <see cref="IDataSeries"/> derived type
    /// </summary>
    /// <remarks></remarks>
    public interface ISeriesColumn
    {
        /// <summary>
        /// Gets a value indicating whether this column has any values.
        /// </summary>
        /// <remarks></remarks>
        bool HasValues { get; }

        /// <summary>
        /// Gets the count of values in this column
        /// </summary>
        int Count { get; }
    }
}