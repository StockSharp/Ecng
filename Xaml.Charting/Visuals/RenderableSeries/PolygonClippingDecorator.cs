using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    internal class PolygonClippingDecoratorFactory : IPathContextFactory
    {
        private readonly IPathContextFactory _factory;
        private readonly Size _viewportSize;

        public PolygonClippingDecoratorFactory(IPathContextFactory factory, Size viewportSize)
        {
            _factory = factory;
            _viewportSize = viewportSize;
        }

        public IPathDrawingContext Begin(IPathColor color, double startX, double startY)
        {
            return new PolygonClippingDecorator(_factory, _viewportSize).Begin(color, startX, startY);
        }
    }

    internal class PolygonClippingDecorator : IPathDrawingContext
    {
        private readonly List<Point> _points;
        private IPathColor _lastColor;

        private readonly IPathContextFactory _factory;
        private readonly Size _viewportSize;

        public PolygonClippingDecorator(IPathContextFactory factory, Size viewportSize)
        {
            _factory = factory;
            _viewportSize = viewportSize;
            
            _points = new List<Point>();
        }

        public IPathDrawingContext Begin(IPathColor color, double x, double y)
        {
            _points.Add(new Point(x, y));
            _lastColor = color;

            return this;
        }

        public IPathDrawingContext MoveTo(double x, double y)
        {
            _points.Add(new Point(x, y));

            return this;
        }

        public void End()
        {
            if (_points.Count > 0)
            {
                var clippedPoints = PointUtil.ClipPolygon(_points,_viewportSize);

                DrawingHelper.DrawPoints(clippedPoints, _factory, _lastColor);
            }
        }

        public void Dispose()
        {
            this.End();
        }
    }
}
