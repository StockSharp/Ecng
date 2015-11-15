using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.PointMarkers;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    public interface ISeriesDrawingHelper
    {
        void DrawBox(Point pt1, Point pt2, IBrush2D pointFill, IPen2D pointPen, double gradientRotationAngle);
        void DrawLine(Point pt1, Point pt2, IPen2D pointPen);
        void FillPolygon(IBrush2D fillBrush, Point[] points);
        void DrawPoint(IPointMarker pt, Point point, IBrush2D brush, IPen2D pen);
        void DrawPoints(IPointMarker pt, IEnumerable<Point> points);
        void DrawQuad(IPen2D pen, Point pt1, Point pt2);
    }

    internal class CartesianSeriesDrawingHelper:ISeriesDrawingHelper
    {
        private readonly IRenderContext2D _renderContext;

        public CartesianSeriesDrawingHelper(IRenderContext2D renderContext)
        {
            _renderContext = renderContext;
        }

        public void DrawQuad(IPen2D pen, Point pt1, Point pt2) {
            _renderContext.DrawQuad(pen, pt1, pt2);
        }

        public void DrawBox(Point pt1, Point pt2, IBrush2D pointFill, IPen2D pointPen, double gradientRotationAngle)
        {
            _renderContext.FillRectangle(pointFill, pt1, pt2, gradientRotationAngle);
            if (pointPen.StrokeThickness > 0 && pointPen.Color.A != 0)
            {
                _renderContext.DrawQuad(pointPen, pt1, pt2);
            }
        }

        public void DrawLine(Point pt1, Point pt2, IPen2D pointPen)
        {
            if (pointPen.StrokeThickness > 0 && pointPen.Color.A != 0)
            {
                _renderContext.DrawLine(pointPen, pt1, pt2);
            }
        }

        public void FillPolygon(IBrush2D fillBrush, Point[] points)
        {
            _renderContext.FillPolygon(fillBrush, points);
        }

        public void DrawPoint(IPointMarker pt, Point point, IBrush2D brush, IPen2D pen)
        {
            pt.Draw(_renderContext, point.X, point.Y, pen, brush);
        }

        public void DrawPoints(IPointMarker pt, IEnumerable<Point> points)
        {
            pt.Draw(_renderContext, points);
        }
    }

    internal class PolarSeriesDrawingHelper : ISeriesDrawingHelper
    {
        private readonly IRenderContext2D _renderContext;
        private readonly ITransformationStrategy _transformationStrategy;

        private readonly IPathContextFactory _polygonsFactory;
        private readonly IPathContextFactory _linesFactory;
        private readonly Size _viewportSize;

        public PolarSeriesDrawingHelper(IRenderContext2D renderContext, ITransformationStrategy transformationStrategy)
        {
            _renderContext = renderContext;
            _transformationStrategy = transformationStrategy;

            _viewportSize = PolarUtil.CalculatePolarViewportSize(renderContext.ViewportSize);

            _linesFactory = SeriesDrawingHelpersFactory.NewPolarLinesFactory(renderContext, transformationStrategy);
            _polygonsFactory = SeriesDrawingHelpersFactory.NewPolarPolygonsFactory(renderContext, transformationStrategy);
        }

        public void DrawQuad(IPen2D pen, Point pt1, Point pt2) {
        }

        public void DrawBox(Point leftTop, Point rightBottom, IBrush2D pointFill, IPen2D pointPen, double gradientRotationAngle)
        {
            using (var drawingContext = _polygonsFactory.Begin(pointFill, leftTop.X, leftTop.Y))
            {
                drawingContext.MoveTo(rightBottom.X, leftTop.Y);
                drawingContext.MoveTo(rightBottom.X, rightBottom.Y);
                drawingContext.MoveTo(leftTop.X, rightBottom.Y);
                drawingContext.MoveTo(leftTop.X, leftTop.Y);
            }
            if (pointPen.StrokeThickness > 0 && pointPen.Color.A != 0)
            {
                using (var drawingContext = _linesFactory.Begin(pointPen, leftTop.X, leftTop.Y))
                {
                    drawingContext.MoveTo(rightBottom.X, leftTop.Y);
                    drawingContext.MoveTo(rightBottom.X, rightBottom.Y);
                    drawingContext.MoveTo(leftTop.X, rightBottom.Y);
                    drawingContext.MoveTo(leftTop.X, leftTop.Y);
                }
            }
        }

        public void DrawLine(Point pt1, Point pt2, IPen2D pointPen)
        {
            if (pointPen.StrokeThickness > 0 && pointPen.Color.A != 0)
            {
                using (var drawingContext = _linesFactory.Begin(pointPen, pt1.X, pt1.Y))
                {
                    drawingContext.MoveTo(pt2.X, pt2.Y);
                }
            }
        }

        public void FillPolygon(IBrush2D fillBrush, Point[] points)
        {
            var currentPoint = points.First();

            using (var drawingContext = _polygonsFactory.Begin(fillBrush, currentPoint.X, currentPoint.Y))
            {
                for (int i = 1; i < points.Length; i++)
                {
                    currentPoint = points[i];
                    drawingContext.MoveTo(currentPoint.X, currentPoint.Y);
                }
            }
        }

        public void DrawPoint(IPointMarker pt, Point point, IBrush2D brush, IPen2D pen)
        {
            if (point.IsInBounds(_viewportSize))
            {
                point = TransformPoint(point);

                pt.Draw(_renderContext, point.X, point.Y, pen, brush);
            }
        }

        public void DrawPoints(IPointMarker pt, IEnumerable<Point> points)
        {
            var pointsToDraw = points.Where(p=>p.IsInBounds(_viewportSize)).Select(TransformPoint);

            pt.Draw(_renderContext, pointsToDraw);
        }

        private Point TransformPoint(Point point)
        {
            return _transformationStrategy.ReverseTransform(point);
        }

    }
}
