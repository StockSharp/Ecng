using System.Windows;
using System.Windows.Media;

namespace Ecng.Xaml.Fonts
{
	/// <summary>
	/// http://www.codeproject.com/Articles/368070/A-WPF-Font-Picker-with-Color
	/// </summary>
	public partial class FontDialog
	{
		public FontDialog()
		{
			Font = null; // Default
			InitializeComponent();
		}

		public FontInfo Font { get; set; }

		private void SyncFontName()
		{
			var fontFamilyName = Font.Family.Source;
			var idx = 0;

			foreach (var item in colorFontChooser.lstFamily.Items)
			{
				var itemName = item.ToString();
				if (fontFamilyName == itemName)
				{
					break;
				}
				idx++;
			}

			colorFontChooser.lstFamily.SelectedIndex = idx;
			colorFontChooser.lstFamily.ScrollIntoView(colorFontChooser.lstFamily.Items[idx]);
		}

		private void SyncFontSize()
		{
			colorFontChooser.fontSizeSlider.Value = Font.Size;
		}

		//private void SyncFontColor()
		//{
		//    var colorIdx = AvailableColors.GetFontColorIndex(this.Font.Color);
		//    colorFontChooser.colorPicker.superCombo.SelectedIndex = colorIdx;
		//    // The following does not work. Why???
		//    // this.colorFontChooser.colorPicker.superCombo.SelectedValue = this.Font.Color;
		//    colorFontChooser.colorPicker.superCombo.BringIntoView();
		//}

		private void SyncFontTypeface()
		{
			var fontTypeFaceSb = Font.Typeface.TypefaceToString();
			var idx = 0;

			foreach (var item in colorFontChooser.lstTypefaces.Items)
			{
				var face = item as FamilyTypeface;
				if (fontTypeFaceSb == face.TypefaceToString())
				{
					break;
				}
				idx++;
			}

			colorFontChooser.lstTypefaces.SelectedIndex = idx;
		}

		private void btnOk_Click(object sender, RoutedEventArgs e)
		{
			Font = colorFontChooser.SelectedFont;
			DialogResult = true;
		}

		private void ColorFontDialog_Loaded(object sender, RoutedEventArgs e)
		{
			//SyncFontColor();
			SyncFontName();
			SyncFontSize();
			SyncFontTypeface();
		}
	}
}