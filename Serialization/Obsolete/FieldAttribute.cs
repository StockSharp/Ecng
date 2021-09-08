namespace Ecng.Serialization
{
	#region Using Directives

	using System;

	using Ecng.Common;
	using Ecng.Reflection;

	#endregion

	[AttributeUsage(ReflectionHelper.Members)]
	public class FieldAttribute : NameAttribute
	{
		public FieldAttribute(string name)
			: base(name)
		{
		}

		public bool ReadOnly { get; set; }
	}
}