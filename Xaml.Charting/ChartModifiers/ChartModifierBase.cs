// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ChartModifierBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers.XmlSerialization;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;
using TinyMessenger;

#if SILVERLIGHT
using Ecng.Xaml.Charting.Common.Extensions;
#endif

namespace Ecng.Xaml.Charting.ChartModifiers
{
    public enum Modifier
    {
        Rollover,
        Cursor,
        Tooltip,
        VerticalSlice
    }

    /// <summary>
    /// Defines the base class to a Chart Modifier, which can be used to extend the interactivity or rendering of the <see cref="UltrachartSurface" />
    /// </summary>
    /// <seealso cref="ModifierGroup" />
    public abstract class ChartModifierBase : ApiElementBase, IChartModifier, IXmlSerializable
    {
#if SILVERLIGHT
        /// <summary>
        /// Defines the DataContextWatcher DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DataContextWatcherProperty = DependencyProperty.Register("DataContextWatcher", typeof(object), typeof(ChartModifierBase), new PropertyMetadata(null, OnDataContextChangedInternal));
#endif
        /// <summary>
        /// Defines the ReceiveHandledEvents DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ReceiveHandledEventsProperty = DependencyProperty.Register("ReceiveHandledEvents", typeof(bool), typeof(ChartModifierBase), new PropertyMetadata(false));

        /// <summary>
        /// Defines the IsEnabled Attached Property
        /// </summary>
        public new static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(ChartModifierBase), new PropertyMetadata(true, OnIsEnabledChangedInternal));

        /// <summary>
        /// Defines the ExecuteOn DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ExecuteOnProperty =
            DependencyProperty.Register("ExecuteOn", typeof(ExecuteOn), typeof(ChartModifierBase), new PropertyMetadata(default(ExecuteOn)));

        /// <summary>
        /// Defines the MouseModifier DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MouseModifierProperty =
            DependencyProperty.Register("MouseModifier", typeof(MouseModifier), typeof(ChartModifierBase), new PropertyMetadata(MouseModifier.None));

        private IUltrachartSurface _ultraChartSurface;
        private static Dictionary<MouseButtons, ExecuteOn> _executeOnMap;
        private IServiceContainer _services;
        private TinyMessageSubscriptionToken _renderedToken;
        private TinyMessageSubscriptionToken _resizedToken;

        private bool _isLeftButtonDown = false;
        private bool _isMiddleButtonDown = false;
        private bool _isRightButtonDown = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChartModifierBase"/> class.
        /// </summary>
        /// <remarks></remarks>
        protected ChartModifierBase()
        {
            SetName();
            
#if SILVERLIGHT
            SetBinding(DataContextWatcherProperty, new System.Windows.Data.Binding());
#else
            DataContextChanged += (s,e) => OnDataContextChangedInternal((DependencyObject)s, e);            
#endif

            IsPolarChartSupported = true;
        }

        /// <summary>
        /// Returns a value indicating whether mouse events should be propagated to the mouse target.
        /// </summary>
        public virtual bool CanReceiveMouseEvents()
        {
            // Added to fix the issue http://ulcsoftware.myjetbrains.com/youtrack/issue/SC-2788
            return IsEnabled && IsAttached && ModifierSurface != null && ParentSurface != null && ParentSurface.IsVisible;
        }

        /// <summary>
        /// Called when the element is attached to the Chart Surface
        /// </summary>
        /// <remarks></remarks>
        public override void OnAttached()
        {
            AssertPolarChartIsSupported();
        }

        private void AssertPolarChartIsSupported()
        {
            if (!IsPolarChartSupported && XAxis != null && XAxis.IsPolarAxis)
            {
                throw new NotSupportedException(string.Format("{0} is not supported by PolarXAxis.", GetType().Name));
            }
        }

        /// <summary>
        /// Called immediately before the element is detached from the Chart Surface
        /// </summary>
        /// <remarks></remarks>
        public override void OnDetached() { }

