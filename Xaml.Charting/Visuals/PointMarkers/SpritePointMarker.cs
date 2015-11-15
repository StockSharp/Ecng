// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// SpritePointMarker.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ecng.Xaml.Charting.Common;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals.PointMarkers
{
    /// <summary>
    /// Allows any WPF UIElement to be rendered as a Sprite (bitmap) at each each data-point location using the following XAML syntax
    /// </summary>
    /// <remarks><see cref="BasePointMarker"/> derived types use fast bitmap rendering to draw data-points to the screen. This means that
    /// traditional WPF style tooltips won't work. For that we need to use the HitTest API. Please see the HitTest sections of the user manual
    /// for more information</remarks>
    /// <seealso cref="BasePointMarker"></seealso>
    /// <seealso cref="SquarePointMarker"></seealso>
    /// <seealso cref="TrianglePointMarker"></seealso>
    /// <seealso cref="EllipsePointMarker"></seealso>
    /// <seealso cref="CrossPointMarker"></seealso>
    /// <seealso cref="SpritePointMarker"></seealso>
    /// <example>
    /// <code title="SpritePointMarker usage" description="Shows how to instantiate a SpritePointMarker inline in XAML. Note when templating or styling a series, you will need to use the BaseRenderableSeries.PointMarkerTemplate property instead" lang="xaml">
    /// &lt;s:FastLineRenderableSeries&gt;
    ///    &lt;s:FastLineRenderableSeries.PointMarker&gt;
    ///       &lt;s:SpritePointMarker&gt;
    ///          &lt;s:SpritePointMarker.PointMarkerTemplate&gt;
    ///             &lt;ControlTemplate&gt;
    ///                &lt;!-- This can be any WPF UIElement, rendered as bitmnap and repeated per point --&gt;
    ///                &lt;Ellipse Width="7" Height="7" Fill="Magenta" Stroke="White"&gt;
    ///             &lt;/ControlTemplate&gt;
    ///          &lt;/s:SpritePointMarker.PointMarkerTemplate&gt;
    ///       &lt;/s:SpritePointMarker&gt;
    ///    &lt;/s:FastLineRenderableSeries.PointMarker&gt;
    /// &lt;/s:FastLineRenderableSeries&gt;
    /// </code>
    /// </example>
    public class SpritePointMarker : BasePointMarker, IPointMarker
    {
        SmartDisposable<ISprite2D> _cachedPointMarker;
        private Type _typeOfRendererForCachedResources;
        private Rect _pmRect;

        double IPointMarker.Width
        {
            get { return _cachedPointMarker != null ? _cachedPointMarker.Inner.Width : ActualWidth; }
            set { Width = value; }
        }

        double IPointMarker.Height
        {
            get { return _cachedPointMarker != null ? _cachedPointMarker.Inner.Height : ActualHeight; }
            set { Height = value; }
        }

        /// <summary>
        /// Disposes any cached resources, e.g. when the Fill or Stroke is changed, any cached pens or brushes are also disposed
        /// </summary>
        public override void Dispose() 
        {
            base.Dispose();            
            if (_cachedPointMarker != null)
            {
                _cachedPointMarker.Dispose();
                _cachedPointMarker = null;
            }
        }

        /// <summary>
        /// When overridden in a derived class, draws the point markers at specified collection of <see cref="Point" /> centers
        /// </summary>
        /// <param name="context">The RenderContext to draw with</param>
        /// <param name="centers">The Centres of the point markers</param>
        /// <param name="pen">The default Stroke pen (if current pen is not set)</param>
        /// <param name="brush">The default Fill brush (if current brush is not set)</param>
        /// <seealso cref="IRenderContext2D" />
        ///   <seealso cref="IPen2D" />
        ///   <seealso cref="IBrush2D" />
        protected override void DrawInternal(IRenderContext2D context, IEnumerable<Point> centers, IPen2D pen, IBrush2D brush)
        {
            if (PointMarkerTemplate == null) return;
            
            CheckCachedSprite(context);

            // TODO {abt} Offsets should be added in the renderer, as DirectX can do this in a shader
            context.DrawSprites(_cachedPointMarker.Inner,
                _pmRect, 
                centers.Select(center => new Point((float)(center.X - _cachedPointMarker.Inner.Width / 2), (float)(center.Y - _cachedPointMarker.Inner.Height / 2))));
        }

        protected override void DrawInternal(IRenderContext2D context, double x, double y, IPen2D pen, IBrush2D brush)
        {
            if (PointMarkerTemplate == null) return;

            CheckCachedSprite(context);

            context.DrawSprite(_cachedPointMarker.Inner, _pmRect, new Point(x - _cachedPointMarker.Inner.Width*0.5, y - _cachedPointMarker.Inner.Height*0.5));
        }

        private void CheckCachedSprite(IRenderContext2D context)
        {
            var contextType = context.GetType();
            if (_cachedPointMarker == null || contextType != _typeOfRendererForCachedResources)
            {
                var feMarker = PointMarker.CreateFromTemplate(PointMarkerTemplate, this);
                _cachedPointMarker = new SmartDisposable<ISprite2D>(context.CreateSprite(feMarker));
                _pmRect = new Rect(0, 0, _cachedPointMarker.Inner.Width, _cachedPointMarker.Inner.Height);
                _typeOfRendererForCachedResources = contextType;
            }
        }
    }
}
