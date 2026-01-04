namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Pure ADO.NET implementation of <see cref="IDatabaseBatchInserterProvider"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AdoBatchInserterProvider"/> class.
/// </remarks>
/// <param name="connectionFactory">Factory function that creates a DbConnection from connection string.</param>
public class AdoBatchInserterProvider(Func<string, DbConnection> connectionFactory) : IDatabaseBatchInserterProvider
{
	private readonly Func<string, DbConnection> _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

	/// <inheritdoc />
	public IDatabaseBatchInserter<T> Create<T>(
		DatabaseConnectionPair connection,
		string tableName,
		Action<IDatabaseMappingBuilder<T>> configureMapping)
		where T : class
	{
		if (connection is null)
			throw new ArgumentNullException(nameof(connection));

		if (tableName.IsEmpty())
			throw new ArgumentNullException(nameof(tableName));

		if (configureMapping is null)
			throw new ArgumentNullException(nameof(configureMapping));

		return new AdoBatchInserter<T>(_connectionFactory, connection.ConnectionString, tableName, configureMapping);
	}

	/// <inheritdoc />
	public void DropTable(DatabaseConnectionPair connection, string tableName)
	{
		if (connection is null)
			throw new ArgumentNullException(nameof(connection));

		if (tableName.IsEmpty())
			throw new ArgumentNullException(nameof(tableName));

		using var db = _connectionFactory(connection.ConnectionString);
		db.Open();

		using var cmd = db.CreateCommand();
		cmd.CommandText = $"IF OBJECT_ID('{tableName}', 'U') IS NOT NULL DROP TABLE [{tableName}]";
		cmd.ExecuteNonQuery();
	}
}

