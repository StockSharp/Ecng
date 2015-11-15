// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// GridExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Annotations;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class GridExtensions
    {        
        internal static void SafeAddChild(this Panel panel, object child, int index = -1)
        {
            var element = child as FrameworkElement;
            if (panel == null || element == null || ReferenceEquals(panel, element)) return;

            if (!panel.Children.Contains(element))
            {
                var existingParent = element.Parent as Panel;
                if (existingParent != null && existingParent.Children.Contains(element))
                {
                    existingParent.Children.Remove(element);
                }

                // if the index is specified & it's valid, insert the element by the index
                if (index >= 0 && index < panel.Children.Count)
                {
                    panel.Children.Insert(index, element);
                }
                else
                {
                    panel.Children.Add(element);
                }
            }
        }

        internal static void SafeAddChild(this IMainGrid panel, object child, int index = -1)
        {            
            (panel as Panel).SafeAddChild(child, index);
        }

        internal static void SafeAddChild(this IAnnotationCanvas panel, object child, int index = -1)
        {
            (panel as Panel).SafeAddChild(child, index);
        }

        internal static void SafeRemoveChild(this Panel panel, object child)
        {
            if (panel == null) return;

            var element = child as UIElement;
            if (element == null) return;

            if (panel.Children.Contains(element))
                panel.Children.Remove(element);
        }

        internal static void SafeRemoveChild(this IMainGrid panel, object child)
        {
            (panel as Panel).SafeRemoveChild(child);
        }

        internal static void SafeRemoveChild(this IAnnotationCanvas panel, object child)
        {
            (panel as Panel).SafeRemoveChild(child);
        }

        internal static void DrawLine(this Panel panel, int x1, int y1, int x2, int y2, Brush stroke, double strokeThickness)
        {
            var line = new Line
            {
                Stroke = stroke,
                StrokeThickness = strokeThickness,
                X1 = x1,
                X2 = x2,
                Y1 = y1,
                Y2 = y2,
            };
            panel.Children.Add(line);
        }
    }
}