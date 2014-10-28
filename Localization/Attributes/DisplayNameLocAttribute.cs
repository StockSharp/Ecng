namespace Ecng.Localization.Attributes
{
	using System;
	using System.ComponentModel;

	[AttributeUsageAttribute(AttributeTargets.Class|AttributeTargets.Method|AttributeTargets.Property|AttributeTargets.Event)]
	public class DisplayNameLocAttribute : DisplayNameAttribute
	{
		public DisplayNameLocAttribute(string resourceId) : base(LocalizedStringsBase.GetString(resourceId))
		{
		}
	}
}