internal class AdoBatchInserter<T> : IDatabaseBatchInserter<T>
	where T : class
{
	private readonly DbConnection _connection;
	private readonly string _tableName;
	private readonly List<ColumnMapping> _columns = [];
	private readonly Func<T, string, object, object> _dynamicGetter;
	private readonly Action<T, string, object> _dynamicSetter;
	private readonly Func<object, object> _parameterConverter;
	private bool _disposed;
	private bool _tableCreated;

	public AdoBatchInserter(
		Func<string, DbConnection> connectionFactory,
		string connectionString,
		string tableName,
		Action<IDatabaseMappingBuilder<T>> configureMapping)
	{
		_connection = connectionFactory(connectionString);
		_tableName = tableName;

		var mappingBuilder = new AdoMappingBuilder<T>(this);
		configureMapping(mappingBuilder);

		_dynamicGetter = mappingBuilder.DynamicGetter;
		_dynamicSetter = mappingBuilder.DynamicSetter;
		_parameterConverter = mappingBuilder.ParameterConverter;

		_connection.Open();
		EnsureTableExists();
	}

	internal void AddColumn(ColumnMapping column) => _columns.Add(column);

	private void EnsureTableExists()
	{
		if (_tableCreated)
			return;

		var createSql = GenerateCreateTableSql();
		using var cmd = _connection.CreateCommand();
		cmd.CommandText = createSql;
		cmd.ExecuteNonQuery();
		_tableCreated = true;
	}

	private string GenerateCreateTableSql()
	{
		var sb = new StringBuilder();
		sb.AppendLine($"IF OBJECT_ID('{_tableName}', 'U') IS NULL");
		sb.AppendLine($"CREATE TABLE [{_tableName}] (");

		var columnDefs = _columns.Select(c =>
		{
			var typeDef = GetSqlType(c.DataType, c.Length, c.Scale);
			var nullDef = c.IsNotNull ? "NOT NULL" : "NULL";
			return $"  [{c.ColumnName}] {typeDef} {nullDef}";
		});

		sb.AppendLine(string.Join(",\n", columnDefs));
		sb.AppendLine(")");

		return sb.ToString();
	}

	private static string GetSqlType(DatabaseDataType dataType, int? length, int? scale) => dataType switch
	{
		DatabaseDataType.NVarChar => length.HasValue ? $"NVARCHAR({length})" : "NVARCHAR(MAX)",
		DatabaseDataType.VarChar => length.HasValue ? $"VARCHAR({length})" : "VARCHAR(MAX)",
		DatabaseDataType.Char => $"CHAR({length ?? 1})",
		DatabaseDataType.Int => "INT",
		DatabaseDataType.BigInt => "BIGINT",
		DatabaseDataType.Decimal => $"DECIMAL(18, {scale ?? 2})",
		DatabaseDataType.DateTime => "DATETIME2",
		DatabaseDataType.Boolean => "BIT",
		DatabaseDataType.Binary => "VARBINARY(MAX)",
		DatabaseDataType.Text => "NVARCHAR(MAX)",
		_ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
	};

	public async Task InsertAsync(T item, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();

		var sql = GenerateInsertSql();
		using var cmd = _connection.CreateCommand();
		cmd.CommandText = sql;

		AddParameters(cmd, item);

		await cmd.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task BulkCopyAsync(IEnumerable<T> items, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();

		var sql = GenerateInsertSql();
		var itemsList = items.ToList();

		using var transaction = _connection.BeginTransaction();

		try
		{
			foreach (var item in itemsList)
			{
				cancellationToken.ThrowIfCancellationRequested();

				using var cmd = _connection.CreateCommand();
				cmd.CommandText = sql;
				cmd.Transaction = transaction;

				AddParameters(cmd, item);

				await cmd.ExecuteNonQueryAsync(cancellationToken);
			}

			transaction.Commit();
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}

	private string GenerateInsertSql()
	{
		var columnNames = string.Join(", ", _columns.Select(c => $"[{c.ColumnName}]"));
		var paramNames = string.Join(", ", _columns.Select(c => $"@{c.ColumnName}"));
		return $"INSERT INTO [{_tableName}] ({columnNames}) VALUES ({paramNames})";
	}

	private void AddParameters(DbCommand cmd, T item)
	{
		foreach (var column in _columns)
		{
			var param = cmd.CreateParameter();
			param.ParameterName = $"@{column.ColumnName}";
			param.DbType = GetDbType(column.DataType);

			object value;
			if (column.IsDynamic)
			{
				value = _dynamicGetter?.Invoke(item, column.PropertyName, null);
			}
			else
			{
				value = column.PropertyGetter(item);
			}

			if (_parameterConverter != null)
			{
				value = _parameterConverter(value);
			}

			param.Value = value ?? DBNull.Value;
			cmd.Parameters.Add(param);
		}
	}

	private static DbType GetDbType(DatabaseDataType dataType) => dataType switch
	{
		DatabaseDataType.NVarChar => DbType.String,
		DatabaseDataType.VarChar => DbType.AnsiString,
		DatabaseDataType.Char => DbType.AnsiStringFixedLength,
		DatabaseDataType.Int => DbType.Int32,
		DatabaseDataType.BigInt => DbType.Int64,
		DatabaseDataType.Decimal => DbType.Decimal,
		DatabaseDataType.DateTime => DbType.DateTime2,
		DatabaseDataType.Boolean => DbType.Boolean,
		DatabaseDataType.Binary => DbType.Binary,
		DatabaseDataType.Text => DbType.String,
		_ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
	};

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_connection?.Dispose();
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(AdoBatchInserter<T>));
	}
}

internal class ColumnMapping
{
	public string PropertyName { get; set; }
	public string ColumnName { get; set; }
	public DatabaseDataType DataType { get; set; }
	public int? Length { get; set; }
	public int? Scale { get; set; }
	public bool IsNotNull { get; set; }
	public bool IsDynamic { get; set; }
	public Func<object, object> PropertyGetter { get; set; }
}

