// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// StackedMountainRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

using Ecng.Xaml.Charting.Rendering.Common;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Defines the interface to <see cref="StackedMountainRenderableSeries"/>
    /// </summary>
    public interface IStackedMountainRenderableSeries : IStackedRenderableSeries
    {
        /// <summary>
        /// Gets the instance of <see cref="IStackedColumnsWrapper"/> used internally for
        /// stacked series composition and rendering.
        /// </summary>
        IStackedMountainsWrapper Wrapper { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this line series is a digital (step) line.
        /// </summary>
        bool IsDigitalLine { get; set; }

        /// <summary>
        /// Draws the <see cref="StackedMountainRenderableSeries"/> using <see cref="IRenderContext2D"/>, <see cref="IRenderPassData"/>.
        /// </summary>
        void DrawMountain(IRenderContext2D renderContext, bool isPreviousSeriesDigital);
    }
}