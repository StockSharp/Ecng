using Ecng.Xaml.Charting.Rendering.Common;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    internal class PolygonPathContextFactory : IPathContextFactory
    {
        private readonly IRenderContext2D _renderContext;

        public PolygonPathContextFactory(IRenderContext2D renderContext)
        {
            _renderContext = renderContext;
        }

        public IPathDrawingContext Begin(IPathColor pen, double startX, double startY)
        {
            return _renderContext.BeginPolygon((IBrush2D)pen, startX, startY);
        }
    }
}