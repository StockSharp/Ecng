namespace Ecng.ComponentModel
{
	using System;
	using System.ComponentModel;

	using Ecng.Localization;

	[AttributeUsage(AttributeTargets.Field)]
	[Obsolete("Use DisplayAttribute instead.")]
	public class EnumDisplayNameAttribute : DisplayNameAttribute
	{
		//public EnumDisplayNameAttribute()
		//{
		//}

		public EnumDisplayNameAttribute(string displayName)
			: this(displayName, false)
		{
		}

		public EnumDisplayNameAttribute(string displayName, bool localize)
			: base(localize ? displayName.Translate() : displayName)
		{
		}
	}
}