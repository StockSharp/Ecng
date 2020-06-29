namespace Ecng.Xaml.Database
{
	using System;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Data;
	using Ecng.Localization;

	public partial class DatabaseConnectionCreateWindow
	{
		public DatabaseConnectionCreateWindow()
		{
			InitializeComponent();

			Title = Title.Translate();
			CheckCtrl.Content = ((string)CheckCtrl.Content).Translate();
			Ok.Content = ((string)Ok.Content).Translate();

			Connection = new DatabaseConnectionPair
			{
				Provider = new SqlServerDatabaseProvider()
			};
		}

		public DatabaseConnectionPair Connection
		{
			get => SettingsGrid.Connection;
			set => SettingsGrid.Connection = value;
		}

		private void TestCtrl_Click(object sender, RoutedEventArgs e)
		{
			Ok.IsEnabled = TestConnection();
		}

		private bool TestConnection(bool showMessageBox = true)
		{
			if (Connection.ConnectionString.IsEmpty())
			{
				if (showMessageBox)
				{
					new MessageBoxBuilder()
						.Text("Cannot create a connection, because some data was not entered.".Translate())
						.Error()
						.Owner(this)
						.Show();
				}

				return false;
			}

			using (var db = new Database("Test", Connection.ConnectionString) { Provider = Connection.Provider })
			{
				try
				{
					using (db.CreateConnection()) { }

					if (showMessageBox)
					{
						new MessageBoxBuilder()
							.Text("Connection successfully checked.".Translate())
							.Owner(this)
							.Show();
					}

					return true;
				}
				catch (Exception ex)
				{
					if (showMessageBox)
					{
						new MessageBoxBuilder()
							.Text("Cannot connect.".Translate() + Environment.NewLine + ex)
							.Error()
							.Owner(this)
							.Show();
					}

					return false;
				}
			}
		}
	}
}