namespace Ecng.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Net;
	using System.Windows;
	using System.Windows.Data;

	using Ecng.Common;

	public partial class EndPointListEditor
	{
		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="EndPoints"/>.
		/// </summary>
		public static readonly DependencyProperty EndPointsProperty =
			DependencyProperty.Register(nameof(EndPoints), typeof(IEnumerable<EndPoint>), typeof(EndPointListEditor), new PropertyMetadata(Enumerable.Empty<EndPoint>()));

		/// <summary>
		/// Addresses.
		/// </summary>
		public IEnumerable<EndPoint> EndPoints
		{
			get => (IEnumerable<EndPoint>)GetValue(EndPointsProperty);
			set => SetValue(EndPointsProperty, value);
		}

		public EndPointListEditor()
		{
			InitializeComponent();
		}
	}

	class EndPointListConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value as IEnumerable<EndPoint>)?.Select(e => e.To<string>()).Join(",");
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value.To<string>().SplitBySep(",").Select(s => s.To<EndPoint>()).ToArray();
		}
	}
}