        /// <summary>
        /// Gets or sets a value indicating whether this element is enabled in the user interface (UI).
        /// </summary>
        /// <returns>true if the element is enabled; otherwise, false. The default value is true.</returns>
        public new bool IsEnabled
        {
            get { return (bool) GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        /// <summary>
        /// Determines when the <see cref="ZoomExtentsModifier"/> executes, 
        /// args.g. <see cref="ChartModifiers.ExecuteOn.MouseDoubleClick"/> will cause a zoom extents on mouse double 
        /// click of the parent <see cref="UltrachartSurface"/>
        /// </summary>
        public ExecuteOn ExecuteOn
        {
            get { return (ExecuteOn)GetValue(ExecuteOnProperty); }
            set { SetValue(ExecuteOnProperty, value); }
        }

        public MouseModifier MouseModifier
        {
            get { return (MouseModifier)GetValue(MouseModifierProperty); }
            set { SetValue(MouseModifierProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value, indicates whether current modifier handles events
        /// which have already been marked as handled
        /// </summary>
        public bool ReceiveHandledEvents
        {
            get { return (bool)GetValue(ReceiveHandledEventsProperty); }
            set { SetValue(ReceiveHandledEventsProperty, value); }
        }

        /// <summary>
        /// Gets modifier name
        /// </summary>
        public string ModifierName
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets if the Left mouse button is currently down
        /// </summary>
        public bool IsMouseLeftButtonDown { get { return _isLeftButtonDown; } }

        /// <summary>
        /// Gets if the Middle mouse button is currently down
        /// </summary>
        public bool IsMouseMiddleButtonDown { get { return _isMiddleButtonDown; } }

        /// <summary>
        /// Gets of the right mouse button is currently down
        /// </summary>
        public bool IsMouseRightButtonDown { get { return _isRightButtonDown; } }

        /// <summary>
        /// Gets or sets a Mouse Event Group, an ID used to share mouse events across multiple targets
        /// </summary>
        public string MouseEventGroup { get; set; }

        /// <summary>
        /// Called when a Mouse DoubleClick occurs on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public virtual void OnModifierDoubleClick(ModifierMouseArgs e) { }

        /// <summary>
        /// Called when a Mouse Button is pressed on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public virtual void OnModifierMouseDown(ModifierMouseArgs e)
        {
            _isLeftButtonDown = e.MouseButtons == MouseButtons.Left;
            _isRightButtonDown = e.MouseButtons == MouseButtons.Right;
            _isMiddleButtonDown = e.MouseButtons == MouseButtons.Middle;
        }

        /// <summary>
        /// Called when the Mouse is moved on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse move operation</param>
        /// <remarks></remarks>
        public virtual void OnModifierMouseMove(ModifierMouseArgs e) { }

        /// <summary>
        /// Called when a Mouse Button is released on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public virtual void OnModifierMouseUp(ModifierMouseArgs e)
        {
            _isLeftButtonDown = e.MouseButtons != MouseButtons.Left && _isLeftButtonDown;
            _isRightButtonDown = e.MouseButtons != MouseButtons.Right && _isRightButtonDown;
            _isMiddleButtonDown = e.MouseButtons != MouseButtons.Middle && _isMiddleButtonDown;
        }

        /// <summary>
        /// Called when the Mouse Wheel is scrolled on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse wheel operation</param>
        /// <remarks></remarks>
        public virtual void OnModifierMouseWheel(ModifierMouseArgs e) { }

        /// <summary>
        /// Called when a Multi-Touch Down interaction occurs on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation</param>
        public virtual void OnModifierTouchDown(ModifierTouchManipulationArgs e){}

        /// <summary>
        /// Called when a Multi-Touch Move interaction occurs on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation</param>
        public virtual void OnModifierTouchMove(ModifierTouchManipulationArgs e){}

        /// <summary>
        /// Called when a Multi-Touch Up interaction occurs on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation</param>
        public virtual void OnModifierTouchUp(ModifierTouchManipulationArgs e){}

        /// <summary>
        /// Gets the <see cref="ServiceContainer"/> which provides access to services throughout Ultrachart. 
        /// ServiceContainers are created one per <see cref="UltrachartSurface"/> instance, 
        /// and shared between peripheral components such as <see cref="AxisBase"/>, <see cref="BaseRenderableSeries"/>, <see cref="ChartModifierBase"/> instances.
        /// For a full list of available services, see the remarks on <see cref="ServiceContainer"/>
        /// </summary>
        public override IServiceContainer Services
        {
            get { return _services; }
            set
            {
                if (_services != null)
                {
                    if (_renderedToken != null)
                        _services.GetService<IEventAggregator>().Unsubscribe<UltrachartRenderedMessage>(_renderedToken);
                    if (_resizedToken != null)
                        _services.GetService<IEventAggregator>().Unsubscribe<UltrachartResizedMessage>(_resizedToken);
                }

                _services = value;

                if (_services != null)
                {
                    _renderedToken = _services.GetService<IEventAggregator>().Subscribe<UltrachartRenderedMessage>(OnParentSurfaceRendered, true);
                    _resizedToken = _services.GetService<IEventAggregator>().Subscribe<UltrachartResizedMessage>(OnParentSurfaceResized, true);
                }
            }
        }

        /// <summary>
        /// Gets or sets the parent <see cref="UltrachartSurface"/> to perform operations on
        /// </summary>
        /// <value>The parent surface.</value>
        /// <remarks></remarks>
        public override IUltrachartSurface ParentSurface
        {
            get { return _ultraChartSurface; }
            set
            {
                // Unsubscribe events from old events
                var oldSurface = _ultraChartSurface as UltrachartSurface;
                if (oldSurface != null)
                {
                    oldSurface.MouseLeave -= UltrachartSurfaceMouseLeave;
                    oldSurface.MouseEnter -= UltrachartSurfaceMouseEnter;
                    oldSurface.SelectedRenderableSeries.CollectionChanged -= SelectedRenderableSeriesCollectionChanged;
                }
                _ultraChartSurface = value;

                // Subscribe to new events
                var newSurface = _ultraChartSurface as UltrachartSurface;
                if (newSurface != null)
                {
                    newSurface.MouseLeave += UltrachartSurfaceMouseLeave;
                    newSurface.MouseEnter += UltrachartSurfaceMouseEnter;
                    newSurface.SelectedRenderableSeries.CollectionChanged += SelectedRenderableSeriesCollectionChanged;
                }           

                OnPropertyChanged("ParentSurface");
            }
        }

        internal bool IsPolarChartSupported { get; set; }

        /// <summary>
        /// Transforms the input point relative to the <see cref="IHitTestable"/> element. Can be used to transform 
        /// points relative to the <see cref="UltrachartSurface.ModifierSurface"/>, or <see cref="UltrachartSurface.XAxis"/> for instance.
        /// </summary>
        /// <param name="point">The input point</param>
        /// <param name="relativeTo">The <see cref="IHitTestable"/> element to translate points relative to</param>
        /// <returns>The output point</returns>
        public Point GetPointRelativeTo(Point point, IHitTestable relativeTo)
        {
            return RootGrid.TranslatePoint(point, relativeTo);
        }

        /// <summary>
        /// Gets whether the mouse point is within the bounds of the hit-testable element. Assumes the mouse-point has not been translated yet (performs translation)
        /// </summary>
        /// <param name="mousePoint"></param>
        /// <param name="hitTestable"></param>
        /// <returns></returns>
        public bool IsPointWithinBounds(Point mousePoint, IHitTestable hitTestable)
        {            
            bool result = hitTestable.IsPointWithinBounds(mousePoint);
            return result;
        }

        /// <summary>
        /// OBSOLETE
        /// </summary>
        /// <param name="point"></param>
        /// <param name="relativeTo"></param>
        /// <returns></returns>
        [Obsolete("Use GetPointRelativeTo instead")]
        public Point GetRelativePosition(Point point, IHitTestable relativeTo)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called with the <see cref="UltrachartSurface.XAxes" /> <see cref="AxisCollection" /> changes. Overridden in derived classes.
        /// </summary>
        protected virtual void OnXAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args) { }

        /// <summary>
        /// Called with the <see cref="UltrachartSurface.YAxes" /> <see cref="AxisCollection" /> changes. Overridden in derived classes.
        /// </summary>
        protected virtual void OnYAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args) { }

        /// <summary>
        /// Called when the AnnotationCollection changes. Overridden in derived classes.
        /// </summary>
        protected virtual void OnAnnotationCollectionChanged(object sender, NotifyCollectionChangedEventArgs args) { }

        /// <summary>
        /// Called when the parent UltrachartSurface is resized
        /// </summary>
        /// <param name="e">The <see cref="UltrachartResizedMessage"/> which contains the event arg data</param>
        public virtual void OnParentSurfaceResized(UltrachartResizedMessage e)
        {
        }

        /// <summary>
        /// Called when the parent <see cref="UltrachartSurface"/> is rendered
        /// </summary>
        /// <param name="e">The <see cref="UltrachartRenderedMessage"/> which contains the event arg data</param>
        public virtual void OnParentSurfaceRendered(UltrachartRenderedMessage e)
        {
        }

        /// <summary>
        /// Sets the Cursor on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="cursor"></param>
        protected void SetCursor(Cursor cursor)
        {
            if (ParentSurface != null)
            {
                ParentSurface.SetMouseCursor(cursor);
            }
        }

        /// <summary>
        /// Called when the DataContext of the <see cref="ChartModifierBase"/> changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }

        /// <summary>
        /// Called when the IsEnabled property changes on this <see cref="ChartModifierBase"/> instance
        /// </summary>
        protected virtual void OnIsEnabledChanged()
        {
        }

        /// <summary>
        /// Called when the mouse leaves the Master of current <see cref="MouseEventGroup"/>
        /// </summary>
        /// <param name="e"></param>
        public virtual void OnMasterMouseLeave(ModifierMouseArgs e)
        {
            OnParentSurfaceMouseLeave();
        }

        /// <summary>
        /// Called when the mouse leaves the parent <see cref="UltrachartSurface"/>
        /// </summary>
        protected virtual void OnParentSurfaceMouseLeave()
        {
        }

        /// <summary>
        /// Called when the mouse enters the parent <see cref="UltrachartSurface"/>
        /// </summary>
        protected virtual void OnParentSurfaceMouseEnter()
        {
        }

        /// <summary>
        /// Called when the parent surface SelectedSeries collection changes
        /// </summary>
        protected virtual void OnSelectedSeriesChanged(IEnumerable<IRenderableSeries> oldSeries, IEnumerable<IRenderableSeries> newSeries)
        {
        }

        /// <summary>
        /// Determines whether the currently pressed mouse buttons matches the <see cref="ExecuteOn"/>. Used to 
        /// filter events such as zoom or pan on right mouse button
        /// </summary>
        /// <param name="mouseButtons"></param>
        /// <param name="executeOn"></param>
        /// <returns></returns>
        protected bool MatchesExecuteOn(MouseButtons mouseButtons, ExecuteOn executeOn) {
            var modifier = MouseExtensions.GetCurrentModifier();

            if (_executeOnMap == null)
            {
                _executeOnMap = new Dictionary<MouseButtons, ExecuteOn>();
                _executeOnMap.Add(MouseButtons.None, ExecuteOn.MouseMove);
                _executeOnMap.Add(MouseButtons.Left, ExecuteOn.MouseLeftButton);
                _executeOnMap.Add(MouseButtons.Middle, ExecuteOn.MouseMiddleButton);
                _executeOnMap.Add(MouseButtons.Right, ExecuteOn.MouseRightButton);
            }

            return _executeOnMap.ContainsKey(mouseButtons) && _executeOnMap[mouseButtons] == executeOn && ((MouseModifier & modifier) == modifier);
        }

        /// <summary>
        /// Returns an XmlSchema that describes the XML representation of the object that is produced by the WriteXml method and consumed by the ReadXml method
        /// </summary>
        /// <remarks>
        /// This method is reserved by <see cref="System.Xml.Serialization.IXmlSerializable"/> and should not be used
        /// </remarks>
        /// <returns></returns>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Generates <see cref="ChartModifierBase"/> from its XML representation.
        /// </summary>
        /// <param name="reader"></param>
        public virtual void ReadXml(XmlReader reader)
        {
            if (reader.MoveToContent() == XmlNodeType.Element)
            {
                ChartModifierSerializationHelper.Instance.DeserializeProperties(this, reader);
            }
        }

        /// <summary>
        /// Converts <see cref="ChartModifierBase"/> into its XML representation.
        /// </summary>
        /// <param name="writer"></param>
        public virtual void WriteXml(XmlWriter writer)
        {
            ChartModifierSerializationHelper.Instance.SerializeProperties(this, writer);
        }

        /// <summary>
        /// Instantly stops any inertia that can be associated with this modifier.
        /// </summary>
        public virtual void ResetInertia()
        {
            
        }

        /// <summary>
        /// Called with the <see cref="UltrachartSurface.XAxes" /> <see cref="AxisCollection" /> changes
        /// </summary>
        void IChartModifier.OnXAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            AssertPolarChartIsSupported();

            OnXAxesCollectionChanged(sender, args);
        }

        /// <summary>
        /// Called with the <see cref="UltrachartSurface.YAxes" /> <see cref="AxisCollection" /> changes
        /// </summary>
        void IChartModifier.OnYAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            OnYAxesCollectionChanged(sender, args);
        }

        /// <summary>
        /// Called when the AnnotationCollection changes
        /// </summary>
        void IChartModifier.OnAnnotationCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            OnAnnotationCollectionChanged(sender, args);
        }

        private static void OnDataContextChangedInternal(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var modifier = (ChartModifierBase)d;
            modifier.OnDataContextChanged(d, e);
        }

        private static void OnIsEnabledChangedInternal(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var modifier = (ChartModifierBase)d;

            modifier.OnIsEnabledChanged();
        }

        private void SetName()
        {
            var type = GetType().ToString();
            var startIndex = type.LastIndexOf('.');
            ModifierName = type.Substring(startIndex + 1);
        }

        private void UltrachartSurfaceMouseLeave(object sender, MouseEventArgs e)
        {
            OnParentSurfaceMouseLeave();
        }

        private void UltrachartSurfaceMouseEnter(object sender, MouseEventArgs e)
        {
            OnParentSurfaceMouseEnter();
        }

        private void SelectedRenderableSeriesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var oldSeries = e.OldItems != null ? e.OldItems.Cast<IRenderableSeries>() : null;
            var newSeries = e.NewItems != null ? e.NewItems.Cast<IRenderableSeries>() : null;

            OnSelectedSeriesChanged(oldSeries, newSeries);
        }
    }
}
