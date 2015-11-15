// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RelativeZoomModifierBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Text;
using System.Windows;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Defines the base class to modifiers which performs relative zoom, such as the <see cref="MouseWheelZoomModifier"/> and <see cref="PinchZoomModifier"/>.
    /// </summary>
    public abstract class RelativeZoomModifierBase : ChartModifierBase
    {
        /// <summary>
        /// Defines the XyDirection dependency property
        /// </summary>
        public static readonly DependencyProperty XyDirectionProperty =
            DependencyProperty.Register("XyDirection", typeof(XyDirection), typeof(RelativeZoomModifierBase), new PropertyMetadata(XyDirection.XYDirection));

        private double _growFactor;

        /// <summary>
        /// Gets or sets the <see cref="XyDirection"/> to restrict zoom interactivity to.
        /// </summary>
        public XyDirection XyDirection
        {
            get { return (XyDirection)GetValue(XyDirectionProperty); }
            set { SetValue(XyDirectionProperty, value); }
        }

        /// <summary>
        /// Gets or sets the grow factor to scale (or shrink) both axes on mouse wheel
        /// </summary>
        /// <value>The grow factor.</value>
        /// <remarks></remarks>
        public double GrowFactor
        {
            get { return _growFactor; }
            set
            {
                Guard.Assert(value, "GrowFactor").IsGreaterThan(0.0);
                _growFactor = value;
            }
        }

        /// <summary>
        /// Performs a zoom on all X and Y Axis around the <paramref name="mousePoint" /> by the specified X and Y factor
        /// </summary>
        /// <param name="mousePoint">The mouse point.</param>
        /// <param name="xValue">The x zoom factor.</param>
        /// <param name="yValue">The y zoom factor.</param>
        protected virtual void PerformZoom(Point mousePoint, double xValue, double yValue)
        {
            if (XyDirection == XyDirection.YDirection || XyDirection == XyDirection.XYDirection)
            {
                PerformZoomBy(GrowFactor * yValue, mousePoint, YAxes, "Growing YRange: {0}");
            }

            if (XyDirection == XyDirection.XDirection || XyDirection == XyDirection.XYDirection)
            {
                PerformZoomBy(GrowFactor * xValue, mousePoint, XAxes, "Growing XRange: {0}");
            }
        }

        private void PerformZoomBy(double fraction, Point mousePoint, IEnumerable<IAxis> axisCollection, string logMessage)
        {
            foreach (var axis in axisCollection)
            {
                // Computation of new visible range
                GrowBy(mousePoint, axis, fraction);
            }

            UltrachartDebugLogger.Instance.WriteLine(logMessage, fraction);
        }

        /// <summary>
        /// Performs a zoom on a specific axis around the <paramref name="mousePoint" /> by the specified scale factor
        /// </summary>
        /// <param name="mousePoint">The mouse point.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="fraction">The scale factor.</param>
        protected void GrowBy(Point mousePoint, IAxis axis, double fraction)
        {
            double size = GetAxisDimension(axis);
            double coord = axis.IsHorizontalAxis ? mousePoint.X : (size - mousePoint.Y);

            // Compute relative fractions to expand or contract the axis Visiblerange by
            double lowFraction = (coord / size) * fraction;
            double highFraction = (1.0 - (coord / size)) * fraction;

            var isVerticalChart = (axis.IsHorizontalAxis && !axis.IsXAxis) || (!axis.IsHorizontalAxis && axis.IsXAxis);
            var flipCoords = (isVerticalChart && !axis.FlipCoordinates) || (!isVerticalChart && axis.FlipCoordinates);

            if (flipCoords)
            {
                NumberUtil.Swap(ref lowFraction, ref highFraction);
            }

            axis.ZoomBy(lowFraction, highFraction);
        }

        private double GetAxisDimension(IAxis axis)
        {
            double size = axis.IsHorizontalAxis ? axis.Width : axis.Height;

            var parentSurface = axis.ParentSurface as UltrachartSurface;
            // if axis.Visibility==Collapsed, try to get appropriate dimension from the ParentSurface
            if (axis.Visibility == Visibility.Collapsed && parentSurface != null)
            {
                size = axis.IsHorizontalAxis ? parentSurface.ActualWidth : parentSurface.ActualHeight;
            }

            return size;
        }
    }
}
