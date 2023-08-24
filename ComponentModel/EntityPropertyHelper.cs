namespace Ecng.ComponentModel
{
	using System;
	using System.Collections;
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

			foreach (var p in name.Split('.'))
			{
				var part = p;

				var brIdx = part.IndexOf('[');
				var index = brIdx != -1 ? part.Substring(brIdx + 1, part.IndexOf(']') - (brIdx + 1)) : null;

				if (index is not null)
					part = part.Substring(0, brIdx);

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

				if (type is not null && index is not null)
				{
					if (type.Is<IList>())
					{
						type = type.GetItemType();
					}
					else if (type.Is<IDictionary>())
					{
						type = typeof(object);
					}
					else if (type.GetGenericType(typeof(IDictionary<,>)) is not null)
					{
						type = type.GetGenericArguments()[1];
					}
					else
						return null;
				}
			}

			return type;
		}

		public static object GetPropValue(this object entity, string name, Func<object, string, object> getVirtualProp = null)
		{
			var value = entity;

			getVirtualProp ??= (t, n) => null;

			foreach (var p in name.Split('.'))
			{
				var part = p;

				if (value is null)
					return null;

				var brIdx = part.IndexOf('[');
				var index = brIdx != -1 ? part.Substring(brIdx + 1, part.IndexOf(']') - (brIdx + 1)) : null;

				if (index is not null)
					part = part.Substring(0, brIdx);

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

				if (value is not null && index is not null)
				{
					if (value is IList list)
					{
						var i = index.To<int>();

						if (i < 0 || i >= list.Count)
							return null;

						value = list[i];
					}
					else if (value is IDictionary dict)
					{
						object key = index;

						var type = dict.GetType();

						if (type.IsGenericType)
						{
							var argTypes = type.GetGenericArguments();
							key = key.To(argTypes[0]);
						}

						if (!dict.Contains(key))
							return null;

						value = dict[key];
					}
					else
						return null;
				}
			}

			return value;
		}
	}
}