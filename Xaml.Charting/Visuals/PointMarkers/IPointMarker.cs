// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IPointMarker.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Media;
using Ecng.Xaml.Charting.Rendering.Common;

namespace Ecng.Xaml.Charting.Visuals.PointMarkers
{
    /// <summary>
    /// specifies interface for rendering point markers. a point marker is something which is displayed at data points
    /// </summary>
    public interface IPointMarker
    {
        /// <summary>
        /// Renders the PointMarker on each <see cref="Point"/> passed in with Fill and Stroke values. Each point is a coordinate in the centre of the PointMarker. 
        /// </summary>
        /// <param name="context">The RenderContext to draw too</param>
        /// <param name="centers">The collection of Points to render the Point Markers at</param>
        /// <seealso cref="IRenderContext2D"/>
        /// <seealso cref="IPen2D"/>
        /// <seealso cref="IBrush2D"/>
        void Draw(IRenderContext2D context, IEnumerable<Point> centers);

        void Draw(IRenderContext2D context, double x, double y, IPen2D defaultPen, IBrush2D defaultBrush);

        /// <summary>
        /// Gets or sets the Stroke Color (outline) of the PointMarker
        /// </summary>
        Color Stroke { get; set; } 

        /// <summary>
        /// Gets or sets the Fill Color (fill) of the PointMarker
        /// </summary>
        Color Fill { get; set; }

        /// <summary>
        /// Gets or sets the Width of the PointMarker in pixels
        /// </summary>
        double Width { get;set; } 

        /// <summary>
        /// Gets or sets the Height of the PointMarker in pixels
        /// </summary>
        double Height { get;set;}

        /// <summary>
        /// Gets or sets the StrokeThickness of the PointMarker outline in pixels
        /// </summary>
        double StrokeThickness { get; set; }

        /// <summary>
        /// Called when a batched draw operation is about to begin. All subsequent draw operations will have the same width, height, rendercontext and pen, brush
        /// </summary>
        void Begin(IRenderContext2D context, IPen2D defaultPen, IBrush2D defaultBrush);

        /// <summary>
        /// Ends a batch draw operation
        /// </summary>
        void End(IRenderContext2D context);
    }
}
