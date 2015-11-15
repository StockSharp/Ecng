// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IXyyDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Defines the interface to an Xyy DataSeries, a series containing X, Y0 and Y1 data-points
    /// </summary>
    public interface IXyyDataSeries : IDataSeries
    {
        /// <summary>
        /// Gets the Y1 Values as a list of <see cref="IComparable"/>
        /// </summary>
        IList Y1Values { get; }
    }

    /// <summary>
    /// Defines the templated interface to an Xyy DataSeries, a series containing X, Y0 and Y1 data-points
    /// </summary>
    public interface IXyyDataSeries<TX, TY> : IDataSeries<TX, TY>, IXyyDataSeries
        where TX : IComparable
        where TY : IComparable
    {
        /// <summary>
        /// Gets the Y1 values
        /// </summary>
        new IList<TY> Y1Values { get; }

        /// <summary>
        /// Appends a single X, Y0, Y1 point to the series, automatically triggering a redraw
        /// </summary>
        /// <param name="x">The X-value</param>
        /// <param name="y0">The Y0-value</param>
        /// <param name="y1">The Y1-value</param>
        void Append(TX x, TY y0, TY y1);

        /// <summary>
        /// Appends a collection of X, Y0 and Y1 points to the series, automatically triggering a redraw
        /// </summary>
        /// <param name="x">The X-values</param>
        /// <param name="y0">The Y0-values</param>
        /// <param name="y1">The Y1-values</param>
        void Append(IEnumerable<TX> x, IEnumerable<TY> y0, IEnumerable<TY> y1);

        /// <summary>
        /// Updates (overwrites) the Y0, Y1 values at the specified X-value. Automatically triggers a redraw
        /// </summary>
        /// <param name="x">The X-value</param>
        /// <param name="y0">The Y0-value</param>
        /// <param name="y1">The Y1-value</param>
        void Update(TX x, TY y0, TY y1);

        /// <summary>
        /// Inserts an X, Y0, Y1 point at the specified index. Automatically triggers a redraw
        /// </summary>
        /// <param name="index">The index to insert at</param>
        /// <param name="x">The X-value</param>
        /// <param name="y0">The Y0-value</param>
        /// <param name="y1">The Y1-value</param>
        void Insert(int index, TX x, TY y0, TY y1);

        /// <summary>
        /// Inserts a collection of X, Y0 and Y1 points at the specified index, automatically triggering a redraw
        /// </summary>
        /// <param name="startIndex">The index to insert at</param>
        /// <param name="x">The X-values</param>
        /// <param name="y0">The Y0-values</param>
        /// <param name="y1">The Y1-values</param>
        void InsertRange(int startIndex, IEnumerable<TX> x, IEnumerable<TY> y0, IEnumerable<TY> y1);
    }
}