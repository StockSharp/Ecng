// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ZoomPanModifierBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Base class for modifiers responsible for pan by mouse drag.
    /// </summary>
    public abstract class ZoomPanModifierBase : ChartModifierBase
    {
        private Point _startPoint;
        private Point _lastPoint;

        /// <summary>
        /// Defines the XyDirection dependency property
        /// </summary>
        public static readonly DependencyProperty XyDirectionProperty = DependencyProperty.Register("XyDirection", typeof(XyDirection), typeof(ZoomPanModifierBase), new PropertyMetadata(XyDirection.XYDirection));

        /// <summary>
        /// Defines the ClipToExtentsX Dependency Property 
        /// </summary>
        public static readonly DependencyProperty ClipModeXProperty = DependencyProperty.Register("ClipModeX", typeof(ClipMode), typeof(ZoomPanModifierBase), new PropertyMetadata(ClipMode.StretchAtExtents));

        /// <summary>
        /// Defines the ZoomExtentsY DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ZoomExtentsYProperty = DependencyProperty.Register("ZoomExtentsY", typeof(bool), typeof(ZoomPanModifierBase), new PropertyMetadata(true));

        /// <summary>
        /// If true, zooms to extents on the Y-Axis on each zoom operation when panning in X-Direction only. Use in conjuction with <see cref="ZoomPanModifierBase.XyDirection"/> to achieve different zooming effects
        /// </summary>
        public bool ZoomExtentsY
        {
            get { return (bool)GetValue(ZoomExtentsYProperty); }
            set { SetValue(ZoomExtentsYProperty, value); }
        }

        /// <summary>
        /// Defines the direction of the InertialZoomPanModifier
        /// </summary>
        public XyDirection XyDirection
        {
            get { return (XyDirection)GetValue(XyDirectionProperty); }
            set { SetValue(XyDirectionProperty, value); }
        }

        /// <summary>
        /// Defines how panning behaves when you reach the edge of the X-Axis extents. 
        /// e.g. ClipMode.ClipAtExtents prevents panning outside of the X-Axis, ClipMode.None allows panning outside
        /// </summary>
        public ClipMode ClipModeX
        {
            get { return (ClipMode)GetValue(ClipModeXProperty); }
            set { SetValue(ClipModeXProperty, value); }
        }

        /// <summary>
        /// Gets whether the user is currently dragging the chart
        /// </summary>
        public bool IsDragging { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomPanModifierBase"/> class.
        /// </summary>
        protected ZoomPanModifierBase()
        {
            this.SetCurrentValue(ExecuteOnProperty, ExecuteOn.MouseLeftButton);

            IsPolarChartSupported = false;
        }

        /// <summary>
        /// Called when a Mouse Button is pressed on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseDown(ModifierMouseArgs e)
        {
            if (IsDragging || !MatchesExecuteOn(e.MouseButtons, ExecuteOn) || XAxes.IsNullOrEmpty() || !e.IsMaster)
                return;

            var modifierSurfaceBounds = ModifierSurface.GetBoundsRelativeTo(RootGrid);
            if (!modifierSurfaceBounds.Contains(e.MousePoint))
            {
                return;
            }

            var mousePoint = e.MousePoint;

            base.OnModifierMouseDown(e);
            e.Handled = true;

            SetCursor(Cursors.Hand);
            UltrachartDebugLogger.Instance.WriteLine("{0} MouseDown: x={1}, y={2}", GetType().Name, e.MousePoint.X, e.MousePoint.Y);
            if (e.IsMaster) ModifierSurface.CaptureMouse();

            _startPoint = mousePoint;
            _lastPoint = _startPoint;

            IsDragging = true;
        }

        /// <summary>
        /// Called when a Mouse Button is released on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseUp(ModifierMouseArgs e)
        {
            base.OnModifierMouseUp(e);

            e.Handled = true;

            IsDragging = false;

            _startPoint = default(Point);

            if (e.IsMaster) ModifierSurface.ReleaseMouseCapture();

            SetCursor(Cursors.Arrow);

            UltrachartDebugLogger.Instance.WriteLine("{0} MouseUp: x={1}, y={2}", GetType().Name, e.MousePoint.X, e.MousePoint.Y);
        }

        /// <summary>
        /// Called when the Mouse is moved on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse move operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseMove(ModifierMouseArgs e)
        {
            if (!IsDragging)
                return;

            base.OnModifierMouseMove(e);
            e.Handled = true;

            UltrachartDebugLogger.Instance.WriteLine("{0} MouseMove: x={1}, y={2}", GetType().Name, e.MousePoint.X, e.MousePoint.Y);

            var currentPoint = e.MousePoint;
            Pan(currentPoint, _lastPoint, _startPoint);

            _lastPoint = currentPoint;
        }

        /// <summary>
        /// Receives pan command from the user.
        /// </summary>
        /// <param name="currentPoint">Current point of the gesture.</param>
        /// <param name="lastPoint">Previous point of the gesture.</param>
        /// <param name="startPoint">Start point of the gesture.</param>
        public abstract void Pan(Point currentPoint, Point lastPoint, Point startPoint);
    }
}
