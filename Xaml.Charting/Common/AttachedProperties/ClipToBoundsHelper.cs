// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ClipToBoundsHelper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Common
{
    /// <summary>
    /// Attached property which helps to set ClipToBounds property
    /// </summary>
    public static class ClipToBoundsHelper
    {
        /// <summary>
        /// Defines the ClipToBounds DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ClipToBoundsProperty = DependencyProperty.RegisterAttached("ClipToBounds", typeof(bool), typeof(ClipToBoundsHelper), new PropertyMetadata(false, OnClipToBoundsPropertyChanged));

        /// <summary>
        /// Defines the ClipToEllipseBounds DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ClipToEllipseBoundsProperty = DependencyProperty.RegisterAttached("ClipToEllipseBounds", typeof (bool), typeof (ClipToBoundsHelper), new PropertyMetadata(false,OnClipToEllipseBoundsPropertyChanged));

        /// <summary>
        /// Gets the ClipToBounds DependencyProperty value
        /// </summary>
        /// <param name="depObj">The dependencyObject target</param>
        /// <returns>The ClipToBounds property value</returns>
        public static bool GetClipToBounds(DependencyObject depObj)
        {
            return (bool)depObj.GetValue(ClipToBoundsProperty);
        }

        /// <summary>
        /// Sets the ClipToBounds DependencyProperty value. If true, the target object clips any child elements to the bounds when rendering.
        /// </summary>
        /// <param name="depObj">The dependencyObject target</param>
        /// <param name="clipToBounds">if set to <c>true</c> clip to bounds.</param>
        public static void SetClipToBounds(DependencyObject depObj, bool clipToBounds)
        {
            depObj.SetValue(ClipToBoundsProperty, clipToBounds);
        }

        /// <summary>
        /// Gets the ClipToEllipseBounds DependencyProperty value
        /// </summary>
        /// <param name="depObj"></param>
        /// <returns></returns>
        public static bool GetClipToEllipseBounds(DependencyObject depObj)
        {
            return (bool)depObj.GetValue(ClipToEllipseBoundsProperty);
        }

        /// <summary>
        /// Sets the ClipToEllipseBounds DependencyProperty value. If true, the target object clips any child elements to the ellipse bounds when rendering.
        /// </summary>
        /// <param name="depObj"></param>
        /// <param name="value"></param>
        public static void SetClipToEllipseBounds(DependencyObject depObj, bool value)
        {
            depObj.SetValue(ClipToEllipseBoundsProperty, value);
        }

        private static void OnClipToBoundsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element != null)
            {
#if !SILVERLIGHT
                element.ClipToBounds = (bool)e.NewValue;
#else
                ClipToBounds(element);
                element.Loaded += (sender, args) => ClipToBounds(sender as FrameworkElement);
                element.SizeChanged += (sender, args) => ClipToBounds(sender as FrameworkElement);
#endif
            }
        }

        private static void ClipToBounds(FrameworkElement element)
        {
            if (GetClipToBounds(element))
            {
                element.Clip = new RectangleGeometry()
                {
                    Rect = new Rect(0, 0, element.ActualWidth, element.ActualHeight)
                };
            }
            else
            {
                element.Clip = null;
            }
        }

        private static void OnClipToEllipseBoundsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element != null)
            {
                ClipToEllipseBounds(element);

                element.Loaded += (sender, args) => ClipToEllipseBounds(sender as FrameworkElement);
                element.SizeChanged += (sender, args) => ClipToEllipseBounds(sender as FrameworkElement);
            }
        }

        private static void ClipToEllipseBounds(FrameworkElement element)
        {
            if (GetClipToEllipseBounds(element))
            {
                var radiusX = element.ActualWidth / 2;
                var radiusY = element.ActualHeight / 2;

                element.Clip = new EllipseGeometry()
                {
                    Center = new Point(radiusX, radiusY),
                    RadiusX = radiusX,
                    RadiusY = radiusY
                };
            }
            else
            {
                element.Clip = null;
            }
        }

    }
}
