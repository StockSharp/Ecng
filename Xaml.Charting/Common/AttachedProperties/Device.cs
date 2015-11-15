// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// Device.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Common
{
    /// <summary>
    /// Used to show or hide UIElements based on framework (WPF, Silverlight)
    /// </summary>
    public class Device : FrameworkElement
    {
        /// <summary>
        /// Defines the SnapsToDevicePixels DependencyProperty
        /// </summary>
        public static readonly 
#if !SILVERLIGHT
            new 
#endif
            DependencyProperty SnapsToDevicePixelsProperty =
            DependencyProperty.RegisterAttached("SnapsToDevicePixels", typeof(bool), typeof(Device), new PropertyMetadata(false, OnSnapToDevicePixelsPropertyChanged));

        /// <summary>
        /// Sets the SnapsToDevicePixels attached property on the specified DependencyObject
        /// </summary>
        /// <param name="element">The DependencyObject</param>
        /// <param name="snapToDevicePixels">The value of the SnapsToDevicePixels attached property to set</param>
        public static void SetSnapsToDevicePixels(DependencyObject element, bool snapToDevicePixels)
        {
            element.SetValue(SnapsToDevicePixelsProperty, snapToDevicePixels);
        }

        /// <summary>
        /// Gets the SnapsToDevicePixels attached property from the specified DependencyObject
        /// </summary>
        /// <param name="element">The DependencyObject</param>
        /// <return>The value of the SnapsToDevicePixels attached property</return>
        public static bool GetSnapsToDevicePixels(DependencyObject element)
        {
            return (bool)element.GetValue(SnapsToDevicePixelsProperty);
        }

        private static void OnSnapToDevicePixelsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = (FrameworkElement) d;
            bool value = (bool)e.NewValue;
            element.SetCurrentValue(UseLayoutRoundingProperty, value);
#if !SILVERLIGHT
            element.SetCurrentValue(SnapsToDevicePixelsProperty, value);
#endif
        }
    }
}