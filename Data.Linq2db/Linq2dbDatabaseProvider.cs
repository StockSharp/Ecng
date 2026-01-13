namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using LinqToDB;
using LinqToDB.Data;

/// <summary>
/// Linq2db implementation of <see cref="IDatabaseProvider"/>.
/// </summary>
public class Linq2dbDatabaseProvider : IDatabaseProvider
{
	/// <summary>
	/// Singleton instance.
	/// </summary>
	public static readonly Linq2dbDatabaseProvider Instance = new();

	/// <inheritdoc />
	public IDatabaseConnection CreateConnection(DatabaseConnectionPair pair)
	{
		if (pair is null)
			throw new ArgumentNullException(nameof(pair));

		var provider = pair.Provider;

		if (provider.IsEmpty())
			throw new InvalidOperationException("Provider is not set.");

		var connStr = pair.ConnectionString;

		if (connStr.IsEmpty())
			throw new InvalidOperationException("Connection string is not set.");

		return new Linq2dbConnection(new DataConnection(ToLinq2dbProvider(provider), connStr));
	}

	private static string ToLinq2dbProvider(string provider) => provider switch
	{
		DatabaseProviderRegistry.SqlServer => ProviderName.SqlServer,
		DatabaseProviderRegistry.SQLite => ProviderName.SQLite,
		DatabaseProviderRegistry.MySql => ProviderName.MySql,
		DatabaseProviderRegistry.PostgreSql => ProviderName.PostgreSQL,
		_ => provider,
	};

	/// <inheritdoc />
	public IDatabaseTable GetTable(IDatabaseConnection connection, string tableName)
	{
		if (connection is null)
			throw new ArgumentNullException(nameof(connection));
		if (tableName.IsEmpty())
			throw new ArgumentNullException(nameof(tableName));

		return new Linq2dbTable(((Linq2dbConnection)connection).DataConnection, tableName);
	}
}

internal class Linq2dbTable : IDatabaseTable
{
	private readonly DataConnection _dc;

