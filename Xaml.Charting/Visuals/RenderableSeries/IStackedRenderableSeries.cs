// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IStackedRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    public interface IStackedRenderableSeries : IRenderableSeries
    {
        /// <summary>
        /// Gets or sets a string stacked-group Id, used to ensure columns are stacked together
        /// </summary>
        string StackedGroupId { get; set; }

        /// <summary>
        /// Gets or sets a value whether all series with the same <see cref="StackedGroupId"/> will appear 100% stacked
        /// </summary>
        bool IsOneHundredPercent { get; set; }

        /// <summary>
        /// Gets or sets the value which determines the zero line in Y direction.
        /// Used to set the bottom of an area
        /// </summary>
        double ZeroLineY { get; set; }
    }
}