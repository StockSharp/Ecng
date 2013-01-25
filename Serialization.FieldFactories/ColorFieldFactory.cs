namespace Ecng.Serialization
{
#if SILVERLIGHT
	using System.Windows.Media;
#else
	using System.Drawing;
#endif

	public class ColorFieldFactory : PrimitiveFieldFactory<Color, int>
	{
		public ColorFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	public sealed class ColorAttribute : ReflectionImplFieldFactoryAttribute
	{
		public ColorAttribute()
			: base(typeof(ColorFieldFactory))
		{
		}
	}
}