namespace Ecng.Data;

using System;
using System.Text;

/// <summary>
/// Interface for database-specific SQL dialect.
/// Provides SQL syntax variations for different database providers.
/// </summary>
public interface ISqlDialect
{
	/// <summary>
	/// Gets the maximum number of parameters allowed in a single query.
	/// </summary>
	/// <remarks>
	/// SQL Server: 2100, SQLite: 999, PostgreSQL: 65535, MySQL: 65535.
	/// </remarks>
	int MaxParameters { get; }

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

	/// <summary>
	/// Gets the SQL expression for retrieving the last inserted identity value.
	/// </summary>
	string GetIdentitySelect(string idCol) => throw new NotSupportedException();

	/// <summary>
	/// Formats a SKIP/OFFSET clause.
	/// </summary>
	string FormatSkip(string skip) => throw new NotSupportedException();

	/// <summary>
	/// Formats a TAKE/FETCH clause.
	/// </summary>
	string FormatTake(string take) => throw new NotSupportedException();

	/// <summary>
	/// Gets the SQL expression for the current local date/time.
	/// </summary>
	string Now() => throw new NotSupportedException();

	/// <summary>
	/// Gets the SQL expression for the current UTC date/time.
	/// </summary>
	string UtcNow() => throw new NotSupportedException();

	/// <summary>
	/// Gets the SQL expression for the current system local date/time with offset.
	/// </summary>
	string SysNow() => throw new NotSupportedException();

	/// <summary>
	/// Gets the SQL expression for the current system UTC date/time.
	/// </summary>
	string SysUtcNow() => throw new NotSupportedException();

	/// <summary>
	/// Gets the SQL expression for generating a new unique identifier.
	/// </summary>
	string NewId() => throw new NotSupportedException();

	/// <summary>
	/// Appends CREATE TABLE IF NOT EXISTS statement to a <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="tableName">Table name (unquoted).</param>
	/// <param name="columnDefs">Pre-built column definitions string.</param>
	void AppendCreateTable(StringBuilder sb, string tableName, string columnDefs);

	/// <summary>
	/// Appends DROP TABLE IF EXISTS statement to a <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="tableName">Table name (unquoted).</param>
	void AppendDropTable(StringBuilder sb, string tableName);

	/// <summary>
	/// Appends pagination (OFFSET/FETCH or LIMIT/OFFSET) to a <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="skip">Rows to skip, or null.</param>
	/// <param name="take">Rows to take, or null.</param>
	/// <param name="hasOrderBy">Whether the query already has an ORDER BY clause.</param>
	void AppendPagination(StringBuilder sb, long? skip, long? take, bool hasOrderBy);

	/// <summary>
	/// Appends UPSERT (MERGE / INSERT ON CONFLICT) statement to a <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="tableName">Table name (unquoted).</param>
	/// <param name="allColumns">All column names.</param>
	/// <param name="keyColumns">Key column names for matching.</param>
	void AppendUpsert(StringBuilder sb, string tableName, string[] allColumns, string[] keyColumns);

	/// <summary>
	/// Gets the SQL suffix for an identity (auto-increment primary key) column definition.
	/// </summary>
	string GetIdentityColumnSuffix();
}
