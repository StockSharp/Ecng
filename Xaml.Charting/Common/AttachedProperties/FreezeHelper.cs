// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FreezeHelper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Common.AttachedProperties
{
    internal class FreezeHelper
    {
        public static readonly DependencyProperty FreezeProperty =
            DependencyProperty.RegisterAttached("Freeze", typeof(bool), typeof(FreezeHelper), new PropertyMetadata(false, OnFreezePropertyChanged));

        private static void OnFreezePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
#if !SILVERLIGHT
            // No Freezable in Silverlight 
            var f = d as Freezable;
            if (f != null)
            {
                if (true.Equals(e.NewValue) && f.CanFreeze)
                    f.Freeze();
            }
#endif
        }

        public static void SetFreeze(DependencyObject element, bool value)
        {
            element.SetValue(FreezeProperty, value);
        }

        public static bool GetFreeze(DependencyObject element)
        {
            return (bool) element.GetValue(FreezeProperty);
        }
    }
}
