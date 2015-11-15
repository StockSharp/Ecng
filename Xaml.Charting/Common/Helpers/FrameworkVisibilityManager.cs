// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FrameworkVisibilityManager.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Enumeration constants to define FrameworkVisibility
    /// </summary>
    public enum FrameworkVisibility
    {
        /// <summary>
        /// Show this element in all frameworks
        /// </summary>
        All,

        /// <summary>
        /// Show this element in WPF only
        /// </summary>
        Wpf, 

        /// <summary>
        /// Show this element in Silverlight only
        /// </summary>
        Silverlight
    }

    /// <summary>
    /// Used to show or hide UIElements based on framework (WPF, Silverlight)
    /// </summary>
    public class FrameworkVisibilityManager : FrameworkElement
    {
        /// <summary>
        /// Defines the VisibleIn DependencyProperty, used to set which frameworks (WPF, Silverlight, All) an element is visible in
        /// </summary>
        public static readonly DependencyProperty VisibleInProperty = DependencyProperty.RegisterAttached("VisibleIn", typeof(FrameworkVisibility), typeof(FrameworkVisibilityManager), new PropertyMetadata(FrameworkVisibility.All, OnVisibleInPropertyChanged));

        /// <summary>
        /// Sets the VisibleIn DependencyProperty, used to set which frameworks (WPF, Silverlight, All) an element is visible in
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="visibleIn">The <see cref="FrameworkVisibility"/> enum</param>
        public static void SetVisibleIn(DependencyObject element, FrameworkVisibility visibleIn)
        {
            element.SetValue(VisibleInProperty, visibleIn);
        }

        /// <summary>
        /// Gets the VisibleIn DependencyProperty, used to set which frameworks (WPF, Silverlight, All) an element is visible in
        /// </summary>
        /// <param name="element">The element.</param>
        public static FrameworkVisibility GetVisibleIn(DependencyObject element)
        {
            return (FrameworkVisibility)element.GetValue(VisibleInProperty);
        }

        private static void OnVisibleInPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
#if SILVERLIGHT
            var visibility = ((FrameworkVisibility) e.NewValue) == FrameworkVisibility.Wpf ? Visibility.Collapsed : Visibility.Visible;
#else
            var visibility = ((FrameworkVisibility) e.NewValue) == FrameworkVisibility.Silverlight ? Visibility.Collapsed : Visibility.Visible;
#endif

            (d as UIElement).Visibility = visibility;
        }
    }
}
