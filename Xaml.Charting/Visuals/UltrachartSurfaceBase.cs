// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UltrachartSurfaceBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Rendering.HighSpeedRasterizer;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;
using TinyMessenger;

namespace Ecng.Xaml.Charting.Visuals
{
    /// <summary>
    /// Common base interface for <see cref="UltrachartSurfaceBase"/> derived classes
    /// </summary>
    public interface IUltrachartSurfaceBase : ISuspendable, IInvalidatableElement
    {
        /// <summary>
        /// Gets whether the UltrachartSurface is currently visible
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Gets or sets a flag whether Ultrachart should output rendering exceptions and 'Why Ultrachart Doesn't Render' reasons to the Debug Console. 
        /// Default is True. 
        /// </summary>
        bool DebugWhyDoesntUltrachartRender { get; set; }

        /// <summary>
        /// Gets the <see cref="ServiceContainer"/> which provides access to services throughout Ultrachart. 
        /// ServiceContainers are created one per <see cref="UltrachartSurface"/> instance, 
        /// and shared between peripheral components such as <see cref="AxisBase"/>, <see cref="BaseRenderableSeries"/>, <see cref="ChartModifierBase"/> instances.
        /// For a full list of available services, see the remarks on <see cref="ServiceContainer"/>
        /// </summary>
        IServiceContainer Services { get; }

        /// <summary>
        /// A synchronization object which is locked during a render pass. If you lock this Ultrachart will not render and will be blocked on the UI thread until the lock is released. 
        /// 
        /// This is used internally by DataSeries to lock the UltrachartSurface when bulk updates are done. 
        /// </summary>
        object SyncRoot { get; }

        /// <summary>
        /// Gets or sets an optional Chart Title, displayed above the chart surface
        /// </summary>
        string ChartTitle { get; set; }

        /// <summary>
        /// Gets or sets a value whether to clip the ChartModifierSurface property to bounds. Fefault false
        /// </summary>
        bool ClipModifierSurface { get; set; }

        /// <summary>
        /// Gets the ModifierSurface, which is used to draw primitives for the Chart Modifiers
        /// </summary>
        /// <remarks></remarks>
        IChartModifierSurface ModifierSurface { get; }

        /// <summary>
        /// Gets or sets the <see cref="Ecng.Xaml.Charting.Visuals.RenderPriority"/>. The default is <see cref="Ecng.Xaml.Charting.Visuals.RenderPriority.Normal"/>
        /// </summary>
        RenderPriority RenderPriority { get; set; }

        /// <summary>
        /// Gets or sets the RenderSurface implementation that this <see cref="UltrachartSurfaceBase"/> uses. Default implementation for a <see cref="UltrachartSurface"/>
        /// is a <see cref="HighSpeedRenderSurface"/>, however Ultrachart supports 
        /// additional render surfaces, providing high quality software and high speed hardware accelerated or 3D renderers. 
        /// </summary>
        IRenderSurface RenderSurface { get; set; }

        /// <summary>
        /// Event raised at the end of a single render pass
        /// </summary>
        event EventHandler<EventArgs> Rendered;

        /// <summary>
        /// Raises the <see cref="UltrachartSurfaceBase.Rendered"/> event, fired at the end of a render pass immediately before presentation to the screen 
        /// </summary>
        void OnUltrachartRendered();

        /// <summary>
        /// Sets a Cursor on the UltrachartSurface
        /// </summary>
        /// <param name="cursor">The new Cursor</param>
        void SetMouseCursor(Cursor cursor);
    }

    /// <summary>
    /// An abstract base class containing shared code between different implementations of <see cref="UltrachartSurface"/>
    /// </summary>
    [TemplatePart(Name = "PART_ChartModifierSurface", Type = typeof(ChartModifierSurface))]
    [TemplatePart(Name = "PART_MainGrid", Type = typeof(Grid))]
    public abstract class UltrachartSurfaceBase : Control, IUltrachartSurfaceBase
    {
#if SILVERLIGHT
        /// <summary>
        /// Defines the DataContextWatcher DependencyProperty, which is used as a proxy to get DataContextChanged events in Silverlight
        /// </summary>
        public static readonly DependencyProperty DataContextWatcherProperty = DependencyProperty.Register("DataContextWatcher",typeof(Object), typeof(UltrachartSurfaceBase), new PropertyMetadata(null, (s,e) => ((UltrachartSurfaceBase)s).OnDataContextChanged(e)));
#endif

