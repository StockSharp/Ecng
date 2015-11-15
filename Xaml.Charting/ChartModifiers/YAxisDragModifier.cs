// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// YAxisDragModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// The <see cref="YAxisDragModifier"/> provides a mouse drag to scale the Y-Axis. 
    /// This behaviour scales the axis in a different direction depending on which half of the axis the user starts the operation in
    /// Add to a <see cref="UltrachartSurface"/> and set IsEnabled to true to enable this behaviour
    /// </summary>
    public class YAxisDragModifier : AxisDragModifierBase
    {
        /// <summary>
        /// Gets the <see cref="IAxis" /> instance, which current modifier is associated with, on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <returns></returns>
        protected override IAxis GetCurrentAxis()
        {
            return GetYAxis(AxisId);
        }

        /// <summary>
        /// Depending on axis orientation and AxisAlignment, returns a Cursor to show during mouse-over of the axis
        /// </summary>
        /// <param name="axis">The axis instance</param>
        protected override Cursor GetUsedCursor(IAxis axis)
        {
            return axis.IsPolarAxis ? Cursors.None : base.GetUsedCursor(axis);
        }

        /// <summary>
        /// Gets whether the specified mouse point is within the second (right-most or top-most) half of the Axis bounds
        /// </summary>
        /// <param name="point">The mouse point</param>
        /// <param name="axisBounds">The axis bounds</param>
        /// <param name="isHorizontalAxis">Value, which indicates whether current axis is horizontal or not</param>
        /// <returns>
        /// True if the point is within the second (right-most or top-most) half of the axis bounds, else false
        /// </returns>
        protected override bool GetIsSecondHalf(Point point, Rect axisBounds, bool isHorizontalAxis)
        {
            if (XAxis.IsPolarAxis)
            {
                axisBounds.Height /= 2;
                return !axisBounds.Contains(point);
            }

            return !base.GetIsSecondHalf(point, axisBounds, isHorizontalAxis);
        }

        /// <summary>
        /// Peforms a pan on the assocaited <see cref="AxisBase" />. The pan is considered to be a drag from <paramref name="currentPoint" /> to <paramref name="lastPoint" />
        /// </summary>
        /// <param name="currentPoint">The current mouse point</param>
        /// <param name="lastPoint">The last mouse point</param>
        protected override void PerformPan(Point currentPoint, Point lastPoint)
        {
            var axis = GetCurrentAxis();

            var xDelta = currentPoint.X - lastPoint.X;
            var yDelta = lastPoint.Y - currentPoint.Y;

            axis.Scroll(axis.IsHorizontalAxis ? -xDelta : yDelta, ClipMode.None);
        }

        /// <summary>
        /// When overriden in a derived class, calculates an output <see cref="IRange" /> to apply to the associated <see cref="AxisBase">Axis</see>,
        /// given the input parameters
        /// </summary>
        /// <param name="currentPoint">The current mouse position</param>
        /// <param name="lastPoint">The last mouse position</param>
        /// <param name="isSecondHalf">A flag, which determines how the scale operates, e.g. which half of the axis (top or bottom, left or right) was dragged</param>
        /// <param name="axis">The axis being operated on</param>
        /// <returns>
        /// The output <see cref="IRange" />
        /// </returns>
        protected override IRange CalculateScaledRange(Point currentPoint, Point lastPoint, bool isSecondHalf, IAxis axis)
        {
            var interactivityHelper = axis.GetCurrentInteractivityHelper();

            var xDelta = currentPoint.X - lastPoint.X;
            var yDelta = lastPoint.Y - currentPoint.Y;

            var pixelsToScroll = axis.IsHorizontalAxis ? -xDelta : yDelta;
            var scaledRange = isSecondHalf
                                  ? interactivityHelper.ScrollInMaxDirection(axis.VisibleRange, pixelsToScroll)
                                  : interactivityHelper.ScrollInMinDirection(axis.VisibleRange, pixelsToScroll);

            if (axis.VisibleRangeLimit != null)
            {
                scaledRange.ClipTo(axis.VisibleRangeLimit, axis.VisibleRangeLimitMode);
            }

            return scaledRange;
        }

        /// <summary>
        /// Called when a Mouse Button is pressed on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        public override void OnModifierMouseDown(ModifierMouseArgs e)
        {
            if (e.IsMaster)
            {
                base.OnModifierMouseDown(e);
            }
        }

        /// <summary>
        /// Called when the Mouse is moved on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the mouse move operation</param>
        public override void OnModifierMouseMove(ModifierMouseArgs e)
        {
            if (e.IsMaster)
            {
                base.OnModifierMouseMove(e);
            }
        }

        /// <summary>
        /// Called when a Mouse Button is released on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        public override void OnModifierMouseUp(ModifierMouseArgs e)
        {
            if (e.IsMaster)
            {
                base.OnModifierMouseUp(e);
            }
        }
    }
}