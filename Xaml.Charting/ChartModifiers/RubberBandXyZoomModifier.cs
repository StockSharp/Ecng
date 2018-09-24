// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RubberBandXyZoomModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// The <see cref="RubberBandXyZoomModifier"/> provides a mouse drag to zoom into a rectangular region, or horizontal section of the chart.
    /// Add to a <see cref="UltrachartSurface"/> and set IsEnabled to true to enable this behaviour
    /// </summary>
    public class RubberBandXyZoomModifier : ChartModifierBase
    {
        /// <summary>
        /// Defines the IsAnimated DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsAnimatedProperty = DependencyProperty.Register("IsAnimated",typeof (bool), typeof (RubberBandXyZoomModifier), new PropertyMetadata(true));

        /// <summary>
        /// Defines the RubberBandFill dependency property
        /// </summary>
        public static readonly DependencyProperty RubberBandFillProperty = DependencyProperty.Register("RubberBandFill", typeof (Brush), typeof (RubberBandXyZoomModifier), new PropertyMetadata(null));

        /// <summary>
        /// Defines the RubberBandStroke dependency property
        /// </summary>
        public static readonly DependencyProperty RubberBandStrokeProperty = DependencyProperty.Register("RubberBandStroke", typeof (Brush), typeof (RubberBandXyZoomModifier), new PropertyMetadata(null));

        /// <summary>
        /// Defines the RubberBandStrokeDashArray dependency property
        /// </summary>
        public static readonly DependencyProperty RubberBandStrokeDashArrayProperty = DependencyProperty.Register("RubberBandStrokeDashArray", typeof(DoubleCollection), typeof(RubberBandXyZoomModifier), new PropertyMetadata(null));

        /// <summary>
        /// Defines the IsXAxisOnly dependency property
        /// </summary>
        public static readonly DependencyProperty IsXAxisOnlyProperty = DependencyProperty.Register("IsXAxisOnly", typeof (bool), typeof (RubberBandXyZoomModifier), new PropertyMetadata(false));

        /// <summary>
        /// Defines the ZoomExtentsY DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ZoomExtentsYProperty = DependencyProperty.Register("ZoomExtentsY", typeof (bool), typeof (RubberBandXyZoomModifier), new PropertyMetadata(true));

        /// <summary>
        /// Defines the MinDragSensitivity DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MinDragSensitivityProperty = DependencyProperty.Register("MinDragSensitivity", typeof (double), typeof (RubberBandXyZoomModifier), new PropertyMetadata(10.0));

        private IRubberBandOverlayPlacementStrategy _overlayPlacementStrategy;

        private Point _startPoint;
        private Point _endPoint;
        private bool _isDragging;

        /// <summary>
        /// reticule
        /// </summary>
        private Shape _shape;

        /// <summary>
        /// Initializes a new instance of the <see cref="RubberBandXyZoomModifier"/> class.
        /// </summary>
        public RubberBandXyZoomModifier()
        {
            DefaultStyleKey = typeof (RubberBandXyZoomModifier);
        }

        /// <summary>
        /// Gets or sets whether zoom operations should be animated. Default true
        /// </summary>
        public bool IsAnimated
        {
            get { return (bool) GetValue(IsAnimatedProperty); }
            set { SetValue(IsAnimatedProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Fill brush of the recticule drawn on the screen as the user zooms
        /// </summary>
        public Brush RubberBandFill
        {
            get { return (Brush) GetValue(RubberBandFillProperty); }
            set { SetValue(RubberBandFillProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Stroke brush of the recticule drawn on the screen as the user zooms
        /// </summary>
        public Brush RubberBandStroke
        {
            get { return (Brush) GetValue(RubberBandStrokeProperty); }
            set { SetValue(RubberBandStrokeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the StrokeDashArray, used to set a dashed outline for the rubber band rectangle. 
        /// See the <see cref="Shape.StrokeDashArray"/> property for usage
        /// </summary>
        public DoubleCollection RubberBandStrokeDashArray
        {
            get { return (DoubleCollection) GetValue(RubberBandStrokeDashArrayProperty); }
            set { SetValue(RubberBandStrokeDashArrayProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the RubberBand should zoom the X-Axis only. 
        /// If true, then the effect will be instead of a rectangle drawn under the mouse, an horizontal section of the 
        /// entire chart will be selected
        /// </summary>
        public bool IsXAxisOnly
        {
            get { return (bool) GetValue(IsXAxisOnlyProperty); }
            set { SetValue(IsXAxisOnlyProperty, value); }
        }

        /// <summary>
        /// If true, zooms to extents on the Y-Axis on each zoom operation. Use in conjuction with <see cref="RubberBandXyZoomModifier.IsXAxisOnly"/> to achieve different zooming effects
        /// </summary>
        public bool ZoomExtentsY
        {
            get { return (bool) GetValue(ZoomExtentsYProperty); }
            set { SetValue(ZoomExtentsYProperty, value); }
        }

        /// <summary>
        /// Gets or sets the drag sensitivity - rectangles dragged smaller than this size in the diagonal will be ignored when zooming. Default is 10 pixels
        /// </summary>
        public double MinDragSensitivity
        {
            get { return (double) GetValue(MinDragSensitivityProperty); }
            set { SetValue(MinDragSensitivityProperty, value); }
        }

        /// <summary>
        /// Gets whether the user is currently dragging the mouse
        /// </summary>
        public bool IsDragging
        {
            get { return _isDragging; }
        }

        /// <summary>
        /// Called when the Chart Modifier is attached to the Chart Surface
        /// </summary>
        /// <remarks></remarks>
        public override void OnAttached()
        {
            base.OnAttached();

            ClearReticule();
        }

        /// <summary>
        /// Called when the Chart Modifier is detached from the Chart Surface
        /// </summary>
        /// <remarks></remarks>
        public override void OnDetached()
        {
            base.OnDetached();

            ClearReticule();
        }

        /// <summary>
        /// Called when a Mouse Button is pressed on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseDown(ModifierMouseArgs e)
        {
            base.OnModifierMouseDown(e);

            if (_isDragging || !MatchesExecuteOn(e.MouseButtons, ExecuteOn))
                return;

            e.Handled = true;

            // Exit if the mouse down was outside the bounds of the Master ModifierSurface
            // e.g. if this is a slave or master, only start dragging if mousedown occurred on master ModifierSurface. 
            var source = e.Source as IChartModifier;
            if (source == null)
                return;

            var modifierSurfaceBounds = ModifierSurface.GetBoundsRelativeTo(RootGrid);
            if (!modifierSurfaceBounds.Contains(e.MousePoint))
            {
                return;
            }

            //Debug.WriteLine("MouseDown... Type: {0}, Tag: {1}, x={2}, y={3}, IsMaster? {4}", GetType().Name, Tag, e.MousePoint.X, e.MousePoint.Y, e.IsMaster);
            //UltrachartDebugLogger.Instance.WriteLine("{0} MouseDown: x={1}, y={2}", GetType().Name, e.MousePoint.X, e.MousePoint.Y);

            if (e.IsMaster)
                ModifierSurface.CaptureMouse();

            // Translate the mouse point (which is in RootGrid coordiantes) relative to the ModifierSurface
            // This accounts for any offset due to left Y-Axis
            var ptTrans = GetPointRelativeTo(e.MousePoint, ModifierSurface);
  
            //Debug.WriteLine("MouseDown (Translated)... Type: {0}, Tag: {1}, x={2}, y={3}, IsMaster? {4}", GetType().Name, Tag, ptTrans.X, ptTrans.Y, e.IsMaster);

            _startPoint = ptTrans;

            _overlayPlacementStrategy = GetOverlayPlacementStrategy();
            _shape = _overlayPlacementStrategy.CreateShape(RubberBandFill, RubberBandStroke, RubberBandStrokeDashArray);
            _overlayPlacementStrategy.SetupShape(IsXAxisOnly, _startPoint, _startPoint);

            ModifierSurface.Children.Add(_shape);       
     
            _isDragging = true;
        }

        private IRubberBandOverlayPlacementStrategy GetOverlayPlacementStrategy()
        {
            IRubberBandOverlayPlacementStrategy strategy;
            if (XAxis != null && XAxis.IsPolarAxis )
            {
                strategy = _overlayPlacementStrategy as PolarRubberBandOverlayPlacementStrategy ?? new PolarRubberBandOverlayPlacementStrategy(this);
            }
            else
            {
                strategy = _overlayPlacementStrategy as CartesianRubberBandOverlayPlacementStrategy ?? new CartesianRubberBandOverlayPlacementStrategy(this);
            }

            return strategy;
        }

        /// <summary>
        /// Called when the Mouse is moved on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse move operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseMove(ModifierMouseArgs e)
        {
            if (!_isDragging)
                return;

            //Debug.WriteLine("MouseMove ... Type: {0}, Tag: {1}, x={2}, y={3}, IsMaster? {4}", GetType().Name, Tag, e.MousePoint.X, e.MousePoint.Y, e.IsMaster);

            base.OnModifierMouseMove(e);
            e.Handled = true;

            UltrachartDebugLogger.Instance.WriteLine("{0} MouseMove: x={1}, y={2}", GetType().Name, e.MousePoint.X,
                e.MousePoint.Y);

            // Translate the mouse point (which is in RootGrid coordiantes) relative to the ModifierSurface
            // This accounts for any offset due to left Y-Axis
            var ptTrans = GetPointRelativeTo(e.MousePoint, ModifierSurface);

            //Debug.WriteLine("MouseMoveT... Type: {0}, Tag: {1}, x={2}, y={3}, IsMaster? {4}", GetType().Name, Tag, ptTrans.X, ptTrans.Y, e.IsMaster);

            _overlayPlacementStrategy.UpdateShape(IsXAxisOnly, _startPoint, ptTrans);
        }

        /// <summary>
        /// Called when a Mouse Button is released on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseUp(ModifierMouseArgs e)
        {
            if (!_isDragging)
                return;

            base.OnModifierMouseUp(e);

            UltrachartDebugLogger.Instance.WriteLine("{0} MouseUp: x={1}, y={2}", GetType().Name, e.MousePoint.X,
                e.MousePoint.Y);

            // Translate the mouse point (which is in RootGrid coordiantes) relative to the ModifierSurface
            // This accounts for any offset due to left Y-Axis
            var ptTrans = GetPointRelativeTo(e.MousePoint, ModifierSurface);

            _endPoint = _overlayPlacementStrategy.UpdateShape(IsXAxisOnly, _startPoint, ptTrans);

            var strategy = Services.GetService<IStrategyManager>().GetTransformationStrategy();
           
            var startPoint = strategy.Transform(_startPoint);
            var endPoint = strategy.Transform(_endPoint);
            var currentPoint = strategy.Transform(ptTrans);

            double distanceDragged = _overlayPlacementStrategy.CalculateDraggedDistance(startPoint, currentPoint);
            if (distanceDragged > MinDragSensitivity)
            {
                // Zoom only if user drew a rectangle
                PerformZoom(startPoint, endPoint);

                e.Handled = true;
            }
            else
            {
                ClearReticule();
            }

            _isDragging = false;

            if (e.IsMaster)
                ModifierSurface.ReleaseMouseCapture();
        }

        private void ClearReticule()
        {
            if (ModifierSurface != null && _shape != null)
            {
                ModifierSurface.Children.Remove(_shape);
                _shape = null;
                _isDragging = false;
            }
        }

        internal void PerformZoom(Point startPoint, Point endPoint)
        {
            ClearReticule();

            if (Math.Abs(startPoint.X - endPoint.X) < double.Epsilon || Math.Abs(startPoint.Y - endPoint.Y) < double.Epsilon)
                return;

			if (XAxes.IsNullOrEmpty() ||  YAxes.IsNullOrEmpty())
                return;

            var zoomRect = new Rect(startPoint, endPoint);

            using (ParentSurface.SuspendUpdates())
            {
                // contain displayed regions for each X Axis
                var xAxesRanges = new Dictionary<string, IRange>();
                foreach (var xAxis in XAxes)
                {
                    // Don't zoom on axes which have a different orientation than the primary X axis
                    if (xAxis.IsHorizontalAxis != XAxis?.IsHorizontalAxis)
                        continue;

                    // Perform zoom in XAxis-direction
                    var xRange = PerformZoomOnAxis(xAxis, zoomRect);

                    if (xRange == null || xRange.IsZero)
                    {
                        continue;
                    }

                    xAxesRanges.Add(xAxis.Id, xRange);
                }

                // If XAxisOnly just zoom extents in Y-direction and exit
                if (IsXAxisOnly)
                {
                    if (ZoomExtentsY)
                    {
                        // for each Y Axis get Y range for zoom extent
                        foreach(var yAxis in YAxes)
                        {
                            var yRange = yAxis.GetWindowedYRange(xAxesRanges);

                            yAxis.TrySetOrAnimateVisibleRange(yRange, IsAnimated ? TimeSpan.FromMilliseconds(500) : TimeSpan.Zero);
                        }
                    }
                }
                else
                {
                    // don't take into account any series data. only zoom on each X and Y axis separately according to rectangle
                    // Perform zoom in YAxis-direction
                    foreach (var yAxis in YAxes)
                    {
                        PerformZoomOnAxis(yAxis, zoomRect);
                    }
                }
            }
        }

        private IRange PerformZoomOnAxis(IAxis axis, Rect zoomRect)
        {
            var fromCoord = axis.IsHorizontalAxis ? zoomRect.Left : zoomRect.Bottom;
            var toCoord = axis.IsHorizontalAxis ? zoomRect.Right : zoomRect.Top;

            return PerformZoomOnAxis(axis, fromCoord, toCoord);
        }

        internal IRange PerformZoomOnAxis(IAxis axis, double fromCoord, double toCoord)
        {
            if (axis == null) return null;

            var interactivityHelper = axis.GetCurrentInteractivityHelper();
            if (interactivityHelper == null) return null;

            //need (toCoord - 1) to fix for SC-626(right-side zoom issue)
            var toRange = interactivityHelper.Zoom(axis.VisibleRange, fromCoord, toCoord - 1);

            axis.TrySetOrAnimateVisibleRange(toRange, IsAnimated ? TimeSpan.FromMilliseconds(500) : TimeSpan.Zero);

            return toRange;
        }

        private void DebugZoom(double xMax, double xMin, Rect zoomRect)
        {
//            Debug.WriteLine("Left Pixel:\t{0}", zoomRect.Left);
//            Debug.WriteLine("Right Pixel:\t{0}", zoomRect.Right);
//            Debug.WriteLine("Surface Width:\t{0}", ModifierSurface.Width);
//            Debug.WriteLine("Before XMin:\t{0}", XAxis.VisibleRange.Min);
//            Debug.WriteLine("Before XMax:\t{0}", XAxis.VisibleRange.Max);
//            Debug.WriteLine("Computed XMin:\t{0}", xMin);
//            Debug.WriteLine("Computed XMax:\t{0}", xMax);
//            Debug.WriteLine("XAxis Data (X values)");
//            if (!(XAxis is CategoryDateTimeAxis)) return;
//            var cPointSeries = ((CategoryDateTimeAxis) XAxis).GetAxisParams().CategoryPointSeries;
//
//            for (int i = 0; i < cPointSeries.Count; i++)
//            {
//                Debug.WriteLine("{0}", cPointSeries[i].X);
//            }
        }

        /// <summary>
        /// Property for testing purposes
        /// </summary>
        internal IRubberBandOverlayPlacementStrategy CurrentStrategy { get { return _overlayPlacementStrategy; } }
    }
}