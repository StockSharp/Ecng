namespace Ecng.ComponentModel
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Reflection;

	using Ecng.Common;

	public static class Extensions
	{
		public static string GetDisplayName(this ICustomAttributeProvider provider, string defaultValue = null)
		{
			var dpAttr = provider.GetAttribute<DisplayAttribute>();

			if (dpAttr == null)
			{
				var nameAttr = provider.GetAttribute<DisplayNameAttribute>();
				return nameAttr == null ? defaultValue ?? provider.GetTypeName() : nameAttr.DisplayName;
			}

			return dpAttr.GetName();
		}

		public static string GetDescription(this ICustomAttributeProvider provider, string defaultValue = null)
		{
			var dpAttr = provider.GetAttribute<DisplayAttribute>();

			if (dpAttr == null)
			{
				var descrAttr = provider.GetAttribute<DescriptionAttribute>();
				return descrAttr == null ? defaultValue ?? provider.GetTypeName() : descrAttr.Description;
			}

			return dpAttr.GetDescription();
		}

		public static string GetCategory(this ICustomAttributeProvider provider, string defaultValue = null)
		{
			var dpAttr = provider.GetAttribute<DisplayAttribute>();

			if (dpAttr == null)
			{
				var categoryAttr = provider.GetAttribute<CategoryAttribute>();
				return categoryAttr == null ? defaultValue ?? provider.GetTypeName() : categoryAttr.Category;
			}

			return dpAttr.GetGroupName();
		}

		private static string GetTypeName(this ICustomAttributeProvider provider)
		{
			return ((MemberInfo)provider).Name;
		}

		public static string GetDisplayName(this object field)
		{
			if (field == null)
				throw new ArgumentNullException(nameof(field));

			if (!(field is Enum))
				throw new ArgumentException("field");

			var fieldInfo = field.GetType().GetField(field.ToString());

			if (fieldInfo == null)
				throw new ArgumentException(field.ToString(), nameof(field));

			var name = fieldInfo.Name;

			var attr = fieldInfo.GetAttribute<EnumDisplayNameAttribute>();
			if (attr != null/* && !attr.DisplayName.IsEmpty()*/)
				name = attr.DisplayName;

			return name;
		}

		public static string GetDocUrl(this Type type)
		{
			var attr = type.GetAttribute<DocAttribute>();
			return attr?.DocUrl;
		}
	}
}