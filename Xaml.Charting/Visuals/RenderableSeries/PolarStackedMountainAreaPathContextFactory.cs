using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    internal class PolarStackedMountainAreaPathContextFactory : IPathContextFactory
    {
        private readonly IPathContextFactory _factory;

        public PolarStackedMountainAreaPathContextFactory(IRenderContext2D renderContext, ITransformationStrategy transformationStrategy)
        {
            _factory = SeriesDrawingHelpersFactory.NewPolarPolygonsFactory(renderContext, transformationStrategy);
        }

        public IPathDrawingContext Begin(IPathColor color, double startX, double startY)
        {
            return _factory.Begin(color, startX, startY);
        }
    }
}
