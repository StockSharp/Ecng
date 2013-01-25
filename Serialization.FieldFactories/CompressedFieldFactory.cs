namespace Ecng.Serialization
{
	using System.IO;

	using ICSharpCode.SharpZipLib.GZip;

	public class CompressedFieldFactory : FieldFactory<Stream, Stream>
	{
		public CompressedFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected override Stream OnCreateInstance(ISerializer serializer, Stream source)
		{
			using (var inStream = new GZipInputStream(source) { IsStreamOwner = false })
			{
				var instance = new MemoryStream();
				inStream.CopyTo(instance);
				instance.Position = 0;
				return instance;
			}
		}

		protected override Stream OnCreateSource(ISerializer serializer, Stream instance)
		{
			var source = new MemoryStream();

			using (var outStream = new GZipOutputStream(source) { IsStreamOwner = false })
				instance.CopyTo(outStream);

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