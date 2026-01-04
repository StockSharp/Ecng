namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

/// <summary>
/// Linq2db implementation of <see cref="IDatabaseBatchInserterProvider{TConnection}"/>.
/// </summary>
public class Linq2dbBatchInserterProvider : IDatabaseBatchInserterProvider<DataConnection>
{
	/// <inheritdoc />
	public DataConnection CreateConnection(DatabaseConnectionPair pair)
	{
		if (pair is null)
			throw new ArgumentNullException(nameof(pair));

		var provider = pair.Provider;

		if (provider.IsEmpty())
			throw new InvalidOperationException("Provider is not set.");

		var connStr = pair.ConnectionString;

		if (connStr.IsEmpty())
			throw new InvalidOperationException("Connection string is not set.");

		return new DataConnection(ToLinq2dbProvider(provider), connStr);
	}

	private static string ToLinq2dbProvider(string provider) => provider switch
	{
		DatabaseProviderRegistry.SqlServer => ProviderName.SqlServer,
		DatabaseProviderRegistry.SQLite => ProviderName.SQLite,
		DatabaseProviderRegistry.MySql => ProviderName.MySql,
		DatabaseProviderRegistry.PostgreSql => ProviderName.PostgreSQL,
		_ => provider, // pass through for direct linq2db provider names
	};

	/// <inheritdoc />
	public IDatabaseBatchInserter<T> Create<T>(
		DataConnection connection,
		string tableName,
		Action<IDatabaseMappingBuilder<T>> configureMapping)
		where T : class
	{
		if (tableName.IsEmpty())
			throw new ArgumentNullException(nameof(tableName));

		return new Linq2dbBatchInserter<T>(connection, configureMapping);
	}

	/// <inheritdoc />
	public void DropTable(DataConnection connection, string tableName)
	{
		if (connection is null)
			throw new ArgumentNullException(nameof(connection));

		if (tableName.IsEmpty())
			throw new ArgumentNullException(nameof(tableName));

		connection.DropTable<object>(tableName, throwExceptionIfNotExists: false);
	}

	/// <inheritdoc />
	public void Verify(DataConnection connection)
	{
		if (connection is null)
			throw new ArgumentNullException(nameof(connection));

		using var conn = connection.DataProvider.CreateConnection(connection.ConnectionString);
		conn.Open();
	}
}

class Linq2dbBatchInserter<T> : Disposable, IDatabaseBatchInserter<T>
	where T : class
{
	private readonly DataConnection _db;
	private readonly ITable<T> _table;

	public Linq2dbBatchInserter(
		DataConnection connection,
		Action<IDatabaseMappingBuilder<T>> configureMapping)
	{
		_db = connection ?? throw new ArgumentNullException(nameof(connection));

		if (configureMapping is null)
			throw new ArgumentNullException(nameof(configureMapping));

		var schema = _db.MappingSchema;
		var fluentBuilder = new FluentMappingBuilder(schema);
		var entityBuilder = fluentBuilder.Entity<T>();

		var mappingBuilder = new Linq2dbMappingBuilder<T>(entityBuilder, schema);
		configureMapping(mappingBuilder);
		fluentBuilder.Build();

		_table = _db.CreateTable<T>(tableOptions: TableOptions.CreateIfNotExists);
	}

	public async Task InsertAsync(T item, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		await _db.InsertAsync(item, token: cancellationToken);
	}

	public async Task BulkCopyAsync(IEnumerable<T> items, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		await _table.BulkCopyAsync(items, cancellationToken);
	}

	protected override void DisposeManaged()
	{
		if (IsDisposed)
			return;

		_db?.Dispose();

		base.DisposeManaged();
	}
}

