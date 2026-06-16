namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Data.Sql;

/// <summary>
/// Pure ADO.NET implementation of <see cref="IDatabaseProvider"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AdoDatabaseProvider"/> class.
/// </remarks>
/// <param name="connectionFactory">Factory function that creates a DbConnection from connection pair.</param>
public class AdoDatabaseProvider(Func<DatabaseConnectionPair, DbConnection> connectionFactory) : IDatabaseProvider
{
	private readonly Func<DatabaseConnectionPair, DbConnection> _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

	/// <summary>
	/// Default singleton instance backed by <see cref="DatabaseProviderRegistry"/>.
	/// Tests can swap this for a test double via <see cref="OverrideInstance"/>;
	/// production code should leave it alone.
	/// </summary>
	public static IDatabaseProvider Instance { get; private set; } = new AdoDatabaseProvider();

	/// <summary>
	/// Override the global <see cref="Instance"/>. Intended for tests; pass
	/// <see langword="null"/> to restore the default <see cref="AdoDatabaseProvider"/>.
	/// </summary>
	public static void OverrideInstance(IDatabaseProvider provider)
		=> Instance = provider ?? new AdoDatabaseProvider();

	/// <summary>
	/// Initializes a new instance using <see cref="DatabaseProviderRegistry"/> to create connections.
	/// </summary>
	public AdoDatabaseProvider()
		: this(CreateConnectionFromRegistry)
	{
	}

	private static DbConnection CreateConnectionFromRegistry(DatabaseConnectionPair pair)
	{
		if (pair is null)
			throw new ArgumentNullException(nameof(pair));

		var factory = DatabaseProviderRegistry.GetFactory(pair.Provider);
		var connection = factory.CreateConnection();
		connection.ConnectionString = pair.ConnectionString;
		return connection;
	}

	/// <inheritdoc />
	public IDatabaseConnection CreateConnection(DatabaseConnectionPair pair)
	{
		if (pair is null)
			throw new ArgumentNullException(nameof(pair));

		var connStr = pair.ConnectionString;

		if (connStr.IsEmpty())
			throw new InvalidOperationException("Connection string is not set.");

		var dialect = DatabaseProviderRegistry.GetDialect(pair.Provider);
		return new AdoConnection(_connectionFactory(pair), dialect);
	}

	/// <inheritdoc />
	public IDatabaseTable GetTable(IDatabaseConnection connection, string tableName)
	{
		if (connection is null)
			throw new ArgumentNullException(nameof(connection));
		if (tableName.IsEmpty())
			throw new ArgumentNullException(nameof(tableName));

		return new AdoTable((AdoConnection)connection, tableName);
	}
}

internal class AdoTable : IDatabaseTable
{
	private readonly AdoConnection _connection;
	private ISqlDialect Dialect => _connection.Dialect;

	// Hard upper bound on the number of row value tuples a single
	// INSERT ... VALUES statement may carry. SQL Server's table value
	// constructor rejects more than 1000 rows (error 10738); this is the
	// lowest common ceiling across supported providers, so it is applied
	// universally in addition to the per-statement parameter limit.
	private const int _maxRowsPerValuesStatement = 1000;

	public AdoTable(AdoConnection connection, string name)
	{
		_connection = connection ?? throw new ArgumentNullException(nameof(connection));
		Name = name ?? throw new ArgumentNullException(nameof(name));
	}

	public string Name { get; }

	#region DDL

	public async Task CreateAsync(IDictionary<string, Type> columns, CancellationToken cancellationToken)
	{
		if (columns is null)
			throw new ArgumentNullException(nameof(columns));

		var sql = Query.CreateCreateTable(Name, columns).Render(Dialect);
		await ExecuteAsync(sql, null, cancellationToken).NoWait();
	}

