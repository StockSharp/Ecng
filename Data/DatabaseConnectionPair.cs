namespace Ecng.Data
{
	using Ecng.Common;
	using Ecng.ComponentModel;

	public class DatabaseConnectionPair : NotifiableObject
	{
		private DatabaseProvider _provider;

		public DatabaseProvider Provider
		{
			get => _provider;
			set
			{
				_provider = value;
				UpdateTitle();
			}
		}

		private string _connectionString;

		public string ConnectionString
		{
			get => _connectionString;
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
	}
}