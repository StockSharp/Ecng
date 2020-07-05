namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;
	using System.Windows.Media.Imaging;

	using DevExpress.Xpf.Core.Native;

	/// <summary>
	/// Converts pack URI to image source. Supports svg and png.
	/// </summary>
	public class UriToImageSourceConverter : IValueConverter
	{
		/// <inheritdoc />
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(!(value is Uri uri))
				return Binding.DoNothing;

			if(!uri.IsAbsoluteUri)
				uri = new Uri("pack://application:,,," + uri, UriKind.Absolute);

			return uri.ToString().ToLowerInvariant().EndsWith(".svg") ?
				WpfSvgRenderer.CreateImageSource(uri) : new BitmapImage(uri);
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}
}