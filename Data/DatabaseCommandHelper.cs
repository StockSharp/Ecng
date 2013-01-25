namespace Ecng.Data
{
	using System;
	using System.Data;
	using System.Data.Common;

	using Ecng.Common;

	public static class DatabaseCommandHelper
	{
		public static DbParameter Parameter(this Database database, string name, object value)
		{
			return InputParameter(database, name, value);
		}

		public static DbParameter InputParameter(this Database database, string name, object value)
		{
			return Parameter(database, name, ParameterDirection.Input, value);
		}

		public static DbParameter InputOutputParameter(this Database database, string name, object value)
		{
			return Parameter(database, name, ParameterDirection.InputOutput, value);
		}

		public static DbParameter OutputParameter(this Database database, string name, object value)
		{
			return Parameter(database, name, ParameterDirection.Output, value);
		}

		public static DbParameter ReturnParameter(this Database database, string name, object value)
		{
			return Parameter(database, name, ParameterDirection.ReturnValue, value);
		}

		public static DbParameter Parameter(this Database database, string name, ParameterDirection direction, object value)
		{
			var type = DbType.Object;

			if (value != null)
				type = value.GetType().To<DbType>();

			return Parameter(database, name, direction, type, value);
		}

		public static DbParameter Parameter(this Database database, string name, ParameterDirection direction, DbType type, object value)
		{
			return Parameter(database, name, direction, type, 0, string.Empty, false, value);
		}

		public static DbParameter Parameter(this Database database, string name, ParameterDirection direction, DbType type, int size, string sourceColumn, bool sourceColumnNullMapping, object value)
		{
			if (database == null)
				throw new ArgumentNullException("database");

			var parameter = database.Provider.Factory.CreateParameter();

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
	}
}