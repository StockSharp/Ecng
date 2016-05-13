namespace Ecng.Data
{
	using System;
	using System.Collections.Generic;
	using System.Data.Common;
	using System.IO;
	using System.Linq;
#if DEBUG
	using System.Data;
	using System.Diagnostics;
	using System.Text.RegularExpressions;
#endif

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;

	public class DatabaseCommandTimeout
	{
		public DatabaseCommandTimeout(TimeSpan timeout)
		{
			Timeout = timeout;
		}

		public TimeSpan Timeout { get; }
	}

	[Serializable]
	public sealed class DatabaseCommand : Disposable
	{
		private static readonly object _syncObj = new object();

		private readonly DbCommand _dbCommand;

		internal DatabaseCommand(Database database, DbCommand dbCommand)
		{
			if (database == null)
				throw new ArgumentNullException(nameof(database));

			if (dbCommand == null)
				throw new ArgumentNullException(nameof(dbCommand));

			Database = database;
			_dbCommand = dbCommand;
		}

		public Database Database { get; }

		public string Text => _dbCommand.CommandText;

		public DbParameterCollection Parameters => _dbCommand.Parameters;

		private TResult Execute<TResult>(IEnumerable<SerializationItem> input, Func<DbCommand, TResult> handler)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			lock (_syncObj)
			{
				//Debug.WriteLine(_dbCommand.CommandText);

				var result = default(TResult);

				Database.GetConnection(connection =>
				{
					using (var cmd = CreateCommand(connection, input))
					{
#if DEBUG
						var dbgStr = cmd.CommandText;

						if (cmd.CommandType == CommandType.Text)
						{
							foreach (DbParameter parameter in cmd.Parameters)
							{
								dbgStr = Regex.Replace(dbgStr, parameter.ParameterName, parameter.Value.To<string>(), RegexOptions.IgnoreCase);
							}
						}
						else
						{
							foreach (DbParameter parameter in cmd.Parameters)
							{
								dbgStr += "{0} = {1}, ".Put(parameter.ParameterName, parameter.Value.To<string>());
							}
						}

						Debug.WriteLine(dbgStr);
#endif
						result = handler(cmd);
					}
				});

				return result;
			}
		}

		public int ExecuteNonQuery(IEnumerable<SerializationItem> input)
		{
			return Execute(input, cmd => cmd.ExecuteNonQuery());
		}

		public TScalar ExecuteScalar<TScalar>(IEnumerable<SerializationItem> input)
		{
			return Execute(input, cmd => cmd.ExecuteScalar().To<TScalar>());
		}

		public SerializationItemCollection ExecuteRow(IEnumerable<SerializationItem> input)
		{
			return Execute(input, cmd =>
			{
				using (var reader = cmd.ExecuteReader())
				{
					if (reader.Read())
					{
						var row = new SerializationItemCollection();

						for (var i = 0; i < reader.FieldCount; i++)
						{
							row.Add(new SerializationItem
							(
								new VoidField(reader.GetName(i), reader.GetFieldType(i)),
								reader.IsDBNull(i) ? null : reader.GetValue(i)
							));
						}

						return row;
					}
					else
						return null;
				}
			});
		}

		public SerializationItemCollection ExecuteTable(SerializationItemCollection input)
		{
			return Execute(input, cmd =>
			{
				using (var reader = cmd.ExecuteReader())
				{
					List<object>[] values = null;
					var table = new SerializationItemCollection();

					while (reader.Read())
					{
						if (values == null)
						{
							values = new List<object>[reader.FieldCount];

							for (var i = 0; i < reader.FieldCount; i++)
							{
								values[i] = new List<object>();
								table.Add(new SerializationItem(new VoidField(reader.GetName(i), typeof(List<object>)), values[i]));
							}
						}

						for (var i = 0; i < reader.FieldCount; i++)
							values[i].Add(reader.IsDBNull(i) ? null : reader.GetValue(i));
					}

					return table;
				}
			});
		}

		private DbCommand CreateCommand(DbConnection connection, IEnumerable<SerializationItem> source)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			if (source == null)
				throw new ArgumentNullException(nameof(source));

			var dict = source.ToDictionary(i => Database.Provider.Renderer.FormatParameter(i.Field.Name), i => i.Value, StringComparer.InvariantCultureIgnoreCase);

			var command = Database.Provider.CreateCommand(_dbCommand.CommandText, _dbCommand.CommandType);
			command.Connection = connection;

			var timeout = Scope<DatabaseCommandTimeout>.Current;
			if (timeout != null)
				command.CommandTimeout = (int)timeout.Value.Timeout.TotalSeconds;

			foreach (DbParameter parameter in _dbCommand.Parameters)
			{
				var clone = Database.Provider.CreateParameter(parameter.ParameterName, parameter.DbType);

				clone.Direction = parameter.Direction;
				clone.IsNullable = parameter.IsNullable;
				clone.Size = parameter.Size;
				clone.SourceColumn = parameter.SourceColumn;
				clone.SourceColumnNullMapping = parameter.SourceColumnNullMapping;
				clone.SourceVersion = parameter.SourceVersion;

				command.Parameters.Add(clone);

				clone.Value = dict.TryGetValue(clone.ParameterName) ?? DBNull.Value;

				// некоторые БД не умеют работать с потоками
				if (clone.Value is Stream)
					clone.Value = clone.Value.To<byte[]>();
			}

			return command;
		}

		protected override void DisposeManaged()
		{
			_dbCommand.Dispose();
			base.DisposeManaged();
		}
	}
}