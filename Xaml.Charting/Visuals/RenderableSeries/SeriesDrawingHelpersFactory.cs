using System.Windows;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.PointMarkers;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    internal static class SeriesDrawingHelpersFactory
    {
        public static IPathContextFactory GetLinesPathFactory(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            if (renderPassData.XCoordinateCalculator.IsPolarAxisCalculator)
            {
                return new PolarLinesPathContextFactory(renderContext, renderPassData.TransformationStrategy);
            }
            else
            {
                return new LinesClippingDecoratorFactory(new LinesPathContextFactory(renderContext), renderContext.ViewportSize);
            }
        }

        public static IPathContextFactory GetMountainAreaPathFactory(IRenderContext2D renderContext, IRenderPassData renderPassData, float zeroCoord, double gradientRotationAngle)
        {
            if (renderPassData.XCoordinateCalculator.IsPolarAxisCalculator)
            {
                return new PolarMountainAreaPathContextFactory(renderContext, renderPassData.TransformationStrategy, zeroCoord);
            }
            else
            {
                return new MountainAreaClippingDecoratorFactory(new MountainAreaPathContextFactory(renderContext, renderPassData.IsVerticalChart, zeroCoord, gradientRotationAngle), renderContext.ViewportSize);
            }
        }

        public static IPathContextFactory GetStackedMountainAreaPathFactory(IRenderContext2D renderContext, IRenderPassData renderPassData, double gradientRotationAngle)
        {
            if (renderPassData.XCoordinateCalculator.IsPolarAxisCalculator)
            {
                return new PolarStackedMountainAreaPathContextFactory(renderContext, renderPassData.TransformationStrategy);
            }
            else
            {
                return new MountainAreaClippingDecoratorFactory(new MountainAreaPathContextFactory(renderContext, renderPassData.IsVerticalChart, gradientRotationAngle), renderContext.ViewportSize);
            }
        }

        public static IPathContextFactory GetPointMarkerPathFactory(IRenderContext2D renderContext, IRenderPassData renderPassData, IPointMarker pointMarker)
        {
            if (renderPassData.XCoordinateCalculator.IsPolarAxisCalculator)
            {
                return new PolarPointMarkerPathContextFactory(renderContext, renderPassData.TransformationStrategy, pointMarker);
            }
            else
            {
                return new PointMarkerPathContextFactory(renderContext, pointMarker);
            }
        }

        public static ISeriesDrawingHelper GetSeriesDrawingHelper(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            if (renderPassData.XCoordinateCalculator.IsPolarAxisCalculator)
            {
                return new PolarSeriesDrawingHelper(renderContext, renderPassData.TransformationStrategy);
            }
            else
            {
                return new CartesianSeriesDrawingHelper(renderContext);
            }
            
        }

        public static IPathContextFactory NewPolarPolygonsFactory(IRenderContext2D renderContext, ITransformationStrategy transformationStrategy)
        {
            var viewportSize = new Size(360, PolarUtil.CalculateViewportRadius(renderContext.ViewportSize));

            return new PolygonClippingDecoratorFactory(new PolarPathDrawingDecoratorFactory(new PolygonPathContextFactory(renderContext), transformationStrategy), viewportSize);
        }

        public static IPathContextFactory NewPolarLinesFactory(IRenderContext2D renderContext, ITransformationStrategy transformationStrategy)
        {
            var viewportSize = new Size(360, PolarUtil.CalculateViewportRadius(renderContext.ViewportSize));

            return new LinesClippingDecoratorFactory(new PolarPathDrawingDecoratorFactory(new LinesPathContextFactory(renderContext), transformationStrategy), viewportSize);
        }
    }
}