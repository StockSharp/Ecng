// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// InertialZoomPanModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// The <see cref="InertialZoomPanModifier"/> provides a mouse drag to pan the X and Y axes.
    /// Add to a <see cref="UltrachartSurface"/> and set IsEnabled to true to enable this behaviour
    /// </summary>
    public class InertialZoomPanModifier : ZoomPanModifierBase
    {
        private readonly DispatcherTimer _timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="InertialZoomPanModifier"/> class.
        /// </summary>
        public InertialZoomPanModifier()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
            _timer.Tick += PerformPan;
        }

        /// <summary>
        /// Receives zoom command from the user.
        /// </summary>
        /// <param name="currentPoint">Current point of zoom gesture.</param>
        /// <param name="lastPoint">Previous point of zoom gesture.</param>
        /// <param name="startPoint">Start point of zoom gesture.</param>
        public override void Pan(Point currentPoint, Point lastPoint, Point startPoint)
        {
            AddPanAcceleration(currentPoint, lastPoint, startPoint);
        }

        private double _xSpeed, _ySpeed, _xCatSpeed, _yCatSpeed;

        private const double XFriction = 10;
        private const double YFriction = 10;
        private const double MaxSpeed = 250;

        private void AddPanAcceleration(Point currentPoint, Point lastPoint, Point startPoint)
        {
            _xSpeed += currentPoint.X - lastPoint.X;
            _ySpeed += lastPoint.Y - currentPoint.Y;
            _xCatSpeed += currentPoint.X - startPoint.X;
            _yCatSpeed += startPoint.Y - currentPoint.Y;

            _xSpeed = Math.Max(Math.Min(_xSpeed, MaxSpeed), -MaxSpeed);
            _ySpeed = Math.Max(Math.Min(_ySpeed, MaxSpeed), -MaxSpeed);
            _xCatSpeed = Math.Max(Math.Min(_xCatSpeed, MaxSpeed), -MaxSpeed);
            _yCatSpeed = Math.Max(Math.Min(_yCatSpeed, MaxSpeed), -MaxSpeed);

            if (!_timer.IsEnabled)
                _timer.Start();
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
        }

        /// <summary>
        /// Instantly stops any inertia that can be associated with this modifier.
        /// </summary>
        public override void ResetInertia()
        {
            _timer.Stop();
            _xSpeed = _ySpeed = _xCatSpeed = _yCatSpeed = 0;
        }

        private void PerformPan(object sender, EventArgs eventArgs)
        {
            var xDelta = _xSpeed;
            var yDelta = _ySpeed;
            var xCatDelta = _xCatSpeed;
            var yCatDelta = _yCatSpeed;

            _xSpeed = _xSpeed > 0 ? Math.Max(0, _xSpeed - XFriction) : Math.Min(0, _xSpeed + XFriction);
            _ySpeed = _ySpeed > 0 ? Math.Max(0, _ySpeed - YFriction) : Math.Min(0, _ySpeed + YFriction);
            _xCatSpeed = _xCatSpeed > 0 ? Math.Max(0, _xCatSpeed - XFriction) : Math.Min(0, _xCatSpeed + XFriction);
            _yCatSpeed = _yCatSpeed > 0 ? Math.Max(0, _yCatSpeed - YFriction) : Math.Min(0, _yCatSpeed + YFriction);

            if (Math.Abs(_xSpeed) <= 0 && Math.Abs(_ySpeed) <= 0 && Math.Abs(_xCatSpeed) <= 0 && Math.Abs(_yCatSpeed) <= 0)
            {
                _timer.Stop();
            }

            if (ParentSurface == null)
            {
                _timer.Stop();
                return;
            }

            using (ParentSurface.SuspendUpdates())
            {
                // Computation of new X-Range
                if (XyDirection != XyDirection.YDirection)
                {
                    // Scroll to new X-Axis range, based on start point (pixel), current point and the initial visible range
                    foreach (var xAxis in XAxes)
                    {
                        // don't pan on axes which have a different orientation than primary X axis
                        if (xAxis.IsHorizontalAxis != XAxis.IsHorizontalAxis)
                            break;

                        using (var suspender = xAxis.SuspendUpdates())
                        {
                            suspender.ResumeTargetOnDispose = false;

                            var curXDelta = xDelta;
                            var curYDelta = yDelta;

                            // handles special case when X axis is category
                            if (xAxis.IsCategoryAxis)
                            {
                               // xAxis.VisibleRange = _startCategoryXRanges[xAxis.Id];

                                curXDelta = xCatDelta;
                                curYDelta = yCatDelta;
                            }

                            curXDelta *= 0.5;
                            curYDelta *= 0.5;

                            xAxis.Scroll(xAxis.IsHorizontalAxis ? curXDelta : -curYDelta, ClipModeX);
                        }
                    }
                }

                if (XyDirection == XyDirection.XDirection)
                {
                    if (ZoomExtentsY)
                        ParentSurface.ZoomExtentsY();
                    return;
                }

                // Computation of new Y-Range.
                foreach (var yAxis in YAxes)
                {
                    yAxis.Scroll(yAxis.IsHorizontalAxis ? -xDelta : yDelta, ClipMode.None);
                }
            }
        }
    }
}