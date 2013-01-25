namespace Ecng.Serialization
{
	using System.IO;

	public class StreamFieldFactory : PrimitiveFieldFactory<Stream, byte[]>
	{
		public StreamFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	public class StreamAttribute : ReflectionImplFieldFactoryAttribute
	{
		public StreamAttribute()
			: base(typeof(StreamFieldFactory))
		{
		}
	}
}