namespace Ecng.Xaml.DevExp.Database
{
	using System;
	using System.ComponentModel;
	using System.Data.Common;
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Data;
	using Ecng.Localization;

	public partial class DatabaseConnectionPanel
	{
		private enum Titles
		{
			Settings,
			SettingsDescription,
			Login,
			LoginDescription,
			Password,
			PasswordDescription,
			Server,
			ServerDescription,
			Database,
			DatabaseDescription,
			Security,
			SecurityDescription,
			String,
			StringDescription,
			Provider,
			ProviderDescription,
		}

		private class DatabaseDisplayNameAttribute : DisplayNameAttribute
		{
			public DatabaseDisplayNameAttribute(Titles title)
				: base(GetString(title))
			{
			}

			private static string GetString(Titles title)
			{
				switch (title)
				{
					case Titles.Settings:
						return "Settings".Translate();
					case Titles.Login:
						return "Login".Translate();
					case Titles.Password:
						return "Password".Translate();
					case Titles.Provider:
						return "Provider".Translate();
					case Titles.Server:
						return "Server".Translate();
					case Titles.Database:
						return "Database".Translate();
					case Titles.Security:
						return "Integrated security".Translate();
					case Titles.String:
						return "Connection string".Translate();
					default:
						throw new ArgumentOutOfRangeException(nameof(title));
				}
			}
		}

		private class DatabaseDescriptionAttribute : DescriptionAttribute
		{
			public DatabaseDescriptionAttribute(Titles title)
				: base(GetString(title))
			{
			}

			private static string GetString(Titles title)
			{
				switch (title)
				{
					case Titles.SettingsDescription:
						return "Database connection string settings.".Translate();
					case Titles.LoginDescription:
						return "User name. Not used in anonymous mode.".Translate();
					case Titles.PasswordDescription:
						return "Password. Not used in anonymous mode.".Translate();
					case Titles.ServerDescription:
						return "Network address or path to file.".Translate();
					case Titles.DatabaseDescription:
						return "Name of database. Not used in SQLite.".Translate();
					case Titles.SecurityDescription:
						return "Use integrated security (like Windows accounts).".Translate();
					case Titles.StringDescription:
						return "Final connection string.".Translate();
					case Titles.ProviderDescription:
						return "Provider settings.".Translate();
					default:
						throw new ArgumentOutOfRangeException(nameof(title));
				}
			}
		}

		private class DatabaseCategoryAttribute : CategoryAttribute
		{
			public DatabaseCategoryAttribute()
				: base("Common".Translate())
			{
			}
		}

		[DatabaseDisplayName(Titles.Settings)]
		[DatabaseDescription(Titles.SettingsDescription)]
		private class Settings : NotifiableObject
		{
			private readonly DbConnectionStringBuilder _builder = new DbConnectionStringBuilder();

			private DatabaseProvider _provider;

			private T GetValue<T>(string key)
			{
				if (_builder.Keys.Cast<string>().Contains(key))
				{
					try
					{
						return _builder[key].To<T>();
					}
					catch (InvalidCastException)
					{
						return default(T);
					}
				}

				return default(T);
			}

			[DatabaseDisplayName(Titles.Provider)]
			[DatabaseDescription(Titles.ProviderDescription)]
			[DatabaseCategory]
			//[PropertyOrder(0)]
			//[Editor(typeof(DatabaseProviderEditor), typeof(DatabaseProviderEditor))]
			public DatabaseProvider Provider
			{
				get => _provider;
				set
				{
					_provider = value;
					NotifyChanged(nameof(Provider));
				}
			}

			[DatabaseDisplayName(Titles.Server)]
			[DatabaseDescription(Titles.ServerDescription)]
			[DatabaseCategory]
			//[PropertyOrder(1)]
			public string Server
			{
				get => GetValue<string>("Data Source");
				set
				{
					_builder["Data Source"] = value;
					NotifyChanged(nameof(ConnectionString));
				}
			}

			[DatabaseDisplayName(Titles.Database)]
			[DatabaseDescription(Titles.DatabaseDescription)]
			[DatabaseCategory]
			//[PropertyOrder(2)]
			public string Database
			{
				get => GetValue<string>("Initial Catalog");
				set
				{
					_builder["Initial Catalog"] = value;
					NotifyChanged(nameof(ConnectionString));
				}
			}

			[DatabaseDisplayName(Titles.Login)]
			[DatabaseDescription(Titles.LoginDescription)]
			[DatabaseCategory]
			//[PropertyOrder(3)]
			public string UserName
			{
				get => GetValue<string>("User ID");
				set
				{
					_builder["User ID"] = value;
					NotifyChanged(nameof(ConnectionString));
				}
			}

			[DatabaseDisplayName(Titles.Password)]
			[DatabaseDescription(Titles.PasswordDescription)]
			[DatabaseCategory]
			//[PropertyOrder(4)]
			public SecureString Password
			{
				get => GetValue<string>("Password").To<SecureString>();
				set
				{
					_builder["Password"] = value.To<string>();
					NotifyChanged(nameof(ConnectionString));
				}
			}

			[DatabaseDisplayName(Titles.Security)]
			[DatabaseDescription(Titles.SecurityDescription)]
			[DatabaseCategory]
			//[PropertyOrder(5)]
			public bool IntegratedSecurity
			{
				get => GetValue<bool>("Integrated Security");
				set
				{
					if (value)
						_builder["Integrated Security"] = true;
					else
						_builder.Remove("Integrated Security");

					NotifyChanged(nameof(ConnectionString));
				}
			}

			[DatabaseDisplayName(Titles.String)]
			[DatabaseDescription(Titles.StringDescription)]
			[DatabaseCategory]
			//[PropertyOrder(6)]
			public string ConnectionString
			{
				get => _builder.ConnectionString;
				set
				{
					_builder.ConnectionString = value;

					NotifyChanged(nameof(Server));
					NotifyChanged(nameof(Database));
					NotifyChanged(nameof(UserName));
					NotifyChanged(nameof(Password));
					NotifyChanged(nameof(IntegratedSecurity));
				}
			}
		}

		public DatabaseConnectionPanel()
		{
			InitializeComponent();
		}

		private DatabaseConnectionPair _connection;
		private Settings _settings;

		public DatabaseConnectionPair Connection
		{
			get
			{
				if (_connection != null)
				{
					_connection.Provider = _settings.Provider;
					_connection.ConnectionString = _settings.ConnectionString;
				}

				return _connection;
			}
			set
			{
				if (value == null)
				{
					SelectedObject = null;
					_connection = null;
					_settings = null;
					return;
				}

				_connection = value;

				_settings = new Settings
				{
					Provider = value.Provider
				};

				if (!value.ConnectionString.IsEmpty())
					_settings.ConnectionString = value.ConnectionString;

				SelectedObject = _settings;
			}
		}
	}
}