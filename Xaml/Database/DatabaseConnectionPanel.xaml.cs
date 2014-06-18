namespace Ecng.Xaml.Database
{
	using System;
	using System.ComponentModel;
	using System.Data.Common;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Data;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	public partial class DatabaseConnectionPanel
	{
		[DisplayName("Настройки")]
		[Description("Настройки строки подключения к базе данных.")]
		private class Settings : NotifiableObject
		{
			private readonly DbConnectionStringBuilder _builder = new DbConnectionStringBuilder();

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

			[DisplayName("Пароль")]
			[Description("Пароль для доступа к базе данных. Не используется при анонимном доступе.")]
			[Category("Основные")]
			[PropertyOrder(0)]
			[Editor(typeof(DatabaseProviderEditor), typeof(DatabaseProviderEditor))]
			public DatabaseProvider Provider { get; set; }

			[DisplayName("Сервер")]
			[Description("Адрес сервера или путь к базе данных.")]
			[Category("Основные")]
			[PropertyOrder(1)]
			public string Server
			{
				get { return GetValue<string>("Data Source"); }
				set
				{
					_builder["Data Source"] = value;
					NotifyChanged("ConnectionString");
				}
			}

			[DisplayName("База данных")]
			[Description("Название базы данных. Не используется для SQLite.")]
			[Category("Основные")]
			[PropertyOrder(2)]
			public string Database
			{
				get { return GetValue<string>("Initial Catalog"); }
				set
				{
					_builder["Initial Catalog"] = value;
					NotifyChanged("ConnectionString");
				}
			}

			[DisplayName("Логин")]
			[Description("Логин для доступа к базе данных. Не используется при анонимном доступе.")]
			[Category("Основные")]
			[PropertyOrder(3)]
			public string UserName
			{
				get { return GetValue<string>("User ID"); }
				set
				{
					_builder["User ID"] = value;
					NotifyChanged("ConnectionString");
				}
			}

			[DisplayName("Пароль")]
			[Description("Пароль для доступа к базе данных. Не используется при анонимном доступе.")]
			[Category("Основные")]
			[PropertyOrder(4)]
			public string Password
			{
				get { return GetValue<string>("Password"); }
				set
				{
					_builder["Password"] = value;
					NotifyChanged("ConnectionString");
				}
			}

			[DisplayName("Windows")]
			[Description("Использовать текущую учетную запись Windows для подключения к базе данных.")]
			[Category("Основные")]
			[PropertyOrder(5)]
			public bool IntegratedSecurity
			{
				get { return GetValue<bool>("Integrated Security"); }
				set
				{
					_builder["Integrated Security"] = value;
					NotifyChanged("ConnectionString");
				}
			}

			[DisplayName("Подключение")]
			[Description("Готовая строка подключения.")]
			[Category("Основные")]
			[PropertyOrder(6)]
			public string ConnectionString
			{
				get { return _builder.ConnectionString; }
				set
				{
					_builder.ConnectionString = value;

					NotifyChanged("Server");
					NotifyChanged("Database");
					NotifyChanged("UserName");
					NotifyChanged("Password");
					NotifyChanged("IntegratedSecurity");
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