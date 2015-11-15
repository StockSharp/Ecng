// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisLayoutHelper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Controls;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Common
{
    /// <summary>
    /// Attached properties to assist with Axis layout. Used internally by Ultrachart in the Themes
    /// </summary>
    public class AxisLayoutHelper
    {
        /// <summary>
        /// The axis alignment property
        /// </summary>
        public static readonly DependencyProperty AxisAlignmentProperty = DependencyProperty.RegisterAttached("AxisAlignment", typeof(AxisAlignment), typeof(AxisLayoutHelper), new PropertyMetadata(AxisAlignment.Default, OnAxisAlignmentChanged));

        /// <summary>
        /// Gets the axis alignment.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static AxisAlignment GetAxisAlignment(DependencyObject obj)
        {
            return (AxisAlignment)obj.GetValue(AxisAlignmentProperty);
        }

        /// <summary>
        /// Sets the axis alignment.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="value">The value.</param>
        public static void SetAxisAlignment(DependencyObject obj, AxisAlignment value)
        {
            obj.SetValue(AxisAlignmentProperty, value);
        }

        /// <summary>
        /// The is inside item property
        /// </summary>
        public static readonly DependencyProperty IsInsideItemProperty = DependencyProperty.RegisterAttached("IsInsideItem", typeof(bool), typeof(AxisLayoutHelper), new PropertyMetadata(false, (d, e) => OnAxisAlignmentChanged(((FrameworkElement)d).Parent, e)));

        /// <summary>
        /// Gets the is inside item.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static bool GetIsInsideItem(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsInsideItemProperty);
        }

        /// <summary>
        /// Sets the is inside item.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public static void SetIsInsideItem(DependencyObject obj, bool value)
        {
            obj.SetValue(IsInsideItemProperty, value);
        }

        /// <summary>
        /// The is outside item property
        /// </summary>
        public static readonly DependencyProperty IsOutsideItemProperty = DependencyProperty.RegisterAttached("IsOutsideItem", typeof(bool), typeof(AxisLayoutHelper), new PropertyMetadata(false, (d, e) => OnAxisAlignmentChanged(((FrameworkElement)d).Parent, e)));

        /// <summary>
        /// Gets the is outside item.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static bool GetIsOutsideItem(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsOutsideItemProperty);
        }

        /// <summary>
        /// Sets the is outside item.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public static void SetIsOutsideItem(DependencyObject obj, bool value)
        {
            obj.SetValue(IsOutsideItemProperty, value);
        }

        private static void OnAxisAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = d as StackPanel;
            if (panel != null)
            {
                UpdateItemsOrder(panel);
            }
        }

        internal static void UpdateItemsOrder(StackPanel panel)
        {
            if (panel.FlowDirection == FlowDirection.RightToLeft) return;

            var alignment = (AxisAlignment) panel.GetValue(AxisAlignmentProperty);

            var isHorizontal = alignment == AxisAlignment.Bottom || alignment == AxisAlignment.Top;

            panel.Orientation = isHorizontal ? Orientation.Vertical : Orientation.Horizontal;

            var insideItem = (FrameworkElement) panel.Children.SingleOrDefault(elem => (bool) elem.GetValue(IsInsideItemProperty));
            var outsideItem = (FrameworkElement) panel.Children.SingleOrDefault(elem => (bool) elem.GetValue(IsOutsideItemProperty));

            var isLeftTop = alignment == AxisAlignment.Left || alignment == AxisAlignment.Top;

            //if (insideItem != null && panel.Children.IndexOf(insideItem) != index)
            
            panel.SafeRemoveChild(insideItem);
            panel.SafeRemoveChild(outsideItem);
            if (isLeftTop)
            {
                panel.SafeAddChild(outsideItem, 0);
                panel.SafeAddChild(insideItem);
            }
            else
            {
                panel.SafeAddChild(insideItem, 0);
                panel.SafeAddChild(outsideItem);
            }
        }
    }
}