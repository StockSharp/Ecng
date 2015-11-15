using System.Windows;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    internal class CartesianPlaceRolloverLineStrategy : IPlaceRolloverLineStrategy
    {
        private readonly IChartModifier _modifier;

        public CartesianPlaceRolloverLineStrategy(IChartModifier modifier)
        {
            _modifier = modifier;
        }

        public Line ShowVerticalLine(Point hitPoint, bool isVerticalChart)
        {
            Line line = null;

            // Check x coordinate where line is going to be drawn
            if ((hitPoint.Y.IsDefined() && isVerticalChart) ||
                (hitPoint.X.IsDefined() && !isVerticalChart))
            {
                line = new Line();

                if (isVerticalChart)
                {
                    line.X1 = 0;
                    line.X2 = _modifier.ModifierSurface.ActualWidth;
                    line.Y1 = hitPoint.Y;
                    line.Y2 = hitPoint.Y;
                }
                else
                {
                    line.X1 = hitPoint.X;
                    line.X2 = hitPoint.X;
                    line.Y1 = 0;
                    line.Y2 = _modifier.ModifierSurface.ActualHeight;
                }
            }

            return line;
        }
    }
}