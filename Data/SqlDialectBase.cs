namespace Ecng.Data;

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

	/// <inheritdoc />
	public abstract string GetIdentityColumnSuffix();

	/// <inheritdoc />
	public abstract void AppendCreateTable(StringBuilder sb, string tableName, string columnDefs);

	/// <inheritdoc />
	public abstract void AppendDropTable(StringBuilder sb, string tableName);

	/// <inheritdoc />
	public abstract void AppendPagination(StringBuilder sb, long? skip, long? take, bool hasOrderBy);

	/// <inheritdoc />
	public abstract void AppendUpsert(StringBuilder sb, string tableName, string[] allColumns, string[] keyColumns);

	/// <inheritdoc />
	public virtual string GetIdentitySelect(string idCol) => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual string FormatSkip(string skip) => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual string FormatTake(string take) => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual string Now() => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual string UtcNow() => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual string SysNow() => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual string SysUtcNow() => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual string NewId() => throw new NotSupportedException();
}
