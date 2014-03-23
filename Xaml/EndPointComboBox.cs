namespace Ecng.Xaml
{
	using System;
	using System.Globalization;
	using System.Linq;
	using System.Net;
	using System.Windows.Data;

	using Ecng.Common;

	public class EndPointValueConverter : IValueConverter
	{
		private readonly AddressComboBox<EndPoint> _comboBox;
		private readonly EndPoint _defaultEndPoint;

		public EndPointValueConverter(AddressComboBox<EndPoint> comboBox, EndPoint defaultEndPoint)
		{
			if (comboBox == null)
				throw new ArgumentNullException("comboBox");

			if (defaultEndPoint == null)
				throw new ArgumentNullException("defaultEndPoint");

			_comboBox = comboBox;
			_defaultEndPoint = defaultEndPoint;
		}

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var item = _comboBox.Items
				.Cast<AddressComboBox<EndPoint>.ComboItem>()
				.Where(t => t.Address.To<string>() == value.To<string>())
				.Select(t => t.Title)
				.FirstOrDefault();

			return item ?? value.To<string>();
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var address = _comboBox.Items
				.Cast<AddressComboBox<EndPoint>.ComboItem>()
				.Where(t => t.Title == (string)value)
				.Select(t => t.Address)
				.FirstOrDefault();

			if (address != null)
				return address;

			EndPoint endPoint;
			return TryParseEndPoint((string)value, out endPoint)
					   ? endPoint
					   : _defaultEndPoint;
		}

		private static bool TryParseEndPoint(string value, out EndPoint endPoint)
		{
			var suffix = value.LastIndexOf(' ');
			var endPointString = suffix != -1 ? value.Substring(0, suffix).TrimEnd(' ') : value;

			endPoint = null;

			var addressLength = endPointString.LastIndexOf(':');
			if (addressLength == -1)
				return false;

			var portString = endPointString.Substring(addressLength + 1);

			int port;
			if (!int.TryParse(portString, out port))
				return false;

			var addressString = value.Substring(0, addressLength);

			IPAddress address;
			endPoint = IPAddress.TryParse(addressString, out address)
						   ? (EndPoint)new IPEndPoint(address, port)
						   : new DnsEndPoint(addressString, port);
			return true;
		}
	}

	public class UriValueConverter : IValueConverter
	{
		private readonly AddressComboBox<Uri> _comboBox;
		private readonly Uri _defaultUrl;

		public UriValueConverter(AddressComboBox<Uri> comboBox, Uri defaultUrl)
		{
			if (comboBox == null)
				throw new ArgumentNullException("comboBox");

			if (defaultUrl == null)
				throw new ArgumentNullException("defaultUrl");

			_comboBox = comboBox;
			_defaultUrl = defaultUrl;
		}

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var item = _comboBox.Items
				.Cast<AddressComboBox<Uri>.ComboItem>()
				.Where(t => t.Address.To<string>() == value.To<string>())
				.Select(t => t.Title)
				.FirstOrDefault();

			return item ?? value.To<string>();
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var address = _comboBox.Items
				.Cast<AddressComboBox<Uri>.ComboItem>()
				.Where(t => t.Title == (string)value)
				.Select(t => t.Address)
				.FirstOrDefault();

			if (address != null)
				return address;

			return Uri.IsWellFormedUriString((string)value, UriKind.Absolute)
					   ? value.To<Uri>()
					   : _defaultUrl;
		}
	}

	public class EndPointComboBox : AddressComboBox<EndPoint>
	{
	}
}