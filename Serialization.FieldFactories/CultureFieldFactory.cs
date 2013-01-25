namespace Ecng.Serialization
{
	using System.Globalization;

	public class CultureFieldFactory : PrimitiveFieldFactory<CultureInfo, int>
	{
		public CultureFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	public sealed class CultureAttribute : ReflectionImplFieldFactoryAttribute
	{
		public CultureAttribute()
			: base(typeof(CultureFieldFactory))
		{
		}
	}
}