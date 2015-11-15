// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// BrushExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class BrushExtensions
    {
        internal static bool IsTransparent(this Brush brush)
        {
            if (brush.Opacity == 0) return true;

            var solidBrush = brush as SolidColorBrush;
            return solidBrush != null && solidBrush.Color.A == 0x0;
        }

#if SILVERLIGHT
        internal static SolidColorBrush GetBrushFromColorName(string name)
        {
            string s = @"<SolidColorBrush xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Color='" + name + "'/>";

            return System.Windows.Markup.XamlReader.Load(s) as SolidColorBrush;
        }
#endif

        internal static Color ExtractColor(this Brush brush)
        {
            var solidBrush = brush as SolidColorBrush;
            if (solidBrush != null) return solidBrush.Color;

            var linearBrush = brush as LinearGradientBrush;
            if (linearBrush != null) return linearBrush.GradientStops[0].Color;

            return Color.FromArgb(0x00, 0x00, 0x00, 0x00);
        }

    }
}
