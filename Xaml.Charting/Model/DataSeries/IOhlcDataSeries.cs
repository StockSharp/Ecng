// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IOhlcDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// Defines the interface to an OHLC DataSeries, a series containing Open, High, Low, Close data-points
    /// </summary>
    public interface IOhlcDataSeries : IDataSeries
    {
        /// <summary>
        /// Gets the Open Values of this DataSeries as a List of <see cref="IComparable"/>
        /// </summary>
        IList OpenValues { get; }

        /// <summary>
        /// Gets the High Values of this DataSeries as a List of <see cref="IComparable"/>
        /// </summary>
        IList HighValues { get; }

        /// <summary>
        /// Gets the Low Values of this DataSeries as a List of <see cref="IComparable"/>
        /// </summary>
        IList LowValues { get; }

        /// <summary>
        /// Gets the Close Values of this DataSeries as a List of <see cref="IComparable"/>
        /// </summary>
        /// <remarks>Close equates to Y Values in either OHLC or simple XY dataseries</remarks>
        IList CloseValues { get; }
    }

    /// <summary>
    /// Defines the typed interface to an OHLC DataSeries, a series containing Open, High, Low, Close data-points
    /// </summary>
    public interface IOhlcDataSeries<TX, TY> : IDataSeries<TX, TY>, IOhlcDataSeries
        where TX:IComparable
        where TY:IComparable
    {
        /// <summary>
        /// Gets the Open Values of this DataSeries, if the data is OHLC
        /// </summary>
        new IList<TY> OpenValues { get; }

        /// <summary>
        /// Gets the High Values of this DataSeries, if the data is OHLC
        /// </summary>
        new IList<TY> HighValues { get; }

        /// <summary>
        /// Gets the Low Values of this DataSeries, if the data is OHLC
        /// </summary>
        new IList<TY> LowValues { get; }

        /// <summary>
        /// Gets the Close Values of this DataSeries, if the data is OHLC
        /// </summary>
        /// <remarks>Close equates to Y Values in either OHLC or simple XY dataseries</remarks>
        new IList<TY> CloseValues { get; }

        /// <summary>
        /// Appends an Open, High, Low, Close point to the series
        /// </summary>
        /// <param name="x">The X value</param>
        /// <param name="open">The Open value</param>
        /// <param name="high">The High value</param>
        /// <param name="low">The Low value</param>
        /// <param name="close">The Close value</param>
        void Append(TX x, TY open, TY high, TY low, TY close);

        /// <summary>
        /// Appends a list of Open, High, Low, Close points to the series
        /// </summary>
        /// <param name="x">The list of X values</param>
        /// <param name="open">The list of Open values</param>
        /// <param name="high">The list of High values</param>
        /// <param name="low">The list of Low values</param>
        /// <param name="close">The list of Close values</param>
        void Append(IEnumerable<TX> x, IEnumerable<TY> open, IEnumerable<TY> high, IEnumerable<TY> low, IEnumerable<TY> close);

        /// <summary>
        /// Updates an Open, High, Low, Close point specified by the X-Value passed in. 
        /// </summary>
        /// <param name="x">The X Value to key on when updating</param>
        /// <param name="open">The Open value</param>
        /// <param name="high">The High value</param>
        /// <param name="low">The Low value</param>
        /// <param name="close">The Close value</param>
        /// <exception cref="InvalidOperationException">Thrown if the x value is not in the DataSeries</exception>
        void Update(TX x, TY open, TY high, TY low, TY close);

        /// <summary>
        /// Inserts an Open, High, Low, Close point at the specified index
        /// </summary>
        /// <param name="index">The index to insert at</param>
        /// <param name="x">The X value</param>
        /// <param name="open">The Open value</param>
        /// <param name="high">The High value</param>
        /// <param name="low">The low value</param>
        /// <param name="close">The close value</param>
        void Insert(int index, TX x, TY open, TY high, TY low, TY close);

        /// <summary>
        /// Inserts a list of Open, High, Low, Close points at the specified index
        /// </summary>
        /// <param name="startIndex">The index to insert at</param>
        /// <param name="x">The list of X values</param>
        /// <param name="open">The list of Open values</param>
        /// <param name="high">The list of High values</param>
        /// <param name="low">The list of Low values</param>
        /// <param name="close">The list of Close values</param>
        void InsertRange(int startIndex, IEnumerable<TX> x, IEnumerable<TY> open, IEnumerable<TY> high, IEnumerable<TY> low, IEnumerable<TY> close);
    }
}