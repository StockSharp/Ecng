namespace Ecng.ComponentModel
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;

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

	public static class EnumDisplayNameAttributeExtensions
	{
		public static string GetDisplayName<T>(this T field)
		{
			var fieldInfo = typeof(T).GetField(field.ToString());

			var name = fieldInfo.Name;

			var attr = fieldInfo.GetAttribute<EnumDisplayNameAttribute>();
			if (attr != null && !attr.DisplayName.IsEmpty())
				name = attr.DisplayName;

			return name;
		}
	}
}