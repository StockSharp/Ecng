namespace Ecng.Xaml.Fonts
{
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;

	public partial class FontChooser
	{
		public FontChooser()
		{
			InitializeComponent();
		}

		public static readonly DependencyProperty SelectedFontProperty = DependencyProperty.Register("SelectedFont", typeof(FontInfo), typeof(FontChooser), new PropertyMetadata(null,
			(o, args) =>
			{
				var chooser = (FontChooser)o;
				var value = (FontInfo)args.NewValue;

				var fontFamilyName = value.Family.Source;
				var idx = (from object item in chooser.FamilyCtrl.Items select item.ToString()).TakeWhile(itemName => fontFamilyName != itemName).Count();
				chooser.FamilyCtrl.SelectedIndex = idx;
				//chooser.FamilyCtrl.ScrollIntoView(FamilyCtrl.Items[idx]);

				chooser.FontSizeSlider.Value = value.Size;

				var fontTypeFaceSb = value.Typeface.TypefaceToString();
				idx = (from object item in chooser.TypefacesCtrl.Items select item as FamilyTypeface).TakeWhile(face => fontTypeFaceSb != face.TypefaceToString()).Count();
				chooser.TypefacesCtrl.SelectedIndex = idx;
			}));

		public FontInfo SelectedFont
		{
			get { return (FontInfo)GetValue(SelectedFontProperty); }
			set { SetValue(SelectedFontProperty, value); }
		}

		private void FamilyCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (SelectedFont == null)
				return;

			SelectedFont.Family = (FontFamily)FamilyCtrl.SelectedItem;
		}

		private void TypefacesCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (SelectedFont == null)
				return;

			SelectedFont.Typeface = (FamilyTypeface)TypefacesCtrl.SelectedItem;
		}
	}
}