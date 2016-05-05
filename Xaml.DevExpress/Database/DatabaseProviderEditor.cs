namespace Ecng.Xaml.DevExp.Database
{
	using DevExpress.Xpf.Editors.Settings;

	/// <summary>
	/// <see cref="ComboBoxEditSettings"/> for <see cref="DatabaseProviderComboBox"/>.
	/// </summary>
	public class DatabaseProviderEditor : ComboBoxEditSettings
	{
		private readonly DatabaseProviderComboBox _cb = new DatabaseProviderComboBox();

		public DatabaseProviderEditor()
		{
			DisplayMember = _cb.DisplayMemberPath;
			ItemsSource = _cb.ItemsSource;
		}
	}
}