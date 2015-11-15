using System.Windows;
using System.Windows.Media.Imaging;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Rendering.Common;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Allows bridging of <see cref="IRenderContext2D"/> DrawLines with <see cref="IPathDrawingContext"/> and <see cref="FastLinesHelper"/>, 
    /// allowing us to re-use fast optimized code for iterating over <see cref="IPointSeries"/> and drawing Lines
    /// </summary>
    internal class LinesPathContextFactory : IPathContextFactory
    {
        private readonly IRenderContext2D _renderContext;

        public LinesPathContextFactory(IRenderContext2D renderContext)
        {
            _renderContext = renderContext;
        }

        public IPathDrawingContext Begin(IPathColor pen, double startX, double startY)
        {
            return _renderContext.BeginLine((IPen2D)pen, startX, startY);
        }
    }
}