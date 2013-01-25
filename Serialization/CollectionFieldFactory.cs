namespace Ecng.Serialization
{
	using System;

	using Ecng.Common;

	public class CollectionFieldFactory<TCollection> : ComplexFieldFactory<TCollection>
	{
		public CollectionFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override TCollection OnCreateInstance(ISerializer serializer, SerializationItemCollection source)
		{
			return serializer.GetSerializer<TCollection>().Deserialize(source);
		}

		protected internal override SerializationItemCollection OnCreateSource(ISerializer serializer, TCollection instance)
		{
			var source = new SerializationItemCollection();
			serializer.GetSerializer<TCollection>().Serialize(instance, source);
			return source;
		}
	}

	public class CollectionAttribute : SerializerAttribute
	{
		protected override Type GetFactoryType(Field field)
		{
			return typeof(CollectionFieldFactory<>).Make(field.Type);
		}
	}
}