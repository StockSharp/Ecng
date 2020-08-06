namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	using Ecng.Common;

	public class FileSizeConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var numValue = value as long?;

			if (numValue == null)
				numValue = value as int?;

			if (numValue == null)
				return Binding.DoNothing;

			return numValue.Value.ToHumanReadableFileSize();
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}