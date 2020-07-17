namespace Ecng.ComponentModel
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Reflection;

	public static class Extensions
	{
		public static string GetDisplayName(this ICustomAttributeProvider provider, string defaultValue = null)
		{
			if (provider is Assembly asm)
				return asm.GetAttribute<AssemblyTitleAttribute>()?.Title;

			var dpAttr = provider.GetAttribute<DisplayAttribute>();

			if (dpAttr?.Name == null)
			{
				var nameAttr = provider.GetAttribute<DisplayNameAttribute>();
				return nameAttr == null ? defaultValue ?? provider.GetTypeName() : nameAttr.DisplayName;
			}

			return dpAttr.GetName();
		}

		public static string GetDisplayName(this PropertyDescriptor pd, string defaultValue = null)
		{
			foreach(var a in pd.Attributes)
			{
				switch (a)
				{
					case DisplayAttribute da:
						return da.GetName();
					case DisplayNameAttribute dna:
						return dna.DisplayName;
				}
			}

			return defaultValue ?? pd.PropertyType.Name;
		}

		public static string GetDescription(this ICustomAttributeProvider provider, string defaultValue = null)
		{
			if (provider is Assembly asm)
				return asm.GetAttribute<AssemblyDescriptionAttribute>()?.Description;

			var dpAttr = provider.GetAttribute<DisplayAttribute>();

			if (dpAttr?.Description == null)
			{
				var descrAttr = provider.GetAttribute<DescriptionAttribute>();
				return descrAttr == null ? defaultValue ?? provider.GetTypeName() : descrAttr.Description;
			}

			return dpAttr.GetDescription();
		}

		public static string GetCategory(this ICustomAttributeProvider provider, string defaultValue = null)
		{
			var dpAttr = provider.GetAttribute<DisplayAttribute>();

			if (dpAttr?.GroupName == null)
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

		public static string GetDisplayName(this object value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var str = value.ToString();
			var type = value.GetType();

			if (!(value is Enum))
			{
				if (value is ICustomAttributeProvider provider)
					return provider.GetDisplayName();

				return type.GetDisplayName(str);
				//throw new ArgumentException(str, nameof(value));
			}

			// Enum field

			var fieldInfo = type.GetField(str);

			if (fieldInfo == null)
			{
				return str;
				//throw new ArgumentException(field.ToString(), nameof(field));
			}

			return fieldInfo.GetDisplayName();
		}

		public static string GetFieldDisplayName<TField>(this TField field)
		{
			return field.GetType().GetField(field.ToString()).GetDisplayName();
		}

		public static string GetFieldDescription<TField>(this TField field)
		{
			return field.GetType().GetField(field.ToString()).GetAttribute<DisplayAttribute>()?.GetDescription();
		}

		public static Uri GetFieldIcon<TField>(this TField field)
		{
			var type = field.GetType();
			var attr = type.GetField(field.ToString()).GetAttribute<IconAttribute>();
			return
				attr == null ? null :
				attr.IsFullPath ? new Uri(attr.Icon, UriKind.Relative) : attr.Icon.GetResourceUrl(type);
		}

		public static string GetDocUrl(this Type type)
		{
			var attr = type.GetAttribute<DocAttribute>();
			return attr?.DocUrl;
		}

		public static Uri GetIconUrl(this Type type)
		{
			var attr = type.GetAttribute<IconAttribute>();
			return attr == null ? null : (attr.IsFullPath ? new Uri(attr.Icon, UriKind.Relative) : attr.Icon.GetResourceUrl(type));
		}

		public static Uri GetResourceUrl(this string resName)
		{
			return Assembly.GetEntryAssembly().GetResourceUrl(resName);
		}

		public static Uri GetResourceUrl(this string resName, Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			return type.Assembly.GetResourceUrl(resName);
		}

		private static Uri GetResourceUrl(this Assembly assembly, string resName)
		{
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));

			if (resName.IsEmpty())
				throw new ArgumentNullException(nameof(resName));

			var name = assembly.FullName;
			return new Uri("/" + name.Substring(0, name.IndexOf(',')) + ";component/" + resName, UriKind.Relative);
		}

		public static IEnumerable<IItemsSourceItem> GetValues(this ItemsSourceAttribute attr)
		{
			if (attr is null)
				throw new ArgumentNullException(nameof(attr));

			return attr.Type.CreateInstance<IItemsSource>().Values;
		}
	}
}