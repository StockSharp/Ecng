namespace Ecng.ComponentModel;

using Ecng.Reflection;

/// <summary>
/// Provides helper methods for retrieving entity properties from a specified type.
/// </summary>
public static class EntityPropertyHelper
{
	/// <summary>
	/// Retrieves the entity properties for the specified type.
	/// </summary>
	/// <param name="type">The type to retrieve entity properties from.</param>
	/// <param name="filter">An optional filter to apply to the property information.</param>
	/// <returns>An enumerable collection of <see cref="EntityProperty"/>.</returns>
	public static IEnumerable<EntityProperty> GetEntityProperties(this Type type, Func<PropertyInfo, bool> filter = null)
	{
		return type.GetEntityProperties(null, filter);
	}

	/// <summary>
	/// Retrieves the entity properties for the specified type with a specified parent entity property.
	/// </summary>
	/// <param name="type">The type to retrieve entity properties from.</param>
	/// <param name="parent">The parent <see cref="EntityProperty"/> in the hierarchy.</param>
	/// <param name="filter">An optional filter to apply to the property information.</param>
	/// <returns>An enumerable collection of <see cref="EntityProperty"/>.</returns>
	public static IEnumerable<EntityProperty> GetEntityProperties(this Type type, EntityProperty parent, Func<PropertyInfo, bool> filter = null)
	{
		return type.GetEntityProperties(parent, [], filter ?? (p => true));
	}

	private static IEnumerable<EntityProperty> GetEntityProperties(this Type type, EntityProperty parent, HashSet<Type> processed, Func<PropertyInfo, bool> filter)
	{
		type = type.GetUnderlyingType() ?? type;

		if (processed.Contains(type))
			yield break;

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

			var propType = pi.PropertyType;
			var underlyingPropType = propType.GetUnderlyingType() ?? propType;

			if (!underlyingPropType.IsPrimitive() && processed.Contains(underlyingPropType))
				continue;

			var prop = new EntityProperty(name, pi.GetDisplayName(), pi.GetDescription(), pi.PropertyType, parent);

			if (!underlyingPropType.IsPrimitive())
			{
				prop.Properties = [.. GetEntityProperties(propType, prop, processed, filter)];
			}

			yield return prop;
		}

		processed.Remove(type);
	}

	/// <summary>
	/// Gets the type of a nested property specified by its name.
	/// </summary>
	/// <param name="type">The type that contains the property.</param>
	/// <param name="name">The dot-separated name of the property.</param>
	/// <param name="getVirtualProp">An optional function to retrieve the type of a virtual property.</param>
	/// <returns>The <see cref="Type"/> of the property if found; otherwise, null.</returns>
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

	// A property a type implements explicitly for an interface is absent from the type's own properties,
	// while GetPropType finds it on the interface the caller named. Reading it back therefore starts from
	// the declared type when the caller supplied one, and only then falls back to the instance.
	private static PropertyInfo FindProperty(Type declaredType, Type actualType, string name)
	{
		var info = declaredType?.GetProperty(name);

		if (info is not null)
			return info;

		info = actualType.GetProperty(name);

		if (info is not null)
			return info;

		PropertyInfo found = null;

		foreach (var iface in actualType.GetInterfaces())
		{
			var candidate = iface.GetProperty(name);

			if (candidate is null)
				continue;

			// Two interfaces can give one name two meanings -- ExecutionMessage carries both
			// ITickTradeMessage.Price and IOrderMessage.Price -- and nothing here can choose between
			// them, so an ambiguous name is left unread rather than answered by whichever came first.
			if (found is not null)
				return null;

			found = candidate;
		}

		return found;
	}

	/// <summary>
	/// Gets the value of a nested property from an object.
	/// </summary>
	/// <param name="entity">The object to retrieve the value from.</param>
	/// <param name="name">The dot-separated name of the property.</param>
	/// <param name="getVirtualProp">An optional function to retrieve the value of a virtual property.</param>
	/// <param name="vars">An optional dictionary of variables for indexing.</param>
	/// <returns>The value of the property if found; otherwise, null.</returns>
	public static object GetPropValue(this object entity, string name, Func<object, string, object> getVirtualProp = null, IDictionary<string, object> vars = null)
		=> entity.GetPropValue(null, name, getVirtualProp, vars);

	/// <summary>
	/// Gets the value of a nested property from an object, reading it by the type the caller declares it under.
	/// </summary>
	/// <param name="entity">The object to retrieve the value from.</param>
	/// <param name="declaredType">The type the name was resolved against, or <see langword="null"/> to resolve it against the object itself. Naming the interface is what makes a member implemented explicitly for it readable, and what settles a name two interfaces both declare.</param>
	/// <param name="name">The dot-separated name of the property.</param>
	/// <param name="getVirtualProp">An optional function to retrieve the value of a virtual property.</param>
	/// <param name="vars">An optional dictionary of variables for indexing.</param>
	/// <returns>The value of the property if found; otherwise, null.</returns>
	public static object GetPropValue(this object entity, Type declaredType, string name, Func<object, string, object> getVirtualProp = null, IDictionary<string, object> vars = null)
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

			var info = FindProperty(declaredType, value.GetType(), part);

			declaredType = info?.PropertyType;

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
					if (!int.TryParse(index, out var i))
					{
						if (vars is null)
							throw new InvalidOperationException($"{index} is not index.");

						i = vars[index].To<int>();
					}

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

	/// <summary>
	/// Retrieves variable names from a nested property path for the specified type.
	/// </summary>
	/// <param name="type">The type that contains the property.</param>
	/// <param name="name">The dot-separated property path.</param>
	/// <param name="getVirtualProp">An optional function to retrieve the type of a virtual property.</param>
	/// <returns>An enumerable collection of variable names present in the property path.</returns>
	public static IEnumerable<string> GetVars(this Type type, string name, Func<Type, string, Type> getVirtualProp = null)
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
					yield break;

				type = virtualPropType;
			}
			else
				type = info.PropertyType;

			type = type.GetUnderlyingType() ?? type;

			if (type is not null && index is not null)
			{
				if (type.Is<IList>())
				{
					if (!int.TryParse(index, out _))
						yield return index;
				}
			}
		}
	}
}
