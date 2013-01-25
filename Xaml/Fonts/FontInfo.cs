namespace Ecng.Xaml.Fonts
{
	using System.Windows;
	using System.Windows.Media;

	using Ecng.Common;

	public class FontInfo : Cloneable<FontInfo>
	{
		public FontInfo()
		{
		}

		public FontInfo(FontFamily family, double size, FontStyle style, FontStretch stretch, FontWeight weight)
		{
			Family = family;
			Size = size;
			Style = style;
			Stretch = stretch;
			Weight = weight;
			//BrushColor = brushColor;
		}

		public FontFamily Family { get; set; }
		public double Size { get; set; }
		public FontStyle Style { get; set; }
		public FontStretch Stretch { get; set; }
		public FontWeight Weight { get; set; }
		//public SolidColorBrush BrushColor { get; set; }

		//public FontColor Color
		//{
		//    get { return AvailableColors.GetFontColor(BrushColor); }
		//}

		public FamilyTypeface Typeface
		{
			get
			{
				return new FamilyTypeface
				{
					Stretch = Stretch,
					Weight = Weight,
					Style = Style
				};
			}
		}

		public override FontInfo Clone()
		{
			return new FontInfo
			{
				Family = Family,
				Size = Size,
				Style = Style,
				Stretch = Stretch,
				Weight = Weight,
			};
		}
	}
}