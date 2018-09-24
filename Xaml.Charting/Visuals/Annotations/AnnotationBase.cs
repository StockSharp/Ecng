// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AnnotationBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Schema;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Utility.Mouse;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.Events;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// Enumeration constants to define the Annotation Canvas that an <see cref="IAnnotation"/> is placed on
    /// </summary>
    public enum AnnotationCanvas
    {
        /// <summary>
        /// The annotation is placed above the chart
        /// </summary>
        AboveChart,

        /// <summary>
        /// The annotation is placed below the chart
        /// </summary>
        BelowChart,

        /// <summary>
        /// The annotation is placed on the YAxis
        /// </summary>
        YAxis,

        /// <summary>
        /// The annotation is placed on the XAxis
        /// </summary>
        XAxis
    }

    /// <summary>
    /// Enumeration constants to define the Coordinate mode used to place an annotation
    /// </summary>
    public enum AnnotationCoordinateMode
    {
        /// <summary>
        /// Absolute, requires that coordinates X1,Y1,X2,Y2 are data-values
        /// </summary>
        Absolute,

        /// <summary>
        /// Relative, requires that coordinates X1,Y1,X2,Y2 are double values between 0.0 and 1.0
        /// </summary>
        Relative,

        /// <summary>
        /// RelativeX, requires that coordinates X1,X2 are double values between 0.0 and 1.0, whereas Y1,Y2 are data-values
        /// </summary>
        RelativeX,

        /// <summary>
        /// RelativeY, requires that coordinates Y1,Y2 are double values between 0.0 and 1.0, whereas X1,X2 are data-values
        /// </summary>
        RelativeY
    }

    /// <summary>
    /// Used internally by the Annotation API. Struct to hold transformed coordinates for placement of an annotation on the chart.
    /// </summary>
    public struct AnnotationCoordinates
    {
        /// <summary>Gets or sets the X1 coordinate.</summary>
        public double X1Coord;
        /// <summary>Gets or sets the X2 coordinate.</summary>
        public double X2Coord;
        /// <summary>Gets or sets the Y1 coordinate.</summary>
        public double Y1Coord;
        /// <summary>Gets or sets the Y2 coordinate.</summary>
        public double Y2Coord;
        /// <summary>Gets or sets the offset of the YAxis which the annotation is associated with.</summary>
        public double YOffset;
        /// <summary>Gets or sets the offset of the YAxis which the annotation is associated with.</summary>
        public double XOffset;
    }

    /// <summary>
    /// Provides a base class for annotations to be rendered over the chart
    /// </summary>
    public abstract class AnnotationBase : ApiElementBase, IAnnotation, ISuspendable
    {
        /// <summary>Defines the YAxisId DependencyProperty</summary>
        public static readonly DependencyProperty XAxisIdProperty = DependencyProperty.Register("XAxisId", typeof(string), typeof(AnnotationBase), new PropertyMetadata(AxisBase.DefaultAxisId, OnXAxisIdChanged));
        /// <summary>Defines the YAxisId DependencyProperty</summary>
        public static readonly DependencyProperty YAxisIdProperty = DependencyProperty.Register("YAxisId", typeof(string), typeof(AnnotationBase), new PropertyMetadata(AxisBase.DefaultAxisId, OnYAxisIdChanged));
        /// <summary>Defines the X1 DependencyProperty</summary>
        public static readonly DependencyProperty X1Property = DependencyProperty.Register("X1", typeof(IComparable), typeof(AnnotationBase), new PropertyMetadata(null, OnAnnotationPositionChanged));
        /// <summary>Defines the Y1 DependencyProperty</summary>
        public static readonly DependencyProperty Y1Property = DependencyProperty.Register("Y1", typeof(IComparable), typeof(AnnotationBase), new PropertyMetadata(null, OnAnnotationPositionChanged));
        /// <summary>Defines the X2 DependencyProperty</summary>
        public static readonly DependencyProperty X2Property = DependencyProperty.Register("X2", typeof(IComparable), typeof(AnnotationBase), new PropertyMetadata(null, OnAnnotationPositionChanged));
        /// <summary>Defines the Y2 DependencyProperty</summary>
        public static readonly DependencyProperty Y2Property = DependencyProperty.Register("Y2", typeof(IComparable), typeof(AnnotationBase), new PropertyMetadata(null, OnAnnotationPositionChanged));
        /// <summary>Defines the AnnotationCanvas DependencyProperty</summary>
        public static readonly DependencyProperty AnnotationCanvasProperty = DependencyProperty.Register("AnnotationCanvas", typeof(AnnotationCanvas), typeof(AnnotationBase), new PropertyMetadata(AnnotationCanvas.AboveChart, OnRenderablePropertyChanged));
        /// <summary>Defines the CoordinateMode DependencyProperty</summary>
        public static readonly DependencyProperty CoordinateModeProperty = DependencyProperty.Register("CoordinateMode", typeof(AnnotationCoordinateMode), typeof(AnnotationBase), new PropertyMetadata(AnnotationCoordinateMode.Absolute, OnRenderablePropertyChanged));
        /// <summary>Defines the IsSelected DependencyProperty</summary>
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(AnnotationBase), new PropertyMetadata(false, OnIsSelectedChanged));
        /// <summary>Defines the IsEditable DependencyProperty</summary>
        public static readonly DependencyProperty IsEditableProperty = DependencyProperty.Register("IsEditable", typeof(bool), typeof(AnnotationBase), new PropertyMetadata(false, OnIsEditableChanged));
         /// <summary>Defines the IsHidden DependencyProperty</summary>
        public static readonly DependencyProperty IsHiddenProperty = DependencyProperty.Register("IsHidden", typeof (bool), typeof (AnnotationBase), new PropertyMetadata(false, OnIsHiddenChanged));
        /// <summary>Defines the DragDirections DependencyProperty</summary>
        public static readonly DependencyProperty DragDirectionsProperty = DependencyProperty.Register("DragDirections", typeof(XyDirection), typeof(AnnotationBase), new PropertyMetadata(XyDirection.XYDirection));
        /// <summary>Defines the ResizeDirection DependencyProperty</summary>
        public static readonly DependencyProperty ResizeDirectionsProperty = DependencyProperty.Register("ResizeDirections", typeof(XyDirection), typeof(AnnotationBase), new PropertyMetadata(XyDirection.XYDirection));
        /// <summary>Defines the CanEditText DependencyProperty</summary>
        public static readonly DependencyProperty CanEditTextProperty = DependencyProperty.Register("CanEditText", typeof (bool), typeof (AnnotationBase), new PropertyMetadata(false));

        /// <summary>
        /// Defines the ResizingGripsStyle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ResizingGripsStyleProperty =
            DependencyProperty.Register("ResizingGripsStyle", typeof(Style), typeof(AnnotationBase), new PropertyMetadata(null));
        

        private bool _isAttached;
        private bool _templateApplied;

        /// <summary>
        /// The Root Element of the Annotation to be displayed on the Canvas
        /// </summary>
        protected FrameworkElement AnnotationRoot;

        private bool _isDragging;
        private Point _startPoint;
        private bool _isMouseLeftDown;
        private bool _isResizable;

#if !SILVERLIGHT
        private DateTime _mouseLeftDownTimestamp;        
#endif

        private AnnotationCoordinates _startDragAnnotationCoordinates;

        private IList<IAnnotationAdorner> _myAdorners = new List<IAnnotationAdorner>();

        private IAxis _yAxis;
        private IAxis _xAxis;

        private bool _isLoaded;

        event EventHandler<TouchManipulationEventArgs> IPublishMouseEvents.TouchDown
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EventHandler<TouchManipulationEventArgs> IPublishMouseEvents.TouchMove
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EventHandler<TouchManipulationEventArgs> IPublishMouseEvents.TouchUp
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Occurs when <see cref="IAnnotation"/> becomes selected. 
        /// </summary>
        public event EventHandler Selected;

        /// <summary>
        /// Occurs when <see cref="IAnnotation"/> becomes unselected. 
        /// </summary>
        public event EventHandler Unselected;

        /// <summary>
        /// Occurs when a Drag or move operation starts
        /// </summary>
        public event EventHandler<EventArgs> DragStarted;

        /// <summary>
        /// Occurs when a Drag or move operation ends
        /// </summary>
        public event EventHandler<EventArgs> DragEnded;

        /// <summary>
        /// Occurs when current <see cref="AnnotationBase"/> is dragged or moved
        /// </summary>
        public event EventHandler<AnnotationDragDeltaEventArgs> DragDelta;

        /// <summary>
        /// Occurs when the <see cref="IsHidden"/> property is changed
        /// </summary>
        public event EventHandler IsHiddenChanged;


