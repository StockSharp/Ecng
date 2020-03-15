namespace Ecng.ComponentModel
{
	using System;
	using System.Collections;
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
				switch (a) {
					case DisplayAttribute da:
						return da.GetName();
					case DisplayNameAttribute dna:
						return dna.DisplayName;
				}

			return defaultValue ?? pd.PropertyType.Name;
		}

		public static string GetDescription(this ICustomAttributeProvider provider, string defaultValue = null)
		{
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

		public static string GetDisplayName(this object field)
		{
			if (field == null)
				throw new ArgumentNullException(nameof(field));

			var fieldName = field.ToString();
			var fieldType = field.GetType();

			if (!(field is Enum))
				throw new ArgumentException(fieldName, nameof(field));

			var fieldInfo = fieldType.GetField(fieldName);

			if (fieldInfo == null)
			{
				return fieldName;
				//throw new ArgumentException(field.ToString(), nameof(field));
			}

			return fieldInfo.GetDisplayName();
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

		public static IEnumerable<Tuple<string, object>> GetValues(this ItemsSourceAttribute attr)
		{
			if (attr is null)
				throw new ArgumentNullException(nameof(attr));

			var prop = attr.Type.GetMember<PropertyInfo>(nameof(IItemsSource.Values));

			var values = (IEnumerable)prop.GetValue(attr.Type.CreateInstance<object>());

			if (values == null)
				yield break;

			foreach (dynamic item in values)
				yield return Tuple.Create((string)item.Item1, (object)item.Item2);
		}
	}
}