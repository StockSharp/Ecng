namespace Ecng.Serialization
{
	using System;

	using Ecng.Common;

	public interface IDynamicSchema
	{
		Schema Schema { get; }
	}

	public abstract class DynamicInnerSchemaFieldFactory<TEntity> : FieldFactory<TEntity, SerializationItemCollection>, IDynamicSchema
	{
		protected DynamicInnerSchemaFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		public abstract Schema Schema { get; }
	}

	public class DynamicInnerSchemaAttribute : ReflectionFieldFactoryAttribute
	{
		protected override Type GetFactoryType(Field field)
		{
			return typeof(DynamicInnerSchemaFieldFactory<>).Make(field.Type);
		}
	}
}