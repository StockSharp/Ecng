namespace Ecng.Reflection;

using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;

using Ecng.Collections;
using Ecng.Common;

/// <summary>
/// Provides helper methods for reflection.
/// </summary>
public static class ReflectionHelper
{
	/// <summary>
	/// Attribute targets for fields and properties.
	/// </summary>
	public const AttributeTargets Members = AttributeTargets.Field | AttributeTargets.Property;

	/// <summary>
	/// Attribute targets for classes, structs, and interfaces.
	/// </summary>
	public const AttributeTargets Types = AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface;

	/// <summary>
	/// Binding flags for all static members.
	/// </summary>
	public const BindingFlags AllStaticMembers = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

	/// <summary>
	/// Binding flags for all instance members.
	/// </summary>
	public const BindingFlags AllInstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	/// <summary>
	/// Binding flags for all members.
	/// </summary>
	public const BindingFlags AllMembers = AllStaticMembers | AllInstanceMembers;

	#region Public Methods

	/// <summary>
	/// Gets the invoke method from the specified delegate type.
	/// </summary>
	/// <param name="delegType">The delegate type.</param>
	/// <returns>The MethodInfo representing the Invoke method.</returns>
	public static MethodInfo GetInvokeMethod(this Type delegType)
		=> delegType.GetMethod("Invoke");

	#endregion

	#region ProxyTypes

	private static readonly Dictionary<Type, Type> _proxyTypes = [];

	/// <summary>
	/// Gets the proxy types.
	/// </summary>
	public static IDictionary<Type, Type> ProxyTypes => _proxyTypes;

	#endregion

	/// <summary>
	/// Determines whether the specified parameter is a params array.
	/// </summary>
	/// <param name="pi">The parameter info.</param>
	/// <returns>True if the parameter is marked as params; otherwise, false.</returns>
	public static bool IsParams(this ParameterInfo pi)
		=> pi.GetAttribute<ParamArrayAttribute>() != null;

	#region GetParameterTypes

	/// <summary>
	/// Gets the parameter types for the specified method.
	/// </summary>
	/// <param name="method">The method base.</param>
	/// <returns>An array of tuples with parameter info and its type.</returns>
	public static (ParameterInfo info, Type type)[] GetParameterTypes(this MethodBase method)
		=> method.GetParameterTypes(false);

