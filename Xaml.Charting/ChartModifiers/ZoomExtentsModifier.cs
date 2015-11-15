// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ZoomExtentsModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Defines constants for when a <see cref="ChartModifierBase"/> operation occurs
    /// </summary>
    public enum ExecuteOn
    {
        /// <summary>
        /// Execute on MouseRightButton
        /// </summary>
        MouseLeftButton,

        /// <summary>
        /// Execute on MouseRightButton
        /// </summary>
        MouseMiddleButton,

        /// <summary>
        /// Execute on MouseRightButton
        /// </summary>
        MouseRightButton,

        /// <summary>
        /// Execute on MouseDoubleClick
        /// </summary>
        MouseDoubleClick,

        /// <summary>
        /// Execute on MouseMove
        /// </summary>
        MouseMove,

        /// <summary>
        /// Execute on MouseRightButtonUp
        /// </summary>
        [Obsolete("MouseRightButtonUp is deprecated, please use MouseRightButton instead", true)]
        MouseRightButtonUp
    }

    /// <summary>
    /// Provides zoom to extents, or zoom to specific X and Y VisibleRange on mouse interaction
    /// </summary>
    /// <example>
    /// The following example will create a modifier which zooms to extents on Mouse Double Click
    /// 
    /// <code>
    /// ZoomExtentsModifier z = new ZoomExtentsModifier();
    /// z.ExecuteOn = ExecuteOn.MouseDoubleClick;
    /// </code>
    /// </example>
    public class ZoomExtentsModifier : ChartModifierBase
    {
        /// <summary>
        /// Defines the IsAnimated DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsAnimatedProperty = DependencyProperty.Register("IsAnimated", typeof(bool), typeof(ZoomExtentsModifier), new PropertyMetadata(true));

        /// <summary>
        /// Defines the XyDirection dependency property
        /// </summary>
        public static readonly DependencyProperty XyDirectionProperty = DependencyProperty.Register("XyDirection", typeof(XyDirection), typeof(ZoomExtentsModifier), new PropertyMetadata(XyDirection.XYDirection));

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomExtentsModifier" /> class.
        /// </summary>
        public ZoomExtentsModifier()
        {
            ReceiveHandledEvents = true;
            this.SetCurrentValue(ExecuteOnProperty, ExecuteOn.MouseDoubleClick);

            DoubleTapThreshold = TimeSpan.FromMilliseconds(500);
            //Touch.FrameReported += OnFrameReported;
        }

        /// <summary>
        /// Defines the direction of the ZoomExtentsModifier
        /// </summary>
        public XyDirection XyDirection
        {
            get { return (XyDirection)GetValue(XyDirectionProperty); }
            set { SetValue(XyDirectionProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether zoom operations should be animated. Default true
        /// </summary>
        public bool IsAnimated
        {
            get { return (bool)GetValue(IsAnimatedProperty); }
            set { SetValue(IsAnimatedProperty, value); }
        }

        /// <summary>
        /// Called when a Mouse DoubleClick occurs on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public override void OnModifierDoubleClick(ModifierMouseArgs e)
        {
            if (ExecuteOn == ExecuteOn.MouseDoubleClick)
            {
                base.OnModifierDoubleClick(e);
                e.Handled = true;

                PerformZoom();
            }
        }

        /// <summary>
        /// Called when a Mouse Button is released on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseUp(ModifierMouseArgs e)
        {
            if (ExecuteOn == ExecuteOn.MouseRightButton && e.MouseButtons == MouseButtons.Right)
            {
                base.OnModifierMouseUp(e);
                e.Handled = true;

                PerformZoom();
            }
        }

        /// <summary>
        /// Performs the zoom function. Called when the user double clicks (right mouse up). May be overridden in derived classes to customize what the zoom actually does
        /// </summary>
        protected virtual void PerformZoom()
        {
            if (ParentSurface == null) return;

            if (ParentSurface.ChartModifier != null)
                ParentSurface.ChartModifier.ResetInertia();

            var duration = IsAnimated ? TimeSpan.FromMilliseconds(500) : TimeSpan.Zero;

            if (XyDirection == XyDirection.XYDirection)
            {
                ParentSurface.AnimateZoomExtents(duration);
            }
            else if (XyDirection == XyDirection.YDirection)
            {
                ParentSurface.AnimateZoomExtentsY(duration);
            }
            else
            {
                ParentSurface.AnimateZoomExtentsX(duration);
            }
        }
        
        private DateTime _lastTap = DateTime.MinValue;

        private Point _lastTapPosition;

        /// <summary>
        /// Gets or sets maximum time between taps to be considered as double tap.
        /// </summary>
        public TimeSpan DoubleTapThreshold { get; set; }

        /// <summary>
        /// Called when a Multi-Touch Down interaction occurs on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation</param>
        public override void OnModifierTouchDown(ModifierTouchManipulationArgs e)
        {
            base.OnModifierTouchDown(e);
            if (e.Manipulators.Count() != 1 || ParentSurface == null || ParentSurface.RootGrid == null)
                return;
            var point = e.Manipulators.Single();
            if (point == null || point.Action != TouchAction.Down)
                return;
            var time = DateTime.Now;
            var place = point.Position;
            if (time - _lastTap < DoubleTapThreshold && PointUtil.Distance(place, _lastTapPosition) < 10d &&
                ParentSurface.RootGrid.IsPointWithinBounds(place) && ExecuteOn == ExecuteOn.MouseDoubleClick)
            {
                PerformZoom();
            }
        }

        /// <summary>
        /// Called when a Multi-Touch Up interaction occurs on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation</param>
        public override void OnModifierTouchUp(ModifierTouchManipulationArgs e)
        {
            base.OnModifierTouchUp(e);
            if (e.Manipulators.Count() != 1)
                return;
            _lastTap = DateTime.Now;
            _lastTapPosition = e.Manipulators.Single().Position;
        }
    }
}