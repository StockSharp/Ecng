namespace Ecng.Logic.BusinessEntities
{
	using System;
	using System.Web.UI.WebControls;

	public class LogicQueryStringParameter<T> : QueryStringParameter
	{
		public LogicQueryStringParameter()
			: this(typeof(T).Name + "Id")
		{
		}

		public LogicQueryStringParameter(string qsKey)
			: base(qsKey, TypeCode.Int64, LogicHelper.GetIdentity<T>())
		{
		}
	}
}