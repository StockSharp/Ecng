namespace Ecng.Xaml.Database
{
	using System;
	using System.Windows;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Data;

	public class DatabaseConnectionPair : NotifiableObject
	{
		private DatabaseProvider _provider;

		public DatabaseProvider Provider
		{
			get { return _provider; }
			set
			{
				_provider = value;
				UpdateTitle();
			}
		}

		private string _connectionString;

		public string ConnectionString
		{
			get { return _connectionString; }
			set
			{
				_connectionString = value;
				UpdateTitle();
			}
		}

		public virtual string Title { get; private set; }

		private void UpdateTitle()
		{
			Title = "({0}) {1}".Put(Provider == null ? string.Empty : Provider.Name, ConnectionString);
			NotifyChanged("Title");
		}

		public override string ToString()
		{
			return Title;
		}

		public bool Test(DependencyObject owner, bool showMessageBox = true)
		{
			if (ConnectionString.IsEmpty())
			{
				if (showMessageBox)
				{
					new MessageBoxBuilder()
						.Text("Строка подключения не указана.")
						.Error()
						.Owner(owner)
						.Show();
				}

				return false;
			}

			using (var db = new Database("Test", ConnectionString) { Provider = Provider })
			{
				try
				{
					using (db.CreateConnection()) { }

					if (showMessageBox)
					{
						new MessageBoxBuilder()
							.Text("Проверка прошла успешно.")
							.Owner(owner)
							.Show();
					}

					return true;
				}
				catch (Exception ex)
				{
					if (showMessageBox)
					{
						new MessageBoxBuilder()
							.Text("Не удалось подключиться к базе данных. Причина '{0}'.".Put(ex.Message))
							.Error()
							.Owner(owner)
							.Show();
					}

					return false;
				}
			}
		}
	}
}