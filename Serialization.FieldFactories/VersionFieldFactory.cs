namespace Ecng.Serialization
{
	using System;

	public class VersionFieldFactory : PrimitiveFieldFactory<Version, string>
	{
		public VersionFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	public sealed class VersionAttribute : ReflectionImplFieldFactoryAttribute
	{
		public VersionAttribute()
			: base(typeof(VersionFieldFactory))
		{
		}
	}
}