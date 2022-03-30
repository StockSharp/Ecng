namespace Ecng.Reflection
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using MemberType = System.Tuple<string, System.Reflection.MemberTypes, MemberSignature>;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection.Emit;

	public static class ReflectionHelper
	{
        public const AttributeTargets Members = AttributeTargets.Field | AttributeTargets.Property;
        public const AttributeTargets Types = AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface;

		public const BindingFlags AllStaticMembers = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		public const BindingFlags AllInstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		public const BindingFlags AllMembers = AllStaticMembers | AllInstanceMembers;

		public static MethodInfo GetInvokeMethod(this Type delegType)
			=> delegType.GetMethod("Invoke");

		#region ProxyTypes

		private static readonly Dictionary<Type, Type> _proxyTypes = new();

		public static IDictionary<Type, Type> ProxyTypes => _proxyTypes;

		#endregion

		public static bool IsParams(this ParameterInfo pi)
			=> pi.GetAttribute<ParamArrayAttribute>() != null;

		#region GetParameterTypes

		public static (ParameterInfo info, Type type)[] GetParameterTypes(this MethodBase method)
			=> method.GetParameterTypes(false);

		public static (ParameterInfo info, Type type)[] GetParameterTypes(this MethodBase method, bool removeRef)
		{
			if (method is null)
				throw new ArgumentNullException(nameof(method));

			return method.GetParameters().Select(param =>
			{
				Type paramType;

				if (removeRef && IsOutput(param))
					paramType = param.ParameterType.GetElementType();
				else
					paramType = param.ParameterType;

				return (param, paramType);
			}).ToArray();
		}

		#endregion

		#region GetGenericType

		private static readonly SynchronizedDictionary<Tuple<Type, Type>, Type> _genericTypeCache = new();

		public static Type GetGenericType(this Type targetType, Type genericType)
		{
			return _genericTypeCache.GetFromCache(new Tuple<Type, Type>(targetType, genericType), key => key.Item1.GetGenericTypeInternal(key.Item2));
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

		public const string IndexerName = "Item";

		public static PropertyInfo GetIndexer(this Type type, params Type[] additionalTypes)
		{
			return GetMember<PropertyInfo>(type, IndexerName, AllInstanceMembers, default, additionalTypes);
		}

		#endregion

		#region GetIndexers

		public static PropertyInfo[] GetIndexers(this Type type, params Type[] additionalTypes)
		{
			return GetMembers<PropertyInfo>(type, AllInstanceMembers, true, IndexerName, default, additionalTypes);
		}

		#endregion

		#region CreateInstance

		public static T CreateInstance<T>(this Type type, object arg)
		{
			return (T)type.CreateInstance(arg);
		}

		public static object CreateInstance(this Type type)
		{
			return type.CreateInstance(null);
		}

		public static object CreateInstance(this Type type, object arg)
		{
			return GetMember<ConstructorInfo>(type, GetArgTypes(arg)).CreateInstance<object>(arg);
		}

		public static T CreateInstance<T>(this ConstructorInfo ctor, object arg)
		{
			return (T)FastInvoker.Create(ctor).Ctor(arg);
		}

		public static TInstance CreateInstance<TInstance>()
		{
			return CreateInstance<object[], TInstance>(null);
		}

		public static TInstance CreateInstance<TArg, TInstance>(TArg arg)
		{
			return CreateInstance<TArg, TInstance>(GetMember<ConstructorInfo>(typeof(TInstance), GetArgTypes(arg)), arg);
		}

		public static TInstance CreateInstance<TArg, TInstance>(ConstructorInfo ctor, TArg arg)
		{
			return FastInvoker<VoidType, TArg, TInstance>.Create(ctor).Ctor(arg);
		}

		#endregion

		#region GetArgTypes

		public static Type[] GetArgTypes<TArg>(TArg arg)
		{
			return arg.IsNull() ? Type.EmptyTypes : arg.To<Type[]>();
		}

		#endregion

		#region SetValue

		public static TInstance SetValue<TInstance, TValue>(this TInstance instance, string memberName, TValue value)
		{
			return instance.SetValue(memberName, AllInstanceMembers, value);
		}

		public static TInstance SetValue<TInstance, TValue>(this TInstance instance, string memberName, BindingFlags flags, TValue value)
		{
			if (instance.IsNull())
				throw new ArgumentNullException(nameof(instance));

			return instance.SetValue(instance.GetType().GetMember<MemberInfo>(memberName, flags, true, GetArgTypes(value)), value);
		}

		public static void SetValue<TValue>(this Type type, string memberName, TValue value)
		{
			type.SetValue(memberName, AllStaticMembers, value);
		}

		public static void SetValue<TValue>(this Type type, string memberName, BindingFlags flags, TValue value)
		{
			type.GetMember<MemberInfo>(memberName, flags, true, GetArgTypes(value)).SetValue(value);
		}

		public static void SetValue<TValue>(this MemberInfo member, TValue value)
		{
			if (member is null)
				throw new ArgumentNullException(nameof(member));

			if (member is PropertyInfo pi)
				FastInvoker<VoidType, TValue, VoidType>.Create(pi, false).SetValue(value);
			else if (member is FieldInfo fi)
				FastInvoker<VoidType, TValue, VoidType>.Create(fi, false).SetValue(value);
			else if (member is MethodInfo mi)
				FastInvoker<VoidType, TValue, VoidType>.Create(mi).VoidInvoke(value);
			else if (member is EventInfo ei)
				FastInvoker<VoidType, TValue, VoidType>.Create(ei, false).VoidInvoke(value);
			else
				throw new ArgumentOutOfRangeException(nameof(member), member.To<string>());
		}

		public static TInstance SetValue<TInstance, TValue>(this TInstance instance, MemberInfo member, TValue value)
		{
			if (member is null)
				throw new ArgumentNullException(nameof(member));

			if (member is PropertyInfo pi)
			{
				if (IsIndexer(member))
					return FastInvoker<TInstance, TValue, TInstance>.Create(pi, false).ReturnInvoke(instance, value);
				else
					return FastInvoker<TInstance, TValue, VoidType>.Create(pi, false).SetValue(instance, value);
			}
			else if (member is FieldInfo fi)
				return FastInvoker<TInstance, TValue, VoidType>.Create(fi, false).SetValue(instance, value);
			else if (member is MethodInfo mi)
				FastInvoker<TInstance, TValue, VoidType>.Create(mi).VoidInvoke(instance, value);
			else if (member is EventInfo ei)
				FastInvoker<TInstance, TValue, VoidType>.Create(ei, false).VoidInvoke(instance, value);
			else
				throw new ArgumentOutOfRangeException(nameof(member), member.To<string>());

			return instance;
		}

		#endregion

		#region GetValue

		public static TValue GetValue<TInstance, TArg, TValue>(this TInstance instance, string memberName, TArg arg)
		{
			return instance.GetValue<TInstance, TArg, TValue>(memberName, AllInstanceMembers, arg);
		}

		public static TValue GetValue<TInstance, TArg, TValue>(this TInstance instance, string memberName, BindingFlags flags, TArg arg)
		{
			if (instance.IsNull())
				throw new ArgumentNullException(nameof(instance));

			return instance.GetValue<TInstance, TArg, TValue>(GetMember<MemberInfo>(instance.GetType(), memberName, flags, false, GetArgTypes(arg)), arg);
		}

		public static TValue GetValue<TArg, TValue>(this Type type, string memberName, TArg arg)
		{
			return type.GetValue<TArg, TValue>(memberName, AllStaticMembers, arg);
		}

		public static TValue GetValue<TArg, TValue>(this Type type, string memberName, BindingFlags flags, TArg arg)
		{
			return GetMember<MemberInfo>(type, memberName, flags, false, GetArgTypes(arg)).GetValue<TArg, TValue>(arg);
		}

		public static TValue GetValue<TArg, TValue>(this MemberInfo member, TArg arg)
		{
			if (member is null)
				throw new ArgumentNullException(nameof(member));

			TValue value;

			if (member is PropertyInfo prop)
				value = FastInvoker<VoidType, VoidType, TValue>.Create(prop, true).GetValue();
			else if (member is FieldInfo field)
				value = FastInvoker<VoidType, VoidType, TValue>.Create(field, true).GetValue();
			else if (member is MethodInfo method)
				value = FastInvoker<VoidType, TArg, TValue>.Create(method).ReturnInvoke(arg);
			else if (member is EventInfo evt)
			{
				FastInvoker<VoidType, TArg, VoidType>.Create(evt, true).VoidInvoke(arg);
				value = default;
			}
			else
				throw new ArgumentOutOfRangeException(nameof(member), member.To<string>());

			return value;
		}

		public static TValue GetValue<TInstance, TArg, TValue>(this TInstance instance, MemberInfo member, TArg arg)
		{
			if (member is null)
				throw new ArgumentNullException(nameof(member));

			TValue value;

			if (member is PropertyInfo prop)
				value = IsIndexer(member) ? FastInvoker<TInstance, TArg, TValue>.Create(prop, true).ReturnInvoke(instance, arg) : FastInvoker<TInstance, VoidType, TValue>.Create(prop, true).GetValue(instance);
			else if (member is FieldInfo field)
				value = FastInvoker<TInstance, VoidType, TValue>.Create(field, true).GetValue(instance);
			else if (member is MethodInfo method)
				value = FastInvoker<TInstance, TArg, TValue>.Create(method).ReturnInvoke(instance, arg);
			else if (member is EventInfo evt)
			{
				FastInvoker<TInstance, TArg, VoidType>.Create(evt, true).VoidInvoke(instance, arg);
				value = default;
			}
			else
				throw new ArgumentOutOfRangeException(nameof(member), member.To<string>());

			return value;
		}

		#endregion

		#region GetMember

		public static T GetMember<T>(this Type type, params Type[] additionalTypes)
			where T : ConstructorInfo
		{
			return type.GetMember<T>(".ctor", AllInstanceMembers, default, additionalTypes);
		}

		public static T GetMember<T>(this Type type, string memberName, params Type[] additionalTypes)
			where T : MemberInfo
		{
			return type.GetMember<T>(memberName, AllMembers, default, additionalTypes);
		}

		public static T GetMember<T>(this Type type, string memberName, BindingFlags flags, bool? isSetter, params Type[] additionalTypes)
			where T : MemberInfo
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			if (memberName.IsEmpty())
				throw new ArgumentNullException(nameof(memberName));

			var members = type.GetMembers<T>(flags, true, memberName, isSetter, additionalTypes);

			if (members.Length > 1)
				members = FilterMembers(members, isSetter, additionalTypes).ToArray();

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

		public static T[] GetMembers<T>(this Type type, params Type[] additionalTypes)
			where T : MemberInfo
		{
			return type.GetMembers<T>(AllMembers, additionalTypes);
		}

		public static T[] GetMembers<T>(this Type type, BindingFlags flags, params Type[] additionalTypes)
			where T : MemberInfo
		{
			return type.GetMembers<T>(flags, true, additionalTypes);
		}

		public static T[] GetMembers<T>(this Type type, BindingFlags flags, bool inheritance, params Type[] additionalTypes)
			where T : MemberInfo
		{
			return type.GetMembers<T>(flags, inheritance, null, default, additionalTypes);
		}

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

			return members.ToArray();
		}

		private static IEnumerable<T> GetMembers<T>(this Type type, string memberName, BindingFlags flags, bool inheritance)
			where T : MemberInfo
		{
			var members = new Dictionary<MemberType, ICollection<T>>();

			if (inheritance)
			{
				foreach (Type item in type.GetInterfaces().Concat(new[] { type } ))
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

		private static void AddMember<T>(this Dictionary<MemberType, ICollection<T>> members, MemberInfo member)
		{
			if (members is null)
				throw new ArgumentNullException(nameof(members));

			if (member is null)
				throw new ArgumentNullException(nameof(member));

			members.SafeAdd(new MemberType(member.Name, member.MemberType, new MemberSignature(member)), delegate
			{
				return new List<T>();
			}).Add(member.To<T>());
		}

		#endregion

		#region FilterMembers

		public static IEnumerable<T> FilterMembers<T>(this IEnumerable<T> members, bool? isSetter, params Type[] additionalTypes)
			where T : MemberInfo
		{
			var ms = FilterMembers(members, false, isSetter, additionalTypes);
			return ms.IsEmpty() ? FilterMembers(members, true, isSetter, additionalTypes) : ms;
		}

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
						// wrap plained params types into object[]
						var paramsTypes = additionalTypes.Skip(tuples.Length - 1).ToArray();

						if (paramsTypes.Length > 0)
						{
							additionalTypes = additionalTypes.Take(tuples.Length - 1).Concat(new[] { tuples.Last().type }).ToArray();
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

		private static readonly SynchronizedDictionary<MemberInfo, bool> _isAbstractCache = new();

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

		private static readonly SynchronizedDictionary<MemberInfo, bool> _isVirtualCache = new();

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

		public static bool IsOverloadable(this MemberInfo member)
		{
			if (member is null)
				throw new ArgumentNullException(nameof(member));

			return member is ConstructorInfo || member.IsAbstract() || member.IsVirtual();
		}

		#endregion

		#region IsIndexer

		public static bool IsIndexer(this MemberInfo member)
		{
			if (member is PropertyInfo prop)
				return prop.IsIndexer();
			else
				return false;
		}

		public static bool IsIndexer(this PropertyInfo property)
		{
			if (property is null)
				throw new ArgumentNullException(nameof(property));

			return property.GetIndexParameters().Length > 0;
		}

		#endregion

		#region GetIndexerTypes

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

		public static bool MemberIs(this MemberInfo member, params MemberTypes[] types)
		{
			if (member is null)
				throw new ArgumentNullException(nameof(member));

			return types.Any(type => member.MemberType == type);
		}

		#endregion

		#region IsOutput

		public static bool IsOutput(this ParameterInfo param)
		{
			if (param is null)
				throw new ArgumentNullException(nameof(param));

			return param.IsOut || param.ParameterType.IsByRef;
		}

		#endregion

		#region GetMemberType

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

		private static readonly SynchronizedDictionary<Type, bool> _isCollectionCache = new();

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

		private static readonly SynchronizedDictionary<MemberInfo, bool> _isStaticCache = new();

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

		private static readonly SynchronizedDictionary<Type, Type> _getItemTypeCache = new();

		public static Type GetItemType(this Type collectionType)
		{
			if (collectionType is null)
				throw new ArgumentNullException(nameof(collectionType));

			return _getItemTypeCache.GetFromCache(collectionType, delegate
			{
				var interfaceType =
					collectionType.GetGenericType(typeof(ICollection<>)) ??
					collectionType.GetGenericType(typeof(IEnumerable<>)) ??
					collectionType.GetGenericType(typeof(IAsyncEnumerable<>));

				if (interfaceType != null)
					return interfaceType.GetGenericArguments()[0];
				else
					throw new InvalidOperationException($"Type '{collectionType}' isn't collection.");
			});
		}

		#endregion

		public const string GetPrefix = "get_";
		public const string SetPrefix = "set_";
		public const string AddPrefix = "add_";
		public const string RemovePrefix = "remove_";

		#region MakePropertyName

		public static string MakePropertyName(this string accessorName)
		{
			if (accessorName.IsEmpty())
				throw new ArgumentNullException(nameof(accessorName));

			return accessorName
							.Remove(GetPrefix)
							.Remove(SetPrefix)
							.Remove(AddPrefix)
							.Remove(RemovePrefix);
		}

		#endregion

		#region GetAccessorOwner

		private static readonly SynchronizedDictionary<MethodInfo, MemberInfo> _getAccessorOwnerCache = new();

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

		#region GetGenericArgs

		public static IEnumerable<GenericArg> GetGenericArgs(this Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			if (!type.IsGenericTypeDefinition)
				throw new ArgumentException(nameof(type));

			return type.GetGenericArguments().GetGenericArgs();
		}

		public static IEnumerable<GenericArg> GetGenericArgs(this MethodInfo method)
		{
			if (method is null)
				throw new ArgumentNullException(nameof(method));

			if (!method.IsGenericMethodDefinition)
				throw new ArgumentException(nameof(method));

			return method.GetGenericArguments().GetGenericArgs();
		}

		private static IEnumerable<GenericArg> GetGenericArgs(this IEnumerable<Type> genericParams)
		{
			if (genericParams is null)
				throw new ArgumentNullException(nameof(genericParams));

			var genericArgs = new List<GenericArg>();

			foreach (var genericParam in genericParams)
			{
				var constraints = genericParam.GetGenericParameterConstraints()
					.Select(constraintBaseType => new Constraint(constraintBaseType))
					.ToList();

				if (genericParam.GenericParameterAttributes != GenericParameterAttributes.None)
					constraints.Add(new Constraint(genericParam.GenericParameterAttributes));

				genericArgs.Add(new GenericArg(genericParam, genericParam.Name, constraints));
			}

			return genericArgs;
		}

		#endregion

		public static MethodInfo Make(this MethodInfo method, params Type[] types)
		{
			if (method is null)
				throw new ArgumentNullException(nameof(method));

			return method.MakeGenericMethod(types);
		}

		public static bool IsRuntimeType(this Type type)
		{
			return type.BaseType == typeof(Type);
		}

		public static bool IsAssembly(this string dllName)
		{
			return dllName.VerifyAssembly() != null;
		}

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

		public static bool CacheEnabled { get; set; } = true;

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

		public static void EnsureRunClass(this Type type)
			=> RuntimeHelpers.RunClassConstructor(type.TypeHandle);
	}
}