namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

	using Ecng.Common;

	public class BoolToVisibilityConverter : IValueConverter
	{
		private Visibility _falseVisibilityValue = Visibility.Collapsed;
		private Visibility _trueVisibilityValue = Visibility.Visible;

		public Visibility FalseVisibilityValue
		{
			get { return _falseVisibilityValue; }
			set { _falseVisibilityValue = value; }
		}

		public Visibility TrueVisibilityValue
		{
			get { return _trueVisibilityValue; }
			set { _trueVisibilityValue = value; }
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var param = parameter == null || parameter.To<bool>();
			var val = (bool)value;

			return val == param ? TrueVisibilityValue : FalseVisibilityValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((Visibility)value == TrueVisibilityValue);
		}
	}
}