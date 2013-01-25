namespace Ecng.Xaml.Fonts
{
	using System;
	using System.Text;
	using System.Windows.Controls;
	using System.Windows.Media;

	public static class FontHelper
	{
		public static string TypefaceToString(this FamilyTypeface ttf)
		{
			if (ttf == null)
				throw new ArgumentNullException("ttf");

			var sb = new StringBuilder(ttf.Stretch.ToString());
			sb.Append("-");
			sb.Append(ttf.Weight.ToString());
			sb.Append("-");
			sb.Append(ttf.Style.ToString());
			return sb.ToString();
		}

		public static void ApplyFont(this Control control, FontInfo font)
		{
			if (control == null)
				throw new ArgumentNullException("control");

			if (font == null)
				throw new ArgumentNullException("font");

			control.FontFamily = font.Family;
			control.FontSize = font.Size;
			control.FontStyle = font.Style;
			control.FontStretch = font.Stretch;
			control.FontWeight = font.Weight;
			//control.Foreground = font.BrushColor;
		}

		public static FontInfo GetFont(this Control control)
		{
			if (control == null)
				throw new ArgumentNullException("control");

			return new FontInfo
			{
				Family = control.FontFamily,
				Size = control.FontSize,
				Style = control.FontStyle,
				Stretch = control.FontStretch,
				Weight = control.FontWeight,
				//BrushColor = (SolidColorBrush)control.Foreground
			};
		}

		public static void ApplyFont(this TextBlock control, FontInfo font)
		{
			if (control == null)
				throw new ArgumentNullException("control");

			if (font == null)
				throw new ArgumentNullException("font");

			control.FontFamily = font.Family;
			control.FontSize = font.Size;
			control.FontStyle = font.Style;
			control.FontStretch = font.Stretch;
			control.FontWeight = font.Weight;
			//control.Foreground = font.BrushColor;
		}

		public static FontInfo GetFont(this TextBlock control)
		{
			if (control == null)
				throw new ArgumentNullException("control");

			return new FontInfo
			{
				Family = control.FontFamily,
				Size = control.FontSize,
				Style = control.FontStyle,
				Stretch = control.FontStretch,
				Weight = control.FontWeight,
				//BrushColor = (SolidColorBrush)control.Foreground
			};
		}
	}
}