internal class AdoMappingBuilder<T>(AdoBatchInserter<T> inserter) : IDatabaseMappingBuilder<T>
	where T : class
{
	private readonly AdoBatchInserter<T> _inserter = inserter ?? throw new ArgumentNullException(nameof(inserter));
	private bool _columnsRequired;

	public Func<T, string, object, object> DynamicGetter { get; private set; }
	public Action<T, string, object> DynamicSetter { get; private set; }
	public Func<object, object> ParameterConverter { get; private set; }

	public IDatabaseMappingBuilder<T> HasTableName(string name)
	{
		// Table name is set in constructor
		return this;
	}

	public IDatabaseMappingBuilder<T> IsColumnRequired()
	{
		_columnsRequired = true;
		return this;
	}

	public IDatabaseColumnBuilder<T> Property<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
	{
		var memberExpr = propertyExpression.Body as MemberExpression
			?? throw new ArgumentException("Expression must be a member access", nameof(propertyExpression));

		var propInfo = memberExpr.Member as PropertyInfo
			?? throw new ArgumentException("Expression must be a property access", nameof(propertyExpression));

		var compiled = propertyExpression.Compile();
		var column = new ColumnMapping
		{
			PropertyName = propInfo.Name,
			ColumnName = propInfo.Name,
			IsNotNull = _columnsRequired,
			IsDynamic = false,
			PropertyGetter = obj => compiled((T)obj),
		};

		_inserter.AddColumn(column);
		return new AdoColumnBuilder<T>(this, column);
	}

	public IDatabaseColumnBuilder<T> DynamicProperty(string propertyName)
	{
		var column = new ColumnMapping
		{
			PropertyName = propertyName,
			ColumnName = propertyName,
			IsNotNull = _columnsRequired,
			IsDynamic = true,
		};

		_inserter.AddColumn(column);
		return new AdoColumnBuilder<T>(this, column);
	}

	public IDatabaseMappingBuilder<T> DynamicPropertyAccessors(
		Expression<Func<T, string, object, object>> getter,
		Expression<Action<T, string, object>> setter)
	{
		DynamicGetter = getter.Compile();
		DynamicSetter = setter.Compile();
		return this;
	}

	public IDatabaseMappingBuilder<T> SetConverter<TFrom, TTo>(Func<TFrom, TTo> converter)
	{
		// Not used in ADO.NET implementation
		return this;
	}

	public IDatabaseMappingBuilder<T> SetParameterValueConverter(Func<object, object> converter)
	{
		ParameterConverter = converter;
		return this;
	}
}

class AdoColumnBuilder<T>(AdoMappingBuilder<T> mappingBuilder, ColumnMapping column) : IDatabaseColumnBuilder<T>
	where T : class
{
	private readonly AdoMappingBuilder<T> _mappingBuilder = mappingBuilder ?? throw new ArgumentNullException(nameof(mappingBuilder));
	private ColumnMapping _column = column ?? throw new ArgumentNullException(nameof(column));

	public IDatabaseColumnBuilder<T> HasLength(int length)
	{
		_column.Length = length;
		return this;
	}

	public IDatabaseColumnBuilder<T> HasScale(int scale)
	{
		_column.Scale = scale;
		return this;
	}

	public IDatabaseColumnBuilder<T> HasColumnName(string name)
	{
		_column.ColumnName = name;
		return this;
	}

	public IDatabaseColumnBuilder<T> HasDataType(DatabaseDataType dataType)
	{
		_column.DataType = dataType;
		return this;
	}

	public IDatabaseColumnBuilder<T> IsNotNull()
	{
		_column.IsNotNull = true;
		return this;
	}

	public IDatabaseColumnBuilder<T> Property<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
	{
		return _mappingBuilder.Property(propertyExpression);
	}

	public IDatabaseColumnBuilder<T> DynamicProperty(string propertyName)
	{
		return _mappingBuilder.DynamicProperty(propertyName);
	}

	public IDatabaseMappingBuilder<T> IsColumnRequired()
	{
		return _mappingBuilder.IsColumnRequired();
	}

	public IDatabaseMappingBuilder<T> DynamicPropertyAccessors(
		Expression<Func<T, string, object, object>> getter,
		Expression<Action<T, string, object>> setter)
	{
		return _mappingBuilder.DynamicPropertyAccessors(getter, setter);
	}
}
