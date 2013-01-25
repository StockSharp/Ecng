namespace Ecng.Serialization
{
	using System;

	public abstract class SchemaFactory
	{
		protected internal abstract Schema CreateSchema(Type entityType);
	}
}