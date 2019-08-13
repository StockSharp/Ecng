namespace Ecng.Serialization
{
	using System.IO;
	using System.IO.Compression;

	public class CompressedFieldFactory : FieldFactory<Stream, Stream>
	{
		public CompressedFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected override Stream OnCreateInstance(ISerializer serializer, Stream source)
		{
			var instance = new MemoryStream();

			using (var inStream = new GZipStream(source, CompressionMode.Decompress, true))
				inStream.CopyTo(instance);

			instance.Position = 0;
			return instance;
		}

		protected override Stream OnCreateSource(ISerializer serializer, Stream instance)
		{
			var source = new MemoryStream();

			using (var outStream = new GZipStream(source, CompressionMode.Compress, true))
				instance.CopyTo(outStream);

			source.Position = 0;
			return source;
		}
	}

	public class CompressedFieldFactoryAttribute : FieldFactoryAttribute
	{
		public override FieldFactory CreateFactory(Field field)
		{
			return new CompressedFieldFactory(field, Order);
		}
	}
}