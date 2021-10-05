namespace Ecng.Net
{
	using System;

	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
	public class RestApiParamAttribute : Attribute
	{
		public string Name { get; set; }
		public bool IsRequired { get; set; }
	}
}