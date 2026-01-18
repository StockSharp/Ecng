namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Ecng.Common;

/// <summary>
/// Base class for SQL dialect implementations.
/// </summary>
public abstract class SqlDialectBase : ISqlDialect
{
	/// <inheritdoc />
	public abstract int MaxParameters { get; }

	/// <inheritdoc />
	public abstract string ParameterPrefix { get; }

	/// <inheritdoc />
	public abstract string QuoteIdentifier(string identifier);

	/// <inheritdoc />
	public abstract string GetSqlTypeName(Type clrType);

	/// <inheritdoc />
	public abstract string GenerateCreateTable(string tableName, IDictionary<string, Type> columns);

	/// <inheritdoc />
	public abstract string GenerateDropTable(string tableName);

	/// <inheritdoc />
	public abstract string GenerateSelect(string tableName, string whereClause, string orderByClause, long? skip, long? take);

	/// <inheritdoc />
	public abstract string GenerateUpsert(string tableName, IEnumerable<string> columns, IEnumerable<string> keyColumns);

	/// <inheritdoc />
	public virtual string GenerateInsert(string tableName, IEnumerable<string> columns)
	{
		var cols = columns.ToArray();
		var quotedCols = cols.Select(QuoteIdentifier);
		var paramCols = cols.Select(c => ParameterPrefix + c);

		return $"INSERT INTO {QuoteIdentifier(tableName)} ({quotedCols.JoinCommaSpace()}) VALUES ({paramCols.JoinCommaSpace()})";
	}

	/// <inheritdoc />
	public virtual string GenerateUpdate(string tableName, IEnumerable<string> columns, string whereClause)
	{
		var setClauses = columns.Select(c => $"{QuoteIdentifier(c)} = {ParameterPrefix}{c}");
		var sql = $"UPDATE {QuoteIdentifier(tableName)} SET {setClauses.JoinCommaSpace()}";

		if (!whereClause.IsEmpty())
			sql += $" WHERE {whereClause}";

		return sql;
	}

	/// <inheritdoc />
	public virtual string GenerateDelete(string tableName, string whereClause)
	{
		var sql = $"DELETE FROM {QuoteIdentifier(tableName)}";

		if (!whereClause.IsEmpty())
			sql += $" WHERE {whereClause}";

		return sql;
	}

	/// <inheritdoc />
	public virtual string BuildCondition(string column, ComparisonOperator op, string paramName)
	{
		var quotedCol = QuoteIdentifier(column);

		// Handle NULL comparison - SQL requires IS NULL / IS NOT NULL syntax
		if (paramName is null)
		{
			return op switch
			{
				ComparisonOperator.Equal => $"{quotedCol} IS NULL",
				ComparisonOperator.NotEqual => $"{quotedCol} IS NOT NULL",
				_ => throw new ArgumentOutOfRangeException(nameof(op), op, "Only Equal and NotEqual are supported with NULL"),
			};
		}

		var param = ParameterPrefix + paramName;

		return op switch
		{
			ComparisonOperator.Equal => $"{quotedCol} = {param}",
			ComparisonOperator.NotEqual => $"{quotedCol} <> {param}",
			ComparisonOperator.Greater => $"{quotedCol} > {param}",
			ComparisonOperator.GreaterOrEqual => $"{quotedCol} >= {param}",
			ComparisonOperator.Less => $"{quotedCol} < {param}",
			ComparisonOperator.LessOrEqual => $"{quotedCol} <= {param}",
			ComparisonOperator.Like => $"{quotedCol} LIKE {param}",
			_ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported operator"),
		};
	}

	/// <inheritdoc />
	public virtual string BuildInCondition(string column, IEnumerable<string> paramNames)
	{
		var names = paramNames.ToArray();
		if (names.Length == 0)
			return "1 = 0"; // Empty IN - always false

		var quotedCol = QuoteIdentifier(column);
		var paramList = names.Select(p => ParameterPrefix + p).JoinCommaSpace();

		return $"{quotedCol} IN ({paramList})";
	}

	/// <inheritdoc />
	public virtual object ConvertToDbValue(object value, Type clrType)
	{
		if (value is null)
			return DBNull.Value;

		// TimeSpan stored as ticks (BIGINT)
		if (value is TimeSpan ts)
			return ts.Ticks;

		return value;
	}

	/// <inheritdoc />
	public virtual object ConvertFromDbValue(object value, Type targetType)
	{
		if (value is null || value is DBNull)
			return null;

		// TimeSpan from ticks
		if (targetType == typeof(TimeSpan) && value is long ticks)
			return new TimeSpan(ticks);

		return value;
	}

	/// <summary>
	/// Builds column definitions for CREATE TABLE.
	/// </summary>
	protected string BuildColumnDefinitions(IDictionary<string, Type> columns)
	{
		var sb = new StringBuilder();
		var first = true;

		foreach (var kv in columns)
		{
			if (!first)
				sb.Append(", ");
			first = false;

			sb.Append(QuoteIdentifier(kv.Key));
			sb.Append(' ');
			sb.Append(GetSqlTypeName(kv.Value));
		}

		return sb.ToString();
	}
}
