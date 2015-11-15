// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RubberBandXyZoomModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    internal class PolarRubberBandOverlayPlacementStrategy : IRubberBandOverlayPlacementStrategy
    {
        private PathFigure _pathFigure;
        private ArcSegment _arcSegment;
        private Path _path;
        private readonly IStrategyManager _strategyManager;

        public PolarRubberBandOverlayPlacementStrategy(IChartModifier modifier)
        {
            _strategyManager = modifier.Services.GetService<IStrategyManager>();
        }

        public double CalculateDraggedDistance(Point start, Point end)
        {
            return PolarUtil.AngleDistance(ref start, ref end);
        }

        public Shape CreateShape(Brush rubberBandFill, Brush rubberBandStroke, DoubleCollection rubberBandStrokeDashArray)
        {
            var pathGeometry = new PathGeometry();
            _pathFigure = new PathFigure();
            _arcSegment = new ArcSegment();

            _pathFigure.Segments.Add(_arcSegment);
            pathGeometry.Figures.Add(_pathFigure);

            _path = new Path
            {
                Stroke = rubberBandFill,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Data = pathGeometry,
            };

            return _path;
        }

        public Point UpdateShape(bool isXAxisOnly, Point start, Point end)
        {
            Point startPoint;
            Point endPoint;
            double bagelThickness;
            double radius;

            var strategy = _strategyManager.GetTransformationStrategy();

            if (isXAxisOnly)
            {
                radius = strategy.ViewportSize.Height / 2;
                bagelThickness = radius;

                startPoint = strategy.ReverseTransform(new Point(strategy.Transform(start).X, radius / 2));
                endPoint = strategy.ReverseTransform(new Point(strategy.Transform(end).X, radius / 2));
            }
            else
            {
                var startPolar = strategy.Transform(start);
                var endPolar = strategy.Transform(end);

                var b = startPolar.Y > endPolar.Y;
                bagelThickness = b ? startPolar.Y - endPolar.Y : endPolar.Y - startPolar.Y;
                radius = b ? startPolar.Y : endPolar.Y;

                startPoint = strategy.ReverseTransform(new Point(strategy.Transform(start).X, radius - bagelThickness / 2));
                endPoint = strategy.ReverseTransform(new Point(strategy.Transform(end).X, radius - bagelThickness / 2));
            }

            var startAngle = strategy.Transform(startPoint).X;
            var endAngle = strategy.Transform(endPoint).X;

            var angle = Math.Abs(startAngle - endAngle);

            _pathFigure.StartPoint = startPoint;

            _arcSegment.Size = new Size(radius - bagelThickness / 2, radius - bagelThickness / 2);
            _arcSegment.Point = endPoint;
            _arcSegment.RotationAngle = angle;
            _arcSegment.SweepDirection = startAngle > endAngle ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;
            _arcSegment.IsLargeArc = angle > 180.0;

            _path.StrokeThickness = bagelThickness;

            return end;
        }

        public void SetupShape(bool isXAxisOnly, Point start, Point end)
        {
            Canvas.SetLeft(_path, 0);
            Canvas.SetTop(_path, 0);
        }
    }
}