namespace Ecng.Xaml.Fonts
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Media;

	using Ecng.Common;
	using Ecng.Serialization;

	public class FontInfo : Cloneable<FontInfo>, IPersistable
	{
		//public FontInfo()
		//{
		//}

		//public FontInfo(FontFamily family, double size, FontStyle style, FontStretch stretch, FontWeight weight)
		//{
		//	Family = family;
		//	Size = size;
		//	Style = style;
		//	Stretch = stretch;
		//	Weight = weight;
		//	//BrushColor = brushColor;
		//}

		public FontFamily Family { get; set; }
		public double Size { get; set; }
		public FontStyle Style { get; set; }
		public FontStretch Stretch { get; set; }
		public FontWeight Weight { get; set; }

		//public FontColor Color
		//{
		//    get { return AvailableColors.GetFontColor(BrushColor); }
		//}

		public FamilyTypeface Typeface
		{
			get => new FamilyTypeface
			{
				Stretch = Stretch,
				Weight = Weight,
				Style = Style
			};
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				Stretch = value.Stretch;
				Weight = value.Weight;
				Style = value.Style;
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

		public void Load(SettingsStorage storage)
		{
			if (storage.ContainsKey("Family"))
			{
				var familyId = storage.GetValue<string>("Family");
				Family = Fonts.SystemFontFamilies.First(f => f.Source.CompareIgnoreCase(familyId));

				var typeface = storage.GetValue<string>("Typeface").TypefaceFromString();

				Style = typeface.Style;
				Weight = typeface.Weight;
				Stretch = typeface.Stretch;
			}
			
			Size = storage.GetValue<double>("Size");
		}

		public void Save(SettingsStorage storage)
		{
			if (Family != null)
			{
				storage.SetValue("Family", Family.Source);
				storage.SetValue("Typeface", Typeface.TypefaceToString());
			}

			storage.SetValue("Size", Size);
		}
	}
}