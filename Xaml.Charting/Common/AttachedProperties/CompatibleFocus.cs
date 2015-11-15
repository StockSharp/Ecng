// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// CompatibleFocus.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Input;

namespace Ecng.Xaml.Charting.Common
{
    /// <summary>
    /// A helper class which provides properties to control element's focus. 
    /// Compatible with both Silverlight and WPF.
    /// </summary>
    public class CompatibleFocus
    {
        /// <summary>
        /// Defines the IsFocusable DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsFocusableProperty =
            DependencyProperty.RegisterAttached("IsFocusable", typeof(bool), typeof(CompatibleFocus), new PropertyMetadata(true, OnIsFocusableChanged));

        /// <summary>
        /// Gets the IsFocusableProperty
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>IsFocusableProperty value</returns>
        public static bool GetIsFocusable(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFocusableProperty);
        }

        /// <summary>
        /// Sets the IsFocusableProperty
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="value">The IsFocusableProperty value</param>
        public static void SetIsFocusable(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFocusableProperty, value);
        }

        private static void OnIsFocusableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as Control;
            var isFocusable = (bool) e.NewValue;

            if (element != null)
            {
#if SILVERLIGHT
            element.IsTabStop = isFocusable;
#else
            element.Focusable = isFocusable;
#endif
            }
        }
    }
}
