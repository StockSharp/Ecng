// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// MouseWheelZoomModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// The <see cref="MouseWheelZoomModifier"/> provides zooming (or shrinking) of the <see cref="UltrachartSurface"/> on mouse wheel scroll
    /// Add to a <see cref="UltrachartSurface"/> and set IsEnabled to true to enable this behaviour
    /// </summary>
    public class MouseWheelZoomModifier : RelativeZoomModifierBase
    {
        /// <summary>
        /// Defines the ActionType DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ActionTypeProperty = DependencyProperty.Register("ActionType", typeof(ActionType), typeof(MouseWheelZoomModifier), new PropertyMetadata(ActionType.Zoom,
            (sender, args) =>
            {
                var mouseWheelZoomModifier =
                sender as MouseWheelZoomModifier;

                var action = (ActionType)args.NewValue;

                if (mouseWheelZoomModifier != null)
                {
                    mouseWheelZoomModifier._performAction = (action == ActionType.Pan
                                                                ? new Action<Point, double>(mouseWheelZoomModifier.PerformPan)
                                                                : new Action<Point, double>(mouseWheelZoomModifier.PerformZoom));
                }
            }));

        private Action<Point, double> _performAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseWheelZoomModifier"/> class.
        /// </summary>
        /// <remarks></remarks>
        public MouseWheelZoomModifier()
        {
            GrowFactor = 0.1;

            //use zoom action by default
            _performAction = PerformZoom;
        }

        /// <summary>
        /// Gets or sets the <see cref="ActionType"/> to perform on mouse-wheel interaction
        /// </summary>
        public ActionType ActionType
        {
            get { return (ActionType)GetValue(ActionTypeProperty); }
            set { SetValue(ActionTypeProperty, value); }
        }

        private void PerformZoom(Point point, double value)
        {
            PerformZoom(point, value, value);
        }

        /// <summary>
        /// Called when the Mouse Wheel is scrolled on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse wheel operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseWheel(ModifierMouseArgs e)
        {
            base.OnModifierMouseWheel(e);

            e.Handled = true;

            const double mouseWheelDeltaCoef = 120;

            using (ParentSurface.SuspendUpdates())
            {
                double value = -e.Delta / mouseWheelDeltaCoef;

                var currDirection = XyDirection;
                var currAction = ActionType;

                if (e.Modifier != MouseModifier.None)
                {
                    this.SetCurrentValue(ActionTypeProperty, ActionType.Pan);

                    if (e.Modifier == MouseModifier.Ctrl)
                    {
                        this.SetCurrentValue(XyDirectionProperty, XyDirection.YDirection);
                    }
                    else if (e.Modifier == MouseModifier.Shift)
                    {
                        this.SetCurrentValue(XyDirectionProperty, XyDirection.XDirection);
                    }
                }

                var mousePoint = GetPointRelativeTo(e.MousePoint, ModifierSurface);
                _performAction(mousePoint, value);

                this.SetCurrentValue(XyDirectionProperty, currDirection);
                this.SetCurrentValue(ActionTypeProperty, currAction);
            }
        }

        private void PerformPan(Point mousePoint, double value)
        {
            if (XyDirection == XyDirection.YDirection || XyDirection == XyDirection.XYDirection)
            {
                // Computation of new Y-Range
                foreach (var yAxis in YAxes)
                {
                    var size = GetAxisSize(yAxis);
                    var pixels = value*GrowFactor*size;

                    yAxis.Scroll(pixels, ClipMode.None);
                }

                UltrachartDebugLogger.Instance.WriteLine("Growing YRange: {0}", value);
            }

            if (XyDirection == XyDirection.XDirection || XyDirection == XyDirection.XYDirection)
            {
                // Scroll to new X-Axis range, based on start point (pixel), current point and the initial visible range
                foreach (var xAxis in XAxes)
                {
                    // don't pan on axes which have a different orientation than primary X axis
                    if (xAxis.IsHorizontalAxis != XAxis.IsHorizontalAxis)
                        break;

                    var size = GetAxisSize(xAxis);
                    var pixels = -value * GrowFactor * size;
                    
                    xAxis.Scroll(pixels, ClipMode.None);
                }

                UltrachartDebugLogger.Instance.WriteLine("Growing XRange: {0}", (int)value);
            }
        }

        private double GetAxisSize(IAxis axis)
        {
            var size = axis.IsHorizontalAxis ? axis.Width : axis.Height;

            if (Math.Abs(size) < double.Epsilon && ParentSurface != null && ParentSurface.RenderSurface != null)
            {
                size = axis.IsHorizontalAxis ? ParentSurface.RenderSurface.ActualWidth : ParentSurface.RenderSurface.ActualHeight;
            }
            if (axis.IsPolarAxis)
            {
                size /= 2;
            }

            return size;
        }
    }
}
