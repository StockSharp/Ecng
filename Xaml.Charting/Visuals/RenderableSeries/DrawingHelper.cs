// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DrawingHelper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Windows;
using Ecng.Xaml.Charting.Rendering.Common;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    internal static class DrawingHelper
    {
        /// <summary>
        /// Returns a point with swapped coordinates if <paramref name="isVerticalChart"/> is True
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="isVerticalChart"></param>
        /// <returns></returns>
        public static Point TransformPoint(float x, float y, bool isVerticalChart)
        {
            //swap coords if vertical chart
            if (isVerticalChart)
            {
                return new Point(y, x);
            }

            return new Point(x, y);
        }

        /// <summary>
        /// Returns a point with swapped coordinates if <paramref name="isVerticalChart"/> is True
        /// </summary>
        /// <param name="point"></param>
        /// <param name="isVerticalChart"></param>
        /// <returns></returns>
        public static Point TransformPoint(Point point, bool isVerticalChart)
        {
            //swap coords if vertical chart
            if (isVerticalChart)
            {
                var x = point.X;

                point.X = point.Y;
                point.Y = x;
            }

            return point;
        }

        /// <summary>
        /// Returns a point with swapped coordinates if <paramref name="isVerticalChart"/> is True
        /// </summary>
        /// <param name="y"> </param>
        /// <param name="isVerticalChart"></param>
        /// <param name="x"> </param>
        /// <returns></returns>
        public static Point TransformPoint(double x, double y, bool isVerticalChart)
        {
            //swap coords if vertical chart
            if (isVerticalChart)
            {
                return new Point(y, x);
            }

            return new Point(x, y);
        }

        public static void DrawPoints(IEnumerable<Point> points, IPathContextFactory factory, IPathColor color)
        {
            var enumerator = points.GetEnumerator();
            
            if (enumerator.MoveNext())
            {
                var startPoint = enumerator.Current;

                using (var drawingContext = factory.Begin(color, startPoint.X, startPoint.Y))
                {
                    while (enumerator.MoveNext())
                    {
                        drawingContext.MoveTo(enumerator.Current.X, enumerator.Current.Y);
                    }
                }
            }
        }
    }
}
