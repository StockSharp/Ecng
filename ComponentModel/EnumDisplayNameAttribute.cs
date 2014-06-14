namespace Ecng.ComponentModel
{
	using System;
	using System.ComponentModel;

#if SILVERLIGHT
	[AttributeUsage(AttributeTargets.Event | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class)]
	public class DisplayNameAttribute : Attribute
	{
		public DisplayNameAttribute()
		{
		}

		public DisplayNameAttribute(string displayName)
		{
			DisplayName = displayName;
		}

		public string DisplayName { get; set; }
	}
#endif

	[AttributeUsage(AttributeTargets.Field)]
	public class EnumDisplayNameAttribute : DisplayNameAttribute
	{
		public EnumDisplayNameAttribute()
		{
		}

		public EnumDisplayNameAttribute(string displayName)
			: base(displayName)
		{
		}
	}
}