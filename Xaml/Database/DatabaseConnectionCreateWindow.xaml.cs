namespace Ecng.Xaml.Database
{
	using System.Windows;

	using Ecng.Data;
	using Ecng.Localization;

	/// <summary>
	/// Окно для создания строки подключения.
	/// </summary>
	public partial class DatabaseConnectionCreateWindow
	{
		/// <summary>
		/// Создать <see cref="DatabaseConnectionCreateWindow"/>.
		/// </summary>
		public DatabaseConnectionCreateWindow()
		{
			InitializeComponent();

			Title = Title.Translate();
			CheckCtrl.Content = ((string)CheckCtrl.Content).Translate();

			Connection = new DatabaseConnectionPair
			{
				Provider = new SqlServerDatabaseProvider()
			};
		}

		public DatabaseConnectionPair Connection
		{
			get { return SettingsGrid.Connection; }
			set { SettingsGrid.Connection = value; }
		}

		private void TestCtrl_Click(object sender, RoutedEventArgs e)
		{
			Ok.IsEnabled = Connection.Test(this);
		}
	}
}