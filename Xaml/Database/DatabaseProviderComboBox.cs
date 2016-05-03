namespace Ecng.Xaml.Database
{
	using System.Windows.Controls;

	using Ecng.Data;
	using Ecng.Data.Providers;

	public class DatabaseProviderComboBox : ComboBox
	{
		public DatabaseProviderComboBox()
		{
			DisplayMemberPath = nameof(DatabaseProvider.Name);
			ItemsSource = DatabaseProviderRegistry.Providers;

			if (Items.Count > 0)
				SelectedIndex = 0;
		}

		public DatabaseProvider SelectedProvider
		{
			get { return (DatabaseProvider)SelectedItem; }
			set { SelectedItem = value; }
		}
	}
}