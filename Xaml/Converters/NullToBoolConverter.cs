namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	public class NullToBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return false;

			string currentValue = value.ToString();

			if (string.IsNullOrWhiteSpace(currentValue))
				return false;

			return true;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}
}