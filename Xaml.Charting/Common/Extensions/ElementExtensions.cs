// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ElementExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class HitTestableExtensions
    {
        /// <summary>
        /// Returns true if the point is inside the bounds of the HitTestable, when translated relative to RootGrid
        /// </summary>
        internal static bool IsPointWithinBounds(this IHitTestable hitTestable, Point point)
        {
            // Tried and tested. Why it works we don't know
            // Returns true if the point is inside the bounds of the HitTestable, when translated relative to RootGrid
            return hitTestable.GetBoundsRelativeTo(hitTestable).Contains(point);
        }
    }

    internal static class ElementExtensions
    {
        internal static void MeasureArrange(this UIElement element)
        {
            element.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            element.Arrange(new Rect(new Point(0, 0), element.DesiredSize));
        }

        internal static bool IsVisible(this UIElement element)
        {
            return element.Visibility == Visibility.Visible;
        }

        internal static bool IsInVisualTree(this DependencyObject element)
        {
            var appRoot =
#if SILVERLIGHT
                Application.Current.RootVisual;
#else
                Application.Current.MainWindow;
#endif

            return IsInVisualTree(element, appRoot);
        }

        internal static bool IsInVisualTree(this DependencyObject element, DependencyObject ancestor)
        {
            DependencyObject fe = element;
            while (fe != null)
            {
                if (fe == ancestor)
                {
                    return true;
                }

                fe = VisualTreeHelper.GetParent(fe) as DependencyObject;
            }

            return false;
        }

        internal static Point TranslatePoint(this FrameworkElement element, Point point, IHitTestable relativeTo)
        {
            var thatUiElement = relativeTo as UIElement;

            if (thatUiElement != null)
            {
                return element.TranslatePoint(point, thatUiElement);
            }

            return new Point(0, 0);
        }

        internal static bool IsPointWithinBounds(this FrameworkElement element, Point point)
        {
            var tPoint = element.TranslatePoint(point, element);

            bool withinBounds = (tPoint.X <= element.ActualWidth && tPoint.X >= 0)
                                && (tPoint.Y <= element.ActualHeight && tPoint.Y >= 0);

            return withinBounds;
        }

        internal static Rect GetBoundsRelativeTo(this FrameworkElement element, IHitTestable relativeTo)
        {
            var another = relativeTo as UIElement;
            Rect? result = null;

            if (another != null)
            {
                result = element.GetBoundsRelativeTo(another);
            }

            return result.HasValue ? result.Value : Rect.Empty;
        }

#if SILVERLIGHT

        internal static object TryFindResource(this FrameworkElement element, object resourceKey)
        {
            if (element.Resources.Contains(resourceKey))
                return element.Resources[resourceKey];

            return null;
        }

        /// <summary>
        /// Translates the point relative to the input UI Element
        /// </summary>
        /// <param name="element"></param>
        /// <param name="point"></param>
        /// <param name="relativeTo"></param>
        /// <returns></returns>
        internal static Point TranslatePoint(this UIElement element, Point point, UIElement relativeTo)
        {
            GeneralTransform gt = element.TransformToVisual(relativeTo);
            return gt.Transform(point);
        }
#endif

        /// <summary>
        /// Get the bounds of an element relative to another element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="otherElement">
        /// The element relative to the other element.
        /// </param>
        /// <returns>
        /// The bounds of the element relative to another element, or null if
        /// the elements are not related.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="element"/> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="otherElement"/> is null.
        /// </exception>
        internal static Rect? GetBoundsRelativeTo(this FrameworkElement element, UIElement otherElement)
        {
            try
            {
                Point origin, bottom;
                GeneralTransform transform = element.TransformToVisual(otherElement);
                if (transform != null &&
                    transform.TryTransform(new Point(), out origin) &&
                    transform.TryTransform(new Point(element.ActualWidth, element.ActualHeight), out bottom))
                {
                    return new Rect(origin, bottom);
                }
            }
            catch
            {
                // Ignore any exceptions thrown while trying to transform
            }

            return null;
        }

        /// <summary>
        /// Finds the logical parent of the <see cref="FrameworkElement"/> 
        /// </summary>
        /// <typeparam name="T">The type of parent to find</typeparam>
        /// <param name="frameworkElement">The FrameworkElement.</param>
        /// <returns></returns>
        public static T FindLogicalParent<T>(this FrameworkElement frameworkElement) where T : FrameworkElement
        {
            var parent = frameworkElement.Parent as FrameworkElement;

            if (parent == null || parent is T)
                return (T)parent;

            return parent.FindLogicalParent<T>();
        }

        public static T FindVisualParent<T>(this UIElement element) where T : UIElement
        {
            UIElement parent = element;
            while (parent != null)
            {
                T correctlyTyped = parent as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }
                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }

        public static WriteableBitmap RenderToBitmap(this FrameworkElement element)
        {
#if SILVERLIGHT
            int width = (int) element.DesiredSize.Width;
            if (width == 0) width = (int)element.ActualWidth;
            int height = (int) element.DesiredSize.Height;
            if (height == 0) height = (int)element.ActualHeight;
            element.InvalidateMeasure();
            var result = new WriteableBitmap(width, height);
            result.Render(element, new TranslateTransform());
            result.Invalidate();
            return result;
#else
            element.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            element.Arrange(new Rect(new Point(0, 0), element.DesiredSize));
            int width = (int) element.DesiredSize.Width;
            int height = (int) element.DesiredSize.Height;
            var bmp = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

            bmp.Render(element);
            return new WriteableBitmap(bmp);
#endif
        }

        public static WriteableBitmap RenderToBitmap(this FrameworkElement element, int width, int height)
        {
#if SILVERLIGHT
            var result = new WriteableBitmap(width, height);
            result.Render(element, new TranslateTransform());
            result.Invalidate();
            return result;
#else
            var bmp = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(element);
            return new WriteableBitmap(bmp);
#endif
        }        

        public static void RegisterForNotification(this FrameworkElement element, string propertyName, PropertyChangedCallback callback)
        {
            // Bind to a dependency property
            Binding b = new Binding(propertyName) { Source = element };
            
            var prop = DependencyProperty.RegisterAttached(
                "ListenAttached" + propertyName,
                typeof(object),
                element.GetType(),
                new PropertyMetadata(callback));

            element.SetBinding(prop, b);
        }

        internal static void ThreadSafeSetValue(this FrameworkElement element, DependencyProperty property,
                                                object value)
        {
            var dispatcher = element.Dispatcher;
            Action setter = () => element.SetValue(property, value);
            if (dispatcher.CheckAccess())
            {
                setter();
                return;
            }

            dispatcher.BeginInvoke(setter);
        }

        internal static void Schedule(this FrameworkElement element, DispatcherPriority priority, Action action)
        {            
            Guard.NotNull(element, "element");

            if (DispatcherUtil.GetTestMode())
            {
                action();
                return;
            }

            var dispatcherInstance = element.Dispatcher;

#if !SILVERLIGHT
            dispatcherInstance.BeginInvoke(action, priority);
#else
            dispatcherInstance.BeginInvoke(action);
#endif
        }
    }
}