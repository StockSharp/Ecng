namespace Ecng.Xaml.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;
    using Ecng.Common;

    public class BoolToBrushConverter : IValueConverter
    {
	    public BoolToBrushConverter()
	    {
		    FalseBrushValue = new SolidColorBrush(Colors.Transparent);
		    TrueBrushValue = new SolidColorBrush(Colors.Transparent);
	    }

	    public Brush TrueBrushValue { get; set; }
		public Brush FalseBrushValue { get; set; }

	    object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var param = parameter == null || parameter.To<bool>();
            var val = (bool)value;

            return val == param ? TrueBrushValue : FalseBrushValue;
        }

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}