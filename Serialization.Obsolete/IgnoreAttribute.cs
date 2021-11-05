namespace Ecng.Serialization
{
	#region Using Directives

	using System;

	using Ecng.Reflection;

	#endregion

	[AttributeUsage(ReflectionHelper.Types | ReflectionHelper.Members, AllowMultiple = true)]
	public class IgnoreAttribute : Attribute
	{
		public string FieldName { get; set; }
	}
}