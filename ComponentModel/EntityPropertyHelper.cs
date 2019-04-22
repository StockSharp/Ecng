namespace Ecng.ComponentModel
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Reflection;

	public static class EntityPropertyHelper
	{
		public static List<EntityProperty> GetEntityProperties(this Type type, Func<PropertyInfo, bool> filter = null)
		{
			return type.GetEntityProperties(null, filter);
		}

		public static List<EntityProperty> GetEntityProperties(this Type type, EntityProperty parent, Func<PropertyInfo, bool> filter = null)
		{
			return type.GetEntityProperties(parent, new HashSet<Type>(), filter ?? (p => true));
		}

		private static List<EntityProperty> GetEntityProperties(this Type type, EntityProperty parent, HashSet<Type> processed, Func<PropertyInfo, bool> filter)
		{
			var properties = new List<EntityProperty>();

			if (processed.Contains(type))
				return properties;

			var propertyInfos = type
	            .GetMembers<PropertyInfo>(BindingFlags.Public | BindingFlags.Instance)
	            .Where(filter);

			var names = new HashSet<string>();

			processed.Add(type);

			foreach (var pi in propertyInfos)
			{
				var name = (parent != null ? parent.Name + "." : string.Empty) + pi.Name;

				if (!names.Add(name))
					continue;

				var displayName = pi.GetDisplayName();

				var prop = new EntityProperty
				{
					Name = name,
					Parent = parent,
					DisplayName = displayName,
				};

				if (!pi.PropertyType.IsPrimitive() && !pi.PropertyType.IsNullable())
				{
					prop.Properties = GetEntityProperties(pi.PropertyType, prop, processed, filter);
				}

				properties.Add(prop);
			}

			processed.Remove(type);

			return properties;
		}

		public static Type GetPropType(this Type type, string name)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (name == null)
				throw new ArgumentNullException(nameof(name));

			var value = type;

			foreach (var part in name.Split('.'))
			{
				var info = value.GetProperty(part);

				if (info == null)
					return null;

				value = info.PropertyType;
			}

			return value;
		}

		public static object GetPropValue(this object entity, string name)
		{
			var value = entity;

			foreach (var part in name.Split('.'))
			{
				var info = value?.GetType().GetProperty(part);

				if (info == null)
					return null;

				value = info.GetValue(value, null);
			}

			return value;
		}
	}
}