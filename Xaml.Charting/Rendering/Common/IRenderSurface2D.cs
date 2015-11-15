// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IRenderSurface2D.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Rendering.Common
{
    /// <summary>
    /// Common interface for a RenderSurface, <see cref="RenderSurfaceBase"/>
    /// </summary>
    public interface IRenderSurface : IDisposable, IHitTestable, IInvalidatableElement
    {
        /// <summary>
        /// Raised each time the render surface is to be drawn. Handle this event to paint to the surface
        /// </summary>
        event EventHandler<DrawEventArgs> Draw;

        /// <summary>
        /// Raised immediately after a render operation has completed
        /// </summary>
        event EventHandler<RenderedEventArgs> Rendered;

        /// <summary>
        /// Returns True if the <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/> size has changed and the viewport needs resizing
        /// </summary>
        bool NeedsResizing { get; }

        /// <summary>
        /// Returns true if the <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/> size is valid for drawing
        /// </summary>
        bool IsSizeValidForDrawing { get; }

        /// <summary>
        /// Gets or sets a <see cref="Style"/> to apply to the <see cref="IRenderSurface2D"/>
        /// </summary>
        Style Style { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IServiceContainer"/> instance
        /// </summary>
        /// <value>The services.</value>
        /// <remarks></remarks>
        IServiceContainer Services { get; set; }

        /// <summary>
        /// Clears all <see cref="IRenderableSeries"/> on the <see cref="IRenderSurface2D"/>
        /// </summary>
        void ClearSeries();

        /// <summary>
        /// Clears the viewport
        /// </summary>
        void Clear();

        /// <summary>
        /// Recreates the elements required by the Viewport, called once at startup and when the surface is resized
        /// </summary>
        void RecreateSurface();
    }

    /// <summary>
    /// Defines the interface to a RenderSurface, which is a viewport used within the <seealso cref="UltrachartSurface"/> to 
    /// render <see cref="BaseRenderableSeries"/> types in a fast manner. The renderer architecture is plugin based, meaning we have
    /// build multiple implementations of <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/>. 
    /// </summary>
    /// <seealso cref="UltrachartSurface"/>
    /// <seealso cref="IRenderSurface2D"/>
    /// <seealso cref="BaseRenderableSeries"/>
    /// <seealso cref="Ecng.Xaml.Charting.Rendering.HighQualityRasterizer.HighQualityRenderSurface"/>
    /// <seealso cref="Ecng.Xaml.Charting.Rendering.HighSpeedRasterizer.HighSpeedRenderSurface"/>
    public interface IRenderSurface2D : IRenderSurface
    {
        /// <summary>
        /// Gets the child RenderableSeries in this <see cref="IRenderSurface2D"/> instance
        /// </summary>
        ReadOnlyCollection<IRenderableSeries> ChildSeries { get; }

        /// <summary>
        /// Creates an <see cref="IRenderContext2D"/> instance to perform drawing operations. Note this is only valid for the current render pass
        /// </summary>
        /// <returns></returns>
        IRenderContext2D GetRenderContext();

        /// <summary>
        /// Returns True if the <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/> contains the <see cref="IRenderableSeries"/> instance
        /// </summary>
        /// <param name="renderableSeries">the <see cref="IRenderableSeries"/> instance</param>
        /// <returns></returns>
        bool ContainsSeries(IRenderableSeries renderableSeries);

        /// <summary>
        /// Adds the <see cref="IRenderableSeries"/> instance to the <see cref="IRenderSurface2D"/>
        /// </summary>
        /// <param name="renderableSeries"></param>
        void AddSeries(IRenderableSeries renderableSeries);

        /// <summary>
        /// Adds the <see cref="IRenderableSeries"/> instance to the <see cref="IRenderSurface2D"/>
        /// </summary>
        /// <param name="renderableSeries"></param>
        void AddSeries(IEnumerable<IRenderableSeries> renderableSeries);

        /// <summary>
        /// Removes the <see cref="IRenderableSeries"/> from the <see cref="IRenderSurface2D"/>
        /// </summary>
        /// <param name="renderableSeries"></param>
        void RemoveSeries(IRenderableSeries renderableSeries);
    }
}