namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.ComponentModel;

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
	/// Singleton instance using <see cref="DatabaseProviderRegistry"/> to create connections.
	/// </summary>
	public static readonly AdoDatabaseProvider Instance = new();

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

		return new AdoConnection(_connectionFactory(pair));
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

		var columnDefs = columns.Select(kv => $"[{kv.Key}] {MapTypeToSql(kv.Value)}");
		var sql = $"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{Name}') CREATE TABLE [{Name}] ({columnDefs.JoinCommaSpace()})";
		await ExecuteAsync(sql, null, cancellationToken).NoWait();
	}

	public async Task ModifyAsync(IDictionary<string, Type> columns, CancellationToken cancellationToken)
	{
		if (columns is null)
			throw new ArgumentNullException(nameof(columns));

		foreach (var (columnName, columnType) in columns)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var sqlType = MapTypeToSql(columnType);
			var sql = $"ALTER TABLE [{Name}] ADD [{columnName}] {sqlType}";
			await ExecuteAsync(sql, null, cancellationToken).NoWait();
		}
	}

	public async Task DropAsync(CancellationToken cancellationToken)
	{
		await ExecuteAsync($"IF OBJECT_ID('{Name}', 'U') IS NOT NULL DROP TABLE [{Name}]", null, cancellationToken).NoWait();
	}

	#endregion

	#region DML

	public async Task InsertAsync(IDictionary<string, object> values, CancellationToken cancellationToken)
	{
		if (values is null)
			throw new ArgumentNullException(nameof(values));

		var columnNames = values.Keys.Select(k => $"[{k}]").JoinCommaSpace();
		var paramNames = values.Keys.Select(k => $"@{k}").JoinCommaSpace();
		var sql = $"INSERT INTO [{Name}] ({columnNames}) VALUES ({paramNames})";

		await ExecuteAsync(sql, values, cancellationToken).NoWait();
	}

	private const int _batchSize = 100;

	public async Task BulkInsertAsync(IEnumerable<IDictionary<string, object>> rows, CancellationToken cancellationToken)
	{
		if (rows is null)
			throw new ArgumentNullException(nameof(rows));

		var rowsList = rows.ToList();
		if (rowsList.Count == 0)
			return;

		_connection.EnsureOpen();

		// Get column names from first row
		var columns = rowsList[0].Keys.ToList();
		var columnNames = columns.Select(c => $"[{c}]").JoinCommaSpace();

		using var transaction = _connection.Connection.BeginTransaction();

		try
		{
			for (var i = 0; i < rowsList.Count; i += _batchSize)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var batch = rowsList.Skip(i).Take(_batchSize).ToList();

				using var cmd = _connection.Connection.CreateCommand();
				cmd.Transaction = transaction;

				var valuesClauses = new List<string>(batch.Count);

				for (var rowIdx = 0; rowIdx < batch.Count; rowIdx++)
				{
					var row = batch[rowIdx];
					var paramNames = new List<string>(columns.Count);

					foreach (var column in columns)
					{
						var paramName = $"@{column}_{rowIdx}";
						paramNames.Add(paramName);

						var param = cmd.CreateParameter();
						param.ParameterName = paramName;
						param.Value = row.TryGetValue(column, out var val) ? val ?? DBNull.Value : DBNull.Value;
						cmd.Parameters.Add(param);
					}

					valuesClauses.Add($"({paramNames.JoinCommaSpace()})");
				}

				cmd.CommandText = $"INSERT INTO [{Name}] ({columnNames}) VALUES {valuesClauses.JoinCommaSpace()}";

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
		var sqlBuilder = new StringBuilder($"SELECT * FROM [{Name}]");
		var parameters = new Dictionary<string, object>();

		BuildWhereClause(sqlBuilder, parameters, filters);
		BuildOrderByClause(sqlBuilder, orderBy);
		BuildPaginationClause(sqlBuilder, skip, take);

		return await QueryAsync(sqlBuilder.ToString(), parameters, cancellationToken).NoWait();
	}

	public async Task UpdateAsync(IDictionary<string, object> values, IEnumerable<FilterCondition> filters, CancellationToken cancellationToken)
	{
		if (values is null)
			throw new ArgumentNullException(nameof(values));

		var setClauses = values.Keys.Select(k => $"[{k}] = @{k}");
		var sqlBuilder = new StringBuilder($"UPDATE [{Name}] SET {setClauses.JoinCommaSpace()}");

		var parameters = new Dictionary<string, object>(values);
		BuildWhereClause(sqlBuilder, parameters, filters);

		await ExecuteAsync(sqlBuilder.ToString(), parameters, cancellationToken).NoWait();
	}

	public async Task<int> DeleteAsync(IEnumerable<FilterCondition> filters, CancellationToken cancellationToken)
	{
		var sqlBuilder = new StringBuilder($"DELETE FROM [{Name}]");
		var parameters = new Dictionary<string, object>();

		BuildWhereClause(sqlBuilder, parameters, filters);

		return await ExecuteAsync(sqlBuilder.ToString(), parameters, cancellationToken).NoWait();
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

		var allColumns = values.Keys.ToList();
		var updateColumns = allColumns.Except(keys).ToList();

		// Build MERGE statement
		var sqlBuilder = new StringBuilder();
		sqlBuilder.Append($"MERGE [{Name}] AS target USING (SELECT ");
		sqlBuilder.Append(keys.Select(k => $"@{k} AS [{k}]").JoinCommaSpace());
		sqlBuilder.Append(") AS source ON ");
		sqlBuilder.Append(keys.Select(k => $"target.[{k}] = source.[{k}]").Join(" AND "));

		if (updateColumns.Count > 0)
		{
			sqlBuilder.Append(" WHEN MATCHED THEN UPDATE SET ");
			sqlBuilder.Append(updateColumns.Select(c => $"[{c}] = @{c}").JoinCommaSpace());
		}

		sqlBuilder.Append(" WHEN NOT MATCHED THEN INSERT (");
		sqlBuilder.Append(allColumns.Select(c => $"[{c}]").JoinCommaSpace());
		sqlBuilder.Append(") VALUES (");
		sqlBuilder.Append(allColumns.Select(c => $"@{c}").JoinCommaSpace());
		sqlBuilder.Append(");");

		await ExecuteAsync(sqlBuilder.ToString(), values, cancellationToken).NoWait();
	}

	#endregion

	#region Helpers

	private async Task<int> ExecuteAsync(string sql, IDictionary<string, object> parameters, CancellationToken cancellationToken)
	{
		_connection.EnsureOpen();

		using var cmd = _connection.Connection.CreateCommand();
		cmd.CommandText = sql;
		AddParameters(cmd, parameters);

		return await cmd.ExecuteNonQueryAsync(cancellationToken).NoWait();
	}

	private async Task<IEnumerable<IDictionary<string, object>>> QueryAsync(string sql, IDictionary<string, object> parameters, CancellationToken cancellationToken)
	{
		_connection.EnsureOpen();

		using var cmd = _connection.Connection.CreateCommand();
		cmd.CommandText = sql;
		AddParameters(cmd, parameters);

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

	private static void BuildWhereClause(StringBuilder sqlBuilder, Dictionary<string, object> parameters, IEnumerable<FilterCondition> filters)
	{
		var filterList = filters?.ToList();
		if (filterList == null || filterList.Count == 0)
			return;

		sqlBuilder.Append(" WHERE ");
		var conditions = new List<string>();
		var paramIndex = 0;

		foreach (var filter in filterList)
		{
			var paramName = $"p{paramIndex++}";

			var op = filter.Operator switch
			{
				ComparisonOperator.Equal => "=",
				ComparisonOperator.NotEqual => "<>",
				ComparisonOperator.Greater => ">",
				ComparisonOperator.GreaterOrEqual => ">=",
				ComparisonOperator.Less => "<",
				ComparisonOperator.LessOrEqual => "<=",
				ComparisonOperator.In => "IN",
				ComparisonOperator.Like => "LIKE",
				_ => "=",
			};

			if (filter.Operator == ComparisonOperator.In && filter.Value is System.Collections.IEnumerable enumerable && filter.Value is not string)
			{
				var values = new List<string>();
				var idx = 0;
				foreach (var val in enumerable)
				{
					var inParamName = $"{paramName}_{idx++}";
					values.Add($"@{inParamName}");
					parameters[inParamName] = val;
				}
				conditions.Add($"[{filter.Column}] IN ({values.JoinCommaSpace()})");
			}
			else
			{
				conditions.Add($"[{filter.Column}] {op} @{paramName}");
				parameters[paramName] = filter.Value;
			}
		}

		sqlBuilder.Append(conditions.Join(" AND "));
	}

	private static void BuildOrderByClause(StringBuilder sqlBuilder, IEnumerable<OrderByCondition> orderBy)
	{
		var orderList = orderBy?.ToList();
		if (orderList == null || orderList.Count == 0)
			return;

		var orderClauses = orderList.Select(o => o.Descending ? $"[{o.Column}] DESC" : $"[{o.Column}] ASC");
		sqlBuilder.Append($" ORDER BY {orderClauses.JoinCommaSpace()}");
	}

	private static void BuildPaginationClause(StringBuilder sqlBuilder, long? skip, long? take)
	{
		if (skip.HasValue || take.HasValue)
		{
			sqlBuilder.Append($" OFFSET {skip ?? 0} ROWS");
			if (take.HasValue)
				sqlBuilder.Append($" FETCH NEXT {take.Value} ROWS ONLY");
		}
	}

	private static void AddParameters(DbCommand cmd, IDictionary<string, object> parameters)
	{
		if (parameters == null)
			return;

		foreach (var kv in parameters)
		{
			var param = cmd.CreateParameter();
			param.ParameterName = $"@{kv.Key}";
			param.Value = kv.Value ?? DBNull.Value;
			cmd.Parameters.Add(param);
		}
	}

	private static string MapTypeToSql(Type type)
	{
		var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

		return Type.GetTypeCode(underlyingType) switch
		{
			TypeCode.Boolean => "BIT",
			TypeCode.Byte => "TINYINT",
			TypeCode.Int16 => "SMALLINT",
			TypeCode.Int32 => "INT",
			TypeCode.Int64 => "BIGINT",
			TypeCode.Single => "REAL",
			TypeCode.Double => "FLOAT",
			TypeCode.Decimal => "DECIMAL(18,4)",
			TypeCode.DateTime => "DATETIME2",
			TypeCode.String => "NVARCHAR(MAX)",
			_ when underlyingType == typeof(Guid) => "UNIQUEIDENTIFIER",
			_ when underlyingType == typeof(byte[]) => "VARBINARY(MAX)",
			_ when underlyingType == typeof(DateTimeOffset) => "DATETIMEOFFSET",
			_ when underlyingType == typeof(TimeSpan) => "BIGINT",
			_ => "NVARCHAR(MAX)",
		};
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
	/// Creates a new ADO.NET connection wrapper.
	/// </summary>
	/// <param name="connection">Database connection.</param>
	public AdoConnection(DbConnection connection)
	{
		Connection = connection ?? throw new ArgumentNullException(nameof(connection));
	}

	/// <summary>
	/// Ensures the connection is open.
	/// </summary>
	public void EnsureOpen()
	{
		if (Connection.State != ConnectionState.Open)
			Connection.Open();
	}

	/// <inheritdoc />
	public async Task VerifyAsync(CancellationToken cancellationToken)
	{
		if (Connection.State != ConnectionState.Open)
			await Connection.OpenAsync(cancellationToken).NoWait();
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		Connection.Dispose();
		base.DisposeManaged();
	}
}
