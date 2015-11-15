using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using Ecng.Xaml.Charting.ChartModifiers;

namespace Ecng.Xaml.Charting.Common.Databinding
{
    public class LegendPlacementToGridPositionConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var placement = (LegendPlacement)value;

            var def = "ROW";
            var getRow = (parameter as string ?? def).ToUpperInvariant().Contains(def);

            int result = 0;
            switch (placement)
            {
                case LegendPlacement.Top:
                    result = getRow ? 1 : 2;
                    break;
                case LegendPlacement.Bottom:
                    result = getRow ? 5 : 2;
                    break;
                case LegendPlacement.Left:
                    result = getRow ? 3 : 0;
                    break;
                case LegendPlacement.Right:
                    result = getRow ? 3 : 4;
                    break;
                case LegendPlacement.TopRight:
                    result = getRow ? 1 : 4;
                    break;
                case LegendPlacement.TopLeft:
                    result = getRow ? 1 : 0;
                    break;
                case LegendPlacement.BottomRight:
                    result = getRow ? 5 : 4;
                    break;
                case LegendPlacement.BottomLeft:
                    result = getRow ? 5 : 0;
                    break;
                default:
                    result = getRow ? 3 : 2;
                    break;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
