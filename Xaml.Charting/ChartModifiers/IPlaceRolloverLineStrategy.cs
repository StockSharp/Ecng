using System.Windows;
using System.Windows.Shapes;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    interface IPlaceRolloverLineStrategy
    {
        Line ShowVerticalLine(Point hitPoint, bool isVerticalChart);
    }
}