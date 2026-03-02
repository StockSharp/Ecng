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

	public static DbParameter Parameter(this DatabaseProvider provider, string name, object value)
	{
		return InputParameter(provider, name, value);
	}

	public static DbParameter InputParameter(this DatabaseProvider provider, string name, object value)
	{
		return Parameter(provider, name, ParameterDirection.Input, value);
	}

	public static DbParameter InputOutputParameter(this DatabaseProvider provider, string name, object value)
	{
		return Parameter(provider, name, ParameterDirection.InputOutput, value);
	}

	public static DbParameter OutputParameter(this DatabaseProvider provider, string name, object value)
	{
		return Parameter(provider, name, ParameterDirection.Output, value);
	}

	public static DbParameter ReturnParameter(this DatabaseProvider provider, string name, object value)
	{
		return Parameter(provider, name, ParameterDirection.ReturnValue, value);
	}

	public static DbParameter Parameter(this DatabaseProvider provider, string name, ParameterDirection direction, object value)
	{
		var type = DbType.Object;

		if (value != null)
			type = value.GetType().ToDbType();

		return Parameter(provider, name, direction, type, value);
	}

	public static DbParameter Parameter(this DatabaseProvider provider, string name, ParameterDirection direction, DbType type, object value)
	{
		return Parameter(provider, name, direction, type, 0, string.Empty, false, value);
	}

	public static DbParameter Parameter(this DatabaseProvider provider, string name, ParameterDirection direction, DbType type, int size, string sourceColumn, bool sourceColumnNullMapping, object value)
	{
		ArgumentNullException.ThrowIfNull(provider);

		var parameter = provider.Factory.CreateParameter();

		parameter.ParameterName = name;
		parameter.Direction = direction;
		parameter.DbType = type;
		parameter.Size = size;
		parameter.SourceColumn = sourceColumn;
		parameter.SourceColumnNullMapping = sourceColumnNullMapping;
		parameter.Value = value;

		//Parameters.Add(parameter);

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