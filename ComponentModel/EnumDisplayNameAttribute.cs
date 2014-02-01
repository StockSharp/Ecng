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
		public static string GetDisplayName(this object field)
		{
			if (field == null)
				throw new ArgumentNullException("field");

			if (!(field is Enum))
				throw new ArgumentException("field");

			var fieldInfo = field.GetType().GetField(field.ToString());

			if (fieldInfo == null)
				throw new ArgumentException(field.ToString(), "field");

			var name = fieldInfo.Name;

			var attr = fieldInfo.GetAttribute<EnumDisplayNameAttribute>();
			if (attr != null && !attr.DisplayName.IsEmpty())
				name = attr.DisplayName;

			return name;
		}
	}
}