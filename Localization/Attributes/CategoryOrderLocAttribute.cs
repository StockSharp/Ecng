namespace Ecng.Localization.Attributes
{
	using System;
	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
	public class CategoryOrderLocAttribute : CategoryOrderAttribute
	{
		public CategoryOrderLocAttribute(string resourceId, int order) : base(LocalizedStringsBase.GetString(resourceId), order)
		{
		}
	}
}
