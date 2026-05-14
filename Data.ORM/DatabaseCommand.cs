namespace Ecng.Data;

#if DEBUG
using System.Diagnostics;
using System.Text.RegularExpressions;
#endif

/// <summary>
/// Wraps a <see cref="DbCommand"/> and provides execution methods for database operations.
/// </summary>
[Serializable]
public sealed class DatabaseCommand : Disposable
{
	private readonly DbProviderFactory _factory;
	private readonly ISqlDialect _dialect;
	private readonly Func<CancellationToken, ValueTask<DbConnection>> _createConnection;
	private readonly DbCommand _dbCommand;

	internal DatabaseCommand(DbProviderFactory factory, ISqlDialect dialect, Func<CancellationToken, ValueTask<DbConnection>> createConnection, DbCommand dbCommand)
	{
		_factory = factory ?? throw new ArgumentNullException(nameof(factory));
		_dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
		_createConnection = createConnection ?? throw new ArgumentNullException(nameof(createConnection));
		_dbCommand = dbCommand ?? throw new ArgumentNullException(nameof(dbCommand));
	}

	/// <summary>
	/// Gets the SQL command text.
	/// </summary>
	public string CommandText => _dbCommand.CommandText;

	/// <summary>
	/// Gets the command type (text, stored procedure, etc.).
	/// </summary>
	public CommandType CommandType => _dbCommand.CommandType;

	/// <summary>
	/// Gets the collection of command parameters.
	/// </summary>
	public DbParameterCollection Parameters => _dbCommand.Parameters;

	private async ValueTask<TResult> Execute<TResult>(IEnumerable<SerializationItem> input, Func<DbCommand, ValueTask<TResult>> handler, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(input);

		ArgumentNullException.ThrowIfNull(handler);

		//Debug.WriteLine(_dbCommand.CommandText);

		using var connection = await _createConnection(cancellationToken).NoWait();
		using var cmd = CreateCommand(connection, input);
#if DEBUG
		var dbgStr = cmd.CommandText;

		static string GetParamArg(DbParameter parameter)
		{
			ArgumentNullException.ThrowIfNull(parameter);

			var pv = parameter.Value;

			if (pv is bool b)
				pv = b ? 1 : 0;
			else if (pv is byte[] bytes)
				return $"byte[{bytes.Length}]";

			var arg = pv.To<string>();

			if (pv is string || pv is DateTime || pv is DateTimeOffset)
				return $"'{arg}'";
			else
				return (arg ?? string.Empty).IsEmpty("null");
		}

		if (cmd.CommandType == CommandType.Text)
		{
			foreach (DbParameter parameter in cmd.Parameters)
			{
				dbgStr = Regex.Replace(dbgStr, @$"{parameter.ParameterName}\b", GetParamArg(parameter), RegexOptions.IgnoreCase);
			}
		}
		else
		{
			foreach (DbParameter parameter in cmd.Parameters)
			{
				dbgStr += $"{parameter.ParameterName} = {GetParamArg(parameter)}, ";
			}
		}

		Debug.WriteLine(dbgStr);
#endif
		return await handler(cmd).NoWait();	}

	/// <summary>
	/// Executes the command and returns the number of rows affected.
	/// </summary>
	public ValueTask<int> ExecuteNonQuery(IEnumerable<SerializationItem> input, CancellationToken cancellationToken)
		=> Execute(input, async cmd => await cmd.ExecuteNonQueryAsync(cancellationToken).NoWait(), cancellationToken);
	/// <summary>
	/// Executes the command and returns the first column of the first row as the specified type.
	/// </summary>
	public ValueTask<TScalar> ExecuteScalar<TScalar>(IEnumerable<SerializationItem> input, CancellationToken cancellationToken)
		=> Execute(input, async cmd => (await cmd.ExecuteScalarAsync(cancellationToken).NoWait()).To<TScalar>(), cancellationToken);
	/// <summary>
	/// Executes the command and returns the first result row as a serialization item collection.
	/// </summary>
	public ValueTask<SerializationItemCollection> ExecuteRow(IEnumerable<SerializationItem> input, CancellationToken cancellationToken)
		=> Execute(input, async cmd =>
		{
			using var reader = await cmd.ExecuteReaderAsync(cancellationToken).NoWait();
			// async version disable for a while
			// https://github.com/dotnet/SqlClient/issues/593
			//if (await reader.ReadAsync(cancellationToken))
			// For batch queries (INSERT/UPDATE + SELECT), the first result set
			// may be empty (INSERT doesn't produce rows in SQLite/PostgreSQL).
			// Skip to the next result set containing actual data.
			if (!reader.Read())
			{
				if (!reader.NextResult() || !reader.Read())
					return null;
			}

			var row = new SerializationItemCollection();

			for (var i = 0; i < reader.FieldCount; i++)
			{
				row.Add(new
				(
					reader.GetName(i),
					reader.GetFieldType(i),
					reader.GetValueEx(i)
				));
			}

			return row;
		}, cancellationToken);

	/// <summary>
	/// Executes the command and returns all result rows as a columnar
	/// serialization item collection. Loads the entire result set into
	/// memory; for arbitrarily-large reads use <see cref="ExecuteRowsAsync"/>.
	/// </summary>
	public ValueTask<SerializationItemCollection> ExecuteTable(SerializationItemCollection input, CancellationToken cancellationToken)
	{
		return Execute(input, async cmd =>
		{
			using var reader = await cmd.ExecuteReaderAsync(cancellationToken).NoWait();
			List<object>[] values = null;
			var table = new SerializationItemCollection();

			while (await reader.ReadAsync(cancellationToken))
			{
				if (values == null)
				{
					values = new List<object>[reader.FieldCount];

					for (var i = 0; i < reader.FieldCount; i++)
					{
						values[i] = [];
						table.Add(new(reader.GetName(i), typeof(List<object>), values[i]));
					}
				}

				for (var i = 0; i < reader.FieldCount; i++)
					values[i].Add(reader.GetValueEx(i));
			}

			return table;
		}, cancellationToken);
	}

	/// <summary>
	/// Streams result rows one at a time without buffering the whole set
	/// in memory — preferred over <see cref="ExecuteTable"/> for queries
	/// that may return millions of rows.
	/// </summary>
	public async IAsyncEnumerable<SerializationItemCollection> ExecuteRowsAsync(
		SerializationItemCollection input,
		[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(input);

		using var connection = await _createConnection(cancellationToken).NoWait();
		using var cmd = CreateCommand(connection, input);
		using var reader = await cmd.ExecuteReaderAsync(cancellationToken).NoWait();

		while (await reader.ReadAsync(cancellationToken).NoWait())
		{
			var row = new SerializationItemCollection();
			for (var i = 0; i < reader.FieldCount; i++)
				row.Add(new(reader.GetName(i), reader.GetFieldType(i), reader.GetValueEx(i)));
			yield return row;
		}
	}

	private DbCommand CreateCommand(DbConnection connection, IEnumerable<SerializationItem> source)
	{
		ArgumentNullException.ThrowIfNull(connection);

		ArgumentNullException.ThrowIfNull(source);

		var dict = source.ToDictionary(i => _dialect.ParameterPrefix + i.Name, i => i.Value, StringComparer.InvariantCultureIgnoreCase);

		var command = _factory.CreateCommand();
		command.CommandText = CommandText;
		command.CommandType = CommandType;
		command.Connection = connection;

		var timeout = Scope<DatabaseCommandTimeout>.Current;
		if (timeout != null)
			command.CommandTimeout = (int)timeout.Value.Timeout.TotalSeconds;

		foreach (DbParameter parameter in Parameters)
		{
			var clone = _factory.CreateParameter();

			clone.ParameterName = parameter.ParameterName;
			clone.DbType = parameter.DbType;
			clone.Direction = parameter.Direction;
			clone.IsNullable = parameter.IsNullable;
			//clone.Size = parameter.Size;
			clone.SourceColumn = parameter.SourceColumn;
			clone.SourceColumnNullMapping = parameter.SourceColumnNullMapping;
			clone.SourceVersion = parameter.SourceVersion;

			command.Parameters.Add(clone);

			if (dict.TryGetValue(clone.ParameterName, out var value) && value is not null)
				clone.Value = value.To(clone.DbType.To<Type>());
			else
				clone.Value = DBNull.Value;

			_dialect.PrepareParameter(clone);
		}

		return command;
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_dbCommand.Dispose();
		base.DisposeManaged();
	}
}
