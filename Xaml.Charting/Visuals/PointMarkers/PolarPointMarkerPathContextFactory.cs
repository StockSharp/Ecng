using System;
using System.Windows;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals.PointMarkers
{
    internal class PolarPointMarkerPathContextFactory : IPathContextFactory
    {
        private readonly PolarPointDrawingDecoratorFactory _factory;

        public PolarPointMarkerPathContextFactory(IRenderContext2D renderContext, ITransformationStrategy transformationStrategy, IPointMarker pointMarker)
        {
            var viewportSize = new Size(360, PolarUtil.CalculateViewportRadius(renderContext.ViewportSize));
            
            _factory = new PolarPointDrawingDecoratorFactory(new PointMarkerPathContextFactory(renderContext, pointMarker), transformationStrategy, viewportSize);
        }

        public IPathDrawingContext Begin(IPathColor color, double startX, double startY)
        {
            return _factory.Begin(color, startX, startY);
        }  
    }

    
    
    internal class PolarPointDrawingDecoratorFactory : IPathContextFactory
    {
        private readonly IPathContextFactory _factory;
        private readonly ITransformationStrategy _transformationStrategy;
        private readonly Size _viewportSize;

        public PolarPointDrawingDecoratorFactory(IPathContextFactory factory, ITransformationStrategy transformationStrategy, Size viewportSize)
        {
            _factory = factory;
            _transformationStrategy = transformationStrategy;
            _viewportSize = viewportSize;
        }

        public IPathDrawingContext Begin(IPathColor color, double startX, double startY)
        {
            return new PolarPointDrawingDecorator(_factory, _transformationStrategy, _viewportSize).Begin(color, startX, startY);
        }
    }

    internal class PolarPointDrawingDecorator : IPathDrawingContext
    {
        private readonly IPathContextFactory _factory;
        private readonly ITransformationStrategy _transformationStrategy;
        private readonly Size _viewportSize;

        private IPathDrawingContext _drawingContext;
        private IPathColor _lastColor;

        public PolarPointDrawingDecorator(IPathContextFactory factory, ITransformationStrategy transformationStrategy, Size viewportSize)
        {
            _factory = factory;
            _transformationStrategy = transformationStrategy;
            _viewportSize = viewportSize;
        }

        public IPathDrawingContext Begin(IPathColor color, double x, double y)
        {
            _lastColor = color;

            DrawPoint(x,y);

            return this;
        }

        private void DrawPoint(double x, double y)
        {
            var point = new Point(x, y);
            if (!point.IsInBounds(_viewportSize)) return;

            var pointToDraw = _transformationStrategy.ReverseTransform(point);
            if (_drawingContext != null)
            {
                _drawingContext.MoveTo(pointToDraw.X, pointToDraw.Y);
            }
            else
            {
                _drawingContext = _factory.Begin(_lastColor, pointToDraw.X, pointToDraw.Y);
            }
        }

        public IPathDrawingContext MoveTo(double x, double y)
        {
            DrawPoint(x,y);

            return this;
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