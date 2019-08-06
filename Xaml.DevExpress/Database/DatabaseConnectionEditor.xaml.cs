namespace Ecng.Xaml.DevExp.Database
{
	using System.Collections.ObjectModel;
	using System.Windows;
	using System.Windows.Controls.Primitives;

	using DevExpress.Xpf.Editors;
	using DevExpress.Xpf.Editors.Helpers;

	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Xaml;

	/// <summary>
	/// Визуальный редактор для выбора строчки подключения к базе данных.
	/// </summary>
	public partial class DatabaseConnectionEditor
	{
		private readonly ObservableCollection<DatabaseConnectionPair> _connections = new ObservableCollection<DatabaseConnectionPair>();

		private ComboBoxEdit _edit;

		static DatabaseConnectionEditor()
		{
			EditorSettingsProvider.Default.RegisterUserEditor2(typeof(DatabaseConnectionComboBox), typeof(DatabaseConnectionEditor), optimized => optimized ? new InplaceBaseEdit() : (IBaseEdit)new DatabaseConnectionComboBox(), () => new DatabaseConnectionEditor());
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseConnectionEditor"/>.
		/// </summary>
		public DatabaseConnectionEditor()
		{
			InitializeComponent();

			_connections.AddRange(Cache.Connections);

			Cache.NewConnectionCreated += OnNewConnectionCreated;
			Cache.ConnectionDeleted += OnConnectionDeleted;

			ItemsSource = _connections;
		}

		private void OnNewConnectionCreated(DatabaseConnectionPair connection)
		{
			this.GuiAsync(() => _connections.Add(connection));
		}

		private void OnConnectionDeleted(DatabaseConnectionPair connection)
		{
			this.GuiAsync(() => _connections.Remove(connection));
		}

		private static DatabaseConnectionCache Cache => ConfigManager.GetService<DatabaseConnectionCache>();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="edit"></param>
		protected override void AssignToEditCore(IBaseEdit edit)
		{
			_edit = edit as ComboBoxEdit;
			base.AssignToEditCore(edit);
		}

		private void SearchBtn_OnClick(object sender, RoutedEventArgs e)
		{
			var wnd = new DatabaseConnectionCreateWindow();

			if (!wnd.ShowModal(((ButtonBase)sender).GetWindow()))
				return;

			var connection = Cache.GetConnection(wnd.Connection.Provider, wnd.Connection.ConnectionString);

			if (_edit != null)
				_edit.SelectedItem = connection;
		}
	}
}