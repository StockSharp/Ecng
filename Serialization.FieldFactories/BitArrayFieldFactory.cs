namespace Ecng.Serialization
{
	using System.Collections;

	public class BitArrayFieldFactory : PrimitiveFieldFactory<BitArray, byte[]>
	{
		public BitArrayFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	public sealed class BitArrayAttribute : ReflectionImplFieldFactoryAttribute
	{
		public BitArrayAttribute()
			: base(typeof(BitArrayFieldFactory))
		{
		}
	}
}