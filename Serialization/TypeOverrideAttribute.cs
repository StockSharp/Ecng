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
			if (fromType == null)
				throw new ArgumentNullException(nameof(fromType));

			if (toType == null)
				throw new ArgumentNullException(nameof(toType));

			FromType = fromType;
			ToType = toType;
		}
	}
}