using System.Windows;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.StrategyManager;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    internal class PolarPlaceRolloverLineStrategy : IPlaceRolloverLineStrategy
    {
        private readonly IChartModifier _modifier;

        public PolarPlaceRolloverLineStrategy(IChartModifier modifier)
        {
            _modifier = modifier;
        }

        public Line ShowVerticalLine(Point hitPoint, bool isVerticalChart)
        {
            Line line = null;

            // Check x coordinate where line is going to be drawn
            if ((hitPoint.Y.IsDefined() && (hitPoint.X.IsDefined())))
            {
                line = new Line();

                var strategy = _modifier.Services.GetService<IStrategyManager>().GetTransformationStrategy();

                var polarHitTestPoint = strategy.Transform(hitPoint);

                var pt1 = strategy.ReverseTransform(new Point(polarHitTestPoint.X, 0));
                var pt2 = strategy.ReverseTransform(new Point(polarHitTestPoint.X, strategy.ViewportSize.Height));

                line.X1 = pt1.X;
                line.X2 = pt2.X;
                line.Y1 = pt1.Y;
                line.Y2 = pt2.Y;
            }
            return line;
        }
    }
}