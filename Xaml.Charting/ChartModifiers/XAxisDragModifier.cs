// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// XAxisDragModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// The <see cref="XAxisDragModifier"/> provides a mouse drag to scale the X-Axis. 
    /// This behaviour scales the axis in a different direction depending on which half of the axis the user starts the operation in
    /// Add to a <see cref="UltrachartSurface"/> and set IsEnabled to true to enable this behaviour
    /// </summary>
    public class XAxisDragModifier : AxisDragModifierBase
    {
        /// <summary>
        /// Defines the ClipToExtentsX Dependency Property 
        /// </summary>
        public static readonly DependencyProperty ClipModeXProperty = DependencyProperty.Register("ClipModeX", typeof(ClipMode), typeof(XAxisDragModifier), new PropertyMetadata(ClipMode.ClipAtExtents));

        private Dictionary<string, IRange> _startCategoryXRanges;
        private Point _startPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="XAxisDragModifier"/> class.
        /// </summary>
        public XAxisDragModifier()
        {
            IsPolarChartSupported = false;
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
        /// Gets the <see cref="IAxis" /> instance, which current modifier is associated with, on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <returns></returns>
        protected override IAxis GetCurrentAxis()
        {
            return GetXAxis(AxisId);
        }

        /// <summary>
        /// Called when a Mouse Button is pressed on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        public override void OnModifierMouseDown(ModifierMouseArgs e)
        {
            base.OnModifierMouseDown(e);

            _startPoint = e.MousePoint;

            _startCategoryXRanges = XAxes.Where(x => x.IsCategoryAxis)
                .ToDictionary(x => x.Id, x => x.VisibleRange);
        }

        /// <summary>
        /// Called when a Mouse Button is released on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        public override void  OnModifierMouseUp(ModifierMouseArgs e)
        {
            base.OnModifierMouseUp(e);

            _startPoint = default(Point);
            _startCategoryXRanges = new Dictionary<string, IRange>();
        }

        /// <summary>
        /// Peforms a pan on the assocaited <see cref="AxisBase" />. The pan is considered to be a drag from <paramref name="currentPoint" /> to <paramref name="lastPoint" />
        /// </summary>
        /// <param name="currentPoint">The current mouse point</param>
        /// <param name="lastPoint">The last mouse point</param>
        protected override void PerformPan(Point currentPoint, Point lastPoint)
        {
            var axis = GetCurrentAxis();

            var pixelsToScroll = PrepareForScrolling(axis, currentPoint, lastPoint);

            axis.Scroll(pixelsToScroll, ClipModeX);
        }

        private double PrepareForScrolling(IAxis axis, Point currentPoint, Point lastPoint)
        {
            var xDelta = currentPoint.X - lastPoint.X;
            var yDelta = lastPoint.Y - currentPoint.Y;

            // handles special case when X axis is category
            if (axis.IsCategoryAxis)
            {
                axis.VisibleRange = _startCategoryXRanges[axis.Id];

                xDelta = currentPoint.X - _startPoint.X;
                yDelta = _startPoint.Y - currentPoint.Y;
            }

            var pixelsToScroll = axis.IsHorizontalAxis ? xDelta : -yDelta;
            return pixelsToScroll;
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

            var pixelsToScroll = PrepareForScrolling(axis, currentPoint, lastPoint);

            var scaledRange = isSecondHalf
                                  ? interactivityHelper.ScrollInMaxDirection(axis.VisibleRange, pixelsToScroll)
                                  : interactivityHelper.ScrollInMinDirection(axis.VisibleRange, pixelsToScroll);

            //Don't clip the range when RelativeScale is used
            //because IAxis.GetMaximumRange() uses GrowBy in max range calculation
            if (axis.AutoRange != AutoRange.Always)
            {
                scaledRange = ClipRange(scaledRange, pixelsToScroll, isSecondHalf, axis);
            }

            return scaledRange;
        }

        private IRange ClipRange(IRange scaledRange, double pixelsToScroll, bool isSecondHalf, IAxis axis)
        {
            /*
                - ClipMode.None means you can pan right off the edge of the data into uncharted space. 
                - ClipMode.StretchAtExtents causes a zooming (stretch) action when you reach the edge of the data. 
                - ClipAtExtents forces the panning operation to stop suddenly at the extents of the data
                - ClipAtMin forces the panning operation to stop suddenly at the minimum of the data, but expand at the maximum
            */

            if (ClipModeX != ClipMode.None)
            {
                var interactivityHelper = axis.GetCurrentInteractivityHelper();

                var maximumRange = axis.GetMaximumRange();
                var clippedRange = ((IRange) scaledRange.Clone()).ClipTo(maximumRange);

                var clipAtMin = (clippedRange.Min.CompareTo(scaledRange.Min) != 0);
                var clipAtMax = (clippedRange.Max.CompareTo(scaledRange.Max) != 0);

                if (isSecondHalf)
                {
                    if (clipAtMax)
                    {
                        if (ClipModeX != ClipMode.ClipAtMin)
                        {
                            scaledRange = RangeFactory.NewWithMinMax(axis.VisibleRange, scaledRange.Min, clippedRange.Max);
                        }

                        if (ClipModeX == ClipMode.StretchAtExtents)
                        {
                            scaledRange = interactivityHelper.ScrollInMinDirection(axis.VisibleRange, pixelsToScroll);
                        }
                    }
                }
                else
                {
                    if (clipAtMin)
                    {
                        if (ClipModeX != ClipMode.ClipAtMax)
                        {
                            scaledRange = RangeFactory.NewWithMinMax(axis.VisibleRange, clippedRange.Min, scaledRange.Max);
                        }

                        if (ClipModeX == ClipMode.StretchAtExtents)
                        {
                            scaledRange = interactivityHelper.ScrollInMaxDirection(axis.VisibleRange, pixelsToScroll);
                        }
                    }
                }
            }

            return scaledRange;
        }
    }
}