namespace Ecng.Xaml.Converters
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Windows.Data;

	using DevExpress.Xpf.Editors;

	using Ecng.Common;

	/// <summary>
	/// <see cref="ComboBoxEdit"/> value converter.
	/// </summary>
	/// <typeparam name="T">Convertible type.</typeparam>
	public sealed class ComboBoxEditValueConverter<T> : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((IEnumerable<T>)value).Select(v => (object)v).ToList();
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return ArrayHelper.Empty<T>();

			return ((IList<object>)value).Select(v => (T)v).ToArray();
		}
	}
}