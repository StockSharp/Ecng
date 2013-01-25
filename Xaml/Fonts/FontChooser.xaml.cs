namespace Ecng.Xaml.Fonts
{
	public partial class FontChooser
	{
		public FontChooser()
		{
			InitializeComponent();
			txtSampleText.IsReadOnly = true;
		}

		public FontInfo SelectedFont
		{
			get
			{
				return new FontInfo(txtSampleText.FontFamily,
				                    txtSampleText.FontSize,
				                    txtSampleText.FontStyle,
				                    txtSampleText.FontStretch,
				                    txtSampleText.FontWeight);
			}
		}

		//private void colorPicker_ColorChanged(object sender, RoutedEventArgs e)
		//{
		//    txtSampleText.Foreground = colorPicker.SelectedColor.Brush;
		//}
	}
}