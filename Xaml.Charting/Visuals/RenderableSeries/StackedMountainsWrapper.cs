// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// StackedMountainsWrapper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Windows;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    internal class StackedMountainsWrapper : StackedSeriesWrapperBase<IStackedMountainRenderableSeries>, IStackedMountainsWrapper
    {
        /// <summary>
        /// Draws the <see cref="IStackedMountainRenderableSeries"/> using the <see cref="IRenderContext2D"/> passed in
        /// </summary>
        public override void DrawStackedSeries(IRenderContext2D renderContext)
        {
            // Checks whether all columns were passed to method, and then draw all one by one
            if (++Counter == SeriesCollection.Count(x => x.IsVisible))
            {
                Counter = 0;

                var visibleSeries = SeriesCollection.Where(x => x.IsVisible).ToList();

                if (visibleSeries.Any())
                {
                    // Draw series in reverse order to prevent the upper series from overlapping lower ones
                    var isBottomDigital = false;
                    for (int i = visibleSeries.Count - 1; i >= 0; --i)
                    {
                        if (i > 0)
                        {
                            isBottomDigital = visibleSeries[i - 1].IsDigitalLine;
                        }

                        visibleSeries[i].DrawMountain(renderContext, isBottomDigital);
                    }
                }
            }
        }

        public bool IsHitTest(Point rawPoint, HitTestInfo nearestHitResult, double hitTestRadius, Tuple<IComparable, IComparable> hitDataValue, IStackedMountainRenderableSeries series)
        {
            var interpolatedY = nearestHitResult.YValue.ToDouble();
            if (!double.IsNaN(interpolatedY))
            {
                var index = nearestHitResult.DataSeriesIndex;
                var hitPoint = new Point(hitDataValue.Item1.ToDouble(), hitDataValue.Item2.ToDouble());

                var leftYValues = AccumulateYValueAtX(series, index);
                var rightYValues = AccumulateYValueAtX(series, index + 1);

                var leftX = ((IComparable)series.DataSeries.XValues[index]).ToDouble();
                var rightX = ((IComparable)series.DataSeries.XValues[index + 1]).ToDouble();

                var topLine = new PointUtil.Line(new Point(leftX, leftYValues.Item1), new Point(rightX, rightYValues.Item1));
                var bottomLine = new PointUtil.Line(new Point(leftX, leftYValues.Item2), new Point(rightX, rightYValues.Item2));
                var perpendicular = new PointUtil.Line(new Point(hitPoint.X, Math.Min(leftYValues.Item2, rightYValues.Item2)), new Point(hitPoint.X, Math.Max(leftYValues.Item1, rightYValues.Item1)));

                Point topIntersect, bottomIntersect;
                PointUtil.LineIntersection2D(topLine, perpendicular, out topIntersect);
                PointUtil.LineIntersection2D(bottomLine, perpendicular, out bottomIntersect);

                var upperBound = topIntersect.Y;
                var lowerBound = bottomIntersect.Y;

                if (upperBound < lowerBound)
                {
                    NumberUtil.Swap(ref upperBound, ref lowerBound);
                }

                nearestHitResult.IsHit = (hitPoint.Y > lowerBound && hitPoint.Y < upperBound);
            }

            return nearestHitResult.IsHit;
        }
    }
}