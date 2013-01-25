namespace Ecng.Serialization
{
	using System;

	using Ecng.Common;

	[Serializable]
	public class LazyLoadFieldFactory<TInstance, TSource> : RelationFieldFactory<LazyLoadObject<TInstance>, TSource>
	{
		public LazyLoadFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected override LazyLoadObject<TInstance> OnCreateInstance(ISerializer serializer, TSource source)
		{
			return new LazyLoadObject<TInstance>(Storage, source);
		}

		protected override TSource OnCreateSource(ISerializer serializer, LazyLoadObject<TInstance> instance)
		{
			if (instance.IsNull())
				throw new ArgumentNullException("instance", "Field '{0}' in schema '{1}' isn't initialized.".Put(Field.Name, Field.Schema.EntityType));

			return (TSource)instance.Id;
		}
	}

	public class LazyLoadAttribute : RelationSingleAttribute
	{
		protected override Type GetFactoryType(Field field)
		{
			return typeof(LazyLoadFieldFactory<,>).Make(field.Type, IdentityType);
		}
	}
}