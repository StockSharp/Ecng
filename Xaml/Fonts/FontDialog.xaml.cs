namespace Ecng.Xaml.Fonts
{
	/// <summary>
	/// http://www.codeproject.com/Articles/368070/A-WPF-Font-Picker-with-Color
	/// </summary>
	public partial class FontDialog
	{
		public FontDialog()
		{
			InitializeComponent();
		}

		public FontInfo Font
		{
			get { return FontChooser.SelectedFont; }
			set { FontChooser.SelectedFont = value; }
		}
	}
}