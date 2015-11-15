// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// I2DArraySegment.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Represents part of 2D data for a specific X coordinate
    /// Provides list of vertical pixels selected from 2D data for a specific X index
    /// </summary>
    interface I2DArraySegment : IPoint
    {
        /// <summary>
        /// X value at left of cell
        /// </summary>
        double XValueAtLeft { get; }

        /// <summary>
        /// X value at right of cell
        /// </summary>
        double XValueAtRight { get; }

        /// <summary>
        /// Y value at bottom of heatmap
        /// </summary>
        double YValueAtBottom { get; }

        /// <summary>
        /// Y value at top of heatmap
        /// </summary>
        double YValueAtTop { get; }

        /// <returns>list of colors in ARGB format</returns>
        IList<int> GetVerticalPixelsArgb(DoubleToColorMappingSettings mappingSettings);

        /// <returns>list of 2d data values</returns>
        IList<double> GetVerticalPixelValues();
    }
}