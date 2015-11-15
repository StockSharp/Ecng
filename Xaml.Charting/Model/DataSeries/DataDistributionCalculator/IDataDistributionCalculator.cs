// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DataDistributionCalculator.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Text;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Defines the interface to a DataDistributionCalculator
    /// </summary>
    /// <typeparam name="TX"></typeparam>
    public interface IDataDistributionCalculator<TX> where TX: IComparable
    {
		/// <summary>
		/// Gets whether this DataSeries contains Sorted data in the X-direction. 
		/// Note: Sorted data will result in far faster indexing operations. If at all possible, try to keep your data sorted in the X-direction
		/// </summary>
        bool DataIsSortedAscending { get; }

        /// <summary>
        /// Gets whether the data is evenly paced
        /// </summary>
        bool DataIsEvenlySpaced { get; }

        /// <summary>
        /// Called when X Values are appended. Should update the Data Distribution flags
        /// </summary>
        /// <param name="xValues">The x values.</param>
        /// <param name="newXValue">The new x value.</param>
        /// <param name="acceptsUnsortedData">if set to <c>true</c> the series accepts unsorted data.</param>
        void OnAppendXValue(ISeriesColumn<TX> xValues, TX newXValue, bool acceptsUnsortedData);

        /// <summary>
        /// Called when X Values are appended. Should update the Data Distribution flags
        /// </summary>
        /// <param name="xValues">The x values.</param>
        /// <param name="newXValue">The new x value.</param>
        /// <param name="acceptsUnsortedData">if set to <c>true</c> the series accepts unsorted data.</param>
        void OnAppendXValues(ISeriesColumn<TX> xValues, int countBeforeAppending, IEnumerable<TX> newXValues, bool acceptsUnsortedData);

        /// <summary>
        /// Called when X Values are inserted. Should update the Data Distribution flags
        /// </summary>
        /// <param name="xValues">The x values.</param>
        /// <param name="newXValue">The new x value.</param>
        /// <param name="acceptsUnsortedData">if set to <c>true</c> the series accepts unsorted data.</param>
        void OnInsertXValue(ISeriesColumn<TX> xValues, int indexWhereInserted, TX newXValue, bool acceptsUnsortedData);

        /// <summary>
        /// Called when X Values are inserted. Should update the Data Distribution flags
        /// </summary>
        /// <param name="xValues">The x values.</param>
        /// <param name="newXValue">The new x value.</param>
        /// <param name="acceptsUnsortedData">if set to <c>true</c> the series accepts unsorted data.</param>
        void OnInsertXValues(ISeriesColumn<TX> xValues, int indexWhereInserted, int insertedCount, IEnumerable<TX> newXValues, bool acceptsUnsortedData);

        /// <summary>
        /// Updates the data distribution flags when x values removed.
        /// </summary>
        void UpdateDataDistributionFlagsWhenRemovedXValues();

        /// <summary>
        /// Clears the DataDistributionCalculator flags
        /// </summary>
        void Clear();
    }
}
