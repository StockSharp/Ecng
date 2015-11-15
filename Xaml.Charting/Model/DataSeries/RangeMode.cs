// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RangeMode.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Defines enumeration constants for a programmatic redraw of the parent <see cref="UltrachartSurface"/>
    /// </summary>
    public enum RangeMode
    {
        /// <summary>
        /// Perform no ranging, just redraw
        /// </summary>
        None,

        /// <summary>
        /// Perform full X and Y ranging on redraw
        /// </summary>
        ZoomToFit,

        /// <summary>
        /// Perform just Y ranging on redraw
        /// </summary>
        ZoomToFitY
    }
}