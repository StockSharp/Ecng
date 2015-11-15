using System;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals.PointMarkers
{
    /// <summary>
    /// Allows bridging of <see cref="IPointMarker"/> and <see cref="FastLinesHelper"/>, allowing us to re-use fast optimized code for iterating over <see cref="IPointSeries"/>
    /// </summary>
    internal class PointMarkerPathContextFactory : IPathContextFactory, IPathDrawingContext
    {
        private readonly IRenderContext2D _renderContext;
        private readonly IPointMarker _pointMarker;
        private IPen2D _defaultPen;
        private IBrush2D _defaultBrush;

        public PointMarkerPathContextFactory(IRenderContext2D renderContext, IPointMarker pointMarker)
        {
            _renderContext = renderContext;
            _pointMarker = pointMarker;
        }

        public IPathDrawingContext Begin(IPathColor pen, double startX, double startY)
        {
            // if pen is null marker draws itself according to Fill/Stroke values
            if (pen != null)
            {
                _defaultPen = (IPen2D) pen;
                _defaultBrush = _renderContext.CreateBrush(pen.Color);
            }
            else
            {
                _defaultPen = null;
                _defaultBrush = null;
            }

            _pointMarker.Begin(_renderContext, _defaultPen, _defaultBrush);
            _pointMarker.Draw(_renderContext, startX, startY, _defaultPen, _defaultBrush);
            return this;
        }

        public IPathDrawingContext MoveTo(double x, double y)
        {
            _pointMarker.Draw(_renderContext, x, y, _defaultPen, _defaultBrush);
            return this;
        }

        public void End()
        {
            _pointMarker.End(_renderContext);
            _defaultBrush = null;
            _defaultPen = null;
        }

        void IDisposable.Dispose()
        {
            this.End();
        }
    }
}