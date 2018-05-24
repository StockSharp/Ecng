// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RenderSurfaceBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Threading;
using System.Windows;
using System.Reflection;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Common.Messaging;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Licensing.Core;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Ecng.Xaml.Charting.Common;
using Ecng.Xaml.Charting.Visuals;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Rendering.Common
{
    
    /// <summary>
    /// An abstract base class for the RenderSurface, which is a viewport used within the <seealso cref="UltrachartSurface"/> to 
    /// render <see cref="BaseRenderableSeries"/> types in a fast manner. The renderer architecture is plugin based, meaning we have
    /// build multiple implementations of <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/>. 
    /// </summary>
    /// <seealso cref="UltrachartSurface"/>
    /// <seealso cref="IRenderSurface2D"/>
    /// <seealso cref="BaseRenderableSeries"/>
    /// <seealso cref="Ecng.Xaml.Charting.Rendering.HighQualityRasterizer.HighQualityRenderSurface"/>
    /// <seealso cref="Ecng.Xaml.Charting.Rendering.HighSpeedRasterizer.HighSpeedRenderSurface"/>
    public abstract class RenderSurfaceBase : ContentControl, IRenderSurface2D, IDisposable
    {
        /// <summary>
        /// Defines the MaxFrameRate DependencyProperty 
        /// </summary>
        public static readonly DependencyProperty MaxFrameRateProperty = DependencyProperty.Register("MaxFrameRate", typeof(double?), typeof(RenderSurfaceBase), new PropertyMetadata(null, OnMaxFramerateChanged));

        /// <summary>
        /// Defines the UseResizeThrottle DependencyProperty 
        /// </summary>
        public static readonly DependencyProperty UseResizeThrottleProperty = DependencyProperty.Register("UseResizeThrottle", typeof(bool), typeof(RenderSurfaceBase), new PropertyMetadata(default(bool)));

        /// <summary>
        /// Defines the ResizeThrottleMs DependencyProperty 
        /// </summary>
        public static readonly DependencyProperty ResizeThrottleMsProperty = DependencyProperty.Register("ResizeThrottleMs", typeof(int), typeof(RenderSurfaceBase), new PropertyMetadata(50));

        /// <summary>
        /// Defines the RenderSurfaceType attached property
        /// </summary>
        public static readonly DependencyProperty RenderSurfaceTypeProperty =
            DependencyProperty.RegisterAttached("RenderSurfaceType", typeof(string), typeof(RenderSurfaceBase), new PropertyMetadata(default(string), OnRenderSurfaceTypeChanged));

        /// <summary>
        /// Sets the RenderSurfaceType attached property
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="value">The value.</param>
        public static void SetRenderSurfaceType(UIElement element, string value)
        {
            element.SetValue(RenderSurfaceTypeProperty, value);
        }

        /// <summary>
        /// Gets the RenderSurfaceType attached property
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        public static string GetRenderSurfaceType(UIElement element)
        {
            return (string)element.GetValue(RenderSurfaceTypeProperty);
        }

        /// <summary>
        /// Raised each time the render surface is to be drawn. Handle this event to paint to the surface
        /// </summary>
        public event EventHandler<DrawEventArgs> Draw;

        /// <summary>
        /// Raised immediately after a render operation has completed
        /// </summary>
        public event EventHandler<RenderedEventArgs> Rendered;

        internal static readonly string RectIdentifier = Guid.NewGuid().ToString();

        private volatile bool _isDirty;
        private RenderTimer _renderTimer;
        private bool _disposed;

        private Grid _grid;
        private readonly Image _image;

        protected WriteableBitmap RenderWriteableBitmap;

        readonly TextureCacheBase _textureCache;

        internal bool IsLicenseValid { get; set; }

        protected abstract TextureCacheBase CreateTextureCache();

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderSurfaceBase" /> class.
        /// </summary>
        protected RenderSurfaceBase() {
            _textureCache = CreateTextureCache();

            _image = new Image
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stretch = Stretch.None,
                IsHitTestVisible = false
            };

            Initialize();            

            SizeChanged += RenderSurfaceSizeChanged;
            
            Grid.Children.Add(new Rectangle
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Fill = new SolidColorBrush(Colors.Transparent),
                Tag = RectIdentifier
            });

            Loaded += OnRenderSurfaceBaseLoaded;
            Unloaded += OnRenderSurfaceBaseUnloaded;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="RenderSurfaceBase" /> class.
        /// </summary>
        ~RenderSurfaceBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets or sets the <see cref="IServiceContainer"/> instance
        /// </summary>
        /// <value>The services.</value>
        /// <remarks></remarks>
        public IServiceContainer Services { get; set; }

        internal Image Image { get { return _image; } }

        /// <summary>
        /// Gets the root element <see cref="Grid"/> which hosts components in the <see cref="RenderSurfaceBase"/>
        /// </summary>
        public Grid Grid { get { return _grid; } }

        protected TextureCacheBase TextureCache { get { return _textureCache; } }

        /// <summary>
        /// Gets or sets the Maximum Framerate of this RenderSurface. By default this is bound to the parent UltrachartSurface.MaxFrameRate 
        /// </summary>
        public double? MaxFrameRate
        {
            get { return (double?)GetValue(MaxFrameRateProperty); }
            set { SetValue(MaxFrameRateProperty, value); }
        }

        /// <summary>
        /// Gets or sets a timeout for resizing, e.g. 100ms means that the RenderSurface will batch up all Resize events received within a 100ms window. This reduces the CPU usage on resize. Also see <see cref="UseResizeThrottle"/> dependency property, which must be true
        /// </summary>
        public int ResizeThrottleMs
        {
            get { return (int)GetValue(ResizeThrottleMsProperty); }
            set { SetValue(ResizeThrottleMsProperty, value); }
        }

        /// <summary>
        /// If true, uses the <see cref="ResizeThrottleMs"/> value to batch up Resize Events received within a certain time window, reducing the CPU load on resize
        /// </summary>
        public bool UseResizeThrottle
        {
            get { return (bool)GetValue(UseResizeThrottleProperty); }
            set { SetValue(UseResizeThrottleProperty, value); }
        }

        /// <summary>
        /// Returns True if the <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/> size has changed and the viewport needs resizing
        /// </summary>
        /// <remarks></remarks>
        public virtual bool NeedsResizing
        {
            get
            {
                return RenderWriteableBitmap == null ||
                       RenderWriteableBitmap.PixelWidth != (int)ActualWidth ||
                       RenderWriteableBitmap.PixelHeight != (int)ActualHeight;
            }
        }

        /// <summary>
        /// Returns true if the <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/> size is valid for drawing
        /// </summary>
        public virtual bool IsSizeValidForDrawing
        {
            get
            {
                return ActualWidth >= double.Epsilon &&
                       ActualHeight >= double.Epsilon &&
                       ActualWidth.IsRealNumber() &&
                       ActualHeight.IsRealNumber();
            }
        }

        /// <summary>
        /// Gets the child RenderableSeries in this <see cref="IRenderSurface2D" /> instance
        /// </summary>
        public ReadOnlyCollection<IRenderableSeries> ChildSeries
        {
            get { return new ReadOnlyCollection<IRenderableSeries>(Grid.Children.OfType<IRenderableSeries>().ToArray()); }
        }

        /// <summary>
        /// Invalidates the RenderSurface, causing a repaint to occur
        /// </summary>
        public void InvalidateElement()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Clears the viewport
        /// </summary>
        public void Clear()
        {
            using (var context = GetRenderContext())
            {
                context.Clear();
            }
        }

        /// <summary>
        /// Returns True if the <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/> contains the <see cref="IRenderableSeries"/> instance
        /// </summary>
        /// <param name="renderableSeries">the <see cref="IRenderableSeries"/> instance</param>
        /// <returns><c>true</c> if the specified renderable series contains series; otherwise, <c>false</c>.</returns>
        /// <remarks></remarks>
        public bool ContainsSeries(IRenderableSeries renderableSeries)
        {
            var element = renderableSeries as UIElement;
            return element != null && _grid.Children.Contains(element);
        }

        /// <summary>
        /// Adds the <see cref="IRenderableSeries" /> instance to the <see cref="IRenderSurface2D" />
        /// </summary>
        /// <param name="renderableSeries"></param>
        public void AddSeries(IEnumerable<IRenderableSeries> renderableSeries)
        {
            foreach (var series in renderableSeries)
            {
                AddSeries(series);
            }
        }

        /// <summary>
        /// Adds the <see cref="IRenderableSeries"/> instance to the <see cref="RenderSurfaceBase"/>
        /// </summary>
        /// <param name="renderableSeries">The renderable series.</param>
        /// <remarks></remarks>
        public void AddSeries(IRenderableSeries renderableSeries)
        {
            // remove element from previous parent if exists
            RemoveSeries(renderableSeries);

            renderableSeries.Services = Services;

            var element = renderableSeries as FrameworkElement;
            if (element != null)
            {
                element.Visibility = Visibility.Collapsed;

                // Add to current RenderSurface                
                _grid.Children.Add(element);
            }
        }

        /// <summary>
        /// Removes the <see cref="IRenderableSeries"/> from the <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/>
        /// </summary>
        /// <param name="renderableSeries">The renderable series.</param>
        /// <remarks></remarks>
        public void RemoveSeries(IRenderableSeries renderableSeries)
        {
            renderableSeries.Services = null;

            var element = renderableSeries as FrameworkElement;
            if (element == null)
                return;

            var oldGrid = element.Parent as Panel;
            oldGrid.SafeRemoveChild(element);
        }

        /// <summary>
        /// Clears all <see cref="IRenderableSeries"/> on the <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/>
        /// </summary>
        /// <remarks></remarks>
        public virtual void ClearSeries()
        {
            for (int i = _grid.Children.Count - 1; i >= 0; i--)
            {
                var renderSeries = _grid.Children[i] as IRenderableSeriesBase;
                if (renderSeries != null)
                {
                    _grid.Children.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        public void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                // Note disposing has been done.
                _disposed = true;
#if !SILVERLIGHT
                // Causes bindings to be cleared
                Dispatcher.BeginInvoke(new Action(() => BindingOperations.ClearAllBindings(this)));
#endif
                if (_renderTimer != null)
                {
                    int ic = Interlocked.Decrement(ref instanceCount);
                    Debug.WriteLine("Disposing RenderTimer: {0}", ic);
                    _renderTimer.Dispose();
                    _renderTimer = null;
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                DisposeUnmanagedResources();
            }
        }

        /// <summary>
        /// Recreates the elements required by the Viewport, called once at startup and when the surface is resized
        /// </summary>
        public virtual void RecreateSurface()
        {
            const int defaultBitmapSize = 1;

            int width = (int)ActualWidth, height = (int)ActualHeight;
            if (!IsSizeValidForDrawing)
            {
                width = height = defaultBitmapSize;
            }

            // When the chart surface is resized, we need to recreate the WriteableBitmap 
            // and draw to the bitmap. See above GetRenderContext() where we pass the _image and _renderWriteableBitmap
            // the BMP is only assigned to the image at the end of the draw call, in WriteableBitmapRenderContext.Dispose()
            RenderWriteableBitmap = CreateWriteableBitmap(width, height);

            PublishResizedMessage(width, height);
        }

        /// <summary>
        /// Publishes <see cref="RenderSurfaceResizedMessage"/> with actual viewport size
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        protected void PublishResizedMessage(int width, int height)
        {
            if (Services != null)
                Services.GetService<IEventAggregator>().Publish(new RenderSurfaceResizedMessage(this, new Size(width, height)));
        }

        /// <summary>
        /// When overridden in a derived class, returns a RenderContext valid for the current render pass
        /// </summary>
        /// <returns></returns>
        public abstract IRenderContext2D GetRenderContext();

        /// <summary>
        /// Derived classes may override this method to be notified when to dispose of unmanaged resources. Called when the
        /// <see cref="RenderSurfaceBase"/> is disposed
        /// </summary>
        protected virtual void DisposeUnmanagedResources() { }

        /// <summary>
        /// Called when the <see cref="CompositionTarget.Rendering" /> event is raised
        /// </summary>
        protected virtual void OnRenderTimeElapsed()
        {
            if (!_isDirty) return;

            _isDirty = false;
            OnDraw();
        }

        /// <summary>
        /// Raises the <see cref="IRenderSurface2D.Draw">Draw</see> event which preceeds the render operation
        /// </summary>
        protected virtual void OnDraw()
        {
            var handler = Draw;
            if (handler != null)
            {
                var stopwatch = Stopwatch.StartNew();
                handler(this, new DrawEventArgs(this));
                stopwatch.Stop();
                double duration = stopwatch.ElapsedMilliseconds;
                OnRendered(duration);
            }
        }

        /// <summary>
        /// Raises the Rendered event with the specified duration
        /// </summary>
        /// <param name="duration">The duration.</param>
        protected virtual void OnRendered(double duration)
        {
            var handler = Rendered;
            if (handler != null)
            {
                handler(this, new RenderedEventArgs(duration));
            }
        }

        /// <summary>
        /// Called when the <see cref="RenderSurfaceBase" /> is loaded into the visual tree 
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnRenderSurfaceBaseLoaded(object sender, RoutedEventArgs e)
        {
            // Binding the RenderSurface to parent UltrachartSurface.MaxFrameRate property
            var binding = new Binding
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(UltrachartSurfaceBase) },
                Path = new PropertyPath("MaxFrameRate"),
            };

            SetBinding(MaxFrameRateProperty, binding);

            StopTimer();
            StartTimer();

            OnRenderTimeElapsed();
        }

        /// <summary>
        /// Called when the <see cref="RenderSurfaceBase" /> is Unloaded from the visual tree
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnRenderSurfaceBaseUnloaded(object sender, RoutedEventArgs e)
        {
            StopTimer();
        }

        private static int instanceCount;
        private TimedMethod _recreateSurfaceTimedMethod;

        private void StopTimer()
        {            
            if (_renderTimer != null)
            {
                int ic = Interlocked.Decrement(ref instanceCount);
                Debug.WriteLine("Disposing RenderTimer: {0}", ic);
                _renderTimer.Dispose();
                _renderTimer = null;
            }
        }

        private void StartTimer()
        {
            if (Services != null && !_disposed)
            {
                int ic;
                if (_renderTimer != null)
                {
                    ic = Interlocked.Decrement(ref instanceCount);
                    Debug.WriteLine("Disposing RenderTimer: {0}", ic);

                    _renderTimer.Dispose();
                    _renderTimer = null;
                }

                ic = Interlocked.Increment(ref instanceCount);
                Debug.WriteLine("Creating RenderTimer: {0}", ic);
                _renderTimer = new RenderTimer(MaxFrameRate, Services.GetService<IDispatcherFacade>(), this.OnRenderTimeElapsed);
            }
        }

        private void RenderSurfaceSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            if (UseResizeThrottle)
            {
                if (_recreateSurfaceTimedMethod != null)
                    _recreateSurfaceTimedMethod.Dispose();

                // Throttle the RenderSurface recreation
                _recreateSurfaceTimedMethod = TimedMethod.Invoke(() =>
                {
                    RecreateSurface();
                    InvalidateElement();
                }).After(ResizeThrottleMs).WithPriority(DispatcherPriority.Render).Go();
            }
            else
            {
                RecreateSurface();
                InvalidateElement();
            }
        }

        private static void OnMaxFramerateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var newFramerate = (double?)e.NewValue;
            if (newFramerate.HasValue && (!newFramerate.IsDefined() || newFramerate <= 0.0 || newFramerate > 100.0))
            {
                throw new ArgumentException(String.Format("{0}.MaxFramerate must be greater than 0.0 and less than or equal to 100.0", d.GetType().Name));
            }

            ((RenderSurfaceBase)d).StopTimer();
            ((RenderSurfaceBase)d).StartTimer();
            
        }

        private static void OnRenderSurfaceTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scs = d as UltrachartSurface;
            var typeString = e.NewValue as string;

            if (scs == null || typeString.IsNullOrWhiteSpace())
                return;

            var type = Type.GetType(typeString);
            if (type != null)
            {
                var renderSurface = Activator.CreateInstance(type) as IRenderSurface;
                if (renderSurface != null)
                {
                    scs.RenderSurface = renderSurface;
                }
            }
        }

        private static WriteableBitmap CreateWriteableBitmap(int width, int height)
        {
            return BitmapFactory.New(width, height);
        }

        [Obfuscation(Feature = "encryptmethod", Exclude = false)]
        private void Initialize()
        {
            IsTabStop = false;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;

            _grid = new Grid();            

            Device.SetSnapsToDevicePixels(this, true);
            Device.SetSnapsToDevicePixels(_grid, true);
            Device.SetSnapsToDevicePixels(_image, true);

            _grid.Children.Add(_image);

#if LICENSEDDEPLOY
            new LicenseManager().Validate(this, new UltrachartLicenseProviderFactory());
#else
            this.IsLicenseValid = true;
#endif

            Content = _grid;
        }

        /// <summary>
        /// Translates the point relative to the other <see cref="IHitTestable" /> element
        /// </summary>
        /// <param name="point">The input point relative to this <see cref="IHitTestable" /></param>
        /// <param name="relativeTo">The other <see cref="IHitTestable" /> to use when transforming the point</param>
        /// <returns>
        /// The transformed Point
        /// </returns>
        public Point TranslatePoint(Point point, IHitTestable relativeTo)
        {
            return ElementExtensions.TranslatePoint(this, point, relativeTo);
        }

        /// <summary>
        /// Returns true if the Point is within the bounds of the current <see cref="IHitTestable" /> element
        /// </summary>
        /// <param name="point">The point to test</param>
        /// <returns>
        /// true if the Point is within the bounds
        /// </returns>
        public bool IsPointWithinBounds(Point point)
        {
            return ElementExtensions.IsPointWithinBounds(this, point);
        }

        /// <summary>
        /// Gets the bounds of the current <see cref="IHitTestable" /> element relative to another <see cref="IHitTestable" /> element
        /// </summary>
        /// <param name="relativeTo"></param>
        /// <returns></returns>
        public Rect GetBoundsRelativeTo(IHitTestable relativeTo)
        {
            return ElementExtensions.GetBoundsRelativeTo(this, relativeTo);
        }

    }
}