        /// <summary>Defines the ClipModifierSurface DependencyProperty</summary>
        public static readonly DependencyProperty ClipModifierSurfaceProperty = DependencyProperty.Register("ClipModifierSurface", typeof(bool), typeof(UltrachartSurfaceBase), new PropertyMetadata(default(bool)));

        /// <summary>Defines the ChartTitle DependencyProperty</summary>
        public static readonly DependencyProperty ChartTitleProperty = DependencyProperty.Register("ChartTitle", typeof(string), typeof(UltrachartSurfaceBase), new PropertyMetadata(default(string), OnInvalidateUltrachartSurface));        

        /// <summary>Defines the RenderSurface DependencyProperty</summary>
        public static readonly DependencyProperty RenderSurfaceProperty = DependencyProperty.Register("RenderSurface", typeof(IRenderSurface), typeof(UltrachartSurfaceBase), new PropertyMetadata(default(IRenderSurface), (s,e) => ((UltrachartSurfaceBase)s).OnRenderSurfaceDependencyPropertyChanged(e)));

        /// <summary>Defines the MaxFrameRate DependencyProperty</summary>
        public static readonly DependencyProperty MaxFrameRateProperty = DependencyProperty.Register("MaxFrameRate", typeof(double?), typeof(UltrachartSurfaceBase), new PropertyMetadata(null));     

        private ChartModifierSurface _modifierSurface;
        private readonly object _syncRoot = new object();
        private volatile bool _isLoaded;
        private volatile bool _disposed;
        private IServiceContainer _serviceContainer;
        private MainGrid _rootGrid;
        private IRenderSurface _rsCache;
        private readonly RoutedEventHandler _loadedHandler;
        private readonly RoutedEventHandler _unloadedHandler;
        private readonly SizeChangedEventHandler _sizeChangedHandler;
        private readonly DependencyPropertyChangedEventHandler _dataContextChangedHandler;
        private volatile bool _firstEverLoaded;

        /// <summary>
        /// Event raised at the end of a single render pass
        /// </summary>
        public event EventHandler<EventArgs> Rendered;

