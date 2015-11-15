using System;
using System.Collections.Generic;
using System.Windows;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.StrategyManager;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    internal class PolarPathDrawingDecoratorFactory : IPathContextFactory
    {
        private readonly IPathContextFactory _factory;
        private readonly ITransformationStrategy _transformationStrategy;

        public PolarPathDrawingDecoratorFactory(IPathContextFactory factory, ITransformationStrategy transformationStrategy)
        {
            _factory = factory;
            _transformationStrategy = transformationStrategy;
        }

        public IPathDrawingContext Begin(IPathColor color, double startX, double startY)
        {
            return new PolarPathDrawingDecorator(_factory, _transformationStrategy).Begin(color, startX, startY);
        }
    }

    internal class PolarPathDrawingDecorator : IPathDrawingContext
    {
        private IPathDrawingContext _drawingContext;
        private readonly IPathContextFactory _factory;
        private readonly ITransformationStrategy _transformationStrategy;
        private Point _lastPoint;

        /// <summary>
        /// Defines length of additional segment in pixels
        /// </summary>
        private const double AdditionalSegmentLength = 4;

        public PolarPathDrawingDecorator(IPathContextFactory factory, ITransformationStrategy transformationStrategy)
        {
            _factory = factory;
            _transformationStrategy = transformationStrategy;
        }

        public IPathDrawingContext Begin(IPathColor color, double x, double y)
        {
            _lastPoint = new Point(x, y);

            var pointToDraw = TransformPoint(_lastPoint);
            _drawingContext = _factory.Begin(color, pointToDraw.X, pointToDraw.Y);

            return this;
        }

        private Point TransformPoint(Point point)
        {
            return _transformationStrategy.ReverseTransform(point);
        }

        public IPathDrawingContext MoveTo(double x, double y)
        {
            var currentPoint = new Point(x, y);

            DrawCurveTo(currentPoint);

            return this;
        }

        private void DrawCurveTo(Point currentPoint)
        {
            DrawAdditionalPoints(_lastPoint, currentPoint);

            DrawPoint(currentPoint);

            _lastPoint = currentPoint;
        }

        private void DrawAdditionalPoints(Point startPoint, Point endPoint)
        {
            var points = CalculateAdditionalDrawingPoints(startPoint, endPoint);

            foreach (var point in points)
            {
                DrawPoint(point);
            }
        }

        private static IEnumerable<Point> CalculateAdditionalDrawingPoints(Point pt1, Point pt2)
        {
            var pointsAmount = CalculateAmountOfAdditionalDrawingPoints(pt1, pt2);

            if (pointsAmount > 0)
            {
                var x1 = pt1.X;
                var x2 = pt2.X;
                var y1 = pt1.Y;
                var y2 = pt2.Y;

                var segmentsAmount = pointsAmount + 1;
                var xDelta = (x2 - x1) / segmentsAmount;
                var yDelta = (y2 - y1) / segmentsAmount;

                for (var i = 0; i < pointsAmount; i++)
                {
                    x1 += xDelta;
                    y1 += yDelta;

                    yield return new Point(x1, y1);
                }
            }
        }

        private static int CalculateAmountOfAdditionalDrawingPoints(Point pt1, Point pt2)
        {
            var deltaAngle = Math.Abs(pt1.X - pt2.X);
            var r = (Math.Abs(pt1.Y) + Math.Abs(pt2.Y)) / 2;

            var l = Math.PI * r * (deltaAngle / 180);

            return (int)(l / AdditionalSegmentLength);
        }

        private void DrawPoint(Point point)
        {
            var currentPointCoord = _transformationStrategy.ReverseTransform(point);
            _drawingContext.MoveTo(currentPointCoord.X, currentPointCoord.Y);
        }

        public void End()
        {
            if (_drawingContext == null) return;

            _drawingContext.End();
            _drawingContext = null;
        }

        public void Dispose()
        {
            this.End();
        }
    }
}