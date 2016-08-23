namespace Ecng.Data
{
	using System;
	using System.Data;
	using System.Data.Common;
	using System.Data.SqlClient;

	using Ecng.Common;

	public abstract class DatabaseProvider : NamedObject
	{
		protected DatabaseProvider(DbProviderFactory factory, SqlRenderer renderer)
			: this(factory, renderer, renderer.GetType().Name.Remove("Renderer"))
		{
		}

		protected DatabaseProvider(DbProviderFactory factory, SqlRenderer renderer, string name)
			: base(name)
		{
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));

			if (renderer == null)
				throw new ArgumentNullException(nameof(renderer));

			Factory = factory;
			Renderer = renderer;
		}

		public DbProviderFactory Factory { get; }
		public SqlRenderer Renderer { get; private set; }

		public DbConnection CreateConnection(string connectionString)
		{
			var connection = Factory.CreateConnection();

			if (connection == null)
				throw new InvalidOperationException();

			connection.ConnectionString = connectionString;
			connection.Open();
			return connection;
		}

		public DbCommand CreateCommand(string text, CommandType type)
		{
			var command = Factory.CreateCommand();

			if (command == null)
				throw new InvalidOperationException();

			command.CommandText = text;
			command.CommandType = type;

			return command;
		}

		public DbParameter CreateParameter(string name, DbType type)
		{
			var param = Factory.CreateParameter();

			if (param == null)
				throw new InvalidOperationException();

			param.ParameterName = name;
			param.DbType = type;

			// http://connect.microsoft.com/VisualStudio/feedback/details/381934/sqlparameter-dbtype-dbtype-time-sets-the-parameter-to-sqldbtype-datetime-instead-of-sqldbtype-time
			var sqlParam = param as SqlParameter;
			if (sqlParam != null && (type == DbType.Date || type == DbType.Time))
				sqlParam.SqlDbType = type == DbType.Date ? SqlDbType.Date : SqlDbType.Time;

			return param;
		}

		protected internal abstract void DeriveParameters(DbCommand command);
	}
}