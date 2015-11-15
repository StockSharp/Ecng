using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    internal class PolarMountainAreaPathContextFactory : IPathContextFactory, IPathDrawingContext
    {
        private readonly double _zeroCoord;
        private readonly List<double> _drawnXValues;
        
        private IPathDrawingContext _fillContext;
        private readonly IPathContextFactory _factory;

        public PolarMountainAreaPathContextFactory(IRenderContext2D renderContext, ITransformationStrategy transformationStrategy, double zeroCoord)
        {
            _factory = SeriesDrawingHelpersFactory.NewPolarPolygonsFactory(renderContext, transformationStrategy);

            _zeroCoord = zeroCoord;
            _drawnXValues = new List<double>();
        }

        public IPathDrawingContext Begin(IPathColor brush, double startX, double startY)
        {
            _fillContext = _factory.Begin(brush, startX, startY);
           
            _drawnXValues.Add(startX);

            return this;
        }
        
        public IPathDrawingContext MoveTo(double x, double y)
        {
            _fillContext.MoveTo(x, y);

            _drawnXValues.Add(x);

            return this;
        }

        public void End()
        {
            //Draw zero lines
            for (var i = _drawnXValues.Count - 1; i >= 0; i--)
            {
                _fillContext.MoveTo(_drawnXValues[i], _zeroCoord);
            }
            
            _fillContext.End();
            _fillContext = null;

            _drawnXValues.Clear();
        }
        
        void IDisposable.Dispose()
        {
            this.End();
        }
    }
}