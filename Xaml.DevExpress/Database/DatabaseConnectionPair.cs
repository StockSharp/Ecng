namespace Ecng.Xaml.DevExp.Database
{
	using System;
	using System.Windows;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Data;
	using Ecng.Localization;

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
			NotifyChanged(nameof(Title));
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
						.Text("Cannot create a connection, because some data was not entered.".Translate())
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
							.Text("Connection successfully checked.".Translate())
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
							.Text("Cannot connect.".Translate() + Environment.NewLine + ex)
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