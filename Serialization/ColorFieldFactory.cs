namespace Ecng.Serialization
{
	using System.Windows.Media;

	public class WpfColorFieldFactory : PrimitiveFieldFactory<Color, int>
	{
		public WpfColorFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	public sealed class WpfColorAttribute : ReflectionImplFieldFactoryAttribute
	{
		public WpfColorAttribute()
			: base(typeof(WpfColorFieldFactory))
		{
		}
	}

#if !SILVERLIGHT
	public class WinColorFieldFactory : PrimitiveFieldFactory<System.Drawing.Color, int>
	{
		public WinColorFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	public sealed class WinColorAttribute : ReflectionImplFieldFactoryAttribute
	{
		public WinColorAttribute()
			: base(typeof(WinColorFieldFactory))
		{
		}
	}
#endif
}