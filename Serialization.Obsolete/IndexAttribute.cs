namespace Ecng.Serialization
{
	using System;

	using Ecng.Reflection;

	[AttributeUsage(ReflectionHelper.Members | ReflectionHelper.Types)]
	public class IndexAttribute : Attribute
	{
		public string FieldName { get; set; }
	}
}