	public async Task ModifyAsync(IDictionary<string, Type> columns, CancellationToken cancellationToken)
	{
		if (columns is null)
			throw new ArgumentNullException(nameof(columns));

		foreach (var t in columns)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var columnName = t.Key;
			var columnType = t.Value;
			var sqlType = Dialect.GetSqlTypeName(columnType);
			var sql = $"ALTER TABLE {Dialect.QuoteIdentifier(Name)} ADD {Dialect.QuoteIdentifier(columnName)} {sqlType}";
			await ExecuteAsync(sql, null, cancellationToken).NoWait();
		}
	}

	public async Task DropAsync(CancellationToken cancellationToken)
	{
		var sql = Query.CreateDropTable(Name).Render(Dialect);
		await ExecuteAsync(sql, null, cancellationToken).NoWait();
	}

	#endregion

	#region DML

	public async Task InsertAsync(IDictionary<string, object> values, CancellationToken cancellationToken)
	{
		if (values is null)
			throw new ArgumentNullException(nameof(values));

		var sql = Query.CreateInsert(Name, values.Keys).Render(Dialect);
		await ExecuteAsync(sql, values, cancellationToken).NoWait();
	}

	public async Task BulkInsertAsync(IEnumerable<IDictionary<string, object>> rows, CancellationToken cancellationToken)
	{
		if (rows is null)
			throw new ArgumentNullException(nameof(rows));

		// Filter out empty rows and convert to list
		var rowsList = rows.Where(r => r != null && r.Count > 0).ToList();
		if (rowsList.Count == 0)
			return;

		await _connection.EnsureOpenAsync(cancellationToken).NoWait();

		// Collect ALL unique column names from ALL rows to handle inconsistent data
		var columns = rowsList.SelectMany(r => r.Keys).Distinct().ToList();
		if (columns.Count == 0)
			return;

		if (columns.Count > Dialect.MaxParameters)
			throw new ArgumentException($"Row has {columns.Count} columns but dialect allows at most {Dialect.MaxParameters} parameters per statement.");

		var columnNames = columns.Select(c => Dialect.QuoteIdentifier(c)).JoinCommaSpace();

		// Calculate batch size based on number of columns to stay within parameter limit,
		// and additionally cap the number of row tuples per statement at the hard
		// table-value-constructor limit (SQL Server allows at most 1000 row expressions
		// in a single INSERT ... VALUES; error 10738). 1000 is the lowest common ceiling
		// across providers, so applying it universally keeps every statement valid.
		var batchSize = 1.Max(Dialect.MaxParameters / columns.Count).Min(_maxRowsPerValuesStatement);

		await using var transaction = await _connection.Connection.BeginTransactionAsync(cancellationToken).NoWait();

		try
		{
			for (var i = 0; i < rowsList.Count; i += batchSize)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var batch = rowsList.Skip(i).Take(batchSize).ToList();

				using var cmd = _connection.Connection.CreateCommand();
				cmd.Transaction = transaction;

				var valuesClauses = new List<string>(batch.Count);

				for (var rowIdx = 0; rowIdx < batch.Count; rowIdx++)
				{
					var row = batch[rowIdx];
					var paramNames = new List<string>(columns.Count);

					for (var colIdx = 0; colIdx < columns.Count; colIdx++)
					{
						var column = columns[colIdx];

						// Build parameter names from positional indices, not from the raw
						// column text. A column may legally contain characters (spaces,
						// quotes) that are valid only when quoted as an identifier but are
						// invalid inside an ADO.NET parameter name (e.g. "order date").
						var paramName = $"{Dialect.ParameterPrefix}c{colIdx}_{rowIdx}";
						paramNames.Add(paramName);

						var param = cmd.CreateParameter();
						param.ParameterName = paramName;
						var value = row.TryGetValue(column, out var val) ? val : null;
						param.Value = Dialect.ConvertToDbValue(value, value?.GetType() ?? typeof(object));
						cmd.Parameters.Add(param);

						// Apply dialect-specific parameter massaging (e.g. PostgreSQL
						// DateTime/timestamptz binding), mirroring the ORM command path.
						Dialect.PrepareParameter(param);
					}

					valuesClauses.Add($"({paramNames.JoinCommaSpace()})");
				}

				cmd.CommandText = $"INSERT INTO {Dialect.QuoteIdentifier(Name)} ({columnNames}) VALUES {valuesClauses.JoinCommaSpace()}";

				await cmd.ExecuteNonQueryAsync(cancellationToken).NoWait();
			}

			await transaction.CommitAsync(cancellationToken).NoWait();
		}
		catch
		{
			// Roll back best-effort: a Rollback failure (e.g. the connection is already
			// dead) must not replace and hide the original fault from the caller. Use no
			// token so a cancellation that triggered this catch doesn't abort the rollback.
			try
			{
				await transaction.RollbackAsync().NoWait();
			}
			catch
			{
			}

			throw;
		}
	}

	public async Task<IEnumerable<IDictionary<string, object>>> SelectAsync(IEnumerable<FilterCondition> filters, IEnumerable<OrderByCondition> orderBy, long? skip, long? take, CancellationToken cancellationToken)
	{
		var parameters = new Dictionary<string, object>();
		var whereClause = BuildWhereClause(parameters, filters);
		var orderByClause = BuildOrderByClause(orderBy);

		var sql = Query.CreateSelect(Name, whereClause, orderByClause, skip, take).Render(Dialect);

		if (skip.HasValue)
			parameters[Query.SkipParameterName] = skip.Value;

		if (take.HasValue)
			parameters[Query.TakeParameterName] = take.Value;

		return await QueryAsync(sql, parameters, cancellationToken).NoWait();
	}

	public async Task UpdateAsync(IDictionary<string, object> values, IEnumerable<FilterCondition> filters, CancellationToken cancellationToken)
	{
		if (values is null)
			throw new ArgumentNullException(nameof(values));

		var parameters = new Dictionary<string, object>(values);
		var whereClause = BuildWhereClause(parameters, filters);

		var sql = Query.CreateUpdate(Name, values.Keys, whereClause).Render(Dialect);

		await ExecuteAsync(sql, parameters, cancellationToken).NoWait();
	}

	public async Task<int> DeleteAsync(IEnumerable<FilterCondition> filters, CancellationToken cancellationToken)
	{
		var parameters = new Dictionary<string, object>();
		var whereClause = BuildWhereClause(parameters, filters);

		var sql = Query.CreateDelete(Name, whereClause).Render(Dialect);

		return await ExecuteAsync(sql, parameters, cancellationToken).NoWait();
	}

	public async Task UpsertAsync(IDictionary<string, object> values, IEnumerable<string> keyColumns, CancellationToken cancellationToken)
	{
		if (values is null)
			throw new ArgumentNullException(nameof(values));
		if (keyColumns is null)
			throw new ArgumentNullException(nameof(keyColumns));

		var keys = keyColumns.ToList();
		if (keys.Count == 0)
			throw new ArgumentException("At least one key column is required.", nameof(keyColumns));

		var sql = Query.CreateUpsert(Name, values.Keys, keys).Render(Dialect);

		await ExecuteAsync(sql, values, cancellationToken).NoWait();
	}

	#endregion

	#region Helpers

	private DbCommand PrepareCommand(string sql, IDictionary<string, object> parameters)
	{
		var cmd = _connection.Connection.CreateCommand();
		cmd.CommandText = sql;
		AddParameters(cmd, parameters);
		return cmd;
	}

	// Light retry policy for transient driver/network errors raised while
	// establishing the connection (before any statement is sent). Provider-
	// specific transient codes (network reset, broker timeout, login
	// throttling) bubble up as DbException with various error codes; we
	// avoid pinning to one DBMS by retrying on any DbException up to a
	// small fixed number of times with exponential backoff.
	//
	// IMPORTANT: the retry covers only the connection-open phase. The actual
	// command execution is NOT retried, because a write statement
	// (INSERT/UPDATE/DELETE) is not idempotent: a transient fault may be
	// raised after the statement has already reached the server, and blindly
	// replaying it would duplicate or corrupt data. So once the command is
	// transmitted it runs exactly once, even if it throws.
	private const int _retryCount = 3;
	private static readonly TimeSpan _retryBaseDelay = TimeSpan.FromMilliseconds(100);

	private async Task EnsureOpenWithRetryAsync(CancellationToken cancellationToken)
	{
		for (var attempt = 0; ; attempt++)
		{
			try
			{
				await _connection.EnsureOpenAsync(cancellationToken).NoWait();
				return;
			}
			catch (DbException) when (attempt < _retryCount)
			{
				var delay = TimeSpan.FromTicks(_retryBaseDelay.Ticks << attempt);
				await Task.Delay(delay, cancellationToken).NoWait();
			}
		}
	}

	private async Task<int> ExecuteAsync(string sql, IDictionary<string, object> parameters, CancellationToken cancellationToken)
	{
		// Retry only the pre-send connection-open phase; the write itself
		// must execute at most once (see _retryCount remarks above).
		await EnsureOpenWithRetryAsync(cancellationToken).NoWait();

		using var cmd = PrepareCommand(sql, parameters);
		return await cmd.ExecuteNonQueryAsync(cancellationToken).NoWait();
	}

	private async Task<IEnumerable<IDictionary<string, object>>> QueryAsync(string sql, IDictionary<string, object> parameters, CancellationToken cancellationToken)
	{
		// SELECT is idempotent; the connection-open phase may still be retried.
		await EnsureOpenWithRetryAsync(cancellationToken).NoWait();

		using var cmd = PrepareCommand(sql, parameters);

		var results = new List<IDictionary<string, object>>();
		using var reader = await cmd.ExecuteReaderAsync(cancellationToken).NoWait();

		while (await reader.ReadAsync(cancellationToken).NoWait())
		{
			var row = new Dictionary<string, object>();
			for (var i = 0; i < reader.FieldCount; i++)
			{
				var columnName = reader.GetName(i);
				row[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
			}
			results.Add(row);
		}

		return results;
	}

	private string BuildWhereClause(Dictionary<string, object> parameters, IEnumerable<FilterCondition> filters)
	{
		var filterList = filters?.ToList();
		if (filterList == null || filterList.Count == 0)
			return null;

		var conditions = new List<string>();
		var paramIndex = 0;

		// Produces the next free p{n} base name. The parameter dictionary may already
		// contain SET assignments keyed by raw column name (see UpdateAsync), so a column
		// literally named like a filter parameter (e.g. "p0") would otherwise be clobbered
		// by the indexer write below. Skipping names already present keeps SET and WHERE
		// parameters in disjoint slots without changing the SET parameter naming contract.
		string NextParamName()
		{
			string name;
			do
			{
				name = $"p{paramIndex++}";
			}
			while (parameters.ContainsKey(name));

			return name;
		}

		foreach (var filter in filterList)
		{
			// Handle NULL values specially - SQL requires IS NULL / IS NOT NULL syntax
			if (filter.Value is null)
			{
				var quotedColumn = Dialect.QuoteIdentifier(filter.Column);
				if (filter.Operator == ComparisonOperator.Equal)
					conditions.Add($"{quotedColumn} IS NULL");
				else if (filter.Operator == ComparisonOperator.NotEqual)
					conditions.Add($"{quotedColumn} IS NOT NULL");
				else
					throw new NotSupportedException($"Operator {filter.Operator} is not supported with NULL value");
				continue;
			}

			if (filter.Operator == ComparisonOperator.In && filter.Value is System.Collections.IEnumerable enumerable && filter.Value is not string)
			{
				var paramNames = new List<string>();
				foreach (var val in enumerable)
				{
					var inParamName = NextParamName();
					paramNames.Add(inParamName);
					parameters[inParamName] = val;
				}

				conditions.Add(Query.CreateBuildInCondition(filter.Column, paramNames).Render(Dialect));
			}
			else
			{
				var paramName = NextParamName();
				conditions.Add(Query.CreateBuildCondition(filter.Column, filter.Operator, paramName, IsNumericComparison(filter)).Render(Dialect));
				parameters[paramName] = filter.Value;
			}
		}

		return conditions.Join(" AND ");
	}

	private static bool IsNumericComparison(FilterCondition filter)
		=> (filter.Operator is
			ComparisonOperator.Equal or
			ComparisonOperator.NotEqual or
			ComparisonOperator.Greater or
			ComparisonOperator.GreaterOrEqual or
			ComparisonOperator.Less or
			ComparisonOperator.LessOrEqual)
			&& (filter.Value?.GetType().GetUnderlyingType() ?? filter.Value?.GetType())?.IsNumeric() == true;

	private string BuildOrderByClause(IEnumerable<OrderByCondition> orderBy)
	{
		var orderList = orderBy?.ToList();
		if (orderList == null || orderList.Count == 0)
			return null;

		var orderClauses = orderList.Select(o =>
			o.Descending
				? $"{Dialect.QuoteIdentifier(o.Column)} DESC"
				: $"{Dialect.QuoteIdentifier(o.Column)} ASC");

		return orderClauses.JoinCommaSpace();
	}

	private void AddParameters(DbCommand cmd, IDictionary<string, object> parameters)
	{
		if (parameters == null)
			return;

		foreach (var kv in parameters)
		{
			var param = cmd.CreateParameter();
			param.ParameterName = Dialect.ParameterPrefix + kv.Key;
			param.Value = Dialect.ConvertToDbValue(kv.Value, kv.Value?.GetType() ?? typeof(object));
			cmd.Parameters.Add(param);

			// Apply dialect-specific parameter massaging (e.g. PostgreSQL
			// DateTime/timestamptz binding), mirroring the ORM command path.
			Dialect.PrepareParameter(param);
		}
	}

	#endregion
}

