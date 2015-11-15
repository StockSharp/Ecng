using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{    
    internal class LinesClippingDecoratorFactory : IPathContextFactory
    {
        private readonly IPathContextFactory _factory;
        private readonly Size _viewportSize;

        public LinesClippingDecoratorFactory(IPathContextFactory factory, Size viewportSize)
        {
            _factory = factory;
            _viewportSize = viewportSize;
        }

        public IPathDrawingContext Begin(IPathColor color, double startX, double startY)
        {
            return new LinesClippingDecorator(_factory, _viewportSize).Begin(color, startX, startY);
        }
    }

    internal class LinesClippingDecorator : IPathDrawingContext
    {
        private IPathColor _lastColor;

        private readonly IPathContextFactory _factory;
        private readonly Rect _viewportRect;
        private double _lastX;
        private double _lastY;
        private IPathDrawingContext _context;
        private bool _wasValid = true;
        private Size _viewportSize;

        public LinesClippingDecorator(IPathContextFactory factory, Size viewportSize)
        {
            _factory = factory;

            _viewportRect = new Rect(new Point(0, 0), viewportSize);
            _viewportSize = viewportSize;
        }

        public IPathDrawingContext Begin(IPathColor color, double x, double y)
        {
            _lastColor = color;

            _lastX = x;
            _lastY = y;
            _context = null;

            return this;
        }

        public IPathDrawingContext MoveTo(double x, double y)
        {
            bool isInBounds = IsInBounds(x, y, _viewportSize);

            if (_context != null)
            {
                if (isInBounds)
                {
                    _context.MoveTo(x, y);
                }
                else
                {
                    double x1 = _lastX, y1 = _lastY, x2 = x, y2 = y;
                    WriteableBitmapExtensions.CohenSutherlandLineClip(_viewportRect, ref x1, ref y1, ref x2, ref y2);
                    _context.MoveTo(x2, y2);
                    this.End();
                }
            }
            else
            {
                double x1 = _lastX, y1 = _lastY, x2 = x, y2 = y;
                if (isInBounds)
                {
                    WriteableBitmapExtensions.CohenSutherlandLineClip(_viewportRect, ref x1, ref y1, ref x2, ref y2);
                    _context = _factory.Begin(_lastColor, x1, y1);
                    _context.MoveTo(x2, y2);
                }
                else
                {
                    if (WriteableBitmapExtensions.CohenSutherlandLineClip(_viewportRect, ref x1, ref y1, ref x2, ref y2))
                    {
                        _context = _factory.Begin(_lastColor, x1, y1);
                        _context.MoveTo(x2, y2);
                        this.End();
                    }
                }
            }

            // Store state for next iteration
            _lastX = x;
            _lastY = y;

            return this;
        }

        public void End()
        {
            if (_context != null)
            {
                _context.End();
                _context = null;
            }
        }

        public void Dispose()
        {
            this.End();
        }

        private static bool IsInBounds(double x, double y, Size viewportSize)
        {
            return (x >= 0 && x < viewportSize.Width && y >= 0 && y < viewportSize.Height);
        }
    }
}
