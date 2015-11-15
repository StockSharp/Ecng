// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IDrawable.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals
{
    /// <summary>
    /// Defines the base interface for an object that can be drawn
    /// </summary>
    public interface IDrawable
    {
        /// <summary>
        /// Gets or sets the width of the <see cref="IDrawable"/> in pixels
        /// </summary>
        double Width { get; set; }

        /// <summary>
        /// Gets or sets the height of the <see cref="IDrawable"/> in pixels
        /// </summary>
        double Height { get; set; }

        /// <summary>
        /// Called when the instance is drawn
        /// </summary>
        /// <param name="renderContext">The <see cref="IRenderContext2D"/> used for drawing</param>
        /// <param name="renderPassData">Contains arguments and parameters for this render pass</param>
        void OnDraw(IRenderContext2D renderContext, IRenderPassData renderPassData);
    }
}