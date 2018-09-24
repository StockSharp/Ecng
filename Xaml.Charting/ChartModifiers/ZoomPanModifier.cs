// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ZoomPanModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// The <see cref="ZoomPanModifier"/> provides a mouse drag to pan the X and Y axes.
    /// Add to a <see cref="UltrachartSurface"/> and set IsEnabled to true to enable this behaviour
    /// </summary>
    public class ZoomPanModifier : ZoomPanModifierBase
    {        
        private Dictionary<string,IRange> _startCategoryXRanges;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomPanModifier"/> class.
        /// </summary>
        public ZoomPanModifier()
        {
        }

        /// <summary>
        /// Called when a Mouse Button is pressed on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseDown(ModifierMouseArgs e)
        {
            base.OnModifierMouseDown(e);
            _startCategoryXRanges = XAxes.Where(x => x.IsCategoryAxis)
                .ToDictionary(x => x.Id, x => x.VisibleRange);
        }

        /// <summary>
        /// Receives zoom command from the user.
        /// </summary>
        /// <param name="currentPoint">Current point of zoom gesture.</param>
        /// <param name="lastPoint">Previous point of zoom gesture.</param>
        /// <param name="startPoint">Start point of zoom gesture.</param>
        public override void Pan(Point currentPoint, Point lastPoint, Point startPoint)
        {
            PerformPan(currentPoint, lastPoint, startPoint);
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

            _startCategoryXRanges = new Dictionary<string, IRange>();
        }

        private void PerformPan(Point currentPoint, Point lastPoint, Point startPoint)
        {
            var xDelta = currentPoint.X - lastPoint.X;
            var yDelta = lastPoint.Y - currentPoint.Y;

            using (ParentSurface.SuspendUpdates())
            {
                // Computation of new X-Range
                if (XyDirection != XyDirection.YDirection)
                {
                    // Scroll to new X-Axis range, based on start point (pixel), current point and the initial visible range
                    foreach (var xAxis in XAxes)
                    {
                        // don't pan on axes which have a different orientation than primary X axis
                        if (xAxis.IsHorizontalAxis != XAxis?.IsHorizontalAxis)
                            break;

                        using (var suspender = xAxis.SuspendUpdates())
                        {
                            suspender.ResumeTargetOnDispose = false;

                            var curXDelta = xDelta;
                            var curYDelta = yDelta;

                            // handles special case when X axis is category
                            if(xAxis.IsCategoryAxis)
                            {
                                xAxis.VisibleRange = _startCategoryXRanges[xAxis.Id];

                                curXDelta = currentPoint.X - startPoint.X;
                                curYDelta = startPoint.Y - currentPoint.Y;
                            }

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