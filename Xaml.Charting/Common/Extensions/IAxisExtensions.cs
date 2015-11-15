// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IAxisExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using Ecng.Xaml.Charting.Themes;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class AxisExtensions
    {
        internal static bool TrySetOrAnimateVisibleRange(this IAxis axis, IRange newRange, TimeSpan duration)
        {
            if (axis.AutoRange != AutoRange.Always && axis.IsValidRange(newRange) && newRange != null && newRange.Equals(axis.VisibleRange) == false)
            {
                if (duration.IsZero())
                {
                    axis.VisibleRange = newRange;
                }
                else
                {
                    axis.AnimateVisibleRangeTo(newRange, duration);
                }

                return true;
            }

            return false;
        }

        internal static void SetVerticalOffset(this IAxis axis, FrameworkElement axisLabel, Point mousePoint)
        {
            axisLabel.SetValue(AxisCanvas.TopProperty, double.NaN);
            axisLabel.SetValue(AxisCanvas.CenterTopProperty, double.NaN);

            if (axis.IsHorizontalAxis)
            {
                // Make the label clinging to the axis
                var property = axis.AxisAlignment == AxisAlignment.Bottom
                    ? AxisCanvas.TopProperty
                    : AxisCanvas.BottomProperty;

                // If the label is too long, overlap the chart area
                if (axisLabel.ActualHeight >= axis.Height)
                {
                    property = property == AxisCanvas.TopProperty ? AxisCanvas.BottomProperty : AxisCanvas.TopProperty;
                }

                axisLabel.SetValue(property, 0d);
            }
            else
            {
                AxisCanvas.SetCenterTop(axisLabel, mousePoint.Y);
            }
        }

        internal static void SetHorizontalOffset(this IAxis axis, FrameworkElement axisLabel, Point mousePoint)
        {
            axisLabel.SetValue(AxisCanvas.LeftProperty, double.NaN);
            axisLabel.SetValue(AxisCanvas.CenterLeftProperty, double.NaN);

            if (axis.IsHorizontalAxis)
            {
                AxisCanvas.SetCenterLeft(axisLabel, mousePoint.X);
            }
            else
            {
                // Make the label clinging to the axis
                var property = axis.AxisAlignment == AxisAlignment.Right
                    ? AxisCanvas.LeftProperty
                    : AxisCanvas.RightProperty;

                // If the label is too wide, overlap the chart area
                if (axisLabel.ActualWidth >= axis.ActualWidth)
                {
                    property = property == AxisCanvas.LeftProperty ? AxisCanvas.RightProperty : AxisCanvas.LeftProperty;
                }

                axisLabel.SetValue(property, 0d);
            }
        }
    }
}
