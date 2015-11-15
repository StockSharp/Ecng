// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DataSeriesType.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Enumeration constants to define the Type of <see cref="DataSeries{Tx,Ty}"/>
    /// </summary>
    public enum DataSeriesType
    {
        /// <summary>
        /// The <see cref="DataSeries{Tx,Ty}"/> contains Xy data
        /// </summary>
        Xy,

        /// <summary>
        /// The <see cref="OhlcDataSeries{Tx,Ty}"/> contains OHLC data
        /// </summary>
        Ohlc, 

        /// <summary>
        /// The <see cref="XyyDataSeries{Tx,Ty}"/> contains Xyy data
        /// </summary>
        Xyy,

        /// <summary>
        /// The <see cref="XyyDataSeries{Tx,Ty}"/> contains Xyz data
        /// </summary>
        Xyz,

        /// <summary>
        /// The <see cref="HlcDataSeries{TX,TY}"/> contains XyError data
        /// </summary>
        Hlc,

        /// <summary>
        /// The <see cref="BoxPlotDataSeries{TX,TY}"/> contains Box (Minimum, Lower Quartile, Median, Upper Quartile, Maximum) data
        /// </summary>
        Box,

        /// <summary>
        /// The <see cref="DataSeries{Tx,Ty}"/> contains Box (Minimum, Lower Quartile, Median, Upper Quartile, Maximum) data
        /// </summary>
        StackedXy,

        /// <summary>
        /// The <see cref="Heatmap2DArrayDataSeries"/> contains data from 2D array
        /// </summary>
        Heatmap,        
            
        /// <summary>
        /// 
        /// </summary>
        OneHundredPercentStackedXy,

        /// <summary>
        /// Box volume
        /// </summary>
        BoxVolume,

        /// <summary>
        /// Cluster profile cluster
        /// </summary>
        ClusterProfile
    }
}