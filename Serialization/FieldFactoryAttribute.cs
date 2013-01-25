namespace Ecng.Serialization
{
	using System;

	using Ecng.Common;
    using Ecng.Reflection;

    [AttributeUsage(ReflectionHelper.Members | ReflectionHelper.Types, AllowMultiple = false)]
	public abstract class FieldFactoryAttribute : OrderedAttribute
	{
		public abstract FieldFactory CreateFactory(Field field);
	}
}