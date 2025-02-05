namespace Ecng.Serialization
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Reflection;

	public static class PersistableHelper
	{
		private static readonly CachedSynchronizedDictionary<Type, Type> _adapterTypes = [];

		private static Type ValidateAdapterType(Type adapterType)
		{
			if (adapterType is null)
				throw new ArgumentNullException(nameof(adapterType));

			if (!adapterType.IsPersistable())
				throw new ArgumentException($"Not {typeof(IPersistable)}.", nameof(adapterType));

			if (!adapterType.Is<IPersistableAdapter>())
				throw new ArgumentException($"Not {typeof(IPersistableAdapter)}.", nameof(adapterType));

			return adapterType;
		}

		public static void RegisterAdapterType(this Type type, Type adapterType)
			=> _adapterTypes.Add(type, ValidateAdapterType(adapterType));

		public static bool RemoveAdapterType(this Type type)
			=> _adapterTypes.Remove(type);

		public static bool TryGetAdapterType(this Type type, out Type adapterType)
			=> _adapterTypes.TryGetValue(type, out adapterType);

		public static bool IsPersistable(this Type type)
			=> type.Is<IPersistable>() || type.Is<IAsyncPersistable>();

		private const string _typeKey = "type";
		private const string _valueKey = "value";
		private const string _settingsKey = "settings";

		/// <summary>
		/// Создать и инициализировать объект.
		/// </summary>
		/// <param name="storage">Хранилище настроек, на основе которого будет создает объект.</param>
		/// <returns>Объект.</returns>
		public static T LoadEntire<T>(this SettingsStorage storage)
			where T : IPersistable
		{
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			var instance = storage.GetValue<Type>(_typeKey).CreateInstance<T>();
			instance.Load(storage, _settingsKey);
			return instance;
		}

		public static SettingsStorage SaveEntire(this IPersistable persistable, bool isAssemblyQualifiedName)
		{
			if (persistable is null)
				throw new ArgumentNullException(nameof(persistable));

			return new SettingsStorage()
				.Set(_typeKey, persistable.GetType().GetTypeAsString(isAssemblyQualifiedName))
				.Set(_settingsKey, persistable.Save());
		}

		public static T Clone<T>(this T obj)
			where T : IPersistable
		{
			if (obj.IsNull())
				return default;

			var clone = obj.GetType().CreateInstance<T>();
			clone.Load(obj.Save());
			return clone;
		}

		public static async ValueTask<T> CloneAsync<T>(this T obj, CancellationToken cancellationToken = default)
			where T : IAsyncPersistable
		{
			if (obj.IsNull())
				return default;

			var clone = obj.GetType().CreateInstance<T>();
			await clone.LoadAsync(await obj.SaveAsync(cancellationToken), cancellationToken);
			return clone;
		}

		public static void Apply<T>(this T obj, T clone)
			where T : IPersistable
		{
			obj.Load(clone.Save());
		}

		public static async ValueTask ApplyAsync<T>(this T obj, T clone, CancellationToken cancellationToken = default)
			where T : IAsyncPersistable
		{
			await obj.LoadAsync(await clone.SaveAsync(cancellationToken), cancellationToken);
		}

		public static async ValueTask<SettingsStorage> SaveAsync(this IAsyncPersistable persistable, CancellationToken cancellationToken = default)
		{
			if (persistable is null)
				throw new ArgumentNullException(nameof(persistable));

			var storage = new SettingsStorage();
			await persistable.SaveAsync(storage, cancellationToken);
			return storage;
		}

		public static async ValueTask<IAsyncPersistable> LoadAsync(this SettingsStorage storage, Type type, CancellationToken cancellationToken = default)
		{
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			var obj = type.CreateInstance<IAsyncPersistable>();
			await obj.LoadAsync(storage, cancellationToken);
			return obj;
		}

		public static async ValueTask<T> LoadAsync<T>(this SettingsStorage storage, CancellationToken cancellationToken = default)
			where T : IAsyncPersistable, new()
			=> (T)await storage.LoadAsync(typeof(T), cancellationToken);

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="persistable">Сохраняемый объект.</param>
		/// <returns>Хранилище настроек.</returns>
		public static SettingsStorage Save(this IPersistable persistable)
		{
			if (persistable is null)
				throw new ArgumentNullException(nameof(persistable));

			var storage = new SettingsStorage();
			persistable.Save(storage);
			return storage;
		}

		public static IPersistable Load(this SettingsStorage storage, Type type)
		{
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			var obj = type.CreateInstance<IPersistable>();
			obj.Load(storage);
			return obj;
		}

		public static T Load<T>(this SettingsStorage storage)
			where T : IPersistable
			=> (T)storage.Load(typeof(T));

		public static void ForceLoad<T>(this T t, SettingsStorage storage)
			where T : IPersistable
			=> t.Load(storage);

		/// <summary>
		/// Добавить значение в настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		/// <param name="name">Название значения.</param>
		/// <param name="persistable">Сохраняемый объект.</param>
		public static void SetValue(this SettingsStorage storage, string name, IPersistable persistable)
		{
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			storage.SetValue(name, persistable.Save());
		}

		[Obsolete("Use overload with serializer param.")]
		public static void LoadFromString<TSerializer>(this IPersistable persistable, string value)
			where TSerializer : ISerializer<SettingsStorage>, new()
			=> new TSerializer().LoadFromString(persistable, value);

		[Obsolete("Use overload with serializer param.")]
		public static SettingsStorage LoadFromString<TSerializer>(this string value)
			where TSerializer : ISerializer<SettingsStorage>, new()
			=> new TSerializer().LoadFromString(value);

		public static void LoadFromString(this ISerializer<SettingsStorage> serializer, IPersistable persistable, string value)
		{
			if (persistable is null)
				throw new ArgumentNullException(nameof(persistable));

			persistable.Load(serializer.LoadFromString(value));
		}

		public static TValue LoadFromString<TValue>(this ISerializer<TValue> serializer, string value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			return Do.Invariant(() => serializer.Deserialize(value.UTF8()));
		}

		[Obsolete("Use overload with serializer param.")]
		public static string SaveToString<TSerializer>(this IPersistable persistable)
			where TSerializer : ISerializer<SettingsStorage>, new()
			=> new TSerializer().SaveToString(persistable);

		[Obsolete("Use overload with serializer param.")]
		public static string SaveToString<TSerializer>(this SettingsStorage settings)
			where TSerializer : ISerializer<SettingsStorage>, new()
			=> new TSerializer().SaveToString(settings);

		public static string SaveToString(this ISerializer<SettingsStorage> serializer, IPersistable persistable)
		{
			if (persistable is null)
				throw new ArgumentNullException(nameof(persistable));

			return serializer.SaveToString(persistable.Save());
		}

		public static string SaveToString<TValue>(this ISerializer<TValue> serializer, TValue settings)
		{
			if (serializer is null)
				throw new ArgumentNullException(nameof(serializer));

			if (settings is null)
				throw new ArgumentNullException(nameof(settings));

			return Do.Invariant(() => serializer.Serialize(settings).UTF8());
		}

		public static bool IsSerializablePrimitive(this Type type)
			=> type.IsPrimitive() || type == typeof(Uri);

		public static SettingsStorage ToStorage(this IRefTuple tuple)
		{
			if (tuple is null)
				throw new ArgumentNullException(nameof(tuple));

			var storage = new SettingsStorage();
			var idx = 0;

			foreach (var value in tuple.Values)
			{
				storage.Set(RefTuple.GetName(idx++), value);
			}

			return storage;
		}

		public static RefPair<T1, T2> ToRefPair<T1, T2>(this SettingsStorage storage)
		{
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			var tuple = new RefPair<T1, T2>();
			tuple.First = storage.GetValue<T1>(nameof(tuple.First));
			tuple.Second = storage.GetValue<T2>(nameof(tuple.Second));
			return tuple;
		}

		public static RefTriple<T1, T2, T3> ToRefTriple<T1, T2, T3>(this SettingsStorage storage)
		{
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			var tuple = new RefTriple<T1, T2, T3>();
			tuple.First = storage.GetValue<T1>(nameof(tuple.First));
			tuple.Second = storage.GetValue<T2>(nameof(tuple.Second));
			tuple.Third = storage.GetValue<T3>(nameof(tuple.Third));
			return tuple;
		}

		public static RefQuadruple<T1, T2, T3, T4> ToRefQuadruple<T1, T2, T3, T4>(this SettingsStorage storage)
		{
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			var tuple = new RefQuadruple<T1, T2, T3, T4>();
			tuple.First = storage.GetValue<T1>(nameof(tuple.First));
			tuple.Second = storage.GetValue<T2>(nameof(tuple.Second));
			tuple.Third = storage.GetValue<T3>(nameof(tuple.Third));
			tuple.Fourth = storage.GetValue<T4>(nameof(tuple.Fourth));
			return tuple;
		}

		public static RefFive<T1, T2, T3, T4, T5> ToRefFive<T1, T2, T3, T4, T5>(this SettingsStorage storage)
		{
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			var tuple = new RefFive<T1, T2, T3, T4, T5>();
			tuple.First = storage.GetValue<T1>(nameof(tuple.First));
			tuple.Second = storage.GetValue<T2>(nameof(tuple.Second));
			tuple.Third = storage.GetValue<T3>(nameof(tuple.Third));
			tuple.Fourth = storage.GetValue<T4>(nameof(tuple.Fourth));
			tuple.Fifth = storage.GetValue<T5>(nameof(tuple.Fifth));
			return tuple;
		}

		public static MemberInfo ToMember(this SettingsStorage storage)
			=> storage.ToMember<MemberInfo>();

		public static T ToMember<T>(this SettingsStorage storage)
			where T : MemberInfo
		{
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			var type = storage.GetValue<Type>(_typeKey);
			var member = storage.GetValue(_valueKey, storage.GetValue("name", string.Empty));

			return member.IsEmpty() ? type.To<T>() : type.GetMember<T>(member);
		}

		public static SettingsStorage ToStorage<T>(this T member, bool isAssemblyQualifiedName = default)
			where T : MemberInfo
		{
			if (member is null)
				throw new ArgumentNullException(nameof(member));

			var storage = new SettingsStorage();

			storage.Set(_typeKey, (member as Type ?? member.ReflectedType).GetTypeAsString(isAssemblyQualifiedName));

			if (member.ReflectedType != null)
				storage.Set(_valueKey, member.Name);
			
			return storage;
		}

		public static void Load(this IPersistable persistable, SettingsStorage settings, string name)
		{
			if (persistable is null)
				throw new ArgumentNullException(nameof(persistable));

			if (settings is null)
				throw new ArgumentNullException(nameof(settings));

			persistable.Load(settings.GetValue<SettingsStorage>(name));
		}

		public static bool LoadIfNotNull(this IPersistable persistable, SettingsStorage settings, string name)
		{
			if (settings is null)
				throw new ArgumentNullException(nameof(settings));

			return persistable.LoadIfNotNull(settings.GetValue<SettingsStorage>(name));
		}

		public static bool LoadIfNotNull(this IPersistable persistable, SettingsStorage storage)
		{
			if (persistable is null)
				throw new ArgumentNullException(nameof(persistable));

			if (storage is null)
				return false;

			persistable.Load(storage);
			return true;
		}

		public static SettingsStorage ToStorage(this object value, bool isAssemblyQualifiedName = default)
			=> new SettingsStorage()
				.Set(_typeKey, value.CheckOnNull().GetType().GetTypeAsString(isAssemblyQualifiedName))
				.Set(_valueKey, value.To<string>())
			;

		public static object FromStorage(this SettingsStorage storage)
		{
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			var value = storage.GetValue<string>(_valueKey).To(storage.GetValue<Type>(_typeKey));

			if (value is DateTime dt)
				value = dt.ToUniversalTime();

			return value;
		}

		private static readonly SynchronizedDictionary<Type, (Func<object, SettingsStorage> serialize, Func<SettingsStorage, object> deserialize)> _customSerializers = [];

		public static void RegisterCustomSerializer<T>(Func<T, SettingsStorage> serialize, Func<SettingsStorage, T> deserialize)
		{
			if (serialize is null)		throw new ArgumentNullException(nameof(serialize));
			if (deserialize is null)	throw new ArgumentNullException(nameof(deserialize));

			_customSerializers[typeof(T)] = (o => serialize((T)o), s => deserialize(s));
		}

		public static void UnRegisterCustomSerializer<T>()
			=> _customSerializers.Remove(typeof(T));

		public static bool TrySerialize<T>(this T value, out SettingsStorage storage)
		{
			storage = default;

			if (!_customSerializers.TryGetValue(typeof(T), out var serializer))
				return false;

			storage = serializer.serialize(value);
			return true;
		}

		public static bool TryDeserialize<T>(this SettingsStorage storage, out T value)
		{
			value = default;

			if (!_customSerializers.TryGetValue(typeof(T), out var serializer))
				return false;

			value = (T)serializer.deserialize(storage);
			return true;
		}
	}
}