#if !SILVERLIGHT
        /// <summary>
        /// Occurs when the middle mouse button is pressed while the mouse pointer is over a <see cref="T:System.Windows.UIElement"/>.
        /// </summary>
        public event MouseButtonEventHandler MouseMiddleButtonDown;

        /// <summary>
        /// Occurs when the middle mouse button is released while the mouse pointer is over a <see cref="T:System.Windows.UIElement"/>. However, this event will only be raised if a caller marks the preceding <see cref="E:System.Windows.UIElement.MouseRightButtonDown"/> event as handled; see Remarks.
        /// </summary>
        public event MouseButtonEventHandler MouseMiddleButtonUp;

        private void PreviewMouseUpHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                var handler = MouseMiddleButtonUp;
                if (handler != null) handler(sender, e);
            }
        }

        private void PreviewMouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                var handler = MouseMiddleButtonDown;
                if (handler != null) handler(sender, e);
            }
        }
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationBase"/> class.
        /// </summary>
        protected AnnotationBase()
        {
            DefaultStyleKey = typeof(AnnotationBase);

            IsResizable = true;
        }


        /// <summary>
        /// Gets or sets the Style which is applied to the resizing grips appearing when the annotation gets selected.
        /// </summary>
        public Style ResizingGripsStyle
        {
            get { return (Style)GetValue(ResizingGripsStyleProperty); }
            set { SetValue(ResizingGripsStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether text can be edited on this <see cref="AnnotationBase"/>. 
        /// Supported by Text or label annotations only
        /// </summary>
        public bool CanEditText
        {
            get { return (bool)GetValue(CanEditTextProperty); }
            set { SetValue(CanEditTextProperty, value); }
        }

        /// <summary>
        /// Gets value, indicates whether current instance is resizable
        /// </summary>
        public bool IsResizable
        {
            get { return _isResizable; }
            protected set
            {
                _isResizable = value;
                OnPropertyChanged("IsResizable");
            }
        }

        /// <summary>
        /// Gets or sets the ID of the X-Axis which this Annotation is measured against
        /// </summary>
        public string XAxisId
        {
            get { return (string)GetValue(XAxisIdProperty); }
            set { SetValue(XAxisIdProperty, value); }
        }

        /// <summary>
        /// Gets or sets the ID of the Y-Axis which this Annotation is measured against
        /// </summary>
        public string YAxisId
        {
            get { return (string)GetValue(YAxisIdProperty); }
            set { SetValue(YAxisIdProperty, value); }
        }

        /// <summary>
        /// Limits the Drag direction when dragging the annotation using the mouse, e.g in the X-Direction, Y-Direction or XyDirection. See the <see cref="XyDirection"/> enumeration for options
        /// </summary>
        public XyDirection DragDirections
        {
            get { return (XyDirection) GetValue(DragDirectionsProperty); }
            set { SetValue(DragDirectionsProperty, value); }
        }

        /// <summary>
        /// Limits the Resize direction when resiaing the annotation using the mouse, e.g in the X-Direction, Y-Direction or XyDirection. See the <see cref="XyDirection"/> enumeration for options
        /// </summary>
        public XyDirection ResizeDirections
        {
            get { return (XyDirection)GetValue(ResizeDirectionsProperty); }
            set { SetValue(ResizeDirectionsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="AnnotationCoordinateMode"/> to use when placing the annotation. E.g. the default of Absolute requires that X1,Y1 coordinates are data-values. The value
        /// of Relative requires that X1,Y1 are double values from 0.0 to 1.0
        /// </summary>
        public AnnotationCoordinateMode CoordinateMode
        {
            get { return (AnnotationCoordinateMode)GetValue(CoordinateModeProperty); }
            set { SetValue(CoordinateModeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="AnnotationCanvas"/> to place the annotation on. The default is <see cref="Annotations.AnnotationCanvas.AboveChart"/>
        /// </summary>
        public AnnotationCanvas AnnotationCanvas
        {
            get { return (AnnotationCanvas)GetValue(AnnotationCanvasProperty); }
            set { SetValue(AnnotationCanvasProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the current annotation is selected. When selected, an Adorner is placed over the annotation to allow dynamic resizing and dragging by the user. 
        /// </summary>
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the current annotation is editable. When editable, the user may click to select and interact with the annotation
        /// </summary>
        public bool IsEditable
        {
            get { return (bool) GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }

        /// <summary>
        /// Gets or sets value, indicates whether current annotation was hidden by <see cref="Hide"/> call
        /// </summary>
        public bool IsHidden
        {
            get { return (bool)GetValue(IsHiddenProperty); }
            set { SetValue(IsHiddenProperty, value); }
        }

        /// <summary>
        /// Gets or sets the X1 Coordinate of the Annotation. 
        /// 
        /// For <see cref="AnnotationCoordinateMode.Absolute"/>, this must be a data-value on the X-Axis such as a DateTime for <see cref="DateTimeAxis"/>, double for <see cref="NumericAxis"/> or integer index for <see cref="CategoryDateTimeAxis"/>.
        /// 
        /// For <see cref="AnnotationCoordinateMode.Relative"/>, this must be a double value between 0.0 and 1.0, where 0.0 is the far left of the XAxis and 1.0 is the far right.
        /// </summary>
        [TypeConverter(typeof(StringToAnnotationCoordinateConverter))]
        public IComparable X1
        {
            get { return (IComparable)GetValue(X1Property); }
            set { SetValue(X1Property, value); }
        }

        /// <summary>
        /// Gets or sets the X2 Coordinate of the Annotation. 
        /// 
        /// For <see cref="AnnotationCoordinateMode.Absolute"/>, this must be a data-value on the X-Axis such as a DateTime for <see cref="DateTimeAxis"/>, double for <see cref="NumericAxis"/> or integer index for <see cref="CategoryDateTimeAxis"/>.
        /// 
        /// For <see cref="AnnotationCoordinateMode.Relative"/>, this must be a double value between 0.0 and 1.0, where 0.0 is the far left of the XAxis and 1.0 is the far right.
        /// </summary>
        [TypeConverter(typeof(StringToAnnotationCoordinateConverter))]
        public IComparable X2
        {
            get { return (IComparable)GetValue(X2Property); }
            set { SetValue(X2Property, value); }
        }

        /// <summary>
        /// Gets or sets the Y1 Coordinate of the Annotation.
        /// 
        /// For <see cref="AnnotationCoordinateMode.Absolute"/>, this must be a data-value on the Y-Axis such as a double for <see cref="NumericAxis"/> 
        /// 
        /// For <see cref="AnnotationCoordinateMode.Relative"/>, this must be a double value between 0.0 and 1.0, where 0.0 is the top of the YAxis and 1.0 is the bottom
        /// </summary>
        [TypeConverter(typeof(StringToAnnotationCoordinateConverter))]
        public IComparable Y1
        {
            get { return (IComparable)GetValue(Y1Property); }
            set { SetValue(Y1Property, value); }
        }

        /// <summary>
        /// Gets or sets the Y2 Coordinate of the Annotation. 
        /// 
        /// For <see cref="AnnotationCoordinateMode.Absolute"/>, this must be a data-value on the Y-Axis such as a double for <see cref="NumericAxis"/> 
        /// 
        /// For <see cref="AnnotationCoordinateMode.Relative"/>, this must be a double value between 0.0 and 1.0, where 0.0 is the top of the YAxis and 1.0 is the bottom
        /// </summary>
        [TypeConverter(typeof(StringToAnnotationCoordinateConverter))]
        public IComparable Y2
        {
            get { return (IComparable)GetValue(Y2Property); }
            set { SetValue(Y2Property, value); }
        }

        /// <summary>
        /// Gets the <see cref="Cursor"/> to use for the annotation when selected
        /// </summary>
        /// <returns></returns>
        protected abstract Cursor GetSelectedCursor();

        /// <summary>
        /// Raises the <see cref="AnnotationBase.DragStarted"/> event, called when a drag operation starts
        /// </summary>
        public virtual void OnDragStarted()
        {
            var handler = DragStarted;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="AnnotationBase.DragEnded"/> event, called when a drag operation ends
        /// </summary>
        public virtual void OnDragEnded()
        {
            var handler = DragEnded;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="AnnotationBase.DragDelta"/> event, called when a drag operation is in progress and each time the X1 Y1 X2 Y2 points update in the annotation
        /// </summary>
        public virtual void OnDragDelta()
        {
            var handler = DragDelta;
            if (handler != null) handler(this, new AnnotationDragDeltaEventArgs(0, 0));
        }

        /// <summary>
        /// Gets or sets whether this Element is attached to a parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is attached; otherwise, <c>false</c>.
        /// </value>
        public override bool IsAttached
        {
            get { return _isAttached; }
            set
            {
                _isAttached = value;

                if (!_templateApplied)
                {
                    ApplyTemplate();
                    _templateApplied = true;
                }
            }
        }

        /// <summary>
        /// Gets the YAxis, which current annotation is bound to
        /// </summary>
        public override IAxis YAxis
        {
            get { return _yAxis ?? (_yAxis = GetYAxis(YAxisId)); }
        }

        /// <summary>
        /// Gets the XAxis, which current annotation is bound to
        /// </summary>
        public override IAxis XAxis
        {
            get { return _xAxis ?? (_xAxis = GetXAxis(XAxisId)); }
        }

        /// <summary>
        /// Gets the canvas over the Series on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        protected IAnnotationCanvas AnnotationOverlaySurface { get { return ParentSurface != null ? ParentSurface.AnnotationOverlaySurface : null; } }

        /// <summary>
        /// Gets the canvas under the Series on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        protected IAnnotationCanvas AnnotationUnderlaySurface { get { return ParentSurface != null ? ParentSurface.AnnotationUnderlaySurface : null; } }

        /// <summary>
        /// Raises notification when parent <see cref="UltrachartSurface.XAxes"/> changes.
        /// </summary>
        void IAnnotation.OnXAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            this.Schedule(DispatcherPriority.DataBind, () =>
            {
                // Reset cached value
                _xAxis = GetXAxis(XAxisId);

                OnXAxesCollectionChanged(sender, args);
            });
        }

        /// <summary>
        /// Raises notification when parent <see cref="UltrachartSurface.YAxes"/> changes.
        /// </summary>
        void IAnnotation.OnYAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            this.Schedule(DispatcherPriority.DataBind, () =>
            {
                // Reset cached value
                _yAxis = GetYAxis(YAxisId);

                OnYAxesCollectionChanged(sender, args);
            });
        }

        private void OnAxisAlignmentChanged(object sender, AxisAlignmentChangedEventArgs e)
        {
            if (e.AxisId == XAxisId || e.AxisId == YAxisId)
            {
                var axis = e.AxisId == XAxisId ? XAxis : YAxis;

                OnAxisAlignmentChanged(axis, e.OldAlignment);
            }
        }

        /// <summary>
        /// Virtual method to override if you wish to be notified that the <see cref="IAxis.AxisAlignment"/> has changed
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="oldAlignment"></param>
        protected virtual void OnAxisAlignmentChanged(IAxis axis, AxisAlignment oldAlignment)
        {
        }

        /// <summary>
        /// Virtual method to override if you wish to be notified that the parent <see cref="UltrachartSurface.XAxes"/> has changed
        /// </summary>
        protected virtual void OnXAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
        }

        /// <summary>
        /// Virtual method to override if you wish to be notified that the parent <see cref="UltrachartSurface.YAxes"/> has changed
        /// </summary>
        protected virtual void OnYAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
        }

        /// <summary>
        /// Virtual method to override if you wish to be notified that the <see cref="YAxisId"/> has changed
        /// </summary>
        protected virtual void OnYAxisIdChanged()
        {
        }

        /// <summary>
        /// Virtual method to override if you wish to be notified that the <see cref="XAxisId"/> has changed
        /// </summary>
        protected virtual void OnXAxisIdChanged()
        {
        }

        /// <summary>
        /// Focuses the input text area. Applicable only for Text and label annotations
        /// </summary>
        protected virtual void FocusInputTextArea()
        {
        }

        /// <summary>
        /// Remove focus from input text area. Applicable only for Text and label annotation
        /// </summary>
        protected virtual void RemoveFocusInputTextArea()
        { 
        }

        /// <summary>
        /// Called when the Annotation is attached to parent surface
        /// </summary>
        /// <remarks></remarks>
        public override void OnAttached()
        {
            AttachInteractionHandlersTo(this);

            Loaded += OnAnnotationLoaded;

            ParentSurface.AxisAlignmentChanged += OnAxisAlignmentChanged;
        }

        /// <summary>
        /// Gets called as soon as the Loaded event occurs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnAnnotationLoaded(object sender, RoutedEventArgs e)
        {
            PrepareForRendering();
        }

        private void PrepareForRendering()
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
            }

            Refresh();
            
            PerformFocusOnInputTextArea();
        }

        protected void PerformFocusOnInputTextArea()
        {
            if (CanEditText && IsSelected)
            {
                FocusInputTextArea();
            }
            else
            {
                RemoveFocusInputTextArea();
            }
        }

        /// <summary>
        /// Attaches handlers to particular events of passed object
        /// </summary>
        /// <param name="source">Mouse events source</param>
        protected virtual void AttachInteractionHandlersTo(FrameworkElement source)
        {
            source.MouseLeftButtonDown += OnAnnotationMouseDown;

            source.MouseLeftButtonUp += OnAnnotationMouseUp;

            source.MouseMove += OnAnnotationMouseMove;

#if !SILVERLIGHT
            source.PreviewMouseDown += PreviewMouseDownHandler;
            source.PreviewMouseUp += PreviewMouseUpHandler;
#endif
        }

        /// <summary>
        /// Contains interaction logic of handling mouse down event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnAnnotationMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = TrySelectAnnotation();

            if (IsSelected && IsEditable)
            {
                _isMouseLeftDown = true;

                _startPoint = e.GetPosition(RootGrid as UIElement);

#if !SILVERLIGHT
                _mouseLeftDownTimestamp = DateTime.UtcNow;
#endif
                CaptureMouse();

                e.Handled = true;

                this.OnDragStarted();
            }
        }

        /// <summary>
        /// Contains interaction logic of handling mouse up event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnAnnotationMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                this.OnDragEnded();
            }
            else
            {
                //CanEditText = true
               PerformFocusOnInputTextArea();
            }

            ReleaseMouseCapture();
            _isMouseLeftDown = false;
        }

        /// <summary>
        /// Contains interaction logic of handling mouse move event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnAnnotationMouseMove(object sender, MouseEventArgs e)
        {
            var mousePoint = e.GetPosition(RootGrid as UIElement);

            if (!_isMouseLeftDown
#if !SILVERLIGHT
                || (DateTime.UtcNow - _mouseLeftDownTimestamp) < TimeSpan.FromMilliseconds(2) || e.LeftButton != MouseButtonState.Pressed
#endif
            )
            {
                return;
            }

            var canvas = GetCanvas(AnnotationCanvas);
            
            var yCalc = YAxis != null ? YAxis.GetCurrentCoordinateCalculator() : null;
            var xCalc = XAxis != null ? XAxis.GetCurrentCoordinateCalculator() : null;

            if (_isDragging)
            {                
                var offsetX = mousePoint.X - _startPoint.X;
                var offsetY = mousePoint.Y - _startPoint.Y;

                offsetX = DragDirections == XyDirection.YDirection ? 0 : offsetX;
                offsetY = DragDirections == XyDirection.XDirection ? 0 : offsetY;

                using (SuspendUpdates())
                {
                    MoveAnnotationTo(_startDragAnnotationCoordinates, offsetX, offsetY);
                }

                this.OnDragDelta();
                return;
            }

            _startPoint = mousePoint;

            _isDragging = true;

            _startDragAnnotationCoordinates = GetCoordinates(canvas, xCalc, yCalc);
        }

        /// <summary>
        /// Called immediately before the Annotation is detached from its parent surface
        /// </summary>
        /// <remarks></remarks>
        public override void OnDetached()
        {
            using (var s = SuspendUpdates())
            {
                s.ResumeTargetOnDispose = false;
                IsSelected = false;

                MakeInvisible();

                // Guarantees the removal of an annotation, for sometimes
                // IAnnotationCanvas has already been detached before
                var canvas = Parent as IAnnotationCanvas;
                canvas.SafeRemoveChild(this);

                DetachInteractionHandlersFrom(this);

                if (ParentSurface != null)
                    ParentSurface.AxisAlignmentChanged -= OnAxisAlignmentChanged;

                Loaded -= OnAnnotationLoaded;
            }
        }

        /// <summary>
        /// When called in a derived class, detaches any mouse events which may have been previously attached to the <see cref="AnnotationBase"/>
        /// </summary>
        /// <param name="source"></param>
        protected virtual void DetachInteractionHandlersFrom(FrameworkElement source)
        {
            source.MouseLeftButtonDown -= OnAnnotationMouseDown;

            source.MouseLeftButtonUp -= OnAnnotationMouseUp;

            source.MouseMove -= OnAnnotationMouseMove;

#if !SILVERLIGHT
            source.PreviewMouseDown -= PreviewMouseDownHandler;
            source.PreviewMouseUp -= PreviewMouseUpHandler;
#endif
        }

        /// <summary>
        /// Refreshes the annnotation position on the parent <see cref="UltrachartSurface"/>, without causing a full redraw of the chart
        /// </summary>
        public bool Refresh()
        {
            if (IsSuspended || !_isLoaded || !IsAttached)
                return false;

            var xCalc = XAxis != null ? XAxis.GetCurrentCoordinateCalculator() : null;
            var yCalc = YAxis != null ? YAxis.GetCurrentCoordinateCalculator() : null;

            if (xCalc != null && yCalc != null)
            {
                Update(xCalc, yCalc);
            }

            return true;
        }

        /// <summary>
        /// Updates the coordinate calculators and refreshes the annotation position on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="xCoordinateCalculator">The XAxis <see cref="ICoordinateCalculator{T}" /></param>
        /// <param name="yCoordinateCalculator">The YAxis <see cref="ICoordinateCalculator{T}" /></param>
        public virtual void Update(ICoordinateCalculator<double> xCoordinateCalculator, ICoordinateCalculator<double> yCoordinateCalculator)
        {
            var canvas = GetCanvas(AnnotationCanvas);

            if (canvas == null) return;

            // Place annotation on canvas if haven't been already placed
            canvas.SafeAddChild(this);

            if(!_isLoaded) return;

            var coordinates = GetCoordinates(canvas, xCoordinateCalculator, yCoordinateCalculator);

            if (IsInBounds(coordinates, canvas))
            {
                if (!IsHidden)
                {
                    MakeVisible(coordinates);
                }
                else if (Visibility != Visibility.Collapsed)
                {
                    MakeInvisible();
                }
            }
            else
            {
                MakeInvisible();
            }
        }

        /// <summary>
        /// Hides the Annotation by removing adorner markers from the parent <see cref="UltrachartSurface.AdornerLayerCanvas"/>
        /// and setting Visibility to Collapsed
        /// </summary>
        public void Hide()
        {
            IsHidden = true;
        }

        /// <summary>
        /// Shows annotation which being hidden by <see cref="Hide"/> call
        /// </summary>
        public void Show()
        {
            IsHidden = false;
        }

        /// <summary>
        /// Called internally by layout system when annotation is out of surface's bounds
        /// </summary>
        protected virtual void MakeInvisible()
        {
            HideAdornerMarkers();

            Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Hides adorner markers by calling <see cref="AdornerBase.Clear"/>
        /// </summary>
        protected void HideAdornerMarkers()
        {
            foreach (var adorner in _myAdorners)
            {
                adorner.Clear();
            }
        }

        /// <summary>
        /// Gets a collection of the Adorners currently used on the Annotation, given the Annotation AdornerLayer Canvas
        /// </summary>
        /// <typeparam name="T">The type of Adorners to search for</typeparam>
        /// <param name="adornerLayer">The adorner layer canvas</param>
        /// <returns>A list of adorners matching type T</returns>
        protected IEnumerable<T> GetUsedAdorners<T>(Canvas adornerLayer) where T:IAnnotationAdorner
        {
            return adornerLayer.Children.
                OfType<T>().
                Where(x => x.AdornedAnnotation == this).
                ToList();
        }

        /// <summary>
        /// Called internally by layout system when annotation come into surface's bounds
        /// </summary>
        protected virtual void MakeVisible(AnnotationCoordinates coordinates)
        {
            Visibility = Visibility.Visible;

            // This check is required for Silvelight - 
            // annotation has to be added to VisualTree (placed on Canvas),
            // then ApplyTemplate is called and then we can do calculations
            // in PlaceAnnotation(...)
            if (AnnotationRoot != null)
            {
                // Measure before showing (if wasn't measured earlier),
                // size is used in PlaceAnnotation(...)
                if (!_isLoaded || AnnotationRoot.RenderSize == default(Size))
                {
                    AnnotationRoot.MeasureArrange();
                }

                PlaceAnnotation(coordinates);
            }

            UpdateAdorners();
        }

        internal void UpdateAdorners()
        {
            var adornerCanvas = GetAdornerLayer();
            if (adornerCanvas == null) return;

            var adorners = GetUsedAdorners<IAnnotationAdorner>(adornerCanvas);

            adorners.ForEachDo(adorner => adorner.UpdatePositions());
        }

        /// <summary>
        /// Performs a simple rectangular bounds-check to see if the X1,X2,Y1,Y2 coordinates passed in are within the Canvas extends
        /// </summary>
        /// <param name="coordinates">The normalised AnnotationCoordinates</param>
        /// <param name="canvas">The canvas to check if the annotation is within bounds</param>
        /// <returns>True if in bounds</returns>
        protected virtual bool IsInBounds(AnnotationCoordinates coordinates, IAnnotationCanvas canvas)
        {
            return GetCurrentPlacementStrategy().IsInBounds(coordinates, canvas);
        }

        /// <summary>
        /// Override in derived classes to handle specific placement of the annotation at the given <see cref="AnnotationCoordinates"/>
        /// </summary>
        /// <param name="coordinates">The normalised <see cref="AnnotationCoordinates"/></param>
        protected virtual void PlaceAnnotation(AnnotationCoordinates coordinates)
        {
            GetCurrentPlacementStrategy().PlaceAnnotation(coordinates);
        }

        /// <summary>
        /// Gets the Canvas instance for this annotation
        /// </summary>
        /// <param name="annotationCanvas">The <see cref="AnnotationCanvas"/> enumeration</param>
        /// <returns>The canvas instance</returns>
        protected IAnnotationCanvas GetCanvas(AnnotationCanvas annotationCanvas)
        {
            if (ParentSurface == null) return null;

            if (annotationCanvas == AnnotationCanvas.AboveChart)
                return ParentSurface.AnnotationOverlaySurface;

            if (annotationCanvas == AnnotationCanvas.BelowChart)
                return ParentSurface.AnnotationUnderlaySurface;

            if (annotationCanvas == AnnotationCanvas.YAxis)
            {
                return YAxis != null ? YAxis.ModifierAxisCanvas : null;
            }

            if (annotationCanvas == AnnotationCanvas.XAxis)
            {
                return XAxis != null ? XAxis.ModifierAxisCanvas : null;
            }

            throw new InvalidOperationException(string.Format("Cannot get an annotation surface for AnnotationCanvas.{0}", annotationCanvas));
        }

        protected virtual IAnnotationPlacementStrategy GetCurrentPlacementStrategy()
        {
            if (XAxis != null && XAxis.IsPolarAxis)
            {
                return new PolarAnnotationPlacementStrategyBase<AnnotationBase>(this);
            }

            return new CartesianAnnotationPlacementStrategyBase<AnnotationBase>(this);
        }

        /// <summary>
        /// DependencyProperty changed handler which can be used to refresh the annotation on property changed
        /// </summary>
        /// <param name="d">The DependencyObject sender.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        protected static void OnRenderablePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AnnotationBase)d).Refresh();
        }

        /// <summary>
        /// DependencyProperty changed handler which can be used to refresh the annotation on property and position changed 
        /// </summary>
        protected static void OnAnnotationPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AnnotationBase)d).Refresh();
            ((AnnotationBase)d).OnPropertyChanged("PositionChanged");
        }

        /// <summary>
        /// Updates the position and values of the annotation during a drag or resize operation, by setting X1,Y1,X2,Y2 and X,Y pixel coordinates together, 
        /// from a pixel coordinate input
        /// </summary>
        /// <param name="point1">The first input pixel coordinate</param>
        /// <param name="point2">The second input pixel coordinate</param>
        public void UpdatePosition(Point point1, Point point2)
        {
            using(SuspendUpdates())
            {
                var dataValues = FromCoordinates(point1);

                this.SetCurrentValue(X1Property, dataValues[0]);
                this.SetCurrentValue(Y1Property, dataValues[1]);

                if (!(this is IAnchorPointAnnotation))
                {
                    dataValues = FromCoordinates(point2);

                    this.SetCurrentValue(X2Property, dataValues[0]);
                    this.SetCurrentValue(Y2Property, dataValues[1]);
                }
            }
        }


        /// <summary>
        /// Converts pixel coordinates to data-values
        /// </summary>
        /// <param name="coords">The X, Y coordinates </param>
        /// <returns>
        /// The data values
        /// </returns>
        protected virtual IComparable[] FromCoordinates(Point coords)
        {
            return FromCoordinates(coords.X, coords.Y);
        }

        /// <summary>
        /// Converts pixel coordinates to data-values
        /// </summary>
        /// <param name="xCoord">The X coordinate</param>
        /// <param name="yCoord">The Y coordinate</param>
        /// <returns>
        /// The data values
        /// </returns>
        protected virtual IComparable[] FromCoordinates(double xCoord, double yCoord)
        {
            var dataValues = new IComparable[2];

            dataValues[0] = XAxis.IsHorizontalAxis ? FromCoordinate(xCoord, XAxis) : FromCoordinate(yCoord, XAxis);
            dataValues[1] = XAxis.IsHorizontalAxis ? FromCoordinate(yCoord, YAxis) : FromCoordinate(xCoord, YAxis);

            return dataValues;
        }

        /// <summary>
        /// Converts a pixel coordinate to data-value
        /// </summary>
        /// <param name="coord">The pixel coordinate.</param>
        /// <param name="axis">The axis for which the data value is calculated</param>
        /// <returns>
        /// The datavalue
        /// </returns>
        protected virtual IComparable FromCoordinate(double coord, IAxis axis)
        {
            IComparable result;

            var direction = axis.IsHorizontalAxis ? XyDirection.XDirection : XyDirection.YDirection;

            if (CoordinateMode == AnnotationCoordinateMode.Relative ||
    (CoordinateMode == AnnotationCoordinateMode.RelativeX && direction == XyDirection.XDirection) ||
    (CoordinateMode == AnnotationCoordinateMode.RelativeY && direction == XyDirection.YDirection))
            {
                result = FromRelativeCoordinate(coord, axis);
            }
            else
            {
                // If CategoryDateTimeAxis, get dataPoint Index
                var categoryCalc = axis.GetCurrentCoordinateCalculator() as ICategoryCoordinateCalculator;
                if (categoryCalc != null)
                {
                    result = (int)categoryCalc.GetDataValue(coord);
                }
                else
                {
                    // Get dataValue at passed coord or default value
                    result = axis.GetDataValue(coord);
                }
            }

            return result;
        }

        /// <summary>
        /// Converts a relative coordinate (e.g. 0.0 to 1.0) to data-value
        /// </summary>
        /// <param name="coord">The relative coordinate, in the range of 0.0 to 1.0 for extremes of the viewport.</param>
        /// <param name="axis">The axis for which the data value is calculated</param>
        /// <returns>
        /// The datavalue
        /// </returns>
        protected virtual IComparable FromRelativeCoordinate(double coord, IAxis axis)
        {
            var canvas = GetCanvas(AnnotationCanvas);

            var canvasMeasurement = axis.IsHorizontalAxis ? canvas.ActualWidth : canvas.ActualHeight;

            var result = coord / canvasMeasurement;

            return result;
        }
        
        /// <summary>
        /// Converts a data value to pixel coordinate
        /// </summary>
        /// <param name="dataValue">The data value to convert</param>
        /// <param name="axis">The axis for which the coordinate is calculated</param>
        /// <returns>The coordinate in pixels</returns>
        protected double ToCoordinate(IComparable dataValue, IAxis axis)
        {
            var canvas = GetCanvas(AnnotationCanvas);
            var coordCalc = axis.GetCurrentCoordinateCalculator();

            var direction = coordCalc.IsHorizontalAxisCalculator ? XyDirection.XDirection : XyDirection.YDirection;
            var canvasMeasurement = coordCalc.IsHorizontalAxisCalculator ? canvas.ActualWidth : canvas.ActualHeight;

            return ToCoordinate(dataValue, canvasMeasurement, coordCalc, direction);
        }

        /// <summary>
        /// Converts a data-values to pixel coordinates
        /// </summary>
        /// <param name="xDataValue">The X data-value, e.g. value of X1 or X2</param>
        /// <param name="yDataValue">The Y data-value, e.g. value of Y1 or Y2 </param>
        /// <param name="canvas">The <see cref="AnnotationCanvas"/> </param>
        /// <param name="xCoordCalc">The current X <see cref="ICoordinateCalculator{T}" /> valid for the current render pass</param>
        /// <param name="yCoordCalc">The current Y <see cref="ICoordinateCalculator{T}" /> valid for the current render pass</param>
        /// <returns>
        /// The pixel coordinates
        /// </returns>
        protected virtual Point ToCoordinates(IComparable xDataValue, IComparable yDataValue, IAnnotationCanvas canvas, ICoordinateCalculator<double> xCoordCalc, ICoordinateCalculator<double> yCoordCalc)
        {
            var xCoord = GetCoordinate(xDataValue, canvas, xCoordCalc);
            var yCoord = GetCoordinate(yDataValue, canvas, yCoordCalc);

            if(xCoordCalc != null && !xCoordCalc.IsHorizontalAxisCalculator)
            {
                NumberUtil.Swap(ref xCoord, ref yCoord);
            }

            return new Point(xCoord, yCoord);
        }

        private double GetCoordinate(IComparable dataValue, IAnnotationCanvas canvas, ICoordinateCalculator<double> coordCalc)
        {
            if (coordCalc == null) return 0;

            var direction = coordCalc.IsHorizontalAxisCalculator ? XyDirection.XDirection : XyDirection.YDirection;
            var canvasMeasurement = coordCalc.IsHorizontalAxisCalculator ? canvas.ActualWidth : canvas.ActualHeight;

            var coord = ToCoordinate(dataValue,
                                     canvasMeasurement,
                                     coordCalc,
                                     direction);

            return coord;
        }

        /// <summary>
        /// Converts a Data-Value to Pixel Coordinate
        /// </summary>
        /// <param name="dataValue">The Data-Value to convert</param>
        /// <param name="canvasMeasurement">The size of the canvas in the X or Y direction</param>
        /// <param name="coordCalc">The current <see cref="ICoordinateCalculator{T}">Coordinate Calculator</see></param>
        /// <param name="direction">The X or Y direction for the transformation</param>
        /// <returns></returns>
        protected virtual double ToCoordinate(IComparable dataValue, double canvasMeasurement, ICoordinateCalculator<double> coordCalc, XyDirection direction)
        {
            if(dataValue == null)
            {
                return double.NaN;
            }

            if (CoordinateMode == AnnotationCoordinateMode.Relative ||
                (CoordinateMode == AnnotationCoordinateMode.RelativeX && direction == XyDirection.XDirection) ||
                (CoordinateMode == AnnotationCoordinateMode.RelativeY && direction == XyDirection.YDirection))
            {
                return dataValue.ToDouble() * canvasMeasurement;
            }

            if (coordCalc.IsCategoryAxisCalculator && dataValue is DateTime)
            {
                return GetCategoryCoordinate(dataValue, coordCalc as ICategoryCoordinateCalculator);
            }

            return coordCalc.GetCoordinate(dataValue.ToDouble());
        }

        private double GetCategoryCoordinate(IComparable dataValue, ICategoryCoordinateCalculator categoryCalc)
        {
            var exactIndex = categoryCalc.TransformDataToIndex((DateTime)dataValue, SearchMode.Exact);
            if (exactIndex != -1)
            {
                return categoryCalc.GetCoordinate(exactIndex);
            }

            var lowerIndex = categoryCalc.TransformDataToIndex((DateTime)dataValue, SearchMode.RoundDown);
            var upperIndex = categoryCalc.TransformDataToIndex((DateTime)dataValue, SearchMode.RoundUp);

            //find values, between which dataValue is
            var lowerDate = categoryCalc.TransformIndexToData(lowerIndex);
            var upperDate = categoryCalc.TransformIndexToData(upperIndex);

            //find ticks range difference
            var dateDiff = upperDate.ToDouble() - lowerDate.ToDouble();
            //find relative coef 
            var coef = (dataValue.ToDouble() - lowerDate.ToDouble()) / dateDiff;

            //find coords of upper and lower values
            var lowerCoord = categoryCalc.GetCoordinate(lowerIndex);
            var upperCoord = categoryCalc.GetCoordinate(upperIndex);

            //find pixel range difference
            var coordDiff = upperCoord - Math.Max(lowerCoord, 0);
            //if coordDiff == 0, it is out of bounds
            //multiply coord range diff to relative coef to find exact position
            var pos = coordDiff > 0 ? lowerCoord + coordDiff * coef : -1;

            return pos;
        }

        /// <summary>
        /// Gets an <see cref="AnnotationCoordinates"/> struct containing pixel coordinates to place or update the annotation in the current render pass
        /// </summary>
        /// <param name="canvas">The canvas the annotation will be placed on</param>
        /// <param name="xCalc">The current XAxis <see cref="ICoordinateCalculator{T}"/> to perform data to pixel transformations</param>
        /// <param name="yCalc">The current YAxis <see cref="ICoordinateCalculator{T}"/> to perform data to pixel transformations</param>
        /// <returns>The <see cref="AnnotationCoordinates"/> struct containing pixel coordinates</returns>
        protected AnnotationCoordinates GetCoordinates(IAnnotationCanvas canvas, ICoordinateCalculator<double> xCalc, ICoordinateCalculator<double> yCalc)
        {
            var x1y1Coords = ToCoordinates(X1, Y1, canvas, xCalc, yCalc);
            var x2y2Coords = ToCoordinates(X2, Y2, canvas, xCalc, yCalc);

            var defaultOffset = 0d;

            var result = new AnnotationCoordinates
            {
                X1Coord = x1y1Coords.X,
                Y1Coord = x1y1Coords.Y,
                X2Coord = x2y2Coords.X,
                Y2Coord = x2y2Coords.Y,
                YOffset = YAxis != null ? YAxis.GetAxisOffset() : defaultOffset,
                XOffset = XAxis != null ? XAxis.GetAxisOffset() : defaultOffset
            };

            return result;
        }

        /// <summary>
        /// This method is used internally by the <see cref="AnnotationDragAdorner"/>. Programmatically moves the annotation by an X,Y offset. 
        /// </summary>
        /// <param name="horizOffset">The horizontal offset to move in pixels</param>
        /// <param name="vertOffset">The vertical offset to move in pxiels</param>
        public void MoveAnnotation(double horizOffset, double vertOffset)
        {
            if (!IsEditable) return;

            var canvas = GetCanvas(AnnotationCanvas);

            if(XAxis == null || YAxis == null)
                return;
            
            var yCalc = YAxis.GetCurrentCoordinateCalculator();
            var xCalc = XAxis.GetCurrentCoordinateCalculator();

            using (SuspendUpdates())
            {
                var coordinates = GetCoordinates(canvas, xCalc, yCalc);

                MoveAnnotationTo(coordinates, horizOffset, vertOffset);
            }
        }

        /// <summary>
        /// Moves the annotation to a specific horizontal and vertical offset
        /// </summary>
        /// <param name="coordinates">The initial coordinates.</param>
        /// <param name="horizOffset">The horizontal offset.</param>
        /// <param name="vertOffset">The vertical offset.</param>
        protected virtual void MoveAnnotationTo(AnnotationCoordinates coordinates, double horizOffset, double vertOffset)
        {
            var canvas = GetCanvas(AnnotationCanvas);

            GetCurrentPlacementStrategy().MoveAnnotationTo(coordinates, horizOffset, vertOffset, canvas);
        }

        /// <summary>
        /// Determines whether the coordinate is valid for placing on the canvas, e.g. is greater than zero and less than <paramref name="canvasMeasurement"/>
        /// </summary>
        /// <param name="coord">The coordinate.</param>
        /// <param name="canvasMeasurement">The canvas dimension in this axis.</param>
        /// <returns>
        ///   <c>true</c> if coordinate is valid; otherwise, <c>false</c>.
        /// </returns>
        protected bool IsCoordinateValid(double coord, double canvasMeasurement)
        {
            return coord >= 0 && coord < canvasMeasurement;
        }

        /// <summary>
        /// This method is used in internally by the <see cref="AnnotationResizeAdorner" />. Gets the adorner point positions
        /// </summary>
        /// <returns></returns>
        public Point[] GetBasePoints()
        {
            var canvas = GetCanvas(AnnotationCanvas);

            var xCalc = XAxis != null ? XAxis.GetCurrentCoordinateCalculator() : null;
            var yCalc = YAxis != null ? YAxis.GetCurrentCoordinateCalculator() : null;

            var coords = GetCoordinates(canvas, xCalc, yCalc);

            return GetBasePoints(coords);
        }

        /// <summary>
        /// This method is used in internally by the <see cref="AnnotationResizeAdorner" />. Gets the adorner point positions
        /// </summary>
        /// <param name="coordinates">The previously calculated <see cref="AnnotationCoordinates"/> in screen pixels.</param>
        /// <returns>A list of points in screen pixels denoting the Adorner corners</returns>
        protected virtual Point[] GetBasePoints(AnnotationCoordinates coordinates)
        {
            return GetCurrentPlacementStrategy().GetBasePoints(coordinates);
        }

        /// <summary>
        /// This method is used in internally by the <see cref="AnnotationResizeAdorner" />. Programmatically sets an adorner point position
        /// </summary>
        /// <param name="newPoint"></param>
        /// <param name="index"></param>
        public void SetBasePoint(Point newPoint, int index)
        {
            if (!IsEditable) return;

            using (SuspendUpdates())
            {
                GetCurrentPlacementStrategy().SetBasePoint(newPoint, index);
            }
        }

        /// <summary>
        /// Called internally to marshal pixel points to X1,Y1,X2,Y2 values. 
        /// Taking a pixel point (<paramref name="newPoint"/>) and base point <paramref name="index"/>, sets the X,Y data-values. 
        /// </summary>
        /// <param name="newPoint">The pixel point</param>
        /// <param name="index">The base point index, where 0, 1, 2, 3 refer to the four corners of an Annotation</param>
        /// <param name="yAxis">The current Y-Axis</param>
        /// <param name="xAxis">The current X-Axis </param>
        protected virtual void SetBasePoint(Point newPoint, int index, IAxis xAxis, IAxis yAxis)
        {
            var dataValues = FromCoordinates(newPoint);

            DependencyProperty X, Y;
            GetPropertiesFromIndex(index, out X, out Y);

            this.SetCurrentValue(X, dataValues[0]);
            this.SetCurrentValue(Y, dataValues[1]);
        }

        /// <summary>
        /// Used internally to derive the X1Property, Y1Property, X1Property, Y2Property pair for the given index around the annotation..
        /// 
        /// e.g. index 0 returns X1,Y1
        /// index 1 returns X2,Y1
        /// index 2 returns X2,Y2
        /// index 3 returns X1,Y2
        /// </summary>
        /// <param name="index">The index</param>
        /// <param name="x">The X coordinate dependency property, either X1Property or X2Property</param>
        /// <param name="y">The Y coordinate dependency property, either Y1Property or Y2Property</param>
        protected virtual void GetPropertiesFromIndex(int index, out DependencyProperty x, out DependencyProperty y)
        {
            x = X1Property;
            y = Y1Property;

            switch (index)
            {
                case 0:
                    x = X1Property;
                    y = Y1Property;
                    break;
                case 1:
                    x = X2Property;
                    y = Y1Property;
                    break;
                case 2:
                    x = X2Property;
                    y = Y2Property;
                    break;
                case 3:
                    x = X1Property;
                    y = Y2Property;
                    break;
            }
        }

        protected virtual void HandleIsEditable()
        {
            var usedCursor = IsEditable ? GetSelectedCursor() : Cursors.Arrow;

            SetCurrentValue(CursorProperty, usedCursor);
            PerformFocusOnInputTextArea();
        }

        /// <summary>
        /// Gets the Adorner Canvas to place annotation adorners
        /// </summary>
        /// <returns></returns>
        protected Canvas GetAdornerLayer()
        {
            return ParentSurface != null ? ParentSurface.AdornerLayerCanvas : null;
        }

        /// <summary>
        /// When overriden in a derived class, places the appropriate adorners on the <see cref="AnnotationBase"/>
        /// </summary>
        /// <param name="adornerLayer">The adorner layer</param>
        protected virtual void AddAdorners(Canvas adornerLayer)
        {
            //var dragAdorner = new AnnotationDragAdorner(this) { ParentCanvas = adornerLayer, Services = Services };
            var adorner = new AnnotationResizeAdorner(this) { ParentCanvas = adornerLayer };
            _myAdorners.Add(adorner);
        }

        /// <summary>
        /// Removes all adorners from the <see cref="AnnotationBase"/>
        /// </summary>
        /// <param name="adornerLayer">The adorner layer</param>
        protected virtual void RemoveAdorners(Canvas adornerLayer)
        {
            var adorners = GetUsedAdorners<AdornerBase>(adornerLayer);

            adorners.ForEachDo(adorner => adorner.OnDetached());
        }

        /// <summary>
        /// Translates the point relative to the other <see cref="IHitTestable" /> element
        /// </summary>
        /// <param name="point">The input point relative to this <see cref="IHitTestable" /></param>
        /// <param name="relativeTo">The other <see cref="IHitTestable" /> to use when transforming the point</param>
        /// <returns>
        /// The transformed Point
        /// </returns>
        public virtual Point TranslatePoint(Point point, IHitTestable relativeTo)
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
        public virtual bool IsPointWithinBounds(Point point)
        {
            return HitTestableExtensions.IsPointWithinBounds(this, point);
        }

        /// <summary>
        /// Gets the bounds of the current <see cref="IHitTestable" /> element relative to another <see cref="IHitTestable" /> element
        /// </summary>
        /// <param name="relativeTo"></param>
        /// <returns></returns>
        public virtual Rect GetBoundsRelativeTo(IHitTestable relativeTo)
        {
            return ElementExtensions.GetBoundsRelativeTo(this, relativeTo);
        }

        /// <summary>
        /// Tries to select the annotation. Returns True if the operation was successful
        /// </summary>
        /// <returns>True if the operation was successful</returns>
        internal bool TrySelectAnnotation()
        {
            return ParentSurface != null && ParentSurface.Annotations.TrySelectAnnotation(this);
        }

        private void OnSelected()
        {
            var handler = Selected;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void OnUnselected()
        {
            var handler = Unselected;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void OnIsHiddenChanged()
        {
            var handler = IsHiddenChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Initiates the DragDelta event
        /// </summary>
        protected void OnAnnotationDragging(AnnotationDragDeltaEventArgs args)
        {
            var handler = DragDelta;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var annotation = d as AnnotationBase;

            if (annotation != null)
            {
                var adornerLayer = annotation.GetAdornerLayer();

                if ((bool)e.NewValue)
                {
                    annotation.AddAdorners(adornerLayer);

                    annotation.OnSelected();
                }
                else
                {
                    annotation.ReleaseMouseCapture();
                    annotation.RemoveAdorners(adornerLayer);

                    annotation.PerformFocusOnInputTextArea();

                    annotation.OnUnselected();
                }
            }
        }

        private static void OnIsEditableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as AnnotationBase)?.HandleIsEditable();
        }

        private static void OnIsHiddenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var annotation = d as AnnotationBase;
            
            if (annotation != null && annotation.IsAttached)
            {
                if ((bool)e.NewValue)
                {
                    annotation.MakeInvisible();
                }
                else
                {
                    var xCalc = annotation.XAxis != null ? annotation.XAxis.GetCurrentCoordinateCalculator() : null;
                    var yCalc = annotation.YAxis != null ? annotation.YAxis.GetCurrentCoordinateCalculator() : null;

                    var canvas = annotation.GetCanvas(annotation.AnnotationCanvas);

                    var coordinates = new AnnotationCoordinates();
                    if (xCalc != null && yCalc != null && canvas != null)
                    {
                        coordinates = annotation.GetCoordinates(canvas, xCalc, yCalc);
                    }

                    annotation.MakeVisible(coordinates);

                    annotation.Refresh();
                }

                annotation.OnIsHiddenChanged();
            }
        }

        private static void OnYAxisIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var annotation = d as AnnotationBase;

            if (annotation != null)
            {
                // Reset cached value
                annotation._yAxis = annotation.GetYAxis(annotation.YAxisId);

                annotation.OnYAxisIdChanged();
                ((AnnotationBase)d).Refresh();
            }
        }

        private static void OnXAxisIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var annotation = d as AnnotationBase;

            if (annotation != null)
            {
                // Reset cached value
                annotation._xAxis = annotation.GetXAxis(annotation.XAxisId);

                annotation.OnXAxisIdChanged();
                ((AnnotationBase)d).Refresh();
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether updates for the target are currently suspended
        /// </summary>
        public bool IsSuspended
        {
            get { return UpdateSuspender.GetIsSuspended(this); }
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
        /// Resumes the updates.
        /// </summary>
        /// <param name="updateSuspender">The update suspender.</param>
        public void ResumeUpdates(IUpdateSuspender updateSuspender)
        {
            if (updateSuspender.ResumeTargetOnDispose)
                Refresh();  
        }

        /// <summary>
        /// Called by IUpdateSuspender each time a target suspender is disposed. When the final
        /// target suspender has been disposed, ResumeUpdates is called
        /// </summary>
        public void DecrementSuspend()
        {
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
        /// Generates <see cref="AnnotationBase"/> from its XML representation.
        /// </summary>
        /// <param name="reader"></param>
        public virtual void ReadXml(XmlReader reader)
        {
            if (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == GetType().Name)
            {
                AnnotationSerializationHelper.Instance.DeserializeProperties(this, reader);
            }
        }

        /// <summary>
        /// Converts <see cref="AnnotationBase"/> into its XML representation.
        /// </summary>
        /// <param name="writer"></param>
        public virtual void WriteXml(XmlWriter writer)
        {
            AnnotationSerializationHelper.Instance.SerializeProperties(this, writer);
        }

        // Internal access for testing
        internal FrameworkElement RootElement
        {
            get { return AnnotationRoot; }
        }

        internal bool IsDragging
        {
            get { return _isDragging; }
        }

        internal bool IsMouseLeftDown
        {
            get { return _isMouseLeftDown; }
        }

        internal class CartesianAnnotationPlacementStrategyBase<T>:AnnotationPlacementStrategyBase<T> where T:AnnotationBase
        {
            public CartesianAnnotationPlacementStrategyBase(T annotation) : base(annotation)
            {
            }


            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {
                
            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                return null;
            }

            public override void SetBasePoint(Point newPoint, int index)
            {
                Annotation.SetBasePoint(newPoint, index, Annotation.XAxis, Annotation.YAxis);
            }

            public override bool IsInBounds(AnnotationCoordinates coordinates, IAnnotationCanvas canvas)
            {
                bool outOfBounds = (coordinates.X1Coord < 0 && coordinates.X2Coord < 0) ||
                                   (coordinates.X1Coord > canvas.ActualWidth && coordinates.X2Coord > canvas.ActualWidth) ||
                                   (coordinates.Y1Coord < 0 && coordinates.Y2Coord < 0) ||
                                   (coordinates.Y1Coord > canvas.ActualHeight &&
                                    coordinates.Y2Coord > canvas.ActualHeight);

                return !outOfBounds;
            }

            public override void MoveAnnotationTo(AnnotationCoordinates coordinates, double horizontalOffset, double verticalOffset, IAnnotationCanvas annotationCanvas)
            {
                InternalMoveAnntotationTo(coordinates, ref horizontalOffset, ref verticalOffset, annotationCanvas);

                Annotation.OnAnnotationDragging(new AnnotationDragDeltaEventArgs(horizontalOffset, verticalOffset));
            }

            protected virtual void InternalMoveAnntotationTo(AnnotationCoordinates coordinates, ref double horizOffset, ref double vertOffset, IAnnotationCanvas canvas)
            {
                var x1 = coordinates.X1Coord + horizOffset;
                var x2 = coordinates.X2Coord + horizOffset;
                var y1 = coordinates.Y1Coord + vertOffset;
                var y2 = coordinates.Y2Coord + vertOffset;

                // If any are out of bounds ... 
                if (!IsCoordinateValid(x1, canvas.ActualWidth) || !IsCoordinateValid(y1, canvas.ActualHeight) ||
                    !IsCoordinateValid(x2, canvas.ActualWidth) || !IsCoordinateValid(y2, canvas.ActualHeight))
                {
                    x1 = double.IsNaN(x1) ? 0 : x1;
                    x2 = double.IsNaN(x2) ? 0 : x2;
                    y1 = double.IsNaN(y1) ? 0 : y1;
                    y2 = double.IsNaN(y2) ? 0 : y2;

                    // Clip to bounds
                    if (Math.Max(x1, x2) < 0) horizOffset -= Math.Max(x1, x2);
                    if (Math.Min(x1, x2) > canvas.ActualWidth) horizOffset -= Math.Min(x1, x2) - (canvas.ActualWidth - 1);

                    if (Math.Max(y1, y2) < 0) vertOffset -= Math.Max(y1, y2);
                    if (Math.Min(y1, y2) > canvas.ActualHeight) vertOffset -= Math.Min(y1, y2) - (canvas.ActualHeight - 1);
                }

                // Reassign
                coordinates.X1Coord = coordinates.X1Coord + horizOffset;
                coordinates.X2Coord = coordinates.X2Coord + horizOffset;
                coordinates.Y1Coord = coordinates.Y1Coord + vertOffset;
                coordinates.Y2Coord = coordinates.Y2Coord + vertOffset;
                
                // Update corners
                Annotation.SetBasePoint(new Point(coordinates.X1Coord, coordinates.Y1Coord), 0, Annotation.XAxis, Annotation.YAxis);
                Annotation.SetBasePoint(new Point(coordinates.X2Coord, coordinates.Y2Coord), 2, Annotation.XAxis, Annotation.YAxis);
            }

            protected bool IsCoordinateValid(double coord, double canvasMeasurement)
            {
                return Annotation.IsCoordinateValid(coord, canvasMeasurement);
            }

            protected IComparable[] FromCoordinates(double xCoord, double yCoord)
            {
                return Annotation.FromCoordinates(xCoord, yCoord);
            }
        }

        internal class PolarAnnotationPlacementStrategyBase<T> : AnnotationPlacementStrategyBase<T>
            where T : AnnotationBase
        {
            private readonly ITransformationStrategy _transformationStrategy;

            public PolarAnnotationPlacementStrategyBase(T annotation) : base(annotation)
            {
                _transformationStrategy = annotation.Services.GetService<IStrategyManager>().GetTransformationStrategy();
            }

            protected ITransformationStrategy TransformationStrategy
            {
                get { return _transformationStrategy; }
            }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {

            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                return null;
            }

            public override void SetBasePoint(Point newPoint, int index)
            {
                var transformedPoint = TransformationStrategy.Transform(newPoint);

                Annotation.SetBasePoint(transformedPoint, index, Annotation.XAxis, Annotation.YAxis);
            }

            public override bool IsInBounds(AnnotationCoordinates coordinates, IAnnotationCanvas canvas)
            {
                var canvasSize = CalculateCanvasSize(canvas);

                return IsInBoundsInternal(coordinates, canvasSize);
            }

            private Size CalculateCanvasSize(IAnnotationCanvas panel)
            {
                var radius = PolarUtil.CalculateViewportRadius(panel.ActualWidth, panel.ActualHeight);

                // return (max degree, max radius)
                return new Size(360, radius);
            }

            protected virtual bool IsInBoundsInternal(AnnotationCoordinates coordinates, Size canvasSize)
            {
                bool outOfBounds = (coordinates.X1Coord < 0 && coordinates.X2Coord < 0) ||
                                   (coordinates.X1Coord > canvasSize.Width && coordinates.X2Coord > canvasSize.Width) ||
                                   (coordinates.Y1Coord < 0 && coordinates.Y2Coord < 0) ||
                                   (coordinates.Y1Coord > canvasSize.Height &&
                                    coordinates.Y2Coord > canvasSize.Height);

                return !outOfBounds;
            }

            public override void MoveAnnotationTo(AnnotationCoordinates coordinates, double horizontalOffset,
                double verticalOffset, IAnnotationCanvas annotationCanvas)
            {
                var offsets = CalculateAnnotationOffsets(coordinates, horizontalOffset, verticalOffset);
                var canvasSize = CalculateCanvasSize(annotationCanvas);

                InternalMoveAnntotationTo(coordinates, offsets.Item1, offsets.Item2, canvasSize);

                Annotation.OnAnnotationDragging(new AnnotationDragDeltaEventArgs(horizontalOffset, verticalOffset));
            }

            protected virtual Tuple<Point, Point> CalculateAnnotationOffsets(AnnotationCoordinates coordinates, double horizontalOffset, double verticalOffset)
            {
                var actualCoordinates = GetCartesianAnnotationCoordinates(coordinates);

                var x1y1Offset = CalculatePointOffset(new Point(actualCoordinates.X1Coord, actualCoordinates.Y1Coord),
                    horizontalOffset, verticalOffset);
                var x2y2Offset = CalculatePointOffset(new Point(actualCoordinates.X2Coord, actualCoordinates.Y2Coord),
                    horizontalOffset, verticalOffset);

                return new Tuple<Point, Point>(x1y1Offset, x2y2Offset);
            }

            protected virtual AnnotationCoordinates GetCartesianAnnotationCoordinates(AnnotationCoordinates coordinates)
            {
                var point1 = TransformationStrategy.ReverseTransform(new Point(coordinates.X1Coord, coordinates.Y1Coord));
                var point2 = TransformationStrategy.ReverseTransform(new Point(coordinates.X2Coord, coordinates.Y2Coord));

                var cartesianCoordinates = new AnnotationCoordinates()
                {
                    X1Coord = point1.X,
                    Y1Coord = point1.Y,
                    X2Coord = point2.X,
                    Y2Coord = point2.Y,
                };

                return cartesianCoordinates;
            }

            private Point CalculatePointOffset(Point point, double horizontalOffset, double verticalOffset)
            {
                var startPoint = TransformationStrategy.Transform(point);

                point.X += horizontalOffset;
                point.Y += verticalOffset;

                var endPoint = TransformationStrategy.Transform(point);

                var xOffset = endPoint.X - startPoint.X;
                var yOffset = endPoint.Y - startPoint.Y;

                var offset = new Point(xOffset, yOffset);

                return offset;
            }

            protected virtual void InternalMoveAnntotationTo(AnnotationCoordinates coordinates, Point x1y1Offset,
                Point x2y2Offset, Size canvasSize)
            {
                var x1 = coordinates.X1Coord + x1y1Offset.X;
                var x2 = coordinates.X2Coord + x2y2Offset.X;
                var y1 = coordinates.Y1Coord + x1y1Offset.Y;
                var y2 = coordinates.Y2Coord + x2y2Offset.Y;

                // If any are out of bounds ... 
                if (!IsCoordinateValid(x1, canvasSize.Width) || !IsCoordinateValid(y1, canvasSize.Height) ||
                    !IsCoordinateValid(x2, canvasSize.Width) || !IsCoordinateValid(y2, canvasSize.Height))
                {
                    x1 = double.IsNaN(x1) ? 0 : x1;
                    x2 = double.IsNaN(x2) ? 0 : x2;
                    y1 = double.IsNaN(y1) ? 0 : y1;
                    y2 = double.IsNaN(y2) ? 0 : y2;

                    // Clip to bounds
                    if (Math.Max(x1, x2) < 0)
                    {
                        var horizOffset = Math.Max(x1, x2);
                        x1y1Offset.X -= horizOffset;
                        x2y2Offset.X -= horizOffset;
                    }
                    if (Math.Min(x1, x2) > canvasSize.Width)
                    {
                        var horizOffset = Math.Min(x1, x2) - (canvasSize.Width - 1);
                        x1y1Offset.X -= horizOffset;
                        x2y2Offset.X -= horizOffset;
                    }

                    if (Math.Max(y1, y2) < 0)
                    {
                        var vertOffset = Math.Max(y1, y2);
                        x1y1Offset.Y -= vertOffset;
                        x2y2Offset.Y -= vertOffset;
                    }
                    if (Math.Min(y1, y2) > canvasSize.Height)
                    {
                        var vertOffset = Math.Min(y1, y2) - (canvasSize.Height - 1);
                        x1y1Offset.Y -= vertOffset;
                        x2y2Offset.Y -= vertOffset;
                    }
                }

                // Reassign
                coordinates.X1Coord = coordinates.X1Coord + x1y1Offset.X;
                coordinates.X2Coord = coordinates.X2Coord + x2y2Offset.X;
                coordinates.Y1Coord = coordinates.Y1Coord + x1y1Offset.Y;
                coordinates.Y2Coord = coordinates.Y2Coord + x2y2Offset.Y;

                Annotation.SetBasePoint(new Point(coordinates.X1Coord, coordinates.Y1Coord), 0, Annotation.XAxis,
                    Annotation.YAxis);
                Annotation.SetBasePoint(new Point(coordinates.X2Coord, coordinates.Y2Coord), 2, Annotation.XAxis,
                    Annotation.YAxis);
            }

            protected bool IsCoordinateValid(double coord, double canvasMeasurement)
            {
                return Annotation.IsCoordinateValid(coord, canvasMeasurement);
            }

            protected IComparable[] FromCoordinates(double xCoord, double yCoord)
            {
                return Annotation.FromCoordinates(xCoord, yCoord);
            }
        }
    }
}