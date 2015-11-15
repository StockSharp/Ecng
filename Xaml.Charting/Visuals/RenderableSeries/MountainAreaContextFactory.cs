using System;
using System.Windows;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Rendering.Common;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    internal class MountainAreaClippingDecoratorFactory : IPathContextFactory
    {
        private readonly MountainAreaPathContextFactory _mountainAreaFactory;
        private readonly Size _viewportSize;

        public MountainAreaClippingDecoratorFactory(MountainAreaPathContextFactory mountainAreaFactory, Size viewportSize)
        {
            _mountainAreaFactory = mountainAreaFactory;
            _viewportSize = viewportSize;
        }

        public IPathDrawingContext Begin(IPathColor color, double startX, double startY)
        {
            return new PolygonClippingDecorator(_mountainAreaFactory, _viewportSize).Begin(color, startX, startY);
        }
    }

    /// <summary>
    /// Allows bridging of <see cref="IRenderContext2D"/> and <see cref="FastLinesHelper"/>, allowing us to re-use fast optimized code for iterating over <see cref="IPointSeries"/> and drawing mountain areas
    /// </summary>
    internal class MountainAreaPathContextFactory : IPathContextFactory, IPathDrawingContext
    {
        private readonly IRenderContext2D _renderContext;
        private IPathDrawingContext _fillContext;

        private readonly bool _isVerticalChart;
        private readonly double _gradientRotationAngle;

        private readonly bool _drawToZeroLine = true;
        private readonly double _zeroCoord;

        private double _firstX;
        private double _firstY;

        private double _lastX;
        private double _lastY;


        public MountainAreaPathContextFactory(IRenderContext2D renderContext, bool isVerticalChart, double zeroCoord, double gradientRotationAngle)
            : this(renderContext, isVerticalChart, true, zeroCoord, gradientRotationAngle)
        {}

        public MountainAreaPathContextFactory(IRenderContext2D renderContext, bool isVerticalChart, double gradientRotationAngle)
            : this(renderContext, isVerticalChart, false, 0d, gradientRotationAngle)
        {}

        protected MountainAreaPathContextFactory(IRenderContext2D renderContext, bool isVerticalChart, bool drawToZeroline, double zeroCoord, double gradientRotationAngle)
        {
            _renderContext = renderContext;

            _isVerticalChart = isVerticalChart;

            _drawToZeroLine = drawToZeroline;
            _zeroCoord = zeroCoord;

            _gradientRotationAngle = gradientRotationAngle;
        }

        public IPathDrawingContext Begin(IPathColor brush, double startX, double startY)
        {
            _firstX = startX;
            _firstY = startY;
            // Proxy the polygon drawing context, return this instead so the MountainAreaContextFactory gets the MoveTo() and End() calls 
            _fillContext = _renderContext.BeginPolygon((IBrush2D)brush, startX, startY, _gradientRotationAngle);
            return this;
        }

        public void End()
        {
            // Complete the mountain by drawing to the zero line
            if (_drawToZeroLine)
            {
                if (_isVerticalChart)
                {
                    _fillContext.MoveTo(_zeroCoord, _lastY);
                    _fillContext.MoveTo(_zeroCoord, _firstY);
                }
                else
                {
                    _fillContext.MoveTo(_lastX, _zeroCoord);
                    _fillContext.MoveTo(_firstX, _zeroCoord);
                }
            }

            _fillContext.MoveTo(_firstX, _firstY);
           
            _fillContext.End();
            _fillContext = null;
        }

        public IPathDrawingContext MoveTo(double x, double y)
        {
            _lastX = x;
            _lastY = y;

            _fillContext.MoveTo(x, y);

            return this;
        }

        void IDisposable.Dispose()
        {
            End();
        }
    }
}