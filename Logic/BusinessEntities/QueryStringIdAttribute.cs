namespace Ecng.Logic.BusinessEntities
{
	using System;

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class QueryStringIdAttribute : Attribute
	{
		public QueryStringIdAttribute(string idField)
		{
			IdField = idField;
		}

		public string IdField { get; }
	}
}