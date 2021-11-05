namespace Ecng.Serialization
{
	using System;

	using Ecng.Reflection;

	[Obsolete("NullableAttribute no more required.")]
	[AttributeUsage(ReflectionHelper.Members)]
	public class NullableAttribute : Attribute
	{
	}
}