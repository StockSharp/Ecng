namespace Ecng.Serialization
{
	using System.IO;

	public class TextFieldFactory : PrimitiveFieldFactory<string, Stream>
	{
		public TextFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	public class TextAttribute : ReflectionImplFieldFactoryAttribute
	{
		public TextAttribute()
			: base(typeof(TextFieldFactory))
		{
		}
	}
}