namespace Ecng.Data;

using System;
using System.Collections.Generic;

using Ecng.Common;

/// <summary>
/// Interface for database-specific SQL dialect.
/// Provides SQL syntax variations for different database providers.
/// </summary>
public interface ISqlDialect
{
	/// <summary>
	/// Gets the parameter prefix (e.g., "@" for SQL Server, "$" for PostgreSQL).
	/// </summary>
	string ParameterPrefix { get; }

	/// <summary>
	/// Quotes an identifier (table name, column name).
	/// </summary>
	/// <param name="identifier">The identifier to quote.</param>
	/// <returns>Quoted identifier.</returns>
	string QuoteIdentifier(string identifier);

	/// <summary>
	/// Gets the SQL type name for a CLR type.
	/// </summary>
	/// <param name="clrType">CLR type.</param>
	/// <returns>SQL type name.</returns>
	string GetSqlTypeName(Type clrType);

	/// <summary>
	/// Generates CREATE TABLE IF NOT EXISTS statement.
	/// </summary>
	/// <param name="tableName">Table name.</param>
	/// <param name="columns">Column definitions (name -> type).</param>
	/// <returns>SQL statement.</returns>
	string GenerateCreateTable(string tableName, IDictionary<string, Type> columns);

	/// <summary>
	/// Generates DROP TABLE IF EXISTS statement.
	/// </summary>
	/// <param name="tableName">Table name.</param>
	/// <returns>SQL statement.</returns>
	string GenerateDropTable(string tableName);

	/// <summary>
	/// Generates INSERT statement.
	/// </summary>
	/// <param name="tableName">Table name.</param>
	/// <param name="columns">Column names.</param>
	/// <returns>SQL statement.</returns>
	string GenerateInsert(string tableName, IEnumerable<string> columns);

	/// <summary>
	/// Generates UPDATE statement.
	/// </summary>
	/// <param name="tableName">Table name.</param>
	/// <param name="columns">Columns to update.</param>
	/// <param name="whereClause">WHERE clause (without WHERE keyword).</param>
	/// <returns>SQL statement.</returns>
	string GenerateUpdate(string tableName, IEnumerable<string> columns, string whereClause);

	/// <summary>
	/// Generates DELETE statement.
	/// </summary>
	/// <param name="tableName">Table name.</param>
	/// <param name="whereClause">WHERE clause (without WHERE keyword).</param>
	/// <returns>SQL statement.</returns>
	string GenerateDelete(string tableName, string whereClause);

	/// <summary>
	/// Generates SELECT statement with optional pagination.
	/// </summary>
	/// <param name="tableName">Table name.</param>
	/// <param name="whereClause">WHERE clause (without WHERE keyword), or null.</param>
	/// <param name="orderByClause">ORDER BY clause (without ORDER BY keywords), or null.</param>
	/// <param name="skip">Number of rows to skip, or null.</param>
	/// <param name="take">Number of rows to take, or null.</param>
	/// <returns>SQL statement.</returns>
	string GenerateSelect(string tableName, string whereClause, string orderByClause, long? skip, long? take);

	/// <summary>
	/// Generates UPSERT (INSERT or UPDATE) statement.
	/// </summary>
	/// <param name="tableName">Table name.</param>
	/// <param name="columns">All columns.</param>
	/// <param name="keyColumns">Key columns for matching.</param>
	/// <returns>SQL statement.</returns>
	string GenerateUpsert(string tableName, IEnumerable<string> columns, IEnumerable<string> keyColumns);

	/// <summary>
	/// Builds WHERE clause condition for a filter.
	/// </summary>
	/// <param name="column">Column name.</param>
	/// <param name="op">Comparison operator.</param>
	/// <param name="paramName">Parameter name (without prefix).</param>
	/// <returns>Condition string.</returns>
	string BuildCondition(string column, ComparisonOperator op, string paramName);

	/// <summary>
	/// Builds WHERE clause condition for IN operator with multiple values.
	/// </summary>
	/// <param name="column">Column name.</param>
	/// <param name="paramNames">Parameter names (without prefix).</param>
	/// <returns>Condition string.</returns>
	string BuildInCondition(string column, IEnumerable<string> paramNames);

	/// <summary>
	/// Converts a value to database format if needed.
	/// </summary>
	/// <param name="value">Original value.</param>
	/// <param name="clrType">CLR type of the value.</param>
	/// <returns>Converted value.</returns>
	object ConvertToDbValue(object value, Type clrType);

	/// <summary>
	/// Converts a value from database format if needed.
	/// </summary>
	/// <param name="value">Database value.</param>
	/// <param name="targetType">Target CLR type.</param>
	/// <returns>Converted value.</returns>
	object ConvertFromDbValue(object value, Type targetType);
}
