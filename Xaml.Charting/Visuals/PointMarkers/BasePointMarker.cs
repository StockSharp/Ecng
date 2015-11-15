// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// BasePointMarker.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Controls;
using Ecng.Xaml.Charting.Common;
using Ecng.Xaml.Charting.Common.AttachedProperties;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Rendering.Common;
using System.Windows.Media;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals.PointMarkers
{
    /// <summary>A base class for Bitmap-rendered Point-Markers, which are small markers drawn once per data-point on a BaseRenderableSeries</summary>
    /// <example>
    /// public class CustomPointMarker : BasePointMarker<br/>
    /// {<br/>
    ///     protected override void DrawInternal(IRenderContext2D context, IEnumerable&lt;Point&gt; centers, IPen2D pen, IBrush2D brush)<br/>
    ///     {<br/>
    ///         // TODO: Render a single point marker using IRenderContext2D<br/>
    ///     }<br/>
    /// }
    /// </example>
    public abstract class BasePointMarker : ContentControl, IPointMarker
    {
        /// <summary>
        /// Defines the PointMarkerTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty PointMarkerTemplateProperty = DependencyProperty.Register("PointMarkerTemplate", typeof(ControlTemplate), typeof(BasePointMarker), new PropertyMetadata(null, (s, e) => ((BasePointMarker)s).OnPropertyChanged(s, e)));

        /// <summary>
        /// Defines the Stroke DependencyProperty
        /// </summary>
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(Color), typeof(BasePointMarker), new PropertyMetadata(default(Color), (s, e) => ((BasePointMarker)s).OnPropertyChanged(s, e)));

        /// <summary>
        /// Defines the StrokeThickness DependencyProperty
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(BasePointMarker), new PropertyMetadata((double)1, (s, e) => ((BasePointMarker)s).OnPropertyChanged(s, e)));       

        /// <summary>
        /// Defines the Fill DependencyProperty
        /// </summary>
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register("Fill", typeof(Color), typeof(BasePointMarker), new PropertyMetadata(default(Color), (s,e) => ((BasePointMarker)s).OnPropertyChanged(s,e)));

        /// <summary>
        /// Defines the AntiAliasing DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AntiAliasingProperty = DependencyProperty.Register(
            "AntiAliasing", typeof(bool), typeof(BasePointMarker), new PropertyMetadata(true, (s, e) => ((BasePointMarker)s).OnPropertyChanged(s, e)));        

        SmartDisposable<IPen2D> _cachedPen;
        SmartDisposable<IBrush2D> _cachedBrush;
        private Type _typeOfRendererForCachedResources;
        private PropertyChangeNotifier _widthNotifier;
        private PropertyChangeNotifier _heightNotifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasePointMarker" /> class.
        /// </summary>
        protected BasePointMarker()
        {
            DefaultStyleKey = GetType();

            _widthNotifier = new PropertyChangeNotifier(this, WidthProperty);
            _heightNotifier = new PropertyChangeNotifier(this, HeightProperty);

            _widthNotifier.ValueChanged += (s, e) => OnPropertyChanged(this, default(DependencyPropertyChangedEventArgs));
            _heightNotifier.ValueChanged += (s, e) => OnPropertyChanged(this, default(DependencyPropertyChangedEventArgs));
        }

        /// <summary>
        /// PROTOTYPE ONLY: Enables MultiThreaded Drawing for certain <see cref="BasePointMarker"/> derived types
        /// </summary>
        public static bool EnableMultithreadedDrawing { get; set; }

        /// <summary>
        /// Gets or sets the PointMarker ControlTemplate, which defines the point-marker Visual to be rendered on each datapoint of the series
        /// </summary>
        /// <remarks>The ControlTemplate is used to template the visuals only for a blank control, creating a new instance per <see cref="BaseRenderableSeries"/>. 
        /// the resulting FrameworkElement is cached to bitmap and drawn on each redraw of the series, so any triggers, mouse interactions on the ControlTemplate will be lost</remarks>
        public ControlTemplate PointMarkerTemplate
        {
            get { return (ControlTemplate)GetValue(PointMarkerTemplateProperty); }
            set { SetValue(PointMarkerTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Stroke (the outline) of the PointMarker. May be Transparent to ignore
        /// </summary>
        public Color Stroke
        {
            get { return (Color)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }
       
        /// <summary>
        /// Gets or sets the solid color Fill of the PointMarker. May be Transparent to ignore
        /// </summary>
        public Color Fill
        {
            get { return (Color)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        /// <summary>
        /// Gets or sets the StrokeThickness of the PointMarker stroke. 
        /// </summary>
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets the AntiAliasing property, which is used when drawing the stroke
        /// </summary>
        public bool AntiAliasing
        {
            get { return (bool)GetValue(AntiAliasingProperty); }
            set { SetValue(AntiAliasingProperty, value); }
        }

        /// <summary>
        /// Renders the PointMarker on each <see cref="Point"/> passed in with Fill and Stroke values. Each point is a coordinate in the centre of the PointMarker. 
        /// </summary>
        /// <param name="context">The RenderContext to draw too</param>
        /// <param name="centers">The collection of Points to render the Point Markers at</param>
        /// <seealso cref="IRenderContext2D"/>
        /// <seealso cref="IPen2D"/>
        /// <seealso cref="IBrush2D"/>
        public virtual void Draw(IRenderContext2D context, IEnumerable<Point> centers)
        {
            DrawInternal(context, centers, _cachedPen.Inner, _cachedBrush.Inner);
        }

        public virtual void Draw(IRenderContext2D context, double x, double y, IPen2D defaultPen, IBrush2D defaultBrush)
        {
            DrawInternal(context, x, y, GetPen(defaultPen), GetBrush(defaultBrush));
        }

        private IPen2D GetPen(IPen2D pen)
        {
            return pen ?? _cachedPen.Inner;
        }

        private IBrush2D GetBrush(IBrush2D brush)
        {
            return brush ?? _cachedBrush.Inner;
        }

        /// <summary>
        /// Disposes any cached resources, e.g. when the Fill or Stroke is changed, any cached pens or brushes are also disposed
        /// </summary>
        public virtual void Dispose() 
        {
            _typeOfRendererForCachedResources = null;
            if (_cachedBrush != null)
            {
                _cachedBrush.Dispose();
                _cachedBrush = null;
            }
            if (_cachedPen != null)
            {
                _cachedPen.Dispose();
                _cachedPen = null;
            }
        }

        /// <summary>
        /// When overridden in a derived class, draws the point markers at specified collection of <see cref="Point"/> centers
        /// </summary>
        /// <param name="context">The RenderContext to draw with</param>
        /// <param name="centers">The Centres of the point markers</param>
        /// <param name="pen">The default Stroke pen (if current pen is not set)</param>
        /// <param name="brush">The default Fill brush (if current brush is not set)</param>
        /// <seealso cref="IRenderContext2D"/>
        /// <seealso cref="IPen2D"/>
        /// <seealso cref="IBrush2D"/>
        protected abstract void DrawInternal(IRenderContext2D context, IEnumerable<Point> centers, IPen2D pen, IBrush2D brush);

        /// <summary>
        /// When overridden in a derived class, draws the point markers at specified x,y pixel coordinate
        /// </summary>
        /// <param name="context">The RenderContext to draw with</param>
        /// <param name="x">The x-coordinate to draw at</param>
        /// <param name="y">The y-coordinate to draw at</param>
        /// <param name="pen">The default Stroke pen (if current pen is not set)</param>
        /// <param name="brush">The default Fill brush (if current brush is not set)</param>
        /// <seealso cref="IRenderContext2D"/>
        /// <seealso cref="IPen2D"/>
        /// <seealso cref="IBrush2D"/>
        protected abstract void DrawInternal(IRenderContext2D context, double x, double y, IPen2D pen, IBrush2D brush);

        /// <summary>
        /// Should be called when any DependencyProperty value changes on the <see cref="BasePointMarker"/> derived class
        /// </summary>
        /// <param name="d">The sender</param>
        /// <param name="e">The arguments</param>
        protected virtual void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BasePointMarker) d).Dispose();
            ((BasePointMarker) d).InvalidateParentSurface();
        }

        private void InvalidateParentSurface()
        {
            var rSeries = DataContext as BaseRenderableSeries;
            if (rSeries != null)
                rSeries.OnInvalidateParentSurface();
        }

        private void CheckCachedResources(IRenderContext2D context)
        {
            var contextType = context.GetType();
            if (contextType != _typeOfRendererForCachedResources || _cachedPen == null || _cachedBrush == null)
            {
                this.Dispose();

                _cachedBrush = new SmartDisposable<IBrush2D>(context.CreateBrush(Fill));
                _cachedPen = new SmartDisposable<IPen2D>(context.CreatePen(Stroke, AntiAliasing, (float)StrokeThickness));
                _typeOfRendererForCachedResources = context.GetType();
            }
        }

        /// <summary>
        /// Called when a batched draw operation is about to begin. All subsequent draw operations will have the same width, height, rendercontext and pen, brush
        /// </summary>
        public virtual void Begin(IRenderContext2D context, IPen2D defaultPen, IBrush2D defaultBrush)
        {
            context.SetPrimitvesCachingEnabled(true);
            CheckCachedResources(context);
        }

        /// <summary>
        /// Ends a batch draw operation
        /// </summary>
        public virtual void End(IRenderContext2D context)
        {
            context.SetPrimitvesCachingEnabled(false);
        }
    }
}
