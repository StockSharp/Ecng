using System;
using System.Linq;
using System.Windows;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    internal class PolarLinesPathContextFactory : IPathContextFactory
    {
        private readonly IPathContextFactory _factory;

        public PolarLinesPathContextFactory(IRenderContext2D renderContext, ITransformationStrategy transformationStrategy)
        {
            _factory = SeriesDrawingHelpersFactory.NewPolarLinesFactory(renderContext, transformationStrategy);
        }

        public IPathDrawingContext Begin(IPathColor pen, double x, double y)
        {
            return _factory.Begin(pen, x, y); ;
        }
    }
}