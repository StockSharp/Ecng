namespace Ecng.Data.Sql
{
	#region Using Directives

	using System;

	using Ecng.Common;
	using Ecng.Reflection;

	#endregion

    [AttributeUsage(ReflectionHelper.Types)]
	public class ProcedureAttribute : NameAttribute
	{
		#region ProcedureAttribute.ctor()

		public ProcedureAttribute(SqlCommandTypes commandType, string procedureName)
			: base(procedureName)
		{
			_commandType = commandType;
		}

		#endregion

		#region CommandType

		private readonly SqlCommandTypes _commandType;

		public SqlCommandTypes CommandType
		{
			get { return _commandType; }
		}

		#endregion

		#region ProcedureName

		public string ProcedureName
		{
			get { return Name; }
		}

		#endregion
	}
}