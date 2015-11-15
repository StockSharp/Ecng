// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DependencyObjectExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;
using System.Windows.Media.Animation;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    /// <summary>
    /// Defines extension methods for the <see cref="DependencyObject"/> type. Used internally to Ultrachart
    /// </summary>
    public static class DependencyObjectExtensions
    {
#if SILVERLIGHT
        internal static void SetCurrentValue(this DependencyObject dependencyObject, DependencyProperty property, object value)
        {
            dependencyObject.SetValue(property, value);
            return;
//            var storyboard = new Storyboard();
//            storyboard.Duration = new Duration(TimeSpan.Zero);
//            storyboard.FillBehavior = FillBehavior.Stop;
//            var animation = new ObjectAnimationUsingKeyFrames();
//            animation.KeyFrames.Add(new DiscreteObjectKeyFramet, up() { Value = value, KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero)});
//            storyboard.Children.Add(animation);
//            Storyboard.SetTarget(animation, dependencyObject);
//            Storyboard.SetTargetProperty(animation, new PropertyPath(property));
////            storyboard.Completed += (s, e) =>
////                                        {
////                                            waitHandle.Set();
////                                        };
//            storyboard.Begin();
////            waitHandle.WaitOne();
        }
#endif

        /// <summary>
        /// Finds the visual child of type <typeparam name="T">T</typeparam>
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The child, or null if not found</returns>
        public static T FindVisualChild<T>(this DependencyObject parent) where T : DependencyObject
        {
            var childrenAmount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childrenAmount; ++i)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child != null && typeof(T).IsAssignableFrom(child.GetType()))
                {
                    return (T)child;
                }

                child = FindVisualChild<T>(child);
                if (child != null) return (T)child;
            }

            return null;
        }
    }
}
