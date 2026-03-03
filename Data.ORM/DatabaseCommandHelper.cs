namespace Ecng.Data;

/// <summary>
/// Helper methods for creating and working with database command parameters.
/// </summary>
public static class DatabaseCommandHelper
{
	/// <summary>
	/// Converts a CLR <see cref="Type"/> to the corresponding <see cref="DbType"/>.
	/// </summary>
	public static DbType ToDbType(this Type type)
	{
		if (type == typeof(TimeSpan))
			type = typeof(long);
		else if (type == typeof(TimeSpan?))
			type = typeof(long?);

		return type.To<DbType>();
	}

	/// <summary>
	/// Creates a <see cref="DbParameter"/> with the specified name, direction, type, and value.
	/// </summary>
	public static DbParameter CreateDbParameter(this DbProviderFactory factory, string name, ParameterDirection direction, DbType type, object value)
	{
		ArgumentNullException.ThrowIfNull(factory);

		var parameter = factory.CreateParameter();

		parameter.ParameterName = name;
		parameter.Direction = direction;
		parameter.DbType = type;
		parameter.Value = value;

		return parameter;
	}

	/// <summary>
	/// Gets the value at the specified column index, returning null for DBNull and normalizing DateTime kinds.
	/// </summary>
	public static object GetValueEx(this DbDataReader reader, int idx)
	{
		ArgumentNullException.ThrowIfNull(reader);

		if (reader.IsDBNull(idx))
			return null;

		var value = reader.GetValue(idx);

		if (value is DateTime dt && dt.Kind == DateTimeKind.Unspecified)
			value = dt.UtcKind();

		return value;
	}
}
