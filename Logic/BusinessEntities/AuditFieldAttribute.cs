namespace Ecng.Logic.BusinessEntities
{
	using System;

	using Ecng.Reflection;

	[AttributeUsage(ReflectionHelper.Members, AllowMultiple = true)]
	public class AuditFieldAttribute : AuditAttribute
	{
		public AuditFieldAttribute(byte id)
			: base(id)
		{
		}

		public string FieldName { get; set; }
	}
}