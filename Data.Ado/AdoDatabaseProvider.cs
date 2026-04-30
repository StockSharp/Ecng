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

		// Calculate batch size based on number of columns to stay within parameter limit
		var batchSize = 1.Max(Dialect.MaxParameters / columns.Count);

		using var transaction = _connection.Connection.BeginTransaction();

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

					foreach (var column in columns)
					{
						var paramName = $"{column}_{rowIdx}";
						paramNames.Add(Dialect.ParameterPrefix + paramName);

						var param = cmd.CreateParameter();
						param.ParameterName = Dialect.ParameterPrefix + paramName;
						var value = row.TryGetValue(column, out var val) ? val : null;
						param.Value = Dialect.ConvertToDbValue(value, value?.GetType() ?? typeof(object));
						cmd.Parameters.Add(param);
					}

					valuesClauses.Add($"({paramNames.JoinCommaSpace()})");
				}

				cmd.CommandText = $"INSERT INTO {Dialect.QuoteIdentifier(Name)} ({columnNames}) VALUES {valuesClauses.JoinCommaSpace()}";

				await cmd.ExecuteNonQueryAsync(cancellationToken).NoWait();
			}

			transaction.Commit();
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}

	public async Task<IEnumerable<IDictionary<string, object>>> SelectAsync(IEnumerable<FilterCondition> filters, IEnumerable<OrderByCondition> orderBy, long? skip, long? take, CancellationToken cancellationToken)
	{
		var parameters = new Dictionary<string, object>();
		var whereClause = BuildWhereClause(parameters, filters);
		var orderByClause = BuildOrderByClause(orderBy);

		var sql = Query.CreateSelect(Name, whereClause, orderByClause, skip, take).Render(Dialect);

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

	private async Task<DbCommand> PrepareCommandAsync(string sql, IDictionary<string, object> parameters, CancellationToken cancellationToken)
	{
		await _connection.EnsureOpenAsync(cancellationToken).NoWait();

		var cmd = _connection.Connection.CreateCommand();
		cmd.CommandText = sql;
		AddParameters(cmd, parameters);
		return cmd;
	}

	// Light retry policy for transient driver/network errors. Provider-
	// specific transient codes (deadlock victim, network reset, broker
	// timeout) bubble up as DbException with various error codes; we
	// avoid pinning to one DBMS by retrying on any DbException up to a
	// small fixed number of times with exponential backoff.
	private const int _retryCount = 3;
	private static readonly TimeSpan _retryBaseDelay = TimeSpan.FromMilliseconds(100);

	private static async Task<T> WithRetry<T>(Func<Task<T>> op, CancellationToken cancellationToken)
	{
		for (var attempt = 0; ; attempt++)
		{
			try
			{
				return await op().NoWait();
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
		return await WithRetry(async () =>
		{
			using var cmd = await PrepareCommandAsync(sql, parameters, cancellationToken).NoWait();
			return await cmd.ExecuteNonQueryAsync(cancellationToken).NoWait();
		}, cancellationToken).NoWait();
	}

	private async Task<IEnumerable<IDictionary<string, object>>> QueryAsync(string sql, IDictionary<string, object> parameters, CancellationToken cancellationToken)
	{
		using var cmd = await PrepareCommandAsync(sql, parameters, cancellationToken).NoWait();

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

			var paramName = $"p{paramIndex++}";

			if (filter.Operator == ComparisonOperator.In && filter.Value is System.Collections.IEnumerable enumerable && filter.Value is not string)
			{
				var paramNames = new List<string>();
				var idx = 0;
				foreach (var val in enumerable)
				{
					var inParamName = $"{paramName}_{idx++}";
					paramNames.Add(inParamName);
					parameters[inParamName] = val;
				}

				conditions.Add(Query.CreateBuildInCondition(filter.Column, paramNames).Render(Dialect));
			}
			else
			{
				conditions.Add(Query.CreateBuildCondition(filter.Column, filter.Operator, paramName).Render(Dialect));
				parameters[paramName] = filter.Value;
			}
		}

		return conditions.Join(" AND ");
	}

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
