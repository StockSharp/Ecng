// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisDragModifierBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Input;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Provides base class for dragging operations on axes
    /// </summary>
    public abstract class AxisDragModifierBase : ChartModifierBase
    {
        /// <summary>
        /// Defines the YAxisId DependencyProperty
        /// </summary>
        private static readonly DependencyProperty AxisIdProperty = DependencyProperty.Register("AxisId", typeof (string), typeof (AxisDragModifierBase), new PropertyMetadata(AxisBase.DefaultAxisId, OnAxisIdChanged));

        /// <summary>
        /// Defines the DragMode DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DragModeProperty = DependencyProperty.Register("DragMode", typeof (AxisDragModes), typeof (AxisDragModifierBase), new PropertyMetadata(AxisDragModes.Scale));

        /// <summary>
        /// Defines the MinTouchArea DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MinTouchAreaProperty = DependencyProperty.Register("MinTouchArea", typeof (double), typeof (AxisDragModifierBase), new PropertyMetadata(0d));

        private static readonly Cursor DefaultCursor = Cursors.Arrow;

        private bool _isDragging;

        private Point _lastPoint;
        private bool _isSecondHalf;

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisDragModifierBase"/> class.
        /// </summary>
        protected AxisDragModifierBase()
        {
            IsPolarChartSupported = false;
        }

        /// <summary>
        /// Gets or sets the DragMode of the <see cref="YAxisDragModifier"/>. This modifier may be used to scale the <see cref="AxisBase.VisibleRange"/>
        /// or pan the <see cref="AxisBase.VisibleRange"/> creating a scrolling or vertical pan effect.
        /// </summary>
        public AxisDragModes DragMode
        {
            get { return (AxisDragModes) GetValue(DragModeProperty); }
            set { SetValue(DragModeProperty, value); }
        }

        /// <summary>
        /// Defines which YAxis to bind the YAxisDragModifier to, matching by string Id
        /// </summary>
        public string AxisId
        {
            get { return (string) GetValue(AxisIdProperty); }
            set { SetValue(AxisIdProperty, value); }
        }

        /// <summary>
        /// Gets or sets minimal area of recognition (min height for horizontal axis or min width for vertical), where user click or touch triggers zoom behavior.
        /// </summary>
        public double MinTouchArea
        {
            get { return (double) GetValue(MinTouchAreaProperty); }
            set { SetValue(MinTouchAreaProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether the user is currently dragging the axis
        /// </summary>
        /// <remarks></remarks>
        public bool IsDragging
        {
            get { return _isDragging; }
            protected set { _isDragging = value; }
        }

        /// <summary>
        /// Called when the Chart Modifier is attached to the Chart Surface
        /// </summary>
        public override void OnAttached()
        {
            base.OnAttached();

            SetAxisCursor();
        }

        /// <summary>
        /// Called immediately before the Chart Modifier is detached from the Chart Surface
        /// </summary>
        public override void OnDetached()
        {
            base.OnDetached();

            SetAxisCursor(DefaultCursor);
        }

        /// <summary>
        /// Sets passed cursor on current axis, or default cursor returned by <see cref="GetUsedCursor"/>
        /// </summary>
        private void SetAxisCursor(Cursor cursor = null)
        {
            var axis = GetCurrentAxis();

            if (axis != null)
            {
                axis.SetMouseCursor(cursor ?? GetUsedCursor(axis));
            }
        }

        /// <summary>
        /// Called when the IsEnabled property changes on this <see cref="ChartModifierBase"/> instance
        /// </summary>
        /// <remarks></remarks>
        protected override void OnIsEnabledChanged()
        {
            if (!IsEnabled)
                SetAxisCursor(DefaultCursor);
        }

        /// <summary>
        /// Gets the <see cref="IAxis"/> instance, which current modifier is associated with, on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        protected abstract IAxis GetCurrentAxis();

        /// <summary>
        /// Gets whether the specified mouse point is within the second (right-most or top-most) half of the Axis bounds
        /// </summary>
        /// <param name="point">The mouse point</param>
        /// <param name="axisBounds">The axis bounds</param>
        /// <param name="isHorizontalAxis">Value, which indicates whether current axis is horizontal or not</param>
        /// <returns>True if the point is within the second (right-most or top-most) half of the axis bounds, else false</returns>
        /// <remarks></remarks>
        protected virtual bool GetIsSecondHalf(Point point, Rect axisBounds, bool isHorizontalAxis)
        {
            if (isHorizontalAxis)
            {
                axisBounds.Width /= 2;
            }
            else
            {
                axisBounds.Height /= 2;
            }

            return !axisBounds.Contains(point);
        }

        /// <summary>
        /// Depending on axis orientation, returns a Cursor to show during mouse-over of the axis
        /// </summary>
        /// <param name="axis">The axis instance</param>
        /// <returns></returns>
        protected virtual Cursor GetUsedCursor(IAxis axis)
        {
            var activeCursor = axis.IsHorizontalAxis ? Cursors.SizeWE : Cursors.SizeNS;

            return IsEnabled ? activeCursor : DefaultCursor;
        }

        /// <summary>
        /// Called when a Mouse Button is pressed on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseDown(ModifierMouseArgs e)
        {
            base.OnModifierMouseDown(e);

            var axis = GetCurrentAxis();

            // Exit if already dragging, not matches the ExecuteOn mode, or the YAxis is null
            if (IsDragging || !MatchesExecuteOn(e.MouseButtons, ExecuteOn) || axis == null)
                return;

            var axisBounds = axis.GetBoundsRelativeTo(RootGrid);

            if (axis.IsHorizontalAxis && axisBounds.Height < MinTouchArea)
            {
                axisBounds.Y -= (MinTouchArea - axisBounds.Height)/2;
                axisBounds.Height = MinTouchArea;
            }
            if (!axis.IsHorizontalAxis && axisBounds.Width < MinTouchArea)
            {
                axisBounds.X -= (MinTouchArea - axisBounds.Width)/2;
                axisBounds.Width = MinTouchArea;
            }

            // Exit if the mouse down was outside the bounds                        
            if (!axisBounds.Contains(e.MousePoint))
                return;

            // Set a flag if the mouse point is in the second half of the bounding rect
            _isSecondHalf = GetIsSecondHalf(e.MousePoint, axisBounds, axis.IsHorizontalAxis);

            // If FlipCoordinates, change directions
            if (axis.FlipCoordinates)
            {
                _isSecondHalf = !_isSecondHalf;
            }

            _lastPoint = e.MousePoint;

            UltrachartDebugLogger.Instance.WriteLine("{0} MouseDown: x={1}, y={2}", GetType().Name, e.MousePoint.X,
                e.MousePoint.Y);

            if (e.IsMaster) axis.CaptureMouse();

            _isDragging = true;

            e.Handled = true;
        }

        /// <summary>
        /// Called when the Mouse is moved on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse move operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseMove(ModifierMouseArgs e)
        {
            SetAxisCursor();

            if (!IsDragging)
                return;

            base.OnModifierMouseMove(e);
            e.Handled = true;

            UltrachartDebugLogger.Instance.WriteLine("{0} MouseMove: x={1}, y={2}", GetType().Name, e.MousePoint.X,
                e.MousePoint.Y);

            var currentPoint = e.MousePoint;

            if (DragMode == AxisDragModes.Scale)
                PerformScale(currentPoint, _lastPoint, _isSecondHalf);
            else
                PerformPan(currentPoint, _lastPoint);

            _lastPoint = currentPoint;
        }

        /// <summary>
        /// Called when a Mouse Button is released on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseUp(ModifierMouseArgs e)
        {
            if (!IsDragging)
                return;

            base.OnModifierMouseUp(e);
            e.Handled = true;

            _isDragging = false;

            if (e.IsMaster) GetCurrentAxis().ReleaseMouseCapture();

            UltrachartDebugLogger.Instance.WriteLine("{0} MouseUp: x={1}, y={2}", GetType().Name, e.MousePoint.X,
                e.MousePoint.Y);
        }

        /// <summary>
        /// Peforms a pan on the assocaited <see cref="AxisBase"/>. The pan is considered to be a drag from <paramref name="currentPoint"/> to <paramref name="lastPoint"/>
        /// </summary>
        /// <param name="currentPoint">The current mouse point</param>
        /// <param name="lastPoint">The last mouse point</param>
        protected abstract void PerformPan(Point currentPoint, Point lastPoint);

        /// <summary>
        /// Performs a Scale on the associated <see cref="AxisBase"/>. The scale is considered to be a drag from <paramref name="currentPoint"/> to <paramref name="lastPoint"/>
        /// </summary>
        /// <param name="currentPoint">The current mouse point</param>
        /// <param name="lastPoint">The last mouse point</param>
        /// <param name="isSecondHalf">Boolean flag to determine which side of the axis is scaled</param>
        protected virtual void PerformScale(Point currentPoint, Point lastPoint, bool isSecondHalf)
        {
            var axis = GetCurrentAxis();
            var scaledRange = CalculateScaledRange(currentPoint, lastPoint, isSecondHalf, axis);

            if (axis.AutoRange == AutoRange.Always)
            {
                ((AxisBase) axis).SetValue(AxisBase.GrowByProperty, CalculateRelativeRange(scaledRange, axis));
            }
            else
            {
                axis.VisibleRange = scaledRange;
            }
        }

        /// <summary>
        /// When overriden in a derived class, calculates an output <see cref="IRange"/> to apply to the associated <see cref="AxisBase">Axis</see>, 
        /// given the input parameters
        /// </summary>
        /// <param name="currentPoint">The current mouse position</param>
        /// <param name="lastPoint">The last mouse position</param>
        /// <param name="isSecondHalf">A flag, which determines how the scale operates, e.g. which half of the axis (top or bottom, left or right) was dragged</param>
        /// <param name="axis">The axis being operated on</param>
        /// <returns>The output <see cref="IRange"/></returns>
        protected abstract IRange CalculateScaledRange(Point currentPoint, Point lastPoint, bool isSecondHalf, IAxis axis);

        /// <summary>
        /// When overriden in a derived class, calculates an output <see cref="IRange"/> to apply to the associated <see cref="AxisBase">Axis</see>, 
        /// given the input parameters.
        /// </summary>
        /// <remarks>A Relative-Range is defined as one that affects the <see cref="AxisBase.GrowBy"/>, not the <see cref="AxisBase.VisibleRange"/>. This 
        /// is used in cases where the YAxis or XAxis has <see cref="AxisBase.AutoRange"/> set to Always, and you still want to be able to drag the axis to 
        /// set a constant, relative margin of spacing around the upper and lower bounds of the data</remarks>
        /// <param name="fromRange">The input range, expecting a VisibleRange</param>
        /// <param name="axis">The axis being operated on</param>
        /// <returns>The output <see cref="IRange"/> which can then be applied to the <see cref="AxisBase.GrowBy"/> property to get the same affect as applying the input visible-range</returns>
        protected virtual DoubleRange CalculateRelativeRange(IRange fromRange, IAxis axis)
        {
            var doubleRange = fromRange.AsDoubleRange();
            double yMax = doubleRange.Max;
            double yMin = doubleRange.Min;

            // Now compute the GrowBy that would have to be applied to achieve this
            //
            // First work back to get data min, max from visible min, max and grow-by
            var currRange = axis.VisibleRange.AsDoubleRange();
            var growBy = axis.GrowBy ?? new DoubleRange(0, 0);

            // Computed data min, max 
            double dMin = (currRange.Min + currRange.Min*growBy.Max + currRange.Max*growBy.Min)/
                          (1 + growBy.Min + growBy.Max);
            double dMax = (currRange.Max + dMin*growBy.Max)/(1 + growBy.Max);

            // Given dMin, dMax, what growby is now needed to set the desired Y Visiblerange?
            double gMax = (yMax - dMax)/(dMax - dMin);
            double gMin = (yMin - dMin)/(-dMax + dMin);

            // prevents from negative values
/*            gMin = Math.Max(gMin, 0.02);
            gMax = Math.Max(gMax, 0.02);*/

            return new DoubleRange(gMin, gMax);
        }

        private static void OnAxisIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dragModifier = (AxisDragModifierBase) d;

            var oldAxisId = (string) e.OldValue;

            if (dragModifier.IsAttached)
            {
                var oldAxis = dragModifier is XAxisDragModifier
                    ? dragModifier.GetXAxis(oldAxisId)
                    : dragModifier.GetYAxis(oldAxisId);
                if (oldAxis != null)
                {
                    oldAxis.SetMouseCursor(DefaultCursor);
                }

                dragModifier.SetAxisCursor();
            }
        }
    }
}
