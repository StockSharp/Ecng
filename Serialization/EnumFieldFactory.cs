namespace Ecng.Serialization
{
	using System;

	using Ecng.Common;
	using Ecng.Reflection;

	public class EnumFieldFactory<I, S> : PrimitiveFieldFactory<I, S>
	{
		public EnumFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	[AttributeUsage(ReflectionHelper.Members | AttributeTargets.Enum)]
	public sealed class EnumAttribute : ReflectionFieldFactoryAttribute
	{
		public bool AsString { get; set; }

		protected override Type GetFactoryType(Field field)
		{
			return typeof(EnumFieldFactory<,>).Make(field.Type, AsString ? typeof(string) : field.Type.GetEnumBaseType());
		}
	}
}