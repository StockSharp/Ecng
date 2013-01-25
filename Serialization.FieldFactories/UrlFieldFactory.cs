namespace Ecng.Serialization
{
	using System;

	public class UrlFieldFactory : PrimitiveFieldFactory<Uri, string>
	{
		public UrlFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	public sealed class UrlAttribute : ReflectionImplFieldFactoryAttribute
    {
		public UrlAttribute()
			: base(typeof(UrlFieldFactory))
		{
		}
    }
}