// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TextureKey.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Rendering.Common;

namespace Ecng.Xaml.Charting.Rendering
{
    internal class TextureKey
    {
        private readonly Brush _brush;
        private readonly FrameworkElement _frameworkElement;
        private readonly Size _size;

        public TextureKey(Size size, Brush brush)
        {
            _size = size;
            _brush = brush;
        }

        public TextureKey(FrameworkElement frameworkElement)
        {
            _frameworkElement = frameworkElement;
        }

        public override int GetHashCode()
        {
            if (_frameworkElement != null)
            {
                return _frameworkElement.GetHashCode();
            }
            else
            {
                int r = _size.Width.GetHashCode() ^ _size.Height.GetHashCode();
                if (_brush is LinearGradientBrush)
                {
                    GradientStopCollection gradientStops = ((LinearGradientBrush) _brush).GradientStops;
                    foreach (GradientStop gradientStop in gradientStops)
                    {
                        r ^= gradientStop.Color.GetHashCode();
                        r ^= gradientStop.Offset.GetHashCode();
                    }
                    r ^= ((LinearGradientBrush) _brush).StartPoint.GetHashCode();
                    r ^= ((LinearGradientBrush) _brush).EndPoint.GetHashCode();
                }
                else
                    r ^= _brush.GetHashCode();

                return r;
            }
        }

        public override bool Equals(object obj)
        {
            var key2 = obj as TextureKey;
            if (key2 == null) return false;

            if (_frameworkElement != null)
            {
                return _frameworkElement.Equals(key2._frameworkElement);
            }
            else
            {
                if (key2._size.Width != _size.Width) return false;
                if (key2._size.Height != _size.Height) return false;

                if (_brush is LinearGradientBrush)
                {
                    if (!(key2._brush is LinearGradientBrush)) return false;
                    if (((LinearGradientBrush) _brush).StartPoint != ((LinearGradientBrush) key2._brush).StartPoint)
                        return false;
                    if (((LinearGradientBrush) _brush).EndPoint != ((LinearGradientBrush) key2._brush).EndPoint)
                        return false;
                    GradientStopCollection gradientStops = ((LinearGradientBrush) _brush).GradientStops;
                    GradientStopCollection gradientStops2 = ((LinearGradientBrush) key2._brush).GradientStops;
                    if (gradientStops.Count != gradientStops2.Count) return false;

                    for (int i = 0; i < gradientStops.Count; i++)
                    {
                        GradientStop gradientStop1 = gradientStops[i];
                        GradientStop gradientStop2 = gradientStops2[i];
                        if (gradientStop1.Color != gradientStop2.Color) return false;
                        if (gradientStop1.Offset != gradientStop2.Offset) return false;
                    }
                    return true;
                }
                else
                    return _brush.Equals(key2._brush);
            }
        }
    }
}