// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IUltrachartController.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Visuals
{
    /// <summary>
    ///     An interface to a subset of methods on the UltrachartSurface.
    /// </summary>
    public interface IUltrachartController : ISuspendable, IInvalidatableElement
    {
        /// <summary>
        /// Zooms the chart to the extents of the data, plus any X or Y Grow By fraction set on the X and Y Axes
        /// </summary>
        void ZoomExtents();
        
        /// <summary>
        /// Zooms to extents with the specified animation duration
        /// </summary>
        /// <param name="duration">The duration of animation when zooming to extents</param>
        void AnimateZoomExtents(TimeSpan duration);

        /// <summary>
        /// Zooms the chart to the extents of the data in the Y-Direction, accounting for the current data in view in the X-direction
        /// </summary>
        void ZoomExtentsY();

        /// <summary>
        /// Zooms the chart to the extents of the data in the Y-Direction, accounting for the current data in view in the X-direction
        /// </summary>
        void AnimateZoomExtentsY(TimeSpan duration);

        /// <summary>
        /// Zooms the chart to the extents of the data in the X-Direction
        /// </summary>
        void ZoomExtentsX();

        /// <summary>
        /// Zooms the chart to the extents of the data in the X-Direction
        /// </summary>
        void AnimateZoomExtentsX(TimeSpan duration);
    }
}