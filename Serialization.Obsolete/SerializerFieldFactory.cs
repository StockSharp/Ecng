namespace Ecng.Serialization
{
	using System;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;

	[Serializable]
	public class SerializerFieldFactory<I> : FieldFactory<I, Stream>
	{
		public SerializerFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override Task<I> OnCreateInstance(ISerializer serializer, Stream source, CancellationToken cancellationToken)
			=> serializer.GetSerializer<I>().DeserializeAsync(source, cancellationToken);

		protected internal override async Task<Stream> OnCreateSource(ISerializer serializer, I instance, CancellationToken cancellationToken)
		{
			Stream source = new MemoryStream();
			await serializer.GetSerializer<I>().SerializeAsync(instance, source, cancellationToken);
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