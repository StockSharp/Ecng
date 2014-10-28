namespace Ecng.Localization.Attributes
{
	using System;
	using Ecng.ComponentModel;

	[AttributeUsageAttribute(AttributeTargets.Field)]
	public class EnumDisplayNameLocAttribute : EnumDisplayNameAttribute
	{
		public EnumDisplayNameLocAttribute(string resourceId) : base(LocalizedStringsBase.GetString(resourceId))
		{
		}
	}
}
