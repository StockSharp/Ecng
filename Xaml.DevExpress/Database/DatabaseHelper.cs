namespace Ecng.Xaml.DevExp.Database
{
	using System;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Data;
	using Ecng.Localization;

	public static class DatabaseHelper
	{
		public static bool Test(this DatabaseConnectionPair pair, DependencyObject owner, bool showMessageBox = true)
		{
			if (pair == null)
				throw new ArgumentNullException(nameof(pair));

			if (pair.ConnectionString.IsEmpty())
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

			using (var db = new Database("Test", pair.ConnectionString) { Provider = pair.Provider })
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