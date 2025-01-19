namespace Ecng.ComponentModel
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.InteropServices;

	using Ecng.Common;

	public static class Extensions
	{
		public static string GetDisplayName(this ICustomAttributeProvider provider, string defaultValue = null)
		{
			if (provider is Assembly asm)
				return asm.GetAttribute<AssemblyTitleAttribute>()?.Title;

			var dpAttr = provider.GetAttribute<DisplayAttribute>();

			if (dpAttr?.Name is null)
			{
				var nameAttr = provider.GetAttribute<DisplayNameAttribute>();
				return nameAttr is null ? defaultValue ?? provider.GetTypeName() : nameAttr.DisplayName;
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

			if (dpAttr?.Description is null)
			{
				var descrAttr = provider.GetAttribute<DescriptionAttribute>();
				return descrAttr is null ? defaultValue ?? provider.GetTypeName() : descrAttr.Description;
			}

			return dpAttr.GetDescription();
		}

		public static string GetCategory(this ICustomAttributeProvider provider, string defaultValue = null)
		{
			var dpAttr = provider.GetAttribute<DisplayAttribute>();

			if (dpAttr?.GroupName is null)
			{
				var categoryAttr = provider.GetAttribute<CategoryAttribute>();
				return categoryAttr is null ? defaultValue ?? provider.GetTypeName() : categoryAttr.Category;
			}

			return dpAttr.GetGroupName();
		}

		private static string GetTypeName(this ICustomAttributeProvider provider)
		{
			return ((MemberInfo)provider).Name;
		}

		public static string GetDisplayName(this object value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			if (value is not Enum)
			{
				if (value is ICustomAttributeProvider provider)
					return provider.GetDisplayName();

				return value.GetType().GetDisplayName(value.ToString());
				//throw new ArgumentException(str, nameof(value));
			}

			return value.GetFieldDisplayName();
		}

		private static TValue Get<TValue>(object field, Func<FieldInfo, TValue> func, Func<object, TValue> getDefault, Func<TValue, TValue, TValue> aggregate, bool canSplit = true)
		{
			if (field is null)			throw new ArgumentNullException(nameof(field));
			if (func is null)			throw new ArgumentNullException(nameof(func));
			if (getDefault is null)		throw new ArgumentNullException(nameof(getDefault));
			if (aggregate is null)		throw new ArgumentNullException(nameof(aggregate));

			if (field is not Enum)
				throw new ArgumentException($"{field}", nameof(field));

			var type = field.GetType();

			if (canSplit && type.IsFlags())
			{
				var parts = field.SplitMask2().Where(f => f.To<long>() != default).ToArray();

				if (parts.Length > 1)
					return parts.Select(p => Get(p, func, getDefault, (_, _) => throw new NotSupportedException(), false)).Aggregate(aggregate);
			}

			var fi = type.GetField(field.ToString());

			// bit mask value or external constant
			if (fi is null)
				return getDefault(field);

			return func(fi);
		}

		public static string GetFieldDisplayName(this object field)
			=> Get(field, fi => fi.GetDisplayName(), f => f.ToString(), (s1, s2) => s1.IsEmpty() ? s2 : (s2.IsEmpty() ? s1 : $"{s1}, {s2}"));

		public static string GetFieldDescription(this object field)
			=> Get(field, fi => fi.GetAttribute<DisplayAttribute>()?.GetDescription(), f => null, (s1, s2) => s1.IsEmpty() ? s2 : (s2.IsEmpty() ? s1 : $"{s1}, {s2}")) ?? string.Empty;

		public static Uri GetFieldIcon(this object field)
			=> Get(field, fi =>
			{
				var attr = fi.GetAttribute<IconAttribute>();

				return
					attr is null ? null :
					attr.IsFullPath ? new Uri(attr.Icon, UriKind.Relative) : attr.Icon.GetResourceUrl(fi.ReflectedType);
			}, f => null, (s1, s2) => s1);

		public static string GetDocUrl(this Type type)
			=> type.GetAttribute<DocAttribute>()?.DocUrl;

		public static Uri GetIconUrl(this Type type)
		{
			var attr = type.GetAttribute<IconAttribute>();
			return attr is null ? null : (attr.IsFullPath ? new Uri(attr.Icon, UriKind.Relative) : attr.Icon.GetResourceUrl(type));
		}

		public static Uri GetResourceUrl(this string resName)
		{
			return Assembly.GetEntryAssembly().GetResourceUrl(resName);
		}

		public static Uri GetResourceUrl(this string resName, Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			return type.Assembly.GetResourceUrl(resName);
		}

		private static Uri GetResourceUrl(this Assembly assembly, string resName)
		{
			if (assembly is null)
				throw new ArgumentNullException(nameof(assembly));

			if (resName.IsEmpty())
				throw new ArgumentNullException(nameof(resName));

			var name = assembly.FullName;
			return new Uri($"/{name.Substring(0, name.IndexOf(','))};component/" + resName, UriKind.Relative);
		}

		public static IEnumerable<IItemsSourceItem> GetValues(this ItemsSourceAttribute attr)
		{
			if (attr is null)
				throw new ArgumentNullException(nameof(attr));

			return attr.Type.CreateInstance<IItemsSource>().Values;
		}

		/// <summary>
		/// Determines the <paramref name="credentials"/> contains necessary data for auto login.
		/// </summary>
		/// <param name="credentials"><see cref="ServerCredentials"/></param>
		/// <returns>Check result.</returns>
		public static bool CanAutoLogin(this ServerCredentials credentials)
		{
			if (credentials is null)
				throw new ArgumentNullException(nameof(credentials));

			return !credentials.Token.IsEmpty() || (!credentials.Email.IsEmptyOrWhiteSpace() && !credentials.Password.IsEmpty());
		}

		public static Guid? TryGetGuid(this Type controlType)
		{
			var guidAttr = controlType.GetAttribute<GuidAttribute>();
			return guidAttr is null ? null : controlType.GUID;
		}

		public static string ToN(this Guid id)
			=> id.ToString("N");

		public static bool IsPythonObject(this object obj)
		{
			if (obj is null)
				throw new ArgumentNullException(nameof(obj));

			return obj.GetType().IsPythonType();
		}

		public static bool IsPythonType(this Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			return type.FullName?.StartsWith("IronPython") ?? false;
		}

		public static IEnumerable<PropertyDescriptor> Typed(this PropertyDescriptorCollection col)
			=> col.Cast<PropertyDescriptor>();
	}
}