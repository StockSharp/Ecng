namespace Ecng.Logic.BusinessEntities
{
	using System;

	using Ecng.Reflection;

	[AttributeUsage(ReflectionHelper.Types)]
	public class AuditAttribute : Attribute
	{
		public AuditAttribute(byte id)
		{
			Id = id;
		}

		public byte Id { get; private set; }
	}
}