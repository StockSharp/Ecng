namespace Ecng.Xaml
{
	using System;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.Data;

	/// <summary>
	/// Окно для создания строки подключения.
	/// </summary>
	public partial class DatabaseConnectionStringCreateWindow
	{
		/// <summary>
		/// Создать <see cref="DatabaseConnectionStringCreateWindow"/>.
		/// </summary>
		public DatabaseConnectionStringCreateWindow()
		{
			InitializeComponent();
		}

		public DatabaseConnectionPair Connection
		{
			get { return new DatabaseConnectionPair(ProvidersCtrl.SelectedProvider, ConnectionStringCtrl.Text); }
			//set { ConnectionStringCtrl.Text = value; }
		}

		private void ConnectionStringCtrl_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			TryEnable();
		}

		private void ProvidersCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			TryEnable();
		}

		private void TryEnable()
		{
			var isEnabled = ProvidersCtrl.SelectedProvider != null && !ConnectionStringCtrl.Text.IsEmpty();
			Ok.IsEnabled = TestCtrl.IsEnabled = isEnabled;
		}

		private void TestCtrl_Click(object sender, RoutedEventArgs e)
		{
			using (var db = new Database("Test", ConnectionStringCtrl.Text) { Provider = ProvidersCtrl.SelectedProvider })
			{
				try
				{
					using (db.CreateConnection()) { }

					new MessageBoxBuilder()
						.Text("Проверка прошла успешно.")
						.Owner(this)
						.Show();
				}
				catch (Exception ex)
				{
					new MessageBoxBuilder()
						.Text("Не удалось подключиться к базе данных. Причина '{0}'.".Put(ex.Message))
						.Error()
						.Owner(this)
						.Show();
				}
			}
		}
	}
}