	public Linq2dbTable(DataConnection dc, string name)
	{
		_dc = dc ?? throw new ArgumentNullException(nameof(dc));
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
		await _dc.ExecuteAsync(sql, cancellationToken).NoWait();
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
			await _dc.ExecuteAsync(sql, cancellationToken).NoWait();
		}
	}

	public async Task DropAsync(CancellationToken cancellationToken)
	{
		await _dc.ExecuteAsync($"IF OBJECT_ID('{Name}', 'U') IS NOT NULL DROP TABLE [{Name}]", cancellationToken).NoWait();
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

		var parameters = values.Select(kv => new DataParameter(kv.Key, kv.Value)).ToArray();
		await _dc.ExecuteAsync(sql, cancellationToken, parameters).NoWait();
	}

	private const int _batchSize = 100;

	public async Task BulkInsertAsync(IEnumerable<IDictionary<string, object>> rows, CancellationToken cancellationToken)
	{
		if (rows is null)
			throw new ArgumentNullException(nameof(rows));

		var rowsList = rows.ToList();
		if (rowsList.Count == 0)
			return;

		// Get column names from first row
		var columns = rowsList[0].Keys.ToList();
		var columnNames = columns.Select(c => $"[{c}]").JoinCommaSpace();

		using var transaction = await _dc.BeginTransactionAsync(cancellationToken).NoWait();

		try
		{
			for (var i = 0; i < rowsList.Count; i += _batchSize)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var batch = rowsList.Skip(i).Take(_batchSize).ToList();
				var parameters = new List<DataParameter>();
				var valuesClauses = new List<string>(batch.Count);

				for (var rowIdx = 0; rowIdx < batch.Count; rowIdx++)
				{
					var row = batch[rowIdx];
					var paramNames = new List<string>(columns.Count);

					foreach (var column in columns)
					{
						var paramName = $"{column}_{rowIdx}";
						paramNames.Add($"@{paramName}");
						var value = row.TryGetValue(column, out var val) ? val : null;
						parameters.Add(new DataParameter(paramName, value));
					}

					valuesClauses.Add($"({paramNames.JoinCommaSpace()})");
				}

				var sql = $"INSERT INTO [{Name}] ({columnNames}) VALUES {valuesClauses.JoinCommaSpace()}";
				await _dc.ExecuteAsync(sql, cancellationToken, parameters.ToArray()).NoWait();
			}

			await transaction.CommitAsync(cancellationToken).NoWait();
		}
		catch
		{
			await transaction.RollbackAsync(cancellationToken).NoWait();
			throw;
		}
	}

	public async Task<IEnumerable<IDictionary<string, object>>> SelectAsync(IEnumerable<FilterCondition> filters, IEnumerable<OrderByCondition> orderBy, long? skip, long? take, CancellationToken cancellationToken)
	{
		var sqlBuilder = new StringBuilder($"SELECT * FROM [{Name}]");
		var parameters = new List<DataParameter>();

		BuildWhereClause(sqlBuilder, parameters, filters);
		BuildOrderByClause(sqlBuilder, orderBy);
		BuildPaginationClause(sqlBuilder, skip, take);

		var results = new List<IDictionary<string, object>>();

		// Use the underlying connection to execute reader
		var connection = _dc.DataProvider.CreateConnection(_dc.ConnectionString);
		await connection.OpenAsync(cancellationToken).NoWait();

		await using (connection)
		{
			using var cmd = connection.CreateCommand();
			cmd.CommandText = sqlBuilder.ToString();

			foreach (var param in parameters)
			{
				var p = cmd.CreateParameter();
				p.ParameterName = param.Name;
				p.Value = param.Value ?? DBNull.Value;
				cmd.Parameters.Add(p);
			}

			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).NoWait();

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
		}

		return results;
	}

	public async Task UpdateAsync(IDictionary<string, object> values, IEnumerable<FilterCondition> filters, CancellationToken cancellationToken)
	{
		if (values is null)
			throw new ArgumentNullException(nameof(values));

		var setClauses = values.Keys.Select(k => $"[{k}] = @{k}");
		var sqlBuilder = new StringBuilder($"UPDATE [{Name}] SET {setClauses.JoinCommaSpace()}");

		var parameters = values.Select(kv => new DataParameter(kv.Key, kv.Value)).ToList();
		BuildWhereClause(sqlBuilder, parameters, filters);

		await _dc.ExecuteAsync(sqlBuilder.ToString(), cancellationToken, parameters.ToArray()).NoWait();
	}

	public async Task DeleteAsync(IEnumerable<FilterCondition> filters, CancellationToken cancellationToken)
	{
		var sqlBuilder = new StringBuilder($"DELETE FROM [{Name}]");
		var parameters = new List<DataParameter>();

		BuildWhereClause(sqlBuilder, parameters, filters);

		await _dc.ExecuteAsync(sqlBuilder.ToString(), cancellationToken, parameters.ToArray()).NoWait();
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

		var parameters = values.Select(kv => new DataParameter(kv.Key, kv.Value)).ToArray();
		await _dc.ExecuteAsync(sqlBuilder.ToString(), cancellationToken, parameters).NoWait();
	}

	#endregion

	#region Helpers

	private static void BuildWhereClause(StringBuilder sqlBuilder, List<DataParameter> parameters, IEnumerable<FilterCondition> filters)
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
					parameters.Add(new DataParameter(inParamName, val));
				}
				conditions.Add($"[{filter.Column}] IN ({values.JoinCommaSpace()})");
			}
			else
			{
				conditions.Add($"[{filter.Column}] {op} @{paramName}");
				parameters.Add(new DataParameter(paramName, filter.Value));
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
/// Linq2db implementation of <see cref="IDatabaseConnection"/>.
/// </summary>
internal class Linq2dbConnection : Disposable, IDatabaseConnection
{
	/// <summary>
	/// Gets the underlying linq2db data connection.
	/// </summary>
	public DataConnection DataConnection { get; }

	/// <summary>
	/// Creates a new linq2db connection wrapper.
	/// </summary>
	/// <param name="connection">Linq2db data connection.</param>
	public Linq2dbConnection(DataConnection connection)
	{
		DataConnection = connection ?? throw new ArgumentNullException(nameof(connection));
	}

	/// <inheritdoc />
	public async Task VerifyAsync(CancellationToken cancellationToken)
	{
		await using var conn = DataConnection.DataProvider.CreateConnection(DataConnection.ConnectionString);
		await conn.OpenAsync(cancellationToken).NoWait();
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		DataConnection.Dispose();
		base.DisposeManaged();
	}
}
