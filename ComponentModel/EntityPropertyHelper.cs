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

			type = type.GetUnderlyingType() ?? type;

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

				var prop = new EntityProperty
				{
					Name = name,
					Parent = parent,
					DisplayName = pi.GetDisplayName(),
					Description = pi.GetDescription()
				};

				var propType = pi.PropertyType;

				if (!propType.IsPrimitive())
				{
					prop.Properties = GetEntityProperties(propType, prop, processed, filter);
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

			type = type.GetUnderlyingType() ?? type;

			foreach (var part in name.Split('.'))
			{
				var info = type.GetProperty(part);

				if (info == null)
					return null;

				type = info.PropertyType.GetUnderlyingType() ?? info.PropertyType;
			}

			return type;
		}

		public static object GetPropValue(this object entity, string name)
		{
			var value = entity;

			foreach (var part in name.Split('.'))
			{
				var info = value?.GetType().GetProperty(part);

				if (info == null || (info.PropertyType.IsNullable() && info.GetValue(value, null) == null))
					return null;

				value = info.GetValue(value, null);
			}

			return value;
		}
	}
}