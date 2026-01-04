namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides batch insert operations for a database table.
/// </summary>
/// <typeparam name="T">The type of entities to insert.</typeparam>
public interface IDatabaseBatchInserter<T> : IDisposable
	where T : class
{
	/// <summary>
	/// Inserts a single item into the database.
	/// </summary>
	/// <param name="item">The item to insert.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	Task InsertAsync(T item, CancellationToken cancellationToken);

	/// <summary>
	/// Performs a bulk copy of multiple items into the database.
	/// </summary>
	/// <param name="items">The items to insert.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	Task BulkCopyAsync(IEnumerable<T> items, CancellationToken cancellationToken);
}

/// <summary>
/// Factory for creating database batch inserters.
/// </summary>
public interface IDatabaseBatchInserterProvider
{
	/// <summary>
	/// Creates a batch inserter for the specified entity type.
	/// </summary>
	/// <typeparam name="T">The type of entities to insert.</typeparam>
	/// <param name="connection">Database connection settings.</param>
	/// <param name="tableName">Name of the target table.</param>
	/// <param name="configureMapping">Callback to configure table mapping.</param>
	/// <returns>A batch inserter instance.</returns>
	IDatabaseBatchInserter<T> Create<T>(
		DatabaseConnectionPair connection,
		string tableName,
		Action<IDatabaseMappingBuilder<T>> configureMapping)
		where T : class;

	/// <summary>
	/// Drops the specified table if it exists.
	/// </summary>
	/// <param name="connection">Database connection settings.</param>
	/// <param name="tableName">Name of the table to drop.</param>
	void DropTable(DatabaseConnectionPair connection, string tableName);
}

/// <summary>
/// Builder for configuring database table mappings.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IDatabaseMappingBuilder<T>
	where T : class
{
	/// <summary>
	/// Sets the table name.
	/// </summary>
	/// <param name="name">Table name.</param>
	/// <returns>The builder for chaining.</returns>
	IDatabaseMappingBuilder<T> HasTableName(string name);

	/// <summary>
	/// Marks all columns as required by default.
	/// </summary>
	/// <returns>The builder for chaining.</returns>
	IDatabaseMappingBuilder<T> IsColumnRequired();

	/// <summary>
	/// Configures a property mapping.
	/// </summary>
	/// <typeparam name="TProperty">The property type.</typeparam>
	/// <param name="propertyExpression">Expression selecting the property.</param>
	/// <returns>A column builder for further configuration.</returns>
	IDatabaseColumnBuilder<T> Property<TProperty>(Expression<Func<T, TProperty>> propertyExpression);

	/// <summary>
	/// Configures a dynamic property by name.
	/// </summary>
	/// <param name="propertyName">The property name.</param>
	/// <returns>A column builder for further configuration.</returns>
	IDatabaseColumnBuilder<T> DynamicProperty(string propertyName);

	/// <summary>
	/// Sets dynamic property accessors for reading and writing dynamic properties.
	/// </summary>
	/// <param name="getter">Expression to get a dynamic property value.</param>
	/// <param name="setter">Expression to set a dynamic property value.</param>
	/// <returns>The builder for chaining.</returns>
	IDatabaseMappingBuilder<T> DynamicPropertyAccessors(
		Expression<Func<T, string, object, object>> getter,
		Expression<Action<T, string, object>> setter);

	/// <summary>
	/// Registers a value converter.
	/// </summary>
	/// <typeparam name="TFrom">Source type.</typeparam>
	/// <typeparam name="TTo">Target type.</typeparam>
	/// <param name="converter">Conversion function.</param>
	/// <returns>The builder for chaining.</returns>
	IDatabaseMappingBuilder<T> SetConverter<TFrom, TTo>(Func<TFrom, TTo> converter);

	/// <summary>
	/// Sets a parameter value converter that transforms values before sending to database.
	/// </summary>
	/// <param name="converter">Function that converts parameter values.</param>
	/// <returns>The builder for chaining.</returns>
	IDatabaseMappingBuilder<T> SetParameterValueConverter(Func<object, object> converter);
}

/// <summary>
/// Builder for configuring a database column.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IDatabaseColumnBuilder<T>
	where T : class
{
	/// <summary>
	/// Sets the maximum length for string columns.
	/// </summary>
	/// <param name="length">Maximum length.</param>
	/// <returns>The builder for chaining.</returns>
	IDatabaseColumnBuilder<T> HasLength(int length);

	/// <summary>
	/// Sets the scale for decimal columns.
	/// </summary>
	/// <param name="scale">Number of decimal places.</param>
	/// <returns>The builder for chaining.</returns>
	IDatabaseColumnBuilder<T> HasScale(int scale);

	/// <summary>
	/// Sets the column name.
	/// </summary>
	/// <param name="name">Column name.</param>
	/// <returns>The builder for chaining.</returns>
	IDatabaseColumnBuilder<T> HasColumnName(string name);

	/// <summary>
	/// Sets the database data type.
	/// </summary>
	/// <param name="dataType">The data type.</param>
	/// <returns>The builder for chaining.</returns>
	IDatabaseColumnBuilder<T> HasDataType(DatabaseDataType dataType);

	/// <summary>
	/// Marks the column as not nullable.
	/// </summary>
	/// <returns>The builder for chaining.</returns>
	IDatabaseColumnBuilder<T> IsNotNull();

	/// <summary>
	/// Configures another property.
	/// </summary>
	/// <typeparam name="TProperty">The property type.</typeparam>
	/// <param name="propertyExpression">Expression selecting the property.</param>
	/// <returns>A column builder for the new property.</returns>
	IDatabaseColumnBuilder<T> Property<TProperty>(Expression<Func<T, TProperty>> propertyExpression);

	/// <summary>
	/// Configures a dynamic property by name.
	/// </summary>
	/// <param name="propertyName">The property name.</param>
	/// <returns>A column builder for further configuration.</returns>
	IDatabaseColumnBuilder<T> DynamicProperty(string propertyName);

	/// <summary>
	/// Marks all columns as required by default.
	/// </summary>
	/// <returns>The mapping builder for chaining.</returns>
	IDatabaseMappingBuilder<T> IsColumnRequired();

	/// <summary>
	/// Sets dynamic property accessors for reading and writing dynamic properties.
	/// </summary>
	/// <param name="getter">Expression to get a dynamic property value.</param>
	/// <param name="setter">Expression to set a dynamic property value.</param>
	/// <returns>The mapping builder for chaining.</returns>
	IDatabaseMappingBuilder<T> DynamicPropertyAccessors(
		Expression<Func<T, string, object, object>> getter,
		Expression<Action<T, string, object>> setter);
}

/// <summary>
/// Database data types.
/// </summary>
public enum DatabaseDataType
{
	/// <summary>
	/// Variable-length Unicode string.
	/// </summary>
	NVarChar,

	/// <summary>
	/// Variable-length non-Unicode string.
	/// </summary>
	VarChar,

	/// <summary>
	/// Fixed-length string.
	/// </summary>
	Char,

	/// <summary>
	/// Integer.
	/// </summary>
	Int,

	/// <summary>
	/// Big integer.
	/// </summary>
	BigInt,

	/// <summary>
	/// Decimal number.
	/// </summary>
	Decimal,

	/// <summary>
	/// Date and time.
	/// </summary>
	DateTime,

	/// <summary>
	/// Boolean.
	/// </summary>
	Boolean,

	/// <summary>
	/// Binary data.
	/// </summary>
	Binary,

	/// <summary>
	/// Large text.
	/// </summary>
	Text,
}