	/// <summary>
	/// Gets the parameter types for the specified method.
	/// </summary>
	/// <param name="method">The method base.</param>
	/// <param name="removeRef">True to remove reference type wrappers.</param>
	/// <returns>An array of tuples with parameter info and its type.</returns>
	public static (ParameterInfo info, Type type)[] GetParameterTypes(this MethodBase method, bool removeRef)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));

		return [.. method.GetParameters().Select(param =>
		{
			Type paramType;

			if (removeRef && IsOutput(param))
				paramType = param.ParameterType.GetElementType();
			else
				paramType = param.ParameterType;

			return (param, paramType);
		})];
	}

	#endregion

	#region GetGenericType

	private static readonly SynchronizedDictionary<(Type, Type), Type> _genericTypeCache = [];

	/// <summary>
	/// Gets the generic type from the target type based on the provided generic type definition.
	/// </summary>
	/// <param name="targetType">The target type.</param>
	/// <param name="genericType">The generic type definition.</param>
	/// <returns>The found generic type or null if not found.</returns>
	public static Type GetGenericType(this Type targetType, Type genericType)
	{
		return _genericTypeCache.GetFromCache(new(targetType, genericType), key => key.Item1.GetGenericTypeInternal(key.Item2));
	}

	private static Type GetGenericTypeInternal(this Type targetType, Type genericType)
	{
		if (targetType is null)
			throw new ArgumentNullException(nameof(targetType));

		if (genericType is null)
			throw new ArgumentNullException(nameof(genericType));

		if (!genericType.IsGenericTypeDefinition)
			throw new ArgumentException(nameof(genericType));

		if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == genericType)
			return targetType;
		else
		{
			if (genericType.IsInterface)
			{
				var findedInterfaces = targetType.GetInterfaces()
					.Where(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == genericType)
					.ToList();

				if (findedInterfaces.Count > 1)
					throw new AmbiguousMatchException("Too many interfaces were found.");
				else if (findedInterfaces.Count == 1)
					return findedInterfaces[0];
				else
					return null;
			}
			else
			{
				return targetType.BaseType != null ? GetGenericType(targetType.BaseType, genericType) : null;
			}
		}
	}

	#endregion

	#region GetGenericTypeArg

	/// <summary>
	/// Gets the specific generic argument type from the target type.
	/// </summary>
	/// <param name="targetType">The target type.</param>
	/// <param name="genericType">The generic type definition.</param>
	/// <param name="index">The index of the generic argument.</param>
	/// <returns>The generic argument type.</returns>
	public static Type GetGenericTypeArg(this Type targetType, Type genericType, int index)
	{
		genericType = GetGenericType(targetType, genericType);

		if (genericType is null)
			throw new ArgumentException(nameof(targetType));
		else
			return genericType.GetGenericArguments()[index];
	}

	#endregion

	#region GetIndexer

	/// <summary>
	/// The name for indexer members.
	/// </summary>
	public const string IndexerName = "Item";

	/// <summary>
	/// Gets the default indexer property of the specified type.
	/// </summary>
	/// <param name="type">The type to search.</param>
	/// <param name="additionalTypes">Additional types to match.</param>
	/// <returns>The PropertyInfo of the indexer.</returns>
	public static PropertyInfo GetIndexer(this Type type, params Type[] additionalTypes)
	{
		return GetMember<PropertyInfo>(type, IndexerName, AllInstanceMembers, default, additionalTypes);
	}

	#endregion

	#region GetIndexers

	/// <summary>
	/// Gets all indexer properties of the specified type.
	/// </summary>
	/// <param name="type">The type to search.</param>
	/// <param name="additionalTypes">Additional types to match.</param>
	/// <returns>An array with the indexer properties.</returns>
	public static PropertyInfo[] GetIndexers(this Type type, params Type[] additionalTypes)
	{
		return GetMembers<PropertyInfo>(type, AllInstanceMembers, true, IndexerName, default, additionalTypes);
	}

	#endregion

	#region GetMember

	/// <summary>
	/// Gets a constructor member info from the specified type.
	/// </summary>
	/// <typeparam name="T">The member type.</typeparam>
	/// <param name="type">The target type.</param>
	/// <param name="additionalTypes">Additional types to match.</param>
	/// <returns>The member info.</returns>
	public static T GetMember<T>(this Type type, params Type[] additionalTypes)
		where T : ConstructorInfo
	{
		return type.GetMember<T>(".ctor", AllInstanceMembers, default, additionalTypes);
	}

	/// <summary>
	/// Gets a member info by name from the specified type.
	/// </summary>
	/// <typeparam name="T">The member type.</typeparam>
	/// <param name="type">The target type.</param>
	/// <param name="memberName">The name of the member.</param>
	/// <param name="additionalTypes">Additional types to match.</param>
	/// <returns>The member info.</returns>
	public static T GetMember<T>(this Type type, string memberName, params Type[] additionalTypes)
		where T : MemberInfo
	{
		return type.GetMember<T>(memberName, AllMembers, default, additionalTypes);
	}

	/// <summary>
	/// Gets a member info by name with specified binding flags.
	/// </summary>
	/// <typeparam name="T">The member type.</typeparam>
	/// <param name="type">The target type.</param>
	/// <param name="memberName">The member name.</param>
	/// <param name="flags">Binding flags to use for lookup.</param>
	/// <param name="isSetter">Indicates if it's a setter.</param>
	/// <param name="additionalTypes">Additional types to match.</param>
	/// <returns>The member info.</returns>
	public static T GetMember<T>(this Type type, string memberName, BindingFlags flags, bool? isSetter, params Type[] additionalTypes)
		where T : MemberInfo
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		if (memberName.IsEmpty())
			throw new ArgumentNullException(nameof(memberName));

		var members = type.GetMembers<T>(flags, true, memberName, isSetter, additionalTypes);

		if (members.Length > 1)
			members = [.. FilterMembers(members, isSetter, additionalTypes)];

		if (members.Length != 1)
		{
			if (members.Length == 2 && members[0] is EventInfo && members[1] is FieldInfo)
				return members[1];

			throw new ArgumentException($"Type '{type}' has '{members.Length}' members with name '{memberName}'.");
		}

		return members[0];
	}

	#endregion

	#region GetMembers

	/// <summary>
	/// Gets members from the type that match the additional types.
	/// </summary>
	/// <typeparam name="T">The member type.</typeparam>
	/// <param name="type">The target type.</param>
	/// <param name="additionalTypes">Additional types to match.</param>
	/// <returns>An array of matched members.</returns>
	public static T[] GetMembers<T>(this Type type, params Type[] additionalTypes)
		where T : MemberInfo
	{
		return type.GetMembers<T>(AllMembers, additionalTypes);
	}

	/// <summary>
	/// Gets members with specified binding flags that match the additional types.
	/// </summary>
	/// <typeparam name="T">The member type.</typeparam>
	/// <param name="type">The target type.</param>
	/// <param name="flags">Binding flags for lookup.</param>
	/// <param name="additionalTypes">Additional types to match.</param>
	/// <returns>An array of matched members.</returns>
	public static T[] GetMembers<T>(this Type type, BindingFlags flags, params Type[] additionalTypes)
		where T : MemberInfo
	{
		return type.GetMembers<T>(flags, true, additionalTypes);
	}

	/// <summary>
	/// Gets members with an option for inheritance and additional types matching.
	/// </summary>
	/// <typeparam name="T">The member type.</typeparam>
	/// <param name="type">The target type.</param>
	/// <param name="flags">Binding flags for lookup.</param>
	/// <param name="inheritance">True to include inherited members.</param>
	/// <param name="additionalTypes">Additional types to match.</param>
	/// <returns>An array of matched members.</returns>
	public static T[] GetMembers<T>(this Type type, BindingFlags flags, bool inheritance, params Type[] additionalTypes)
		where T : MemberInfo
	{
		return type.GetMembers<T>(flags, inheritance, null, default, additionalTypes);
	}

	/// <summary>
	/// Gets members by name with options for inheritance, setter indication, and additional types matching.
	/// </summary>
	/// <typeparam name="T">The member type.</typeparam>
	/// <param name="type">The target type.</param>
	/// <param name="flags">Binding flags for lookup.</param>
	/// <param name="inheritance">True to include inherited members.</param>
	/// <param name="memberName">The member name.</param>
	/// <param name="isSetter">Indicates if the member is a setter.</param>
	/// <param name="additionalTypes">Additional types to match.</param>
	/// <returns>An array of matched members.</returns>
	public static T[] GetMembers<T>(this Type type, BindingFlags flags, bool inheritance, string memberName, bool? isSetter, params Type[] additionalTypes)
		where T : MemberInfo
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		if (_proxyTypes.TryGetValue(type, out var proxyType))
			type = proxyType;

		var members = type.GetMembers<T>(memberName, flags, inheritance);

		if (!members.IsEmpty() && additionalTypes.Length > 0)
			members = FilterMembers(members, isSetter, additionalTypes);

		return [.. members];
	}

	private static IEnumerable<T> GetMembers<T>(this Type type, string memberName, BindingFlags flags, bool inheritance)
		where T : MemberInfo
	{
		var members = new Dictionary<(string, MemberTypes, MemberSignature), ICollection<T>>();

		if (inheritance)
		{
			foreach (Type item in type.GetInterfaces().Concat([type]))
			{
				var allMembers = memberName.IsEmpty() ? item.GetMembers(flags) : item.GetMember(memberName, flags);

				foreach (var member in allMembers)
				{
					if (member is T && member is not Type)
						members.AddMember(member);
				}
			}
		}
		else
		{
			var allMembers = memberName.IsEmpty() ? type.GetMembers(flags) : type.GetMember(memberName, flags);

			foreach (var member in allMembers)
			{
				if (member is T && member is not Type && member.ReflectedType == type)
					members.AddMember(member);
			}
		}

		if (type.IsValueType && (typeof(T) == typeof(ConstructorInfo) || memberName == ".ctor"))
		{
			MemberInfo member = new DefaultConstructorInfo(type);
			members.AddMember(member);
		}

		if (inheritance)
		{
			if (type.BaseType != null)
			{
				foreach (var member in type.BaseType.GetMembers<T>(memberName, flags, true))
					members.AddMember(member);
			}

			foreach (var pair in members.Where(arg => arg.Value.Count > 1))
			{
				var sortedMembers = pair.Value.OrderBy((x, y) =>
				{
					var result = x.ReflectedType.Compare(y.ReflectedType);

					if (result == 0)
						result = x.DeclaringType.Compare(y.DeclaringType);

					return result;
				}).ToArray();

				for (var i = 1; i < sortedMembers.Length; i++)
				{
					members[pair.Key].Remove(sortedMembers[i]);
					//members.Remove(pair.Key, sortedMembers[i]);
				}

				if (members[pair.Key].IsEmpty())
					members.Remove(pair.Key);
			}
		}

		var retVal = new List<T>();

		foreach (var collection in members.Values)
			retVal.AddRange(collection);

		return retVal;
	}

	private static void AddMember<T>(this Dictionary<(string, MemberTypes, MemberSignature), ICollection<T>> members, MemberInfo member)
	{
		if (members is null)
			throw new ArgumentNullException(nameof(members));

		if (member is null)
			throw new ArgumentNullException(nameof(member));

		members.SafeAdd(new(member.Name, member.MemberType, new(member)), delegate
		{
			return [];
		}).Add(member.To<T>());
	}

	#endregion

	#region FilterMembers

	/// <summary>
	/// Filters members based on setter indication and additional type parameters.
	/// </summary>
	/// <typeparam name="T">The member type.</typeparam>
	/// <param name="members">The collection of members.</param>
	/// <param name="isSetter">Indicates if filtering by setter should be applied.</param>
	/// <param name="additionalTypes">Additional types to match.</param>
	/// <returns>An enumerable of filtered members.</returns>
	public static IEnumerable<T> FilterMembers<T>(this IEnumerable<T> members, bool? isSetter, params Type[] additionalTypes)
		where T : MemberInfo
	{
		var ms = FilterMembers(members, false, isSetter, additionalTypes);
		return ms.IsEmpty() ? FilterMembers(members, true, isSetter, additionalTypes) : ms;
	}

	/// <summary>
	/// Filters members based on inheritance, setter indication, and additional type parameters.
	/// </summary>
	/// <typeparam name="T">The member type.</typeparam>
	/// <param name="members">The collection of members.</param>
	/// <param name="useInheritance">True to use inheritance comparison.</param>
	/// <param name="isSetter">Indicates if filtering by setter should be applied.</param>
	/// <param name="additionalTypes">Additional types to match.</param>
	/// <returns>An enumerable of filtered members.</returns>
	public static IEnumerable<T> FilterMembers<T>(this IEnumerable<T> members, bool useInheritance, bool? isSetter, params Type[] additionalTypes)
		where T : MemberInfo
	{
		if (members is null)
			throw new ArgumentNullException(nameof(members));

		if (additionalTypes is null)
			throw new ArgumentNullException(nameof(additionalTypes));

		return members.Where(arg =>
		{
			if (IsIndexer(arg) && additionalTypes.Length > 0)
			{
				var pi = arg.To<PropertyInfo>();

				return GetIndexerTypes(pi).SequenceEqual(isSetter == true ? additionalTypes.Take(additionalTypes.Length - 1) : additionalTypes, (paramType, additionalType) =>
				{
					if (additionalType == typeof(void))
						return true;
					else
						return paramType.Compare(additionalType, useInheritance);
				});
			}
			else if (additionalTypes.Length == 1 && (arg is FieldInfo || arg is PropertyInfo || arg is EventInfo))
			{
				return GetMemberType(arg).Compare(additionalTypes[0], useInheritance);
			}
			else if (arg is MethodBase mb)
			{
				var tuples = mb.GetParameterTypes(true);

				if (tuples.Length > 0 && tuples.Last().info.IsParams())
				{
					// wrap plain params types into object[]
					var paramsTypes = additionalTypes.Skip(tuples.Length - 1).ToArray();

					if (paramsTypes.Length > 0)
					{
						additionalTypes = [.. additionalTypes.Take(tuples.Length - 1), tuples.Last().type];
					}
				}

				return tuples.Select(t => t.type).SequenceEqual(additionalTypes, (paramType, additionalType) =>
				{
					if (additionalType == typeof(void))
						return true;
					else
						return paramType.Compare(additionalType, useInheritance);
				});
			}
			else
				return false;
		});
	}

	#endregion

	#region IsAbstract

	private static readonly SynchronizedDictionary<MemberInfo, bool> _isAbstractCache = [];

	/// <summary>
	/// Determines whether the specified member is abstract.
	/// </summary>
	/// <param name="member">The member info.</param>
	/// <returns>True if the member is abstract; otherwise, false.</returns>
	public static bool IsAbstract(this MemberInfo member)
	{
		if (member is null)
			throw new ArgumentNullException(nameof(member));

		return _isAbstractCache.GetFromCache(member, delegate
		{
			return member switch
			{
				MethodBase mb => mb.IsAbstract,
				Type type => type.IsAbstract,
				PropertyInfo prop => (prop.CanRead && prop.GetGetMethod(true).IsAbstract) || (prop.CanWrite && prop.GetSetMethod(true).IsAbstract),
				EventInfo evt => evt.GetAddMethod(true).IsAbstract || evt.GetRemoveMethod(true).IsAbstract,
				_ => false,
			};
		});
	}

	#endregion

	#region IsVirtual

	private static readonly SynchronizedDictionary<MemberInfo, bool> _isVirtualCache = [];

	/// <summary>
	/// Determines whether the specified member is virtual.
	/// </summary>
	/// <param name="member">The member info.</param>
	/// <returns>True if the member is virtual; otherwise, false.</returns>
	public static bool IsVirtual(this MemberInfo member)
	{
		if (member is null)
			throw new ArgumentNullException(nameof(member));

		return _isVirtualCache.GetFromCache(member, delegate
		{
			if (member is MethodBase mb)
				return mb.IsVirtual;
			//else if (member is Type type)
			//	return type.IsVirtual;
			else if (member is PropertyInfo prop)
				return (prop.CanRead && prop.GetGetMethod(true).IsVirtual) || (prop.CanWrite && prop.GetSetMethod(true).IsVirtual);
			else if (member is EventInfo evt)
				return evt.GetAddMethod(true).IsVirtual || evt.GetRemoveMethod(true).IsVirtual;
			else
				return false;
		});
	}

	#endregion

	#region IsOverloadable

	/// <summary>
	/// Determines whether the specified member is overloadable.
	/// </summary>
	/// <param name="member">The member info.</param>
	/// <returns>True if the member is overloadable; otherwise, false.</returns>
	public static bool IsOverloadable(this MemberInfo member)
	{
		if (member is null)
			throw new ArgumentNullException(nameof(member));

		return member is ConstructorInfo || member.IsAbstract() || member.IsVirtual();
	}

	#endregion

	#region IsIndexer

	/// <summary>
	/// Determines whether the specified member is an indexer.
	/// </summary>
	/// <param name="member">The member info.</param>
	/// <returns>True if the member is an indexer; otherwise, false.</returns>
	public static bool IsIndexer(this MemberInfo member)
	{
		if (member is PropertyInfo prop)
			return prop.IsIndexer();
		else
			return false;
	}

	/// <summary>
	/// Determines whether the specified property is an indexer.
	/// </summary>
	/// <param name="property">The property info.</param>
	/// <returns>True if the property is an indexer; otherwise, false.</returns>
	public static bool IsIndexer(this PropertyInfo property)
	{
		if (property is null)
			throw new ArgumentNullException(nameof(property));

		return property.GetIndexParameters().Length > 0;
	}

	#endregion

	#region GetIndexerTypes

	/// <summary>
	/// Gets the types of the parameters of the indexer property.
	/// </summary>
	/// <param name="property">The indexer property.</param>
	/// <returns>An enumerable of parameter types.</returns>
	public static IEnumerable<Type> GetIndexerTypes(this PropertyInfo property)
	{
		if (property is null)
			throw new ArgumentNullException(nameof(property));

		var accessor = property.GetGetMethod(true) ?? property.GetSetMethod(true);

		if (accessor is null)
			throw new ArgumentException(nameof(property), "No any accessors.");

		return accessor.GetParameterTypes().Select(t => t.type);
	}

	#endregion

	#region MemberIs

	/// <summary>
	/// Determines whether the member has one of the specified member types.
	/// </summary>
	/// <param name="member">The member info.</param>
	/// <param name="types">The member types to check.</param>
	/// <returns>True if the member matches one of the types; otherwise, false.</returns>
	public static bool MemberIs(this MemberInfo member, params MemberTypes[] types)
	{
		if (member is null)
			throw new ArgumentNullException(nameof(member));

		return types.Any(type => member.MemberType == type);
	}

	#endregion

	#region IsOutput

	/// <summary>
	/// Determines whether the specified parameter is an output parameter.
	/// </summary>
	/// <param name="param">The parameter info.</param>
	/// <returns>True if the parameter is output; otherwise, false.</returns>
	public static bool IsOutput(this ParameterInfo param)
	{
		if (param is null)
			throw new ArgumentNullException(nameof(param));

		return param.IsOut || param.ParameterType.IsByRef;
	}

	#endregion

	#region GetMemberType

	/// <summary>
	/// Gets the type associated with the member.
	/// </summary>
	/// <param name="member">The member info.</param>
	/// <returns>The type of the member.</returns>
	public static Type GetMemberType(this MemberInfo member)
	{
		if (member is null)
			throw new ArgumentNullException(nameof(member));

		return member switch
		{
			PropertyInfo pi => pi.PropertyType,
			FieldInfo fi => fi.FieldType,
			MethodInfo mi => mi.ReturnType,
			EventInfo ei => ei.EventHandlerType,
			ConstructorInfo _ => member.ReflectedType,
			_ => throw new ArgumentOutOfRangeException(nameof(member), member.To<string>()),
		};
	}

	#endregion

	#region IsCollection

	private static readonly SynchronizedDictionary<Type, bool> _isCollectionCache = [];

	/// <summary>
	/// Determines whether the specified type is a collection.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if the type is a collection; otherwise, false.</returns>
	public static bool IsCollection(this Type type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		return _isCollectionCache.GetFromCache(type, delegate
		{
			return type.Is<ICollection>()
						|| type.GetGenericType(typeof(ICollection<>)) != null
						|| type.Is<IEnumerable>()
						|| type.GetGenericType(typeof(IEnumerable<>)) != null;
		});


	}

	#endregion

	#region IsStatic

	private static readonly SynchronizedDictionary<MemberInfo, bool> _isStaticCache = [];

	/// <summary>
	/// Determines whether the specified member is static.
	/// </summary>
	/// <param name="member">The member info.</param>
	/// <returns>True if the member is static; otherwise, false.</returns>
	public static bool IsStatic(this MemberInfo member)
	{
		if (member is null)
			throw new ArgumentNullException(nameof(member));

		return _isStaticCache.GetFromCache(member, delegate
		{
			if (member is MethodBase mb)
				return mb.IsStatic;
			else if (member is PropertyInfo prop)
			{
				if (prop.CanRead)
					return IsStatic(prop.GetGetMethod(true));
				else if (prop.CanWrite)
					return IsStatic(prop.GetSetMethod(true));
				else
					throw new ArgumentOutOfRangeException(nameof(member), member.To<string>());
			}
			else if (member is FieldInfo fi)
				return fi.IsStatic;
			else if (member is EventInfo evt)
				return IsStatic(evt.GetAddMethod(true));
			else if (member is Type type)
				return type.IsAbstract && type.IsSealed;
			else
				throw new ArgumentOutOfRangeException(nameof(member), member.To<string>());
		});
	}

	#endregion

	#region GetItemType

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Type TryGetAsyncEnumerableItem(this Type collectionType)
	{
		try
		{
			return collectionType.GetGenericType(typeof(IAsyncEnumerable<>));
		}
		catch (FileNotFoundException)
		{
			return null;
		}
	}

	private static readonly SynchronizedDictionary<Type, Type> _getItemTypeCache = [];

	/// <summary>
	/// Gets the item type for a collection type.
	/// </summary>
	/// <param name="collectionType">The collection type.</param>
	/// <returns>The item type contained in the collection.</returns>
	public static Type GetItemType(this Type collectionType)
	{
		if (collectionType is null)
			throw new ArgumentNullException(nameof(collectionType));

		return _getItemTypeCache.GetFromCache(collectionType, delegate
		{
			var interfaceType =
				collectionType.GetGenericType(typeof(ICollection<>)) ??
				collectionType.GetGenericType(typeof(IEnumerable<>)) ??
				collectionType.TryGetAsyncEnumerableItem();

			if (interfaceType != null)
				return interfaceType.GetGenericArguments()[0];
			else
				throw new InvalidOperationException($"Type '{collectionType}' isn't collection.");
		});
	}

	#endregion

	/// <summary>
	/// Getters prefix.
	/// </summary>
	public const string GetPrefix = "get_";

	/// <summary>
	/// Setters prefix.
	/// </summary>
	public const string SetPrefix = "set_";

	/// <summary>
	/// Adders prefix.
	/// </summary>
	public const string AddPrefix = "add_";

	/// <summary>
	/// Removers prefix.
	/// </summary>
	public const string RemovePrefix = "remove_";

	#region MakePropertyName

	/// <summary>
	/// Makes the property name from the accessor name.
	/// </summary>
	/// <param name="accessorName">The accessor name.</param>
	/// <returns>
	/// The property name.</returns>
	public static string MakePropertyName(this string accessorName)
	{
		return accessorName.ThrowIfEmpty(nameof(accessorName))
						.Remove(GetPrefix)
						.Remove(SetPrefix)
						.Remove(AddPrefix)
						.Remove(RemovePrefix);
	}

	#endregion

	#region GetAccessorOwner

	private static readonly SynchronizedDictionary<MethodInfo, MemberInfo> _getAccessorOwnerCache = [];

	/// <summary>
	/// Gets the member that owns the specified accessor method.
	/// </summary>
	/// <param name="method">Accessor method.</param>
	/// <returns>The owner member or <c>null</c> if not found.</returns>
	public static MemberInfo GetAccessorOwner(this MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));

		return _getAccessorOwnerCache.GetFromCache(method, delegate
		{
			var flags = method.IsStatic ? AllStaticMembers : AllInstanceMembers;

			if (method.Name.Contains(GetPrefix) || method.Name.Contains(SetPrefix))
			{
				var name = MakePropertyName(method.Name);

				return GetMembers<PropertyInfo>(method.ReflectedType, flags, true, name, default)
					.FirstOrDefault(property => property.GetGetMethod(true) == method || property.GetSetMethod(true) == method);
			}
			else if (method.Name.Contains(AddPrefix) || method.Name.Contains(RemovePrefix))
			{
				var name = MakePropertyName(method.Name);

				return GetMembers<EventInfo>(method.ReflectedType, flags, true, name, default)
					.FirstOrDefault(@event => @event.GetAddMethod(true) == method || @event.GetRemoveMethod(true) == method);
			}

			return null;
		});
	}

	#endregion

	/// <summary>
	/// Constructs a generic method with the provided type arguments.
	/// </summary>
	/// <param name="method">Generic method definition.</param>
	/// <param name="types">Generic argument types.</param>
	/// <returns>The constructed generic method.</returns>
	public static MethodInfo Make(this MethodInfo method, params Type[] types)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));

		return method.MakeGenericMethod(types);
	}

	/// <summary>
	/// Determines whether the specified type is a runtime type.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if the type inherits from <see cref="Type"/>; otherwise, false.</returns>
	public static bool IsRuntimeType(this Type type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		return type.BaseType == typeof(TypeInfo);
	}

	/// <summary>
	/// Determines whether the specified file contains a valid assembly.
	/// </summary>
	/// <param name="dllName">Assembly file name.</param>
	/// <returns>True if the file is an assembly; otherwise, false.</returns>
	public static bool IsAssembly(this string dllName)
	{
		return dllName.VerifyAssembly() != null;
	}

	/// <summary>
	/// Tries to read the assembly name from the specified file.
	/// </summary>
	/// <param name="dllName">Assembly file name.</param>
	/// <returns>The assembly name or <c>null</c> if the file is not a valid assembly.</returns>
	public static AssemblyName VerifyAssembly(this string dllName)
	{
		try
		{
			return AssemblyName.GetAssemblyName(dllName);
		}
		catch (BadImageFormatException)
		{
			return null;
		}
	}

	/// <summary>
	/// Is cache enabled.
	/// </summary>
	public static bool CacheEnabled { get; set; } = true;

	/// <summary>
	/// Clear cache.
	/// </summary>
	public static void ClearCache()
	{
		_genericTypeCache.Clear();
		_getAccessorOwnerCache.Clear();
		_getItemTypeCache.Clear();
		_isAbstractCache.Clear();
		_isCollectionCache.Clear();
		_isStaticCache.Clear();
		_isVirtualCache.Clear();
	}

	private static TValue GetFromCache<TKey, TValue>(this SynchronizedDictionary<TKey, TValue> cache, TKey key, Func<TKey, TValue> createValue)
	{
		if (!CacheEnabled)
			return createValue(key);

		return cache.SafeAdd(key, createValue);
	}

	/// <summary>
	/// Find all <typeparamref name="T"/> implementation in the specified assembly.
	/// </summary>
	/// <typeparam name="T">Filter interface type.</typeparam>
	/// <param name="assembly">Assembly in where types scan required.</param>
	/// <param name="showObsolete">Show types marked as obsolete.</param>
	/// <param name="showNonPublic">Show non public types.</param>
	/// <param name="showNonBrowsable">Show types marked as non browsable.</param>
	/// <param name="extraFilter">Extra filter.</param>
	/// <returns>Found types.</returns>
	public static IEnumerable<Type> FindImplementations<T>(this Assembly assembly, bool showObsolete = default, bool showNonPublic = default, bool showNonBrowsable = default, Func<Type, bool> extraFilter = default)
	{
		if (assembly is null)
			throw new ArgumentNullException(nameof(assembly));

		extraFilter ??= t => true;

		return assembly
			.GetTypes()
			.Where(t => t.Is<T>() && !t.IsAbstract && !t.IsInterface &&
				(showNonPublic || t.IsPublic) &&
				(showObsolete || !t.IsObsolete()) &&
				(showNonBrowsable || t.IsBrowsable()) &&
				extraFilter(t));
	}

	/// <summary>
	/// Is type compatible.
	/// </summary>
	/// <typeparam name="T">Required type.</typeparam>
	/// <param name="type">Type.</param>
	/// <returns>Check result.</returns>
	public static bool IsRequiredType<T>(this Type type)
		=> IsRequiredType(type, typeof(T));

	/// <summary>
	/// Is type compatible.
	/// </summary>
	/// <param name="type">Type.</param>
	/// <param name="required">Required type.</param>
	/// <returns>Check result.</returns>
	public static bool IsRequiredType(this Type type, Type required)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		if (required is null)
			throw new ArgumentNullException(nameof(required));

		return !type.IsAbstract &&
			type.IsPublic &&
			!type.IsGenericTypeDefinition &&
			type.Is(required) &&
			type.GetConstructor([]) is not null;
	}

	/// <summary>
	/// Try find type.
	/// </summary>
	/// <param name="types">Types.</param>
	/// <param name="isTypeCompatible">Is type compatible.</param>
	/// <param name="typeName">The type name.</param>
	/// <returns>The found type.</returns>
	public static Type TryFindType(this IEnumerable<Type> types, Func<Type, bool> isTypeCompatible, string typeName)
	{
		if (types is null)
			throw new ArgumentNullException(nameof(types));

		if (isTypeCompatible is null && typeName.IsEmpty())
			throw new ArgumentNullException(nameof(typeName));

		if (!typeName.IsEmpty())
			return types.FirstOrDefault(t => t.Name.EqualsIgnoreCase(typeName));
		else
			return types.FirstOrDefault(isTypeCompatible);
	}

	/// <summary>
	/// Order the members by declaration.
	/// </summary>
	/// <typeparam name="TMember">Member type.</typeparam>
	/// <param name="members">Members.</param>
	/// <returns>Ordered members.</returns>
	public static IEnumerable<TMember> OrderByDeclaration<TMember>(this IEnumerable<TMember> members)
		where TMember : MemberInfo
		=> members.OrderBy(m => m.MetadataToken);

	/// <summary>
	/// Determines the property is modifiable.
	/// </summary>
	/// <param name="pi">The property info.</param>
	/// <returns>Operations result.</returns>
	public static bool IsModifiable(this PropertyInfo pi)
		=> pi.CanWrite && pi.GetAttribute<ReadOnlyAttribute>()?.IsReadOnly != true && pi.SetMethod?.IsPublic == true;

	/// <summary>
	/// Determines the property is match.
	/// </summary>
	/// <param name="propertyInfo">The property info.</param>
	/// <param name="bindingFlags"><see cref="BindingFlags"/></param>
	/// <returns>Operations result.</returns>
	public static bool IsMatch(this PropertyInfo propertyInfo, BindingFlags bindingFlags)
	{
		var hasPublic = bindingFlags.HasFlag(BindingFlags.Public);
		var hasNonPublic = bindingFlags.HasFlag(BindingFlags.NonPublic);

		if (!hasPublic || !hasNonPublic)
		{
			var isPublic = propertyInfo.GetMethod?.IsPublic == true || propertyInfo.SetMethod?.IsPublic == true;

			if (hasPublic && !isPublic)
				return false;

			if (hasNonPublic && isPublic)
				return false;
		}

		var hasStatic = bindingFlags.HasFlag(BindingFlags.Static);
		var hasInstance = bindingFlags.HasFlag(BindingFlags.Instance);

		if (!hasStatic || !hasInstance)
		{
			var isStatic = propertyInfo.IsStatic();

			if (hasStatic && !isStatic)
				return false;

			if (hasInstance && isStatic)
				return false;
		}

		return true;
	}

	/// <summary>
	/// Determines the method is match.
	/// </summary>
	/// <param name="methodInfo">The method info.</param>
	/// <param name="bindingFlags"><see cref="BindingFlags"/></param>
	/// <returns>Operations result.</returns>
	public static bool IsMatch(this MethodBase methodInfo, BindingFlags bindingFlags)
	{
		var hasPublic = bindingFlags.HasFlag(BindingFlags.Public);
		var hasNonPublic = bindingFlags.HasFlag(BindingFlags.NonPublic);

		if (!hasPublic || !hasNonPublic)
		{
			var isPublic = methodInfo.IsPublic;

			if (hasPublic && !isPublic)
				return false;

			if (hasNonPublic && isPublic)
				return false;
		}

		var hasStatic = bindingFlags.HasFlag(BindingFlags.Static);
		var hasInstance = bindingFlags.HasFlag(BindingFlags.Instance);

		if (!hasStatic || !hasInstance)
		{
			var isStatic = methodInfo.IsStatic;

			if (hasStatic && !isStatic)
				return false;

			if (hasInstance && isStatic)
				return false;
		}

		return true;
	}

	/// <summary>
	/// Determines the field is match.
	/// </summary>
	/// <param name="field">The field.</param>
	/// <param name="bindingFlags"><see cref="BindingFlags"/></param>
	/// <returns>Operations result.</returns>
	public static bool IsMatch(this FieldInfo field, BindingFlags bindingFlags)
	{
		var hasPublic = bindingFlags.HasFlag(BindingFlags.Public);
		var hasNonPublic = bindingFlags.HasFlag(BindingFlags.NonPublic);

		if (!hasPublic || !hasNonPublic)
		{
			var isPublic = field.IsPublic;

			if (hasPublic && !isPublic)
				return false;

			if (hasNonPublic && isPublic)
				return false;
		}

		var hasStatic = bindingFlags.HasFlag(BindingFlags.Static);
		var hasInstance = bindingFlags.HasFlag(BindingFlags.Instance);

		if (!hasStatic || !hasInstance)
		{
			var isStatic = field.IsStatic;

			if (hasStatic && !isStatic)
				return false;

			if (hasInstance && isStatic)
				return false;
		}

		return true;
	}
}