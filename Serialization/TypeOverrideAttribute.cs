namespace Ecng.Serialization
{
	using System;

	using Ecng.Reflection;

	[AttributeUsage(ReflectionHelper.Members | ReflectionHelper.Types)]
	public class TypeOverrideAttribute : Attribute
	{
		public Type FromType { get; }
		public Type ToType { get; }

		public TypeOverrideAttribute(Type fromType, Type toType)
		{
			FromType = fromType ?? throw new ArgumentNullException(nameof(fromType));
			ToType = toType ?? throw new ArgumentNullException(nameof(toType));
		}
	}
}