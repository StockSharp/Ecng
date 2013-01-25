namespace Ecng.Xaml.Grids
{
	using System;
	using System.Windows.Data;
	using System.Globalization;

	class FormatConverter<TValue> : IValueConverter
	{
		private readonly Func<object, TValue> _getValue;

		public FormatConverter(Func<object, TValue> getValue)
		{
			if (getValue == null)
				throw new ArgumentNullException("getValue");

			_getValue = getValue;
		}

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return _getValue(value);
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}