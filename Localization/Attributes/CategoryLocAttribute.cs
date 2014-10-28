namespace Ecng.Localization.Attributes
{
	using System;
	using System.ComponentModel;

	[AttributeUsageAttribute(AttributeTargets.All)]
	public class CategoryLocAttribute : CategoryAttribute
	{
		public CategoryLocAttribute(string resourceId) : base(LocalizedStringsBase.GetString(resourceId))
		{
		}
	}
}
