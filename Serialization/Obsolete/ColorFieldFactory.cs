namespace Ecng.Serialization
{
	using System;

	using Ecng.Common;

	public class ColorFieldFactory<T> : PrimitiveFieldFactory<T, int>
	{
		public ColorFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	public sealed class ColorAttribute : ReflectionFieldFactoryAttribute
	{
		protected override Type GetFactoryType(Field field)
		{
			return typeof(ColorFieldFactory<>).Make(field.Type);
		}
	}
}