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
		public static IEnumerable<EntityProperty> GetEntityProperties(this Type type, Func<PropertyInfo, bool> filter = null)
		{
			return type.GetEntityProperties(null, filter);
		}

		public static IEnumerable<EntityProperty> GetEntityProperties(this Type type, EntityProperty parent, Func<PropertyInfo, bool> filter = null)
		{
			return type.GetEntityProperties(parent, new HashSet<Type>(), filter ?? (p => true));
		}

		private static IEnumerable<EntityProperty> GetEntityProperties(this Type type, EntityProperty parent, HashSet<Type> processed, Func<PropertyInfo, bool> filter)
		{
			if (processed.Contains(type))
				yield break;

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

				yield return prop;
			}

			processed.Remove(type);
		}

		public static Type GetPropType(this Type type, string name, Func<Type, string, Type> getVirtualProp = null)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			if (name is null)
				throw new ArgumentNullException(nameof(name));

			getVirtualProp ??= (t, n) => null;

			type = type.GetUnderlyingType() ?? type;

			foreach (var part in name.Split('.'))
			{
				var info = type.GetProperty(part);

				if (info is null)
				{
					var virtualPropType = getVirtualProp(type, part);

					if (virtualPropType is null)
						return null;

					type = virtualPropType;
				}
				else
					type = info.PropertyType;

				type = type.GetUnderlyingType() ?? type;
			}

			return type;
		}

		public static object GetPropValue(this object entity, string name, Func<object, string, object> getVirtualProp = null)
		{
			var value = entity;

			getVirtualProp ??= (t, n) => null;

			foreach (var part in name.Split('.'))
			{
				if (value is null)
					return null;

				var info = value.GetType().GetProperty(part);

				if (info is null)
				{
					var virtualPropValue = getVirtualProp(value, part);

					if (virtualPropValue is null)
						return null;

					value = virtualPropValue;
				}
				else
					value = info.GetValue(value, null);
			}

			return value;
		}
	}
}