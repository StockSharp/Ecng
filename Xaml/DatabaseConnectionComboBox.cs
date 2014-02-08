namespace Ecng.Xaml
{
	using System.Collections.ObjectModel;
	using System.Configuration;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Data;
	using Ecng.Data.Providers;

	/// <summary>
	/// Комбо элемент для выбора подключения к базе данных.
	/// </summary>
	public class DatabaseConnectionComboBox : ComboBox
	{
		private class NewConnectionPair : DatabaseConnectionPair
		{
			public NewConnectionPair()
				: base(new FirebirdDatabaseProvider(), "Новое подключение...")
			{
			}

			public override string ToString()
			{
				return ConnectionString;
			}
		}
		private static readonly DatabaseConnectionPair _newConnection = new NewConnectionPair();
		private readonly ObservableCollection<DatabaseConnectionPair> _connections = new ObservableCollection<DatabaseConnectionPair>();
		private int _prevIndex;
 
		/// <summary>
		/// Создать <see cref="DatabaseConnectionComboBox"/>.
		/// </summary>
		public DatabaseConnectionComboBox()
		{
			ItemsSource = _connections;

			_connections.AddRange(from ConnectionStringSettings setting in ConfigurationManager.ConnectionStrings
								  select new DatabaseConnectionPair(DatabaseProviderRegistry.GetProvider(setting.ProviderName), setting.ConnectionString));
			
			_connections.Add(_newConnection);

			_prevIndex = SelectedIndex;
		}

		private void AddNewConnection(DatabaseConnectionPair connection)
		{
			_connections.Insert(_connections.Count - 1, connection);
		}

		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			base.OnSelectionChanged(e);

			if ((DatabaseConnectionPair)SelectedItem == _newConnection)
			{
				var wnd = new DatabaseConnectionStringCreateWindow();
				if (wnd.ShowModal(this) && !_connections.Contains(wnd.Connection))
				{
					AddNewConnection(wnd.Connection);
					SelectedConnection = wnd.Connection;
				}
				else
				{
					SelectedIndex = _prevIndex;
				}
			}
			else
				SelectedConnection = (DatabaseConnectionPair)SelectedItem;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="SelectedConnection"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedConnectionProperty =
			DependencyProperty.Register("SelectedConnection", typeof(DatabaseConnectionPair), typeof(DatabaseConnectionComboBox), new PropertyMetadata(SelectedConnectionPropertyChanged));

		private static void SelectedConnectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ctrl = d as DatabaseConnectionComboBox;
			if (ctrl == null)
				return;

			if (e.NewValue == null)
			{
				ctrl.SelectedIndex = -1;
				ctrl._prevIndex = -1;
			}
			else
			{
				var pair = (DatabaseConnectionPair)e.NewValue;

				if (!ctrl._connections.Contains(pair))
					ctrl.AddNewConnection(pair);

				ctrl.SelectedIndex = ctrl._prevIndex = ctrl._connections.IndexOf(pair);
			}
		}

		/// <summary>
		/// Выбранное подключения.
		/// </summary>
		public DatabaseConnectionPair SelectedConnection
		{
			get { return (DatabaseConnectionPair)GetValue(SelectedConnectionProperty); }
			set { SetValue(SelectedConnectionProperty, value); }
		}
	}
}