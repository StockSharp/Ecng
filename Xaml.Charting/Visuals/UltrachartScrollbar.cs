// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UltrachartScrollbar.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Visuals
{
    /// <summary>
    /// A scrollbar which allows to scroll <see cref="IAxis"/> content
    /// </summary>
    [TemplatePart(Name = "PART_NonSelectedArea", Type = typeof(Path)),
     TemplatePart(Name = "PART_Border", Type = typeof(Border)),
     TemplatePart(Name = "PART_BottomThumb", Type = typeof(UltraThumb)),
     TemplatePart(Name = "PART_TopThumb", Type = typeof(UltraThumb)),
     TemplatePart(Name = "PART_LeftThumb", Type = typeof(UltraThumb)),
     TemplatePart(Name = "PART_MiddleThumb", Type = typeof(UltraThumb)),
     TemplatePart(Name = "PART_RightThumb", Type = typeof(UltraThumb))]
    public class UltrachartScrollbar : Control
    {
        /// <summary>
        /// Provides the Axis which this scrollbar control is associated with
        /// </summary>
        public static readonly DependencyProperty AxisProperty = DependencyProperty.Register("Axis", typeof (IAxis), typeof (UltrachartScrollbar), new PropertyMetadata(default(IAxis),OnAxisDependencyPropertyChanged));

        /// <summary>
        /// Selected range of the range slider
        /// </summary>
        public static readonly DependencyProperty SelectedRangeProperty = DependencyProperty.Register("SelectedRange", typeof(IRange), typeof(UltrachartScrollbar), new PropertyMetadata(OnSelectedRangeDependencyPropertyChanged));

        /// <summary>
        /// Defines the SelectedRangePoint DependencyProperty, used internally for animations
        /// </summary>
        public static readonly DependencyProperty SelectedRangePointProperty = DependencyProperty.Register("SelectedRangePoint", typeof(Point), typeof(UltrachartScrollbar), new PropertyMetadata(default(Point), OnSelectedRangePointDependencyPropertyChanged));

        /// <summary>
        /// Defines the GripsThickness DependencyProperty
        /// </summary>
        public static readonly DependencyProperty GripsThicknessProperty = DependencyProperty.Register("GripsThickness", typeof (double), typeof (UltrachartScrollbar), new PropertyMetadata(10d));

        /// <summary>
        /// Defines the GripsLength DependencyProperty
        /// </summary>
        public static readonly DependencyProperty GripsLengthProperty = DependencyProperty.Register("GripsLength", typeof (double), typeof (UltrachartScrollbar), new PropertyMetadata(double.NaN));

        /// <summary>
        /// Defines the GripsStyle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty GripsStyleProperty = DependencyProperty.Register("GripsStyle", typeof (Style), typeof (UltrachartScrollbar), new PropertyMetadata(default(Style)));

        /// <summary>
        /// Defines the ViewportStyle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ViewportStyleProperty = DependencyProperty.Register("ViewportStyle", typeof (Style), typeof (UltrachartScrollbar), new PropertyMetadata(default(Style)));

        /// <summary>
        /// Defines the NonSelectedArea DependencyProperty
        /// </summary>
        public static readonly DependencyProperty NonSelectedAreaStyleProperty = DependencyProperty.Register("NonSelectedAreaStyle", typeof (Style), typeof (UltrachartScrollbar), new PropertyMetadata(default(Style)));

        /// <summary>
        /// Defines The Orientation DependencyProperty
        /// </summary>
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof (Orientation), typeof (UltrachartScrollbar), new PropertyMetadata(Orientation.Horizontal));

        /// <summary>
        /// Defines the ZoomLimit DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ZoomLimitProperty = DependencyProperty.Register("ZoomLimit", typeof (double), typeof (UltrachartScrollbar), new PropertyMetadata(20d, OnZoomLimitDependencyPropertyChanged));

        private UltraThumb _centerThumb; //the center thumb to move the range around
        private UltraThumb _leftThumb;//the left thumb that is used to expand the range selected
        private UltraThumb _rightThumb;//the right thumb that is used to expand the range selected
        private UltraThumb _topThumb;
        private UltraThumb _bottomThumb;

        private Border _border;
        private Path _path;
        private RectangleGeometry _holeRectangle;

        private ScrollbarCalculationgHelper _helper;

        private readonly RenderTimerHelper _renderTimerHelper;
        private SelectedRangeEventType _eventType;
        
        /// <summary>
        /// Raised when the <see cref="SelectedRange"/> changes
        /// </summary>
        public event EventHandler<SelectedRangeChangedEventArgs> SelectedRangeChanged;

        /// <summary>
        /// Default constructor
        /// </summary>
        public UltrachartScrollbar()
        {
            DefaultStyleKey = typeof(UltrachartScrollbar);

            _renderTimerHelper = new RenderTimerHelper(OnInvalidateRenderTimer, new DispatcherUtil(this.Dispatcher));

            Loaded += (sender, args) => _renderTimerHelper.OnLoaded();
            Unloaded += (sender, args) => _renderTimerHelper.OnUnlodaed();

            SizeChanged += OnSizeChanged;

            AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(OnNonSelectedAreaMouseLeftButtonUp), false);
        }

        private void OnInvalidateRenderTimer()
        {
            if(Axis != null && SelectedRange != null)
                UpdateScrollbar(SelectedRange);
        }

        /// <summary>
        /// Selected range of the horizontal range slider
        /// </summary>
        public IRange SelectedRange
        {
            get { return (IRange)GetValue(SelectedRangeProperty); }
            set { SetValue(SelectedRangeProperty, value); }
        }

        /// <summary>
        /// Gets or sets Axis which this scrollbar control is bound to
        /// </summary>
        public IAxis Axis
        {
            get { return (IAxis)GetValue(AxisProperty); }
            set { SetValue(AxisProperty, value); }
        }

        /// <summary>
        /// Gets or sets thickness of grips
        /// </summary>
        public double GripsThickness
        {
            get { return (double)GetValue(GripsThicknessProperty); }
            set { SetValue(GripsThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets length of resizing grips
        /// </summary>
        public double GripsLength
        {
            get { return (double)GetValue(GripsLengthProperty); }
            set { SetValue(GripsLengthProperty, value); }
        }

        /// <summary>
        /// Gets or sets style for non selected area of scrollbar
        /// </summary>
        public Style NonSelectedAreaStyle
        {
            get { return (Style)GetValue(NonSelectedAreaStyleProperty); }
            set { SetValue(NonSelectedAreaStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets style for viewport area of scrollbar which contains <see cref="SelectedRange"/>
        /// </summary>
        public Style ViewportStyle
        {
            get { return (Style)GetValue(ViewportStyleProperty); }
            set { SetValue(ViewportStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets style for grips
        /// </summary>
        public Style GripsStyle
        {
            get { return (Style)GetValue(GripsStyleProperty); }
            set { SetValue(GripsStyleProperty, value); }
        }

        /// <summary>
        /// Get or set whether <see cref="UltrachartScrollbar"/> is displayed horizontally or vertically
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Gets or sets minimal size of viewport in pixels
        /// </summary>
        public double ZoomLimit
        {
            get { return (double)GetValue(ZoomLimitProperty); }
            set { SetValue(ZoomLimitProperty, value); }
        }

        private void UpdateScrollbar(IRange range)
        {
            _helper.UpdateRange(range);

            UpdateThumbs();
        }

        private void InvalidateElement()
        {
            _renderTimerHelper.Invalidate();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Axis != null)
            {
                RecreateHelper(Axis, ZoomLimit);
            }

            UpdateThumbs();
        }
        
        private void RecreateHelper(IAxis axis, double zoomLimit)
        {
            var size = Orientation == Orientation.Horizontal ? ActualWidth : ActualHeight;
            _helper = new ScrollbarCalculationgHelper(axis, size, zoomLimit);
        }

        //recalculates the movableWidth. called whenever the width of the control changes
        private void UpdateThumbs()
        {
            if (_border != null && Axis != null)
            {
                var startOffset = _helper.StartOffset;
                var stopOffset = _helper.StopOffset;

                var start = _helper.Start;
                var width = Math.Max(_helper.Stop - _helper.Start, 0);
                
                if (Orientation == Orientation.Horizontal)
                {
                    _border.Padding = new Thickness(startOffset, 0, stopOffset, 0);
                    _holeRectangle.Rect = new Rect(start, 0, width, 1);
                }
                else
                {
                    _border.Padding = new Thickness(0, startOffset, 0, stopOffset);
                    _holeRectangle.Rect = new Rect(0, start, 1, width);
                }

            }
        }

        /// <summary>
        /// Overide to get the visuals from the control template
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _centerThumb = EnforceInstance<UltraThumb>("PART_MiddleThumb");
            _leftThumb = EnforceInstance<UltraThumb>("PART_LeftThumb");
            _rightThumb = EnforceInstance<UltraThumb>("PART_RightThumb");
            _topThumb = EnforceInstance<UltraThumb>("PART_TopThumb");
            _bottomThumb = EnforceInstance<UltraThumb>("PART_BottomThumb");
           
            _border = EnforceInstance<Border>("PART_Border");
            _path = EnforceInstance<Path>("PART_NonSelectedArea");

            var geometryCollection = new GeometryCollection();

            _holeRectangle = new RectangleGeometry();
            var boundsRectangle = new RectangleGeometry() {Rect = new Rect(0, 0, 1, 1)};

            geometryCollection.Add(boundsRectangle);
            geometryCollection.Add(_holeRectangle);

            _path.Data = new GeometryGroup()
            {
                Children = geometryCollection,
            };
            SubscribeEvents();
           
            OnSizeChanged(this, null);
        }

        // Helper
        private T EnforceInstance<T>(string partName) where T : FrameworkElement, new()
        {
            var element = GetTemplateChild(partName) as T ?? new T();

            return element;
        }

        private void SubscribeEvents()
        {
            //handle the drag delta
            _leftThumb.UltraDragDelta += LeftThumbDragDelta;
            _rightThumb.UltraDragDelta += RightThumbDragDelta;
            _topThumb.UltraDragDelta += TopThumbDragDelta;
            _bottomThumb.UltraDragDelta += BottomThumbDragDelta;

            _centerThumb.UltraDragDelta += CenterThumbDragDelta;

            _path.MouseLeftButtonUp += OnNonSelectedAreaMouseLeftButtonUp;
        }

        //drag thumb from the right splitter
        private void RightThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            UpdateEventType(SelectedRangeEventType.Resize);

            ResizeThumb(0, e.HorizontalChange);
        }

        //drag thumb from the left splitter
        private void LeftThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            UpdateEventType(SelectedRangeEventType.Resize);

            ResizeThumb(e.HorizontalChange, 0);
        }

        private void TopThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            UpdateEventType(SelectedRangeEventType.Resize);

            ResizeThumb(e.VerticalChange, 0);
        }

        private void BottomThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            UpdateEventType(SelectedRangeEventType.Resize);

            ResizeThumb(0, e.VerticalChange);
        }

        //drag thumb from the middle, or click on outside of visible area
        private void CenterThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            var offset = Orientation == Orientation.Horizontal ? e.HorizontalChange : e.VerticalChange;

            UpdateEventType(SelectedRangeEventType.Drag);

            ResizeThumb(offset, offset);

        }

        private void ResizeThumb(double start, double stop)
        {
            if (Axis == null) return;

            var newRange = _helper.Resize(start, stop);

            UpdateSelectedRange(newRange);
        }

        private void OnNonSelectedAreaMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!ReferenceEquals(sender, _path) || Axis == null)
                return;

            var pointer = e.GetPosition(this);

            var coordinate = Orientation == Orientation.Horizontal ? pointer.X : pointer.Y;

            UpdateEventType(SelectedRangeEventType.Moved);

            var newRange = _helper.MoveTo(coordinate);
            
            UpdateSelectedRange(newRange,true);
        }

        private void UpdateEventType(SelectedRangeEventType eventType)
        {
            _eventType = eventType;
        }

        //recalculates the rangeStartSelected called when the left thumb is moved and when the middle thumb is moved
        private void UpdateSelectedRange(IRange newRange, bool isAnimated = false)
        {
            if (Axis == null || Axis.VisibleRange == null || newRange == null) return;
            
            if (isAnimated)
                AnimateSelectedRangeTo(newRange, TimeSpan.FromMilliseconds(500));
            else
               SetSelectedRangeInternal(newRange);
        }

        private void SetSelectedRangeInternal(IRange newRange, bool resetEventType = true)
        {
            this.SetCurrentValue(SelectedRangeProperty, newRange);

            if (resetEventType)
            {
                ResetEventType();
            }
        }

        private void ResetEventType()
        {
            _eventType = SelectedRangeEventType.ExternalSource;
        }

        /// <summary>
        /// Animates the SelectedRange property from its current start position to the destination over the specified duration. 
        /// Use this to get a smooth animated effect from one position to the next
        /// </summary>
        /// <param name="to"></param>
        /// <param name="duration"></param>
        public void AnimateSelectedRangeTo(IRange to, TimeSpan duration)
        {
            Point fromPoint, toPoint;

            if (Axis.IsLogarithmicAxis)
            {
                var logBase = ((ILogarithmicAxis)Axis).LogarithmicBase;

                fromPoint = new Point(Math.Log(SelectedRange.Min.ToDouble(), logBase), Math.Log(SelectedRange.Max.ToDouble(), logBase));
                toPoint = new Point(Math.Log(to.Min.ToDouble(), logBase), Math.Log(to.Max.ToDouble(), logBase));
            }
            else
            {
                fromPoint = new Point(SelectedRange.Min.ToDouble(), SelectedRange.Max.ToDouble());
                toPoint = new Point(to.Min.ToDouble(), to.Max.ToDouble());
            }

            var pointAnimation = new PointAnimation
            {
                From = fromPoint,
                To = toPoint,
                Duration = duration,
                EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut, Exponent = 7.0 }
            };

            //Debug.WriteLine("To Range: Min {0}, Max {1}", pointAnimation.To.Value.X, pointAnimation.To.Value.Y);

            Storyboard.SetTarget(pointAnimation, this);
            Storyboard.SetTargetProperty(pointAnimation, new PropertyPath("SelectedRangePoint"));
            var storyboard = new Storyboard();

            pointAnimation.Completed += (s, e) =>
            {
                //Debug.WriteLine("Animation Completed");
                SetSelectedRangeInternal(to);
#if !SILVERLIGHT
                Storyboard.SetTarget(pointAnimation, null);
#endif
                pointAnimation.FillBehavior = FillBehavior.Stop;
            };

            storyboard.Duration = duration;
            storyboard.Children.Add(pointAnimation);
            storyboard.Begin();
        }

        private static void OnSelectedRangePointDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Point p = (Point)e.NewValue;
            var scrollbar = (UltrachartScrollbar)d;

            //Debug.WriteLine("New Range: Min {0}, Max {1}", p.X, p.Y);
            scrollbar.Dispatcher.BeginInvokeAlways(() =>
            {
                IRange range;
                if (scrollbar.Axis.IsLogarithmicAxis)
                {
                    var logBase = ((ILogarithmicAxis)scrollbar.Axis).LogarithmicBase;

                    range = RangeFactory.NewRange(scrollbar.SelectedRange.GetType(),
                        Math.Pow(logBase, p.X), Math.Pow(logBase, p.Y));
                }
                else
                {
                    range = RangeFactory.NewRange(scrollbar.SelectedRange.GetType(),
                        p.X, p.Y);
                }

                scrollbar.SetSelectedRangeInternal(range, false);
            });
        }

        private static void OnAxisDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scrollbar = d as UltrachartScrollbar;
            if (scrollbar != null)
            {
                var oldAxis = e.OldValue as IAxis;
                if (oldAxis != null)
                {
                    oldAxis.DataRangeChanged -= scrollbar.OnDataRangeChanged;
                }

                var newAxis = e.NewValue as IAxis;
                if (newAxis != null)
                {
                    newAxis.DataRangeChanged += scrollbar.OnDataRangeChanged;
                    
                    scrollbar.AttachNewAxis(newAxis);
                }
                else
                {
                    scrollbar.ClearValue(SelectedRangeProperty);
                }

                scrollbar.InvalidateElement();
            }
        }
        
        private void OnDataRangeChanged(object sender, EventArgs eventArgs)
        {
            InvalidateElement();
        }

        private void AttachNewAxis(IAxis axis)
        {
            RecreateHelper(axis, ZoomLimit);
            
            UpdateThumbs();

            // Bind XAxis.VisibleRange to SelectedRange two way. Previously was setting this in code which was 
            // overwriting the ValueSource of dependency properties when the user bound to SelectedRange inside an itemtemplate
            var binding = new Binding
            {
                Source = axis,
                Path = new PropertyPath(AxisBase.VisibleRangeProperty),
                Mode = BindingMode.TwoWay
            };

            this.SetBinding(SelectedRangeProperty, binding);
        }

        private static void OnZoomLimitDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scrollbar = d as UltrachartScrollbar;
            if (scrollbar != null && scrollbar.Axis != null)
            {
                var zoomLimit = (double)e.NewValue;
                scrollbar.RecreateHelper(scrollbar.Axis, zoomLimit);
            }
        }

        private static void OnSelectedRangeDependencyPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var ultraChartScrollbar = (UltrachartScrollbar) s;

            var oldRange = e.OldValue as IRange;
            var newRange = e.NewValue as IRange;

            if (oldRange != null)
            {
                oldRange.PropertyChanged -= ultraChartScrollbar.OnMaxMinSelectedRangePropertiesChanged;
            }

            if (newRange != null)
            {
                newRange.PropertyChanged += ultraChartScrollbar.OnMaxMinSelectedRangePropertiesChanged;

                UpdateScrollbar(ultraChartScrollbar, newRange);
            }
        }

        private static void UpdateScrollbar(UltrachartScrollbar ultraChartScrollbar, IRange newRange)
        {
            // Update if Axis is set
            if (ultraChartScrollbar.Axis != null)
            {
                ultraChartScrollbar.UpdateScrollbar(newRange);

                ultraChartScrollbar.OnSelectedRangeChagned();
            }
        }

        private void OnMaxMinSelectedRangePropertiesChanged(object sender, PropertyChangedEventArgs e)
        {
            var oldMin = SelectedRange.Min;
            var oldMax = SelectedRange.Max;

            switch (e.PropertyName)
            {
                case "Min":
                    oldMin = (IComparable)((PropertyChangedEventArgsWithValues)e).OldValue;
                    break;
                case "Max":
                    oldMax = (IComparable)((PropertyChangedEventArgsWithValues)e).OldValue;
                    break;
            }

            var oldRange = RangeFactory.NewWithMinMax(SelectedRange, oldMin, oldMax);

            if (!SelectedRange.Equals(oldRange))
            {
                UpdateScrollbar(this, SelectedRange);

                var expr = GetBindingExpression(SelectedRangeProperty);

                if (expr != null && expr.ParentBinding.UpdateSourceTrigger != UpdateSourceTrigger.Explicit)
                {
                    expr.UpdateSource();
#if !SILVERLIGHT
                    expr.UpdateTarget();
#endif
                }
            }
        }
        
        private void OnSelectedRangeChagned()
        {
            var args = new SelectedRangeChangedEventArgs(SelectedRange, _eventType);
            
            var handler = SelectedRangeChanged;
            if (handler != null)
                handler(this, args);
        }
    }

    internal class ScrollbarCalculationgHelper
    {
        private const double MinEdge = 0.0d;
        private const double MaxEdge = 1.0d;

        private readonly double _zooomConstrain;
        private readonly IAxis _axis;
        private readonly double _size;
        
        private double _start;
        private double _stop;

        public ScrollbarCalculationgHelper(IAxis axis, double actulaSize, double zoomConstrainPixels)
        {
            _axis = axis;
            _size = actulaSize;
            _zooomConstrain = actulaSize > 0 ? zoomConstrainPixels / actulaSize : 0d;
                
            UpdateRange(axis.VisibleRange);
        }

        public ScrollbarCalculationgHelper(IAxis axis, double actulaSize) : this(axis, actulaSize, 0)
        {

        }

        public double Start
        {
            get { return _start; }
        }

        public double Stop
        {
            get { return _stop; }
        }

        public double StartOffset
        {
            get { return Math.Max(_start * _size, 0).RoundOff(); }
        }

        public double StopOffset
        {
            get { return Math.Max((MaxEdge - _stop) * _size, 0).RoundOff(); }
        }

        public void UpdateRange(IRange visibleRange)
        {
            if (visibleRange == null)
            {
                ResetStartStop();
            }
            else
            {
                var visibleRangeDouble = visibleRange.AsDoubleRange();
                var dataRange = _axis.DataRange;
                if (dataRange == null)
                {
                    ResetStartStop();
                }
                else
                {
                    if (_axis.GrowBy != null)
                    {
                        dataRange = dataRange.GrowBy(_axis.GrowBy.Min, _axis.GrowBy.Max);
                    }

                    var maxRangeDouble = dataRange.AsDoubleRange();
                    var diff = maxRangeDouble.Diff;

                    if (diff.CompareTo(0d) == 0)
                    {
                        ResetStartStop();
                    }
                    else
                    {
                        UpdateStartStop(maxRangeDouble, visibleRangeDouble, diff);
                    }
                }
            }
        }

        private void ResetStartStop()
        {
            _start = MinEdge;
            _stop = MaxEdge;
        }

        private void UpdateStartStop(DoubleRange maxRange, DoubleRange visibleRange, double diff)
        {
            double stop, start;
            if (_axis.IsLogarithmicAxis)
            {
                var logBase = ((ILogarithmicAxis)_axis).LogarithmicBase;

                var logDiff = Math.Log(maxRange.Max, logBase) - Math.Log(maxRange.Min, logBase);

                var logMinDiff = Math.Log(visibleRange.Min, logBase) - Math.Log(maxRange.Min, logBase);
                var logMaxDiff = Math.Log(visibleRange.Max, logBase) - Math.Log(maxRange.Min, logBase);

                start = logMinDiff / logDiff;
                stop = logMaxDiff / logDiff;
            }
            else
            {
                start = (visibleRange.Min - maxRange.Min) / diff;
                stop =  (visibleRange.Max - maxRange.Min) / diff;
            }

            TryCoerceEnds(ref start, ref stop);

            if (_axis.FlipCoordinates ^ !_axis.IsXAxis)
            {
                _start = MaxEdge - stop;
                _stop = MaxEdge - start;
            }
            else
            {
                _start = start;
                _stop = stop;
            }
        }

        private void TryCoerceEnds(ref double start, ref double stop)
        {
            start = NumberUtil.Constrain(start, MinEdge, MaxEdge);
            stop = NumberUtil.Constrain(stop, MinEdge, MaxEdge);

            if (Math.Abs(stop - start) < _zooomConstrain)
            {
                var center = start + (stop - start) / 2;
                var halfConstrain = _zooomConstrain / 2;

                start = center - halfConstrain;
                stop = center + halfConstrain;
            }

            if (stop > MaxEdge)
            {
                var diff = stop - MaxEdge;
                stop -= diff;
                start -= diff;
            }
            else if (start < MinEdge)
            {
                var diff = MinEdge - start;
                stop += diff;
                start += diff;
            }
        }

        public IRange MoveTo(double coordinate)
        {
            var center = _start + (_stop - _start) / 2;
            var centerCoordinate = center*_size;
            
            var offset = coordinate - centerCoordinate;

            return Resize(offset, offset);
        }
        
        public IRange Resize(double start, double stop)
        {
            double startOffset = start / _size;
            double endOffset = stop / _size;

            var newRangeStart = _start + startOffset;
            var newRangeEnd = _stop + endOffset;

            var isLeftShift = Math.Abs(start) > double.Epsilon;
            var isRightShift = Math.Abs(stop) > double.Epsilon;
            if (isLeftShift && isRightShift)
            {
                newRangeStart = _start + ConstrainEndsOffset(startOffset);
                newRangeEnd = _stop + ConstrainEndsOffset(endOffset);
            }
            else
            {
                if (isLeftShift)
                {
                    newRangeStart = NumberUtil.Constrain(newRangeStart, MinEdge, _stop - _zooomConstrain);
                }
                else
                {
                    newRangeEnd = NumberUtil.Constrain(newRangeEnd, _start + _zooomConstrain, MaxEdge);
                }
            }

            _start = newRangeStart;
            _stop = newRangeEnd;

            return CalculateRange();
        }

        private double ConstrainEndsOffset(double offset)
        {
            if (offset < MinEdge && Math.Abs(offset) > _start)
            {
                offset = -_start;
            }

            if (offset > MaxEdge - _stop)
            {
                offset = MaxEdge - _stop;
            }

            return offset;
        }

        public IRange CalculateRange()
        {
            IRange range = null;

            if (_axis != null && _axis.VisibleRange!=null)
            {
                var dataRange = _axis.DataRange;
                if (dataRange != null)
                {
                    if (_axis.GrowBy != null)
                    {
                        dataRange = dataRange.GrowBy(_axis.GrowBy.Min, _axis.GrowBy.Max);
                    }

                    var maxRangeDouble = dataRange.AsDoubleRange();

                    var diff = maxRangeDouble.Diff;

                    double stop;
                    double start;
                    if (_axis.FlipCoordinates ^ !_axis.IsXAxis)
                    {
                        start = MaxEdge - _stop;
                        stop = MaxEdge - _start;
                    }
                    else
                    {
                        stop = _stop;
                        start = _start;
                    }

                    double visibleMin, visibleMax;
                    if (_axis.IsLogarithmicAxis)
                    {
                        var logBase = ((ILogarithmicAxis) _axis).LogarithmicBase;

                        var logDiff = Math.Log(maxRangeDouble.Max, logBase) - Math.Log(maxRangeDouble.Min, logBase);

                        var shiftMin = start*logDiff;
                        var shiftMax = stop*logDiff;

                        var logMin = Math.Log(maxRangeDouble.Min, logBase);

                        visibleMin = Math.Pow(logBase, shiftMin + logMin);
                        visibleMax = Math.Pow(logBase, shiftMax + logMin);
                    }
                    else
                    {
                        visibleMin = start*diff + maxRangeDouble.Min;
                        visibleMax = stop*diff + maxRangeDouble.Min;
                    }

                    if (visibleMin < visibleMax && visibleMin.IsDefined() && visibleMax.IsDefined())
                    {
                        range = RangeFactory.NewRange(_axis.VisibleRange.GetType(), visibleMin, visibleMax);
                    }
                }
                else
                {
                    ResetStartStop();
                }
            }

            return range;
        }
    }
}