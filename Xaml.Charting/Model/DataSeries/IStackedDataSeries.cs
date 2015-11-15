// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IStackedDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// Defines the base interface to a DataSeries which can be stacked. Provides a data-source for all stacked renderable series types
    /// </summary>
    public interface IStackedDataSeries : IDataSeries
    {
        /// <summary>
        /// Gets underlying stacked data series component
        /// </summary>
        IDataSeries UnderlyingDataSeries
        {
            get;
        }

        /// <summary>
        /// Gets other data series from the same stacking group
        /// </summary>
        IList<IDataSeries> PreviousDataSeriesInSameGroup
        {
            get;
        }
    }
}
