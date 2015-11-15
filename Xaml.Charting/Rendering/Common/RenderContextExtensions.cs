// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RenderContextExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common.Databinding;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Rendering.Common
{
    internal static class RenderContextExtensions
    {
        internal static IPen2D GetStyledPen(this IRenderContext2D renderContext, Line styledLine, bool isAntiAliased = false)
        {
            // Workaround for Silverlight, because styledLine.StrokeDashArray always returns an empty collection
#if SILVERLIGHT
            var dashes = styledLine.GetValue(Shape.StrokeDashArrayProperty) as DoubleCollection;
#endif
            var strokeDashArray =
#if SILVERLIGHT
                dashes != null ? dashes.ToArray() : new double[0];
#else
                styledLine.StrokeDashArray.ToArray();
#endif

            var color = default(Color);

            var brush = styledLine.Stroke as SolidColorBrush;
            if (brush != null)
            {
                color = brush.Color;
            }

            var thickness = (float)styledLine.StrokeThickness;

            var styledPen = renderContext.CreatePen(color, isAntiAliased, thickness, styledLine.Opacity, strokeDashArray, styledLine.StrokeEndLineCap);

            return styledPen;
        }
    }
}