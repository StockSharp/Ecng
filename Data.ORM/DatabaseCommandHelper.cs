namespace Ecng.Data;

public static class DatabaseCommandHelper
{
	public static DbType ToDbType(this Type type)
	{
		if (type == typeof(TimeSpan))
			type = typeof(long);
		else if (type == typeof(TimeSpan?))
			type = typeof(long?);

		return type.To<DbType>();
	}

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
