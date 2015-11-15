// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DrawEventArgs.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Rendering.HighQualityRasterizer;
using Ecng.Xaml.Charting.Rendering.HighSpeedRasterizer;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Rendering.Common
{
    /// <summary>
    /// EventArgs raised when the <see cref="IRenderSurface2D.Draw"/> event is raised, which occurs at the start of the render pass
    /// </summary>
    /// <seealso cref="IRenderSurface2D"/>
    /// <seealso cref="RenderSurfaceBase"/>
    /// <seealso cref="HighQualityRenderSurface"/>
    /// <seealso cref="HighSpeedRenderSurface"/>
    public class DrawEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DrawEventArgs" /> class.
        /// </summary>
        /// <param name="renderSurface">The render surface.</param>
        public DrawEventArgs(IRenderSurface2D renderSurface)
        {
            Guard.NotNull(renderSurface, "renderSurface");
            RenderSurface2D = renderSurface;
        }

        /// <summary>
        /// Gets the <see cref="IRenderSurface2D"/> instance which raised the Draw event
        /// </summary>
        public IRenderSurface2D RenderSurface2D { get; private set; }
    }
}
