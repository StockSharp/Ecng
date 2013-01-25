namespace Ecng.Serialization
{
	using System;
	using System.IO;

	using Ecng.Common;

	[Serializable]
	public class SerializerFieldFactory<I> : FieldFactory<I, Stream>
	{
		public SerializerFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override I OnCreateInstance(ISerializer serializer, Stream source)
		{
			return serializer.GetSerializer<I>().Deserialize(source);
		}

		protected internal override Stream OnCreateSource(ISerializer serializer, I instance)
		{
			var source = new MemoryStream();
			serializer.GetSerializer<I>().Serialize(instance, source);
			return source;
		}
	}

	public class SerializerAttribute : ReflectionFieldFactoryAttribute
	{
		protected override Type GetFactoryType(Field field)
		{
			return typeof(SerializerFieldFactory<>).Make(field.Type);
		}
	}
}