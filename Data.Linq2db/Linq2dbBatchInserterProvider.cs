namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

/// <summary>
/// Linq2db implementation of <see cref="IDatabaseBatchInserterProvider"/>.
/// </summary>
public class Linq2dbBatchInserterProvider : IDatabaseBatchInserterProvider
{
	/// <inheritdoc />
	public IDatabaseBatchInserter<T> Create<T>(
		DatabaseConnectionPair connection,
		string tableName,
		Action<IDatabaseMappingBuilder<T>> configureMapping)
		where T : class
	{
		if (connection is null)
			throw new ArgumentNullException(nameof(connection));

		if (string.IsNullOrEmpty(tableName))
			throw new ArgumentNullException(nameof(tableName));

		if (configureMapping is null)
			throw new ArgumentNullException(nameof(configureMapping));

		return new Linq2dbBatchInserter<T>(connection, tableName, configureMapping);
	}
}

internal class Linq2dbBatchInserter<T> : IDatabaseBatchInserter<T>
	where T : class
{
	private readonly DataConnection _db;
	private readonly ITable<T> _table;
	private bool _disposed;

	public Linq2dbBatchInserter(
		DatabaseConnectionPair connection,
		string tableName,
		Action<IDatabaseMappingBuilder<T>> configureMapping)
	{
		_db = connection.CreateConnection();

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

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_db?.Dispose();
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(Linq2dbBatchInserter<T>));
	}
}

internal class Linq2dbMappingBuilder<T> : IDatabaseMappingBuilder<T>
	where T : class
{
	private readonly EntityMappingBuilder<T> _entityBuilder;
	private readonly MappingSchema _schema;

	public Linq2dbMappingBuilder(EntityMappingBuilder<T> entityBuilder, MappingSchema schema)
	{
		_entityBuilder = entityBuilder ?? throw new ArgumentNullException(nameof(entityBuilder));
		_schema = schema ?? throw new ArgumentNullException(nameof(schema));
	}

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

internal class Linq2dbColumnBuilder<T> : IDatabaseColumnBuilder<T>
	where T : class
{
	private dynamic _propBuilder;
	private readonly EntityMappingBuilder<T> _entityBuilder;
	private readonly MappingSchema _schema;

	public Linq2dbColumnBuilder(
		dynamic propBuilder,
		EntityMappingBuilder<T> entityBuilder,
		MappingSchema schema)
	{
		_propBuilder = propBuilder;
		_entityBuilder = entityBuilder;
		_schema = schema;
	}

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

	private static LinqToDB.DataType ToLinq2dbDataType(DatabaseDataType dataType) => dataType switch
	{
		DatabaseDataType.NVarChar => LinqToDB.DataType.NVarChar,
		DatabaseDataType.VarChar => LinqToDB.DataType.VarChar,
		DatabaseDataType.Char => LinqToDB.DataType.Char,
		DatabaseDataType.Int => LinqToDB.DataType.Int32,
		DatabaseDataType.BigInt => LinqToDB.DataType.Int64,
		DatabaseDataType.Decimal => LinqToDB.DataType.Decimal,
		DatabaseDataType.DateTime => LinqToDB.DataType.DateTime,
		DatabaseDataType.Boolean => LinqToDB.DataType.Boolean,
		DatabaseDataType.Binary => LinqToDB.DataType.Binary,
		DatabaseDataType.Text => LinqToDB.DataType.Text,
		_ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
	};
}
