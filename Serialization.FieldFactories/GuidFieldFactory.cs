namespace Ecng.Serialization
{
	using System;

	public class GuidFieldFactory : PrimitiveFieldFactory<Guid, string>
	{
		public GuidFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	public sealed class GuidAttribute : ReflectionImplFieldFactoryAttribute
	{
		public GuidAttribute()
			: base(typeof(GuidFieldFactory))
		{
		}
	}
}