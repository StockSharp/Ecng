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

	/// <summary>
	/// Editor for the collection <see cref="EndPoint"/>.
	/// </summary>
	public partial class EndPointListEditor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EndPointListEditor"/>.
		/// </summary>
		public EndPointListEditor()
		{
			InitializeComponent();
			//Address.Mask = @"[а-яА-Яa-zA-Z0-9\.\-\,]+:?\d+";
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="EndPointListEditor.EndPoints"/>.
		/// </summary>
		public static readonly DependencyProperty EndPointsProperty =
			DependencyProperty.Register("EndPoints", typeof(IEnumerable<EndPoint>), typeof(EndPointListEditor), new PropertyMetadata(Enumerable.Empty<EndPoint>()));

		/// <summary>
		/// Addresses.
		/// </summary>
		public IEnumerable<EndPoint> EndPoints
		{
			get { return (IEnumerable<EndPoint>)GetValue(EndPointsProperty); }
			set { SetValue(EndPointsProperty, value); }
		}
	}

	class EndPointListConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var endPoints = value as IEnumerable<EndPoint>;
			return endPoints == null ? null : endPoints.Select(e => e.To<string>()).Join(",");
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value.To<string>().Split(",").Select(s => s.To<EndPoint>()).ToArray();
		}
	}
}