        /// <summary>
        /// Initializes a new instance of the <see cref="UltrachartSurfaceBase"/> class.
        /// </summary>
        protected UltrachartSurfaceBase()
        {
            UltrachartDebugLogger.Instance.WriteLine("Instantiating {0}", GetType().Name);

            _serviceContainer = new ServiceContainer();

// ReSharper disable DoNotCallOverridableMethodsInConstructor 
            RegisterServices(_serviceContainer);
// ReSharper restore DoNotCallOverridableMethodsInConstructor

            _loadedHandler = (s, e) => OnUltrachartSurfaceLoaded();
            _unloadedHandler = (s, e) => OnUltrachartSurfaceUnloaded();
            _sizeChangedHandler = (s, e) => OnUltrachartSurfaceSizeChanged();
            _dataContextChangedHandler = (s, e) => OnDataContextChanged(e);

            SizeChanged += _sizeChangedHandler;
            Loaded += _loadedHandler;
            Unloaded += _unloadedHandler;
#if SILVERLIGHT
            SetBinding(DataContextWatcherProperty, new System.Windows.Data.Binding());
#else
            DataContextChanged += _dataContextChangedHandler;
#endif

            DebugWhyDoesntUltrachartRender = false;
            RenderPriority = RenderPriority.Normal;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="UltrachartSurface" /> class.
        /// </summary>
        ~UltrachartSurfaceBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets or sets a flag whether Ultrachart should output rendering exceptions and 'Why Ultrachart Doesn't Render' reasons to the Debug Console. 
        /// Default is True. 
        /// </summary>
        public bool DebugWhyDoesntUltrachartRender { get; set; }

#if SILVERLIGHT

        /// <summary>Gets a valid indicating whether this FrameworkElement has been loaded for presentation </summary>
        public bool IsLoaded { get { return _isLoaded; } }

        public bool IsVisible { get { return this.IsVisible(); } }

#endif

        /// <summary>
        /// Gets or sets the Maximum Framerate of this UltrachartSurface in Hertz (Frames per Second). Default is 100.0
        /// </summary>
        public double? MaxFrameRate
        {
            get { return (double?)GetValue(MaxFrameRateProperty); }
            set { SetValue(MaxFrameRateProperty, value); }
        }   

        /// <summary>
        /// Gets the <see cref="ServiceContainer"/> which provides access to services throughout Ultrachart. 
        /// ServiceContainers are created one per <see cref="UltrachartSurface"/> instance, 
        /// and shared between peripheral components such as <see cref="AxisBase"/>, <see cref="BaseRenderableSeries"/>, <see cref="ChartModifierBase"/> instances.
        /// For a full list of available services, see the remarks on <see cref="ServiceContainer"/>
        /// </summary>
        public IServiceContainer Services
        {
            get { return _serviceContainer; }
            protected internal set { _serviceContainer = value; }
        }

        /// <summary>
        /// True if the <see cref="UltrachartSurfaceBase"/> has been disposed. If so do not draw!
        /// </summary>
        protected bool IsDisposed { get { return _disposed; } }

        /// <summary>
        /// True if the <see cref="UltrachartSurfaceBase"/> has been Loaded
        /// </summary>
        protected new bool IsUltrachartSurfaceLoaded { get { return _isLoaded; }}

        /// <summary>
        /// A synchronization object which is locked during a render pass. If you lock this Ultrachart will not render and will be blocked on the UI thread until the lock is released. 
        /// 
        /// This is used internally by DataSeries to lock the UltrachartSurface when bulk updates are done. 
        /// </summary>
        public object SyncRoot
        {
            get { return _syncRoot; }
        }

        /// <summary>
        /// Gets or sets an optional Chart Title, displayed above the chart surface
        /// </summary>
        public string ChartTitle
        {
            get { return (string)GetValue(ChartTitleProperty); }
            set { SetValue(ChartTitleProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value whether to clip the ChartModifierSurface property to bounds. Fefault false
        /// </summary>
        public bool ClipModifierSurface
        {
            get { return (bool)GetValue(ClipModifierSurfaceProperty); }
            set { SetValue(ClipModifierSurfaceProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether updates for the target are currently suspended
        /// </summary>
        /// <remarks></remarks>
        public bool IsSuspended
        {
            get { return UpdateSuspender.GetIsSuspended(this) || _firstEverLoaded == false; }
        }

        /// <summary>
        /// Gets the ModifierSurface, which is used to draw primitives for the Chart Modifiers
        /// </summary>
        /// <remarks></remarks>
        public IChartModifierSurface ModifierSurface
        {
            get { return _modifierSurface; }
        }

        /// <summary>
        /// Gets or sets the <see cref="Ecng.Xaml.Charting.Visuals.RenderPriority"/>. The default is <see cref="Ecng.Xaml.Charting.Visuals.RenderPriority.Normal"/>
        /// </summary>
        public RenderPriority RenderPriority { get; set; }

        /// <summary>
        /// Gets or sets the RenderSurface implementation that this <see cref="UltrachartSurfaceBase"/> uses. Default implementation for a <see cref="UltrachartSurface"/>
        /// is a <see cref="HighSpeedRenderSurface"/>, however Ultrachart supports 
        /// additional render surfaces, providing high quality software and high speed hardware accelerated or 3D renderers. 
        /// </summary>
        public IRenderSurface RenderSurface
        {
            get { return (IRenderSurface)GetValue(RenderSurfaceProperty); }
            set { SetValue(RenderSurfaceProperty, value); }
        }

        /// <summary>
        /// Gets the Root Grid that hosts the Ultrachart RenderSurface, GridLinesPanel, X-Axis and Y-Axes (Left and right)
        /// </summary>
        public IMainGrid RootGrid
        {
            get { return _rootGrid; }
        }

        /// <summary>
        /// Forces initialization of the UltrachartSurface in the case it is being used to render off-screen (on server)
        /// </summary>
        public virtual void OnLoad()
        {
            OnUltrachartSurfaceLoaded();
        }

        /// <summary>
        /// Suspends drawing updates on the target until the returned object is disposed, when a final draw call will be issued
        /// </summary>
        /// <returns>
        /// The disposable Update Suspender
        /// </returns>
        public IUpdateSuspender SuspendUpdates()
        {
            return new UpdateSuspender(this);
        }

        /// <summary>
        /// Resumes updates on the target, intended to be called by IUpdateSuspender
        /// </summary>
        /// <param name="suspender"></param>
        public void ResumeUpdates(IUpdateSuspender suspender)
        {
            if (suspender.ResumeTargetOnDispose)
            {
                InvalidateElement();
            }
        }

        /// <summary>
        /// Called by IUpdateSuspender each time a target suspender is disposed. When the final
        /// target suspender has been disposed, ResumeUpdates is called
        /// </summary>
        public void DecrementSuspend()
        {
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public override void OnApplyTemplate()
        {
            UltrachartDebugLogger.Instance.WriteLine("OnApplyTemplate");

            base.OnApplyTemplate();

            var sc = ((ServiceContainer)Services);
            if (sc.HasService<IChartModifierSurface>())
            {
                sc.DeRegisterService<IChartModifierSurface>();
            }

            _modifierSurface = GetAndAssertTemplateChild<ChartModifierSurface>("PART_ChartModifierSurface");
            _rootGrid = GetAndAssertTemplateChild<MainGrid>("PART_MainGrid");

            ((ServiceContainer)Services).RegisterService<IChartModifierSurface>(ModifierSurface);
        }


        /// <summary>
        /// Asynchronously requests that the element redraws itself plus children.
        /// Will be ignored if the element is ISuspendable and currently IsSuspended (within a SuspendUpdates/ResumeUpdates call)
        /// </summary>
        public virtual void InvalidateElement()
        {
            if (IsSuspended)
            {
                UltrachartDebugLogger.Instance.WriteLine(
                    "UltrachartSurface.IsSuspended=true. Ignoring InvalidateElement() call");
                return;
            }

            if (DispatcherUtil.GetTestMode() || RenderPriority == RenderPriority.Immediate)
            {
                // No CompositionTargetRendering event when running Ultrachart from tests
                Services.GetService<IDispatcherFacade>().BeginInvokeIfRequired(DoDrawingLoop, DispatcherPriority.Normal);
                return;
            }

            // That's it, Render loop will pick up the work. 
            if (_rsCache != null)
            {
                _rsCache.InvalidateElement();
            }
        }

        /// <summary>
        /// Raises the <see cref="UltrachartSurfaceBase.Rendered"/> event, fired at the end of a render pass immediately before presentation to the screen 
        /// </summary>
        public virtual void OnUltrachartRendered()
        {
            var handler = Rendered;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Sets a Cursor on the UltrachartSurface
        /// </summary>
        /// <param name="cursor">The new Cursor</param>
        /// <remarks></remarks>
        public void SetMouseCursor(Cursor cursor)
        {
            this.SetCurrentValue(CursorProperty, cursor);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {
#if !SILVERLIGHT
                    DataContextChanged -= _dataContextChangedHandler;
                    SizeChanged -= _sizeChangedHandler;
                    Loaded -= _loadedHandler;
                    Unloaded -= _unloadedHandler;
#endif
                    OnUltrachartSurfaceUnloaded();

                    if (_rootGrid != null) _rootGrid.UnregisterEventsOnShutdown();
                }));

                // Note disposing has been done.
                _disposed = true;

                // need this check to prevent from getting exception in Silverlight in RenderSurfaceBase.CleanSeries()
#if !SILVERLIGHT
                // Causes RenderSurface to be disposed, essential if using a DirectX Surface
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    RenderSurface = null;
                    BindingOperations.ClearAllBindings(this);
                }));
#endif
            }
        }

        /// <summary>
        /// Calls InvalidateElement on the <see cref="UltrachartSurfaceBase"/>, should be used as the callback for Dependency Properties in <see cref="UltrachartSurface"/> that should trigger a redraw
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        protected static void OnInvalidateUltrachartSurface(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var i = d as IInvalidatableElement;
            if (i == null) return;
            i.InvalidateElement();
        }

        /// <summary>
        /// Gets the TemplateChild by the specified name and casts to type <typeparamref name="T" />, asserting that the result is not null
        /// </summary>
        /// <typeparam name="T">The Type of the templated part</typeparam>
        /// <param name="childName">Name of the templated part.</param>
        /// <returns>The template part instance</returns>
        /// <exception cref="System.InvalidOperationException">Unable to Apply the Control Template. Child is missing or of the wrong type</exception>
        protected T GetAndAssertTemplateChild<T>(string childName) where T : class
        {
            var templateChild = GetTemplateChild(childName) as T;
            if (templateChild == null)
            {
                throw new InvalidOperationException(string.Format(
                    "Unable to Apply the Control Template. {0} is missing or of the wrong type", childName));
            }
            return templateChild;
        }

        /// <summary>
        /// Called when the <see cref="UltrachartSurfaceBase"/> is Unloaded and removed from the visual tree. Perform cleanup operations here
        /// </summary>
        protected virtual void OnUltrachartSurfaceUnloaded()
        {
            _isLoaded = false;
        }

        /// <summary>
        /// Called when the <see cref="UltrachartSurfaceBase"/> is loaded. Perform initialization operations here. 
        /// </summary>
        protected virtual void OnUltrachartSurfaceLoaded()
        {
            _firstEverLoaded = true;

            _isLoaded = true;

            InvalidateElement();
        }

        /// <summary>
        /// Called when the <see cref="UltrachartSurfaceBase"/> Size changes. Perform render surface resize or redraw operations here
        /// </summary>
        protected virtual void OnUltrachartSurfaceSizeChanged()
        {
        }

        /// <summary>
        /// Called when the <see cref="UltrachartSurfaceBase" /> DataContext changes.
        /// </summary>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
        {
            UltrachartDebugLogger.Instance.WriteLine("OnDataContextChanged");
        }

        /// <summary>
        /// Called with the <see cref="UltrachartSurfaceBase.RenderSurfaceProperty" /> changes.
        /// </summary>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnRenderSurfaceDependencyPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            _rsCache = e.NewValue as IRenderSurface;
        }

        /// <summary>
        /// Called in the constructor of <see cref="UltrachartSurfaceBase" />, gives derived classes the opportunity to register services per <see cref="UltrachartSurfaceBase" /> instance
        /// </summary>
        /// <param name="serviceContainer">The service container instance.</param>
        protected virtual void RegisterServices(IServiceContainer serviceContainer)
        {
            serviceContainer.RegisterService<IDispatcherFacade>(new DispatcherUtil(GetDispatcher(this)));
            serviceContainer.RegisterService<IEventAggregator>(new TinyMessengerHub());
        }

        /// <summary>
        /// The inner drawing loop. Called once per frame. Do your drawing here. 
        /// </summary>
        protected abstract void DoDrawingLoop();

        protected internal void OnRenderFault(Exception caught) {
            var message = string.Format(" {0} didn't render, because an exception was thrown:\n  Message: {1}\n  Stack Trace: {2}", GetType().Name, caught.Message, caught.StackTrace);

            Console.WriteLine(message);
            UltrachartDebugLogger.Instance.WriteLine(message);
        }

        internal void OnDataSeriesUpdated(object sender, EventArgs e)
        {
#if SILVERLIGHT
            if (!IsLoaded)
                return;
#endif

            if (RenderPriority == RenderPriority.Manual)
                return;

            InvalidateElement();
        }

        internal static Dispatcher GetDispatcher(DependencyObject obj)
        {
#if SILVERLIGHT
            var dispatcher = Deployment.Current.Dispatcher;

            // if we did not get the Dispatcher throw an exception
            if (dispatcher != null)
                return dispatcher;

            throw new InvalidOperationException("Unable to get the Silverlight Deployment.Current.Dispatcher");
#else
            return obj.Dispatcher;
#endif
        }
    }
}