/// <summary>
/// ADO.NET implementation of <see cref="IDatabaseConnection"/>.
/// </summary>
internal class AdoConnection : Disposable, IDatabaseConnection
{
	/// <summary>
	/// Gets the underlying ADO.NET connection.
	/// </summary>
	public DbConnection Connection { get; }

	/// <summary>
	/// Gets the SQL dialect for this connection.
	/// </summary>
	public ISqlDialect Dialect { get; }

	/// <summary>
	/// Creates a new ADO.NET connection wrapper.
	/// </summary>
	/// <param name="connection">Database connection.</param>
	/// <param name="dialect">SQL dialect.</param>
	public AdoConnection(DbConnection connection, ISqlDialect dialect)
	{
		Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		Dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
	}

	/// <summary>
	/// Ensures the connection is open, awaiting the driver's async open if
	/// it is not. Replaces the previous synchronous <c>EnsureOpen</c>
	/// which blocked async pipelines while waiting on the underlying
	/// network/driver.
	/// </summary>
	public async ValueTask EnsureOpenAsync(CancellationToken cancellationToken)
	{
		if (Connection.State != ConnectionState.Open)
			await Connection.OpenAsync(cancellationToken).NoWait();
	}

	/// <inheritdoc />
	public Task VerifyAsync(CancellationToken cancellationToken)
		=> EnsureOpenAsync(cancellationToken).AsTask();

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		Connection.Dispose();
		base.DisposeManaged();
	}
}
