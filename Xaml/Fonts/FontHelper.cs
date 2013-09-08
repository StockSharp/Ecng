namespace Ecng.Xaml.Fonts
{
	using System;
	using System.Text;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;

	using Ecng.Common;
	using Ecng.Reflection;

	public static class FontHelper
	{
		public static string TypefaceToString(this FamilyTypeface ttf)
		{
			if (ttf == null)
				throw new ArgumentNullException("ttf");

			var sb = new StringBuilder(ttf.Stretch.ToString());
			sb.Append("-");
			sb.Append(ttf.Weight);
			sb.Append("-");
			sb.Append(ttf.Style);
			return sb.ToString();
		}

		public static FamilyTypeface TypefaceFromString(this string str)
		{
			if (str.IsEmpty())
				throw new ArgumentNullException("str");

			var parts = str.Split('-');

			return new FamilyTypeface
			{
				Stretch = typeof(FontStretches).GetValue<VoidType, FontStretch>(parts[0], null),
				Weight = typeof(FontWeights).GetValue<VoidType, FontWeight>(parts[1], null),
				Style = typeof(FontStyles).GetValue<VoidType, FontStyle>(parts[2], null),
			};
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