// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RectExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Runtime.CompilerServices;
using System.Windows;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class RectExtensions
    {
        internal static Point ClipToBounds(this Rect rect, Point point)
        {
            double rightEdge = rect.Right;
            double leftEdge = rect.Left;
            double topEdge = rect.Top;
            double bottomEdge = rect.Bottom;

            point.X = point.X > rightEdge ? rightEdge : point.X;            
            point.X = point.X < leftEdge ? leftEdge : point.X;
            point.Y = point.Y > bottomEdge ? bottomEdge : point.Y;
            point.Y = point.Y < topEdge ? topEdge : point.Y;

            return point;
        }

#if SILVERLIGHT
        internal static bool IntersectsWith(this Rect rect1, Rect rect2)
        {
            rect1.Intersect(rect2);

            return !rect1.IsEmpty;
        }
#endif

        internal static Rect Expand(this Rect rect, double offset)
        {
            return new Rect(rect.X - offset, rect.Y - offset, rect.Width + 2*offset, rect.Height + 2*offset);
        }
    }
}