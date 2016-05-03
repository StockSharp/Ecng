namespace Ecng.Xaml.DevExp
{
	using DevExpress.Xpf.Editors.Settings;

	using Ecng.Xaml;

	class EncodingEditor : ComboBoxEditSettings
	{
		private readonly EncodingComboBox _cb = new EncodingComboBox();

		public EncodingEditor()
		{
			DisplayMember = _cb.DisplayMemberPath;
			ItemsSource = _cb.ItemsSource;
		}
	}
}