namespace Ecng.Serialization
{
	#region Using Directives

	using System;

    using Ecng.Reflection;

	#endregion

    [AttributeUsage(ReflectionHelper.Types)]
	public abstract class SchemaFactoryAttribute : Attribute
	{
		protected internal abstract SchemaFactory CreateFactory();
	}
}