class Linq2dbMappingBuilder<T>(EntityMappingBuilder<T> entityBuilder, MappingSchema schema) : IDatabaseMappingBuilder<T>
	where T : class
{
	private readonly EntityMappingBuilder<T> _entityBuilder = entityBuilder ?? throw new ArgumentNullException(nameof(entityBuilder));
	private readonly MappingSchema _schema = schema ?? throw new ArgumentNullException(nameof(schema));

	public IDatabaseMappingBuilder<T> HasTableName(string name)
	{
		_entityBuilder.HasTableName(name);
		return this;
	}

	public IDatabaseMappingBuilder<T> IsColumnRequired()
	{
		_entityBuilder.IsColumnRequired();
		return this;
	}

	public IDatabaseColumnBuilder<T> Property<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
	{
		var propBuilder = _entityBuilder.Property(propertyExpression);
		return new Linq2dbColumnBuilder<T>(propBuilder, _entityBuilder, _schema);
	}

	public IDatabaseColumnBuilder<T> DynamicProperty(string propertyName)
	{
		var propBuilder = _entityBuilder.Property(x => Sql.Property<object>(x, propertyName));
		return new Linq2dbColumnBuilder<T>(propBuilder, _entityBuilder, _schema);
	}

	public IDatabaseMappingBuilder<T> DynamicPropertyAccessors(
		Expression<Func<T, string, object, object>> getter,
		Expression<Action<T, string, object>> setter)
	{
		_entityBuilder.DynamicPropertyAccessors(getter, setter);
		return this;
	}

	public IDatabaseMappingBuilder<T> SetConverter<TFrom, TTo>(Func<TFrom, TTo> converter)
	{
		_schema.SetConverter(converter);
		return this;
	}

	public IDatabaseMappingBuilder<T> SetParameterValueConverter(Func<object, object> converter)
	{
		if (converter is null)
			throw new ArgumentNullException(nameof(converter));

		_schema.SetConverter<object, DataParameter>(obj => new DataParameter { Value = converter(obj) });
		return this;
	}
}

class Linq2dbColumnBuilder<T>(
	dynamic propBuilder,
	EntityMappingBuilder<T> entityBuilder,
	MappingSchema schema) : IDatabaseColumnBuilder<T>
	where T : class
{
	private dynamic _propBuilder = propBuilder ?? throw new ArgumentNullException(nameof(propBuilder));
	private readonly EntityMappingBuilder<T> _entityBuilder = entityBuilder ?? throw new ArgumentNullException(nameof(entityBuilder));
	private readonly MappingSchema _schema = schema ?? throw new ArgumentNullException(nameof(schema));

	public IDatabaseColumnBuilder<T> HasLength(int length)
	{
		_propBuilder.HasLength(length);
		return this;
	}

	public IDatabaseColumnBuilder<T> HasScale(int scale)
	{
		_propBuilder.HasScale(scale);
		return this;
	}

	public IDatabaseColumnBuilder<T> HasColumnName(string name)
	{
		_propBuilder.HasColumnName(name);
		return this;
	}

	public IDatabaseColumnBuilder<T> HasDataType(DatabaseDataType dataType)
	{
		_propBuilder.HasDataType(ToLinq2dbDataType(dataType));
		return this;
	}

	public IDatabaseColumnBuilder<T> IsNotNull()
	{
		_propBuilder.IsNotNull();
		return this;
	}

	public IDatabaseColumnBuilder<T> Property<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
	{
		_propBuilder = _entityBuilder.Property(propertyExpression);
		return this;
	}

	public IDatabaseColumnBuilder<T> DynamicProperty(string propertyName)
	{
		_propBuilder = _entityBuilder.Property(x => Sql.Property<object>(x, propertyName));
		return this;
	}

	public IDatabaseMappingBuilder<T> IsColumnRequired()
	{
		_entityBuilder.IsColumnRequired();
		return new Linq2dbMappingBuilder<T>(_entityBuilder, _schema);
	}

	public IDatabaseMappingBuilder<T> DynamicPropertyAccessors(
		Expression<Func<T, string, object, object>> getter,
		Expression<Action<T, string, object>> setter)
	{
		_entityBuilder.DynamicPropertyAccessors(getter, setter);
		return new Linq2dbMappingBuilder<T>(_entityBuilder, _schema);
	}

	private static DataType ToLinq2dbDataType(DatabaseDataType dataType) => dataType switch
	{
		DatabaseDataType.NVarChar => DataType.NVarChar,
		DatabaseDataType.VarChar => DataType.VarChar,
		DatabaseDataType.Char => DataType.Char,
		DatabaseDataType.Int => DataType.Int32,
		DatabaseDataType.BigInt => DataType.Int64,
		DatabaseDataType.Decimal => DataType.Decimal,
		DatabaseDataType.DateTime => DataType.DateTime,
		DatabaseDataType.Boolean => DataType.Boolean,
		DatabaseDataType.Binary => DataType.Binary,
		DatabaseDataType.Text => DataType.Text,
		_ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
	};
}
