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
	using Ecng.Localization;

	/// <summary>
	/// Provides a collection of extension methods for components, attributes, and debugging.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Retrieves the display name for the given attribute provider using DisplayAttribute or DisplayNameAttribute.
		/// </summary>
		/// <param name="provider">The custom attribute provider.</param>
		/// <param name="defaultValue">The default value if no display name is found.</param>
		/// <returns>The display name.</returns>
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

		/// <summary>
		/// Retrieves the display name for the specified property descriptor using DisplayAttribute or DisplayNameAttribute.
		/// </summary>
		/// <param name="pd">The property descriptor.</param>
		/// <param name="defaultValue">The default value if no display name is found.</param>
		/// <returns>The display name.</returns>
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

		/// <summary>
		/// Retrieves the description for the given attribute provider using DisplayAttribute or DescriptionAttribute.
		/// </summary>
		/// <param name="provider">The custom attribute provider.</param>
		/// <param name="defaultValue">The default description if none is found.</param>
		/// <returns>The description string.</returns>
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

		/// <summary>
		/// Retrieves the category name for the given attribute provider using DisplayAttribute or CategoryAttribute.
		/// </summary>
		/// <param name="provider">The custom attribute provider.</param>
		/// <param name="defaultValue">The default category if none is found.</param>
		/// <returns>The category name.</returns>
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

		/// <summary>
		/// Retrieves the type name from the member information of the custom attribute provider.
		/// </summary>
		/// <param name="provider">The custom attribute provider.</param>
		/// <returns>The name of the type.</returns>
		private static string GetTypeName(this ICustomAttributeProvider provider)
		{
			return ((MemberInfo)provider).Name;
		}

		/// <summary>
		/// Retrieves the display name of the given object.
		/// If the object is an Enum, its field display name is returned; otherwise, it uses custom attribute providers or type name.
		/// </summary>
		/// <param name="value">The object for which to get the display name.</param>
		/// <returns>The display name.</returns>
		public static string GetDisplayName(this object value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			if (value is not Enum)
			{
				if (value is ICustomAttributeProvider provider)
					return provider.GetDisplayName();

				return value.GetType().GetDisplayName(value.ToString());
			}

			return value.GetFieldDisplayName();
		}

		/// <summary>
		/// Internal helper method to get a value from an enum field using the provided functions.
		/// </summary>
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

			// Bit mask value or external constant handling.
			if (fi is null)
				return getDefault(field);

			return func(fi);
		}

		/// <summary>
		/// Retrieves the display name for an enum field using its custom attributes.
		/// </summary>
		/// <param name="field">The enum field.</param>
		/// <returns>The display name associated with the field.</returns>
		public static string GetFieldDisplayName(this object field)
			=> Get(field, fi => fi.GetDisplayName(), f => f.ToString(), (s1, s2) => s1.IsEmpty() ? s2 : (s2.IsEmpty() ? s1 : $"{s1}, {s2}"));

		/// <summary>
		/// Retrieves the description for an enum field using the DisplayAttribute or DescriptionAttribute.
		/// </summary>
		/// <param name="field">The enum field.</param>
		/// <returns>The field's description or an empty string if none is provided.</returns>
		public static string GetFieldDescription(this object field)
			=> Get(field, fi => fi.GetAttribute<DisplayAttribute>()?.GetDescription(), f => null, (s1, s2) => s1.IsEmpty() ? s2 : (s2.IsEmpty() ? s1 : $"{s1}, {s2}")) ?? string.Empty;

		/// <summary>
		/// Retrieves the icon URI for an enum field based on its IconAttribute.
		/// </summary>
		/// <param name="field">The enum field.</param>
		/// <returns>The icon URI if available; otherwise, null.</returns>
		public static Uri GetFieldIcon(this object field)
			=> Get(field, fi =>
			{
				var attr = fi.GetAttribute<IconAttribute>();

				return
					attr is null ? null :
					attr.IsFullPath ? new Uri(attr.Icon, UriKind.Relative) : attr.Icon.GetResourceUrl(fi.ReflectedType);
			}, f => null, (s1, s2) => s1);

		/// <summary>
		/// Retrieves the documentation URL for a type based on its DocAttribute.
		/// </summary>
		/// <param name="type">The type to retrieve the doc URL for.</param>
		/// <returns>The documentation URL if available; otherwise, null.</returns>
		public static string GetDocUrl(this Type type)
			=> type.GetAttribute<DocAttribute>()?.DocUrl;

		/// <summary>
		/// Retrieves the icon URL for a type based on its IconAttribute.
		/// </summary>
		/// <param name="type">The type to retrieve the icon URL for.</param>
		/// <returns>The icon URI if available; otherwise, null.</returns>
		public static Uri GetIconUrl(this Type type)
		{
			var attr = type.GetAttribute<IconAttribute>();
			return attr is null ? null : (attr.IsFullPath ? new Uri(attr.Icon, UriKind.Relative) : attr.Icon.GetResourceUrl(type));
		}

		/// <summary>
		/// Retrieves the resource URL for the given resource name using the entry assembly.
		/// </summary>
		/// <param name="resName">The resource name.</param>
		/// <returns>The resource URI.</returns>
		public static Uri GetResourceUrl(this string resName)
		{
			return Assembly.GetEntryAssembly().GetResourceUrl(resName);
		}

		/// <summary>
		/// Retrieves the resource URL for the given resource name using the specified type's assembly.
		/// </summary>
		/// <param name="resName">The resource name.</param>
		/// <param name="type">The type whose assembly is used to obtain the resource.</param>
		/// <returns>The resource URI.</returns>
		public static Uri GetResourceUrl(this string resName, Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			return type.Assembly.GetResourceUrl(resName);
		}

		/// <summary>
		/// Internal helper to build a resource URL for a given assembly and resource name.
		/// </summary>
		/// <param name="assembly">The assembly containing the resource.</param>
		/// <param name="resName">The name of the resource.</param>
		/// <returns>The constructed resource URI.</returns>
		private static Uri GetResourceUrl(this Assembly assembly, string resName)
		{
			if (assembly is null)
				throw new ArgumentNullException(nameof(assembly));

			if (resName.IsEmpty())
				throw new ArgumentNullException(nameof(resName));

			var name = assembly.FullName;
			return new Uri($"/{name.Substring(0, name.IndexOf(','))};component/" + resName, UriKind.Relative);
		}

		/// <summary>
		/// Retrieves the items source items from an ItemsSourceAttribute instance.
		/// </summary>
		/// <param name="attr">The items source attribute.</param>
		/// <returns>An enumerable of IItemsSourceItem.</returns>
		public static IEnumerable<IItemsSourceItem> GetValues(this ItemsSourceAttribute attr)
		{
			if (attr is null)
				throw new ArgumentNullException(nameof(attr));

			return attr.Type.CreateInstance<IItemsSource>().Values;
		}

		/// <summary>
		/// Determines if the server credentials contain sufficient data for auto login.
		/// </summary>
		/// <param name="credentials">The server credentials.</param>
		/// <returns>True if auto login is possible; otherwise, false.</returns>
		public static bool CanAutoLogin(this ServerCredentials credentials)
		{
			if (credentials is null)
				throw new ArgumentNullException(nameof(credentials));

			return !credentials.Token.IsEmpty() || (!credentials.Email.IsEmptyOrWhiteSpace() && !credentials.Password.IsEmpty());
		}

		/// <summary>
		/// Attempts to retrieve the GUID associated with the specified control type using its GuidAttribute.
		/// </summary>
		/// <param name="controlType">The control type.</param>
		/// <returns>The GUID if available; otherwise, null.</returns>
		public static Guid? TryGetGuid(this Type controlType)
		{
			var guidAttr = controlType.GetAttribute<GuidAttribute>();
			return guidAttr is null ? null : controlType.GUID;
		}

		/// <summary>
		/// Converts the Guid to its 32-digit hexadecimal ("N") format.
		/// </summary>
		/// <param name="id">The Guid value.</param>
		/// <returns>The "N" formatted string.</returns>
		public static string ToN(this Guid id)
			=> id.ToString("N");

		/// <summary>
		/// Casts a PropertyDescriptorCollection to an enumerable of PropertyDescriptor.
		/// </summary>
		/// <param name="col">The collection of property descriptors.</param>
		/// <returns>An enumerable of PropertyDescriptor.</returns>
		public static IEnumerable<PropertyDescriptor> Typed(this PropertyDescriptorCollection col)
			=> col.Cast<PropertyDescriptor>();

		/// <summary>
		/// Retrieves basic properties (marked with BasicSettingAttribute) for the given instance, optionally retrieving nested properties recursively.
		/// </summary>
		/// <param name="instance">The object instance.</param>
		/// <param name="maxDepth">The maximum recursion depth.</param>
		/// <returns>An enumerable of tuples containing the property descriptor and its path.</returns>
		public static IEnumerable<(PropertyDescriptor prop, string path)> GetBasicProperties(this object instance, int maxDepth = 0)
		{
			static IEnumerable<(PropertyDescriptor, string)> getRecursive(object instance, int maxDepth, string prefix)
			{
				if (instance is null)
					throw new ArgumentNullException(nameof(instance));

				if (maxDepth < 0)
					throw new ArgumentOutOfRangeException(nameof(maxDepth), maxDepth, "Invalid value.".Localize());

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

		/// <summary>
		/// Retrieves the filtered property descriptors from a custom type descriptor based on the provided attributes.
		/// </summary>
		/// <param name="descriptor">The custom type descriptor.</param>
		/// <param name="attributes">The attributes to filter properties.</param>
		/// <returns>A filtered PropertyDescriptorCollection.</returns>
		public static PropertyDescriptorCollection GetFilteredProperties(this ICustomTypeDescriptor descriptor, Attribute[] attributes)
		{
			var allProperties = descriptor.GetProperties();

			if (attributes == null || attributes.Length == 0)
				return allProperties;

			return new([.. allProperties.Typed().Where(p => attributes.All(attr =>
			{
				var propAttr = p.Attributes[attr.GetType()];

				if (propAttr is null)
				{
					if (attr.GetType().GetField("Default", BindingFlags.Static | BindingFlags.Public)?.GetValue(null) is not Attribute defaultAttr)
						return false;

					propAttr = defaultAttr;
				}

				return attr.Match(propAttr);
			}))]);
		}

		/// <summary>
		/// Attempts to retrieve the default property descriptor from a collection based on the DefaultPropertyAttribute.
		/// </summary>
		/// <param name="properties">The property descriptor collection.</param>
		/// <param name="type">The type to check for default property.</param>
		/// <returns>The default PropertyDescriptor if found; otherwise, null.</returns>
		public static PropertyDescriptor TryGetDefault(this PropertyDescriptorCollection properties, Type type)
		{
			var attr = type.GetAttribute<DefaultPropertyAttribute>();

			if (attr != null)
				return properties.Find(attr.Name, ignoreCase: true);

			return null;
		}

		/// <summary>
		/// Attempts to retrieve the default event descriptor from a collection based on the DefaultEventAttribute.
		/// </summary>
		/// <param name="events">The event descriptor collection.</param>
		/// <param name="type">The type to check for default event.</param>
		/// <returns>The default EventDescriptor if found; otherwise, null.</returns>
		public static EventDescriptor TryGetDefault(this EventDescriptorCollection events, Type type)
		{
			var attr = type.GetAttribute<DefaultEventAttribute>();

			if (attr != null)
				return events.Find(attr.Name, ignoreCase: true);

			return null;
		}

		/// <summary>
		/// Sets the ExpandableObjectConverter attribute for the entity.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <param name="expandable">True to set as expandable; otherwise, false.</param>
		/// <returns>The updated attribute entity.</returns>
		public static TEntity SetExpandable<TEntity>(this TEntity entity, bool expandable)
			where TEntity : IAttributesEntity
			=> SetAttribute(entity, expandable, () => new TypeConverterAttribute(typeof(ExpandableObjectConverter)));

		/// <summary>
		/// Sets a custom editor attribute for the entity.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <param name="editor">The editor attribute to set.</param>
		/// <returns>The updated attribute entity.</returns>
		public static TEntity SetEditor<TEntity>(this TEntity entity, Attribute editor)
			where TEntity : IAttributesEntity
		{
			if (editor == null)
				throw new ArgumentNullException(nameof(editor));

			return SetAttribute(entity, true, () => editor);
		}

		/// <summary>
		/// Sets the DisplayAttribute for the entity with specified group, display name, description, and order.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <param name="groupName">The group/category name.</param>
		/// <param name="displayName">The display name.</param>
		/// <param name="description">The description text.</param>
		/// <param name="order">The display order.</param>
		/// <returns>The updated attribute entity.</returns>
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
		/// Sets the ReadOnlyAttribute for the entity.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <param name="readOnly">True to mark as read-only; otherwise, false.</param>
		/// <returns>The updated attribute entity.</returns>
		public static TEntity SetReadOnly<TEntity>(this TEntity entity, bool readOnly = true)
			where TEntity : IAttributesEntity
			=> SetAttribute(entity, readOnly, () => new ReadOnlyAttribute(true));

		/// <summary>
		/// Sets the BasicSettingAttribute for the entity.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <param name="isBasic">True to mark as basic; otherwise, false.</param>
		/// <returns>The updated attribute entity.</returns>
		public static TEntity SetBasic<TEntity>(this TEntity entity, bool isBasic = true)
			where TEntity : IAttributesEntity
			=> SetAttribute(entity, isBasic, () => new BasicSettingAttribute());

		/// <summary>
		/// Sets the BrowsableAttribute (non-browsable) for the entity.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <param name="nonBrowsable">True to mark as non-browsable; otherwise, false.</param>
		/// <returns>The updated attribute entity.</returns>
		public static TEntity SetNonBrowsable<TEntity>(this TEntity entity, bool nonBrowsable = true)
			where TEntity : IAttributesEntity
			=> SetAttribute(entity, nonBrowsable, () => new BrowsableAttribute(false));

		/// <summary>
		/// Sets or removes an attribute of type TAttribute for the entity based on the provided value.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <typeparam name="TAttribute">The type of attribute to set.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <param name="value">True to add the attribute; false to remove it.</param>
		/// <param name="create">A function to create the attribute instance.</param>
		/// <returns>The updated attribute entity.</returns>
		public static TEntity SetAttribute<TEntity, TAttribute>(this TEntity entity, bool value, Func<TAttribute> create)
			where TEntity : IAttributesEntity
			where TAttribute : Attribute
		{
			if (create is null)
				throw new ArgumentNullException(nameof(create));

			var attrs = entity.Attributes;

			if (typeof(TAttribute) == typeof(Attribute))
			{
				var attr = create();
				var type = attr.GetType();
				attrs.RemoveWhere(a => a.GetType().Is(type));

				if (value)
					attrs.Add(attr);
			}
			else
			{
				attrs.RemoveWhere(a => a is TAttribute);

				if (value)
					attrs.Add(create());
			}

			return entity;
		}

		/// <summary>
		/// Checks if the entity has a BasicSettingAttribute.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <returns>True if the entity is marked as basic; otherwise, false.</returns>
		public static bool IsBasic<TEntity>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> IsAny(entity, (BasicSettingAttribute a) => true);

		/// <summary>
		/// Checks if the entity is marked as read-only via a ReadOnlyAttribute.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <returns>True if the entity is read-only; otherwise, false.</returns>
		public static bool IsReadOnly<TEntity>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> IsAny(entity, (ReadOnlyAttribute a) => a.IsReadOnly);

		/// <summary>
		/// Checks if all BrowsableAttributes on the entity indicate it is browsable.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <returns>True if the entity is browsable; otherwise, false.</returns>
		public static bool IsBrowsable<TEntity>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> IsAll(entity, (BrowsableAttribute a) => a.Browsable);

		/// <summary>
		/// Retrieves the display name for the entity from its DisplayAttribute.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <returns>The display name if set; otherwise, null.</returns>
		public static string GetDisplayName<TEntity>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> entity.GetDisplay()?.Name;

		/// <summary>
		/// Retrieves the description for the entity from its DisplayAttribute.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <returns>The description if set; otherwise, null.</returns>
		public static string GetDescription<TEntity>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> entity.GetDisplay()?.Description;

		/// <summary>
		/// Retrieves the group name for the entity from its DisplayAttribute.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <returns>The group name if set; otherwise, null.</returns>
		public static string GetGroupName<TEntity>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> entity.GetDisplay()?.GroupName;

		/// <summary>
		/// Retrieves the DisplayAttribute from the entity.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <returns>The DisplayAttribute if exists; otherwise, null.</returns>
		public static DisplayAttribute GetDisplay<TEntity>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> entity.Attributes.OfType<DisplayAttribute>().FirstOrDefault();

		/// <summary>
		/// Helper method to determine if any attribute of type TAttribute on the entity satisfies the condition.
		/// </summary>
		private static bool IsAny<TEntity, TAttribute>(this TEntity entity, Func<TAttribute, bool> condition)
			where TEntity : IAttributesEntity
			=> Attrs<TEntity, TAttribute>(entity).Any(condition);

		/// <summary>
		/// Helper method to determine if all attributes of type TAttribute on the entity satisfy the condition.
		/// </summary>
		private static bool IsAll<TEntity, TAttribute>(this TEntity entity, Func<TAttribute, bool> condition)
			where TEntity : IAttributesEntity
			=> Attrs<TEntity, TAttribute>(entity).All(condition);

		/// <summary>
		/// Retrieves all attributes of the specified type TAttribute from the entity.
		/// </summary>
		private static IEnumerable<TAttribute> Attrs<TEntity, TAttribute>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> entity.Attributes.OfType<TAttribute>();

		/// <summary>
		/// Sets a RequiredAttribute as a validator for the entity.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <returns>The updated attribute entity.</returns>
		public static TEntity SetRequired<TEntity>(this TEntity entity)
			where TEntity : IAttributesEntity
			=> entity.SetValidator(new RequiredAttribute());

		/// <summary>
		/// Adds a custom validation attribute as a validator for the entity.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <param name="validator">The validation attribute to add.</param>
		/// <returns>The updated attribute entity.</returns>
		public static TEntity SetValidator<TEntity>(this TEntity entity, ValidationAttribute validator)
			where TEntity : IAttributesEntity
		{
			entity.Attributes.Add(validator);
			return entity;
		}

		/// <summary>
		/// Validates the given value against all ValidationAttribute validators on the entity.
		/// </summary>
		/// <typeparam name="TEntity">The type of the attribute entity.</typeparam>
		/// <param name="entity">The attribute entity.</param>
		/// <param name="value">The value to validate.</param>
		/// <returns>True if all validators deem the value valid; otherwise, false.</returns>
		public static bool IsValid<TEntity>(this TEntity entity, object value)
			where TEntity : IAttributesEntity
			=> entity.Attrs<TEntity, ValidationAttribute>().All(v => v.IsValid(value));

		/// <summary>
		/// Determines whether the debugger is currently waiting for input or output.
		/// </summary>
		/// <param name="debugger">The debugger instance.</param>
		/// <returns>True if the debugger is waiting; otherwise, false.</returns>
		public static bool IsWaiting(this IDebugger debugger)
		{
			if (debugger is null)
				throw new ArgumentNullException(nameof(debugger));

			return debugger.IsWaitingOnInput || debugger.IsWaitingOnOutput;
		}
	}
}