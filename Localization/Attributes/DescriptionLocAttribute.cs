namespace Ecng.Localization.Attributes
{
	using System;
	using System.ComponentModel;

	[AttributeUsage(AttributeTargets.All)]
	public class DescriptionLocAttribute : DescriptionAttribute
	{
		public DescriptionLocAttribute(string resourceId) : base(LocalizedStringsBase.GetString(resourceId))
		{
		}
	}
}
