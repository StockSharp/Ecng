// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DataSeriesChangedEventArgs.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Constants to define the type of update when the <see cref="IDataSeries.DataSeriesChanged"/> event is raised
    /// </summary>
    [Flags]
    public enum DataSeriesUpdate
    {
        /// <summary>
        /// The underlying data has changed
        /// </summary>
        DataChanged = 0x1, 

        /// <summary>
        /// The Data Series has been cleared
        /// </summary>
        DataSeriesCleared = 0x2,

        /// <summary>
        /// The DataSeriesSset has been cleared
        /// </summary>
        [Obsolete("DataSetCleared is obsolete because there is no DataSeriesSet now")]
        DataSetCleared = 0x4
    }

    /// <summary>
    /// Event args used by event <see cref="IDataSeries.DataSeriesChanged"/> event
    /// </summary>
    public class DataSeriesChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of <see cref="IDataSeries"/> Update
        /// </summary>
        /// <remarks></remarks>
        public DataSeriesUpdate DataSeriesUpdate { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSeriesChangedEventArgs"/> class.
        /// </summary>
        /// <param name="dataSeriesUpdate">The data series update type.</param>
        /// <remarks></remarks>
        public DataSeriesChangedEventArgs(DataSeriesUpdate dataSeriesUpdate)
        {
            DataSeriesUpdate = dataSeriesUpdate;
        }
    }
}