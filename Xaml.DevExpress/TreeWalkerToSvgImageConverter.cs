namespace Ecng.Xaml.DevExp
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Media;

	using DevExpress.Xpf.Core;

	public class TreeWalkerToSvgImageConverter : IMultiValueConverter
	{
		private readonly DrawingImage _image;

		public TreeWalkerToSvgImageConverter(DrawingImage image)
		{
			_image = image ?? throw new ArgumentNullException(nameof(image));
		}

		object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var targetObject = values[0] as DependencyObject;
			var inheritedPalette = values[2] as WpfSvgPalette;
			var palette = (values[1] as ThemeTreeWalker)?.InplaceResourceProvider.GetSvgPalette(targetObject);
			return ReplaceBrush(_image, inheritedPalette, palette);
		}

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}

		private static Brush GetActualBrush(Brush origin, WpfSvgPalette actualPalette, WpfSvgPalette basePalette)
		{
			var color = origin.ToString().Remove(1, 2);

			Brush result = null;

			var hasColor = actualPalette != null && actualPalette.ReplaceBrush(color, null, color, out result);

			if (hasColor)
				return result;

			if (basePalette != null && basePalette.ReplaceBrush(color, null, color, out result) && (actualPalette == null || !actualPalette.OverridesThemeColors))
				return result;

			return result;
		}

		private static DrawingImage ReplaceBrush(DrawingImage image, WpfSvgPalette actualPalette, WpfSvgPalette basePalette)
		{
			var origin = image.GetBrush();

			if (origin == null)
				return image;

			var brush = GetActualBrush(origin, actualPalette, basePalette);

			if (brush != null && brush.ToString() != origin.ToString())
			{
				image = image.Clone();

				image.UpdateBrush(brush);
			}

			return image;
		}
	}
}