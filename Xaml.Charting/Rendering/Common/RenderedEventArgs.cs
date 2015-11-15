// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RenderedEventArgs.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Rendering.Common
{
    /// <summary>
    /// Event args used when the <see cref="IRenderSurface2D.Rendered"/> event is raised
    /// </summary>
    /// <seealso cref="IRenderSurface2D"/>
    /// <seealso cref="RenderSurfaceBase"/>
    /// <seealso cref="HighQualityRenderSurface"/>
    /// <seealso cref="HighSpeedRenderSurface"/>
    public class RenderedEventArgs : EventArgs
    {
        private readonly double _duration;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderedEventArgs" /> class.
        /// </summary>
        /// <param name="duration">The duration of the last render operation in milliseconds</param>
        public RenderedEventArgs(double duration)
        {
            _duration = duration;
        }

        /// <summary>
        /// Gets the duration of the last render operation in milliseconds
        /// </summary>
        public double Duration
        {
            get { return _duration; }
        }
    }
}
