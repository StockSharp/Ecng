namespace Ecng.ComponentModel
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.InteropServices;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

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

		public static IEnumerable<PropertyDescriptor> Typed(this PropertyDescriptorCollection col)
			=> col.Cast<PropertyDescriptor>();

		/// <summary>
		/// Get basic properties (properties with <see cref="BasicSettingAttribute"/>).
		/// </summary>
		/// <param name="instance">Object instance.</param>
		/// <param name="recursive">Find nested basic properties.</param>
		/// <returns>Basic properties</returns>
		public static IEnumerable<(PropertyDescriptor prop, string path)> GetBasicProperties(this object instance, int maxDepth = 0)
		{
			static IEnumerable<(PropertyDescriptor, string)> getRecursive(object instance, int maxDepth, string prefix)
			{
				if (instance is null)
					throw new ArgumentNullException(nameof(instance));

				if (maxDepth < 0)
					throw new ArgumentOutOfRangeException(nameof(maxDepth), maxDepth, "Invalid value.");

				var properties = TypeDescriptor.GetProperties(instance, [new BasicSettingAttribute()]).Typed();

				foreach (var property in properties)
				{
					var fullPath = $"{prefix}{property.Name}";

					var hasNested = false;

					if (maxDepth > 0 &&
						!property.PropertyType.IsSerializablePrimitive() &&
						property.GetValue(instance) is object nestedInstance)
					{
						foreach (var nestedProperty in getRecursive(nestedInstance, maxDepth - 1, $"{fullPath}."))
						{
							hasNested = true;
							yield return nestedProperty;
						}
					}

					if (!hasNested)
						yield return (property, fullPath);
				}
			}

			return getRecursive(instance, maxDepth, string.Empty);
		}

		public static PropertyDescriptorCollection GetFilteredProperties(this ICustomTypeDescriptor descriptor, Attribute[] attributes)
		{
			var allProperties = descriptor.GetProperties();

			if (attributes == null || attributes.Length == 0)
				return allProperties;

			return new(allProperties.Typed().Where(p => attributes.All(attr =>
			{
				var propAttr = p.Attributes[attr.GetType()];

				if (propAttr is null)
				{
					if (attr.GetType().GetField("Default", BindingFlags.Static | BindingFlags.Public)?.GetValue(null) is not Attribute defaultAttr)
						return false;

					propAttr = defaultAttr;
				}

				return attr.Match(propAttr);
			})).ToArray());
		}

		public static PropertyDescriptor TryGetDefault(this PropertyDescriptorCollection properties, Type type)
		{
			var attr = type.GetAttribute<DefaultPropertyAttribute>();

			if (attr != null)
				return properties.Find(attr.Name, ignoreCase: true);

			return null;
		}

		public static EventDescriptor TryGetDefault(this EventDescriptorCollection events, Type type)
		{
			var attr = type.GetAttribute<DefaultEventAttribute>();

			if (attr != null)
				return events.Find(attr.Name, ignoreCase: true);

			return null;
		}

		/// <summary>
		/// To set the <see cref="ExpandableObjectConverter"/> attribute for the diagram element parameter.
		/// </summary>
		/// <param name="expandable">Value.</param>
		/// <returns>The diagram element parameter.</returns>
		public static TEntity SetExpandable<TEntity>(this TEntity entity, bool expandable)
			where TEntity : IAttributesEntity
			=> SetAttribute(entity, expandable, () => new TypeConverterAttribute(typeof(ExpandableObjectConverter)));

		/// <summary>
		/// To add the attribute <see cref="Attribute"/> for the diagram element parameter.
		/// </summary>
		/// <param name="editor">Attribute.</param>
		/// <returns>The diagram element parameter.</returns>
		public static TEntity SetEditor<TEntity>(this TEntity entity, Attribute editor)
			where TEntity : IAttributesEntity
		{
			if (editor == null)
				throw new ArgumentNullException(nameof(editor));

			return SetAttribute(entity, true, () => editor);
		}

		/// <summary>
		/// To set the <see cref="DisplayAttribute"/> attribute for the diagram element parameter.
		/// </summary>
		/// <param name="groupName">The category of the diagram element parameter.</param>
		/// <param name="displayName">The display name.</param>
		/// <param name="description">The description of the diagram element parameter.</param>
		/// <param name="order">The property order.</param>
		/// <returns>The diagram element parameter.</returns>
		public static TEntity SetDisplay<TEntity>(this TEntity entity, string groupName, string displayName, string description, int order)
			where TEntity : IAttributesEntity
			=> SetAttribute(entity, true, () => new DisplayAttribute
			{
				Name = displayName,
				Description = description,
				GroupName = groupName,
				Order = order,
			});

		/// <summary>
		/// To set the <see cref="ReadOnlyAttribute"/> attribute for the diagram element parameter.
		/// </summary>
		/// <param name="readOnly">Read-only.</param>
		/// <returns>The diagram element parameter.</returns>
		public static TEntity SetReadOnly<TEntity>(this TEntity entity, bool readOnly = true)
			where TEntity : IAttributesEntity
			=> SetAttribute(entity, readOnly, () => new ReadOnlyAttribute(true));

		/// <summary>
		/// To set the <see cref="BasicSettingAttribute"/> attribute for the diagram element parameter.
		/// </summary>
		/// <param name="isBasic">Is basic parameter.</param>
		/// <returns>The diagram element parameter.</returns>
		public static TEntity SetBasic<TEntity>(this TEntity entity, bool isBasic = true)
			where TEntity : IAttributesEntity
			=> SetAttribute(entity, isBasic, () => new BasicSettingAttribute());

		/// <summary>
		/// To set the <see cref="BrowsableAttribute"/> attribute for the diagram element parameter.
		/// </summary>
		/// <param name="nonBrowsable">Hidden parameter.</param>
		/// <returns>The diagram element parameter.</returns>
		public static TEntity SetNonBrowsable<TEntity>(this TEntity entity, bool nonBrowsable = true)
			where TEntity : IAttributesEntity
			=> SetAttribute(entity, nonBrowsable, () => new BrowsableAttribute(false));

		public static TEntity SetAttribute<TEntity, TAttribute>(this TEntity entity, bool value, Func<TAttribute> create)
			where TEntity : IAttributesEntity
			where TAttribute : Attribute
		{
			if (create is null)
				throw new ArgumentNullException(nameof(create));

			entity.Attributes.RemoveWhere(a => a is TAttribute);

			if (value)
				entity.Attributes.Add(create());

			return entity;
		}

		public static bool IsReadOnly<TEntity>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> IsAny(entity, (ReadOnlyAttribute a) => a.IsReadOnly);

		public static bool IsBrowsable<TEntity>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> IsAll(entity, (BrowsableAttribute a) => a.Browsable);

		public static string GetDisplayName<TEntity>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> entity.GetDisplay()?.Name;

		public static string GetDescription<TEntity>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> entity.GetDisplay()?.Description;

		public static string GetGroupName<TEntity>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> entity.GetDisplay()?.GroupName;

		public static DisplayAttribute GetDisplay<TEntity>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> entity.Attributes.OfType<DisplayAttribute>().FirstOrDefault();

		private static bool IsAny<TEntity, TAttribute>(this TEntity entity, Func<TAttribute, bool> condition)
			where TEntity : IAttributesEntity
			=> Attrs<TEntity, TAttribute>(entity).Any(condition);

		private static bool IsAll<TEntity, TAttribute>(this TEntity entity, Func<TAttribute, bool> condition)
			where TEntity : IAttributesEntity
			=> Attrs<TEntity, TAttribute>(entity).All(condition);

		private static IEnumerable<TAttribute> Attrs<TEntity, TAttribute>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> entity.Attributes.OfType<TAttribute>();
	}
}