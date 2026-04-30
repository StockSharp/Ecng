namespace Ecng.Data;

using System.Data.Common;
using System.Text.RegularExpressions;

using Ecng.Common;
using Ecng.Data.Sql;

/// <summary>
/// Builds and caches schema-driven <see cref="DatabaseCommand"/> instances
/// for the standard CRUD operations (the path that goes through
/// <see cref="QueryProvider"/>). Extracted from <see cref="Database"/> so
/// the command-construction concern is unit-testable in isolation and
/// the (already large) <see cref="Database"/> file does not own one more
/// thing.
/// </summary>
internal sealed class CommandFactory
{
	private readonly DbProviderFactory _providerFactory;
	private readonly ISqlDialect _dialect;
	private readonly QueryProvider _queryProvider;
	private readonly Func<CancellationToken, ValueTask<DbConnection>> _createConnection;
	private readonly Regex _parameterRegex = new("@(?<parameterName>[_a-zA-Z0-9]+)", RegexOptions.Multiline);
	private readonly SynchronizedDictionary<Query, TaskCompletionSource<DatabaseCommand>> _commandsByQuery = [];

	public CommandFactory(
		DbProviderFactory providerFactory,
		ISqlDialect dialect,
		QueryProvider queryProvider,
		Func<CancellationToken, ValueTask<DbConnection>> createConnection)
	{
		_providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
		_dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
		_queryProvider = queryProvider ?? throw new ArgumentNullException(nameof(queryProvider));
		_createConnection = createConnection ?? throw new ArgumentNullException(nameof(createConnection));
	}

	/// <summary>
	/// Returns a cached <see cref="DatabaseCommand"/> for the given schema
	/// and CRUD operation, building one on first access. Parameter binding
	/// is derived from the rendered SQL (matched by <c>@param</c> regex)
	/// against the column metadata.
	/// </summary>
	public ValueTask<DatabaseCommand> GetCommandAsync(
		Schema meta,
		SqlCommandTypes type,
		IReadOnlyList<SchemaColumn> keyColumns,
		IReadOnlyList<SchemaColumn> valueColumns,
		CancellationToken cancellationToken)
	{
		var commandQuery = _queryProvider.Create(meta, type, keyColumns, valueColumns, _dialect);

		return _commandsByQuery.SafeAddAsync(commandQuery, (key, t) =>
		{
			var query = key.Render(_dialect);
			var dbCommand = CreateDbCommand(query);

			var command = new DatabaseCommand(_providerFactory, _dialect, _createConnection, dbCommand);

			foreach (Match match in _parameterRegex.Matches(query))
			{
				if (!match.Success)
					continue;

				var fieldName = match.Groups["parameterName"].Value;
				var col = FindColumn(fieldName, keyColumns, valueColumns, meta);

				if (col is null)
					throw new InvalidOperationException($"Column '{fieldName}' not found in {meta.EntityType}.");

				command.Parameters.Add(
					_providerFactory.CreateDbParameter(
						_dialect.ParameterPrefix + fieldName,
						ParameterDirection.Input,
						col.ClrType.ToDbType(),
						DBNull.Value));
			}

			return Task.FromResult(command);
		}, cancellationToken).AsValueTask();
	}

	private static SchemaColumn FindColumn(
		string name,
		IReadOnlyList<SchemaColumn> keyColumns,
		IReadOnlyList<SchemaColumn> valueColumns,
		Schema meta)
	{
		foreach (var c in keyColumns)
		{
			if (c.Name.EqualsIgnoreCase(name))
				return c;
		}

		foreach (var c in valueColumns)
		{
			if (c.Name.EqualsIgnoreCase(name))
				return c;
		}

		return meta.TryGetColumn(name);
	}

	private DbCommand CreateDbCommand(string text)
	{
		var command = _providerFactory.CreateCommand()
			?? throw new InvalidOperationException("DbProviderFactory.CreateCommand returned null.");
		command.CommandText = text;
		command.CommandType = CommandType.Text;
		return command;
	}
}
