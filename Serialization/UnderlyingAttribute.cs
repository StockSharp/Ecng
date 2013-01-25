namespace Ecng.Serialization
{
	using System;

	using Ecng.Reflection;

	[AttributeUsage(ReflectionHelper.Members)]
	public class UnderlyingAttribute : Attribute
	{
	}
}