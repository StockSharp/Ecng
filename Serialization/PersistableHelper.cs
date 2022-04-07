namespace Ecng.Serialization
{
	using System;
	using System.Globalization;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Reflection;

	public static class PersistableHelper
	{
		private static readonly CachedSynchronizedDictionary<Type, Type> _adapterTypes = new();

		private static Type ValidateAdapterType(Type adapterType)
		{
			if (adapterType is null)
				throw new ArgumentNullException(nameof(adapterType));

			if (!adapterType.IsPersistable())
				throw new ArgumentException(nameof(adapterType));

			if (!adapterType.Is<IPersistableAdapter>())
				throw new ArgumentException(nameof(adapterType));

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

			var instance = storage.GetValue<Type>("type").CreateInstance<T>();
			instance.Load(storage.GetValue<SettingsStorage>("settings"));
			return instance;
		}

		public static SettingsStorage SaveEntire(this IPersistable persistable, bool isAssemblyQualifiedName)
		{
			if (persistable is null)
				throw new ArgumentNullException(nameof(persistable));

			return new SettingsStorage()
				.Set("type", persistable.GetType().GetTypeName(isAssemblyQualifiedName))
				.Set("settings", persistable.Save());
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

		public static void LoadFromString<TSerializer>(this IPersistable persistable, string value)
			where TSerializer : ISerializer<SettingsStorage>, new()
		{
			if (persistable is null)
				throw new ArgumentNullException(nameof(persistable));

			persistable.Load(value.LoadFromString<TSerializer>());
		}

		public static SettingsStorage LoadFromString<TSerializer>(this string value)
			where TSerializer : ISerializer<SettingsStorage>, new()
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			return CultureInfo.InvariantCulture.DoInCulture(() => new TSerializer().Deserialize(value.UTF8()));
		}

		public static string SaveToString<TSerializer>(this IPersistable persistable)
			where TSerializer : ISerializer<SettingsStorage>, new()
		{
			if (persistable is null)
				throw new ArgumentNullException(nameof(persistable));

			return persistable.Save().SaveToString<TSerializer>();
		}

		public static string SaveToString<TSerializer>(this SettingsStorage settings)
			where TSerializer : ISerializer<SettingsStorage>, new()
		{
			if (settings is null)
				throw new ArgumentNullException(nameof(settings));

			return CultureInfo.InvariantCulture.DoInCulture(() => new TSerializer().Serialize(settings).UTF8());
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

			var type = storage.GetValue<Type>("type");
			var member = storage.GetValue("name", string.Empty);

			return member.IsEmpty() ? type.To<T>() : type.GetMember<T>(member);
		}

		public static SettingsStorage ToStorage<T>(this T member, bool isAssemblyQualifiedName)
			where T : MemberInfo
		{
			if (member is null)
				throw new ArgumentNullException(nameof(member));

			var storage = new SettingsStorage();

			storage.Set("type", (member as Type ?? member.ReflectedType).GetTypeName(isAssemblyQualifiedName));

			if (member.ReflectedType != null)
				storage.Set("name", member.Name);
			
			return storage;
		}

		public static bool LoadIfNotNull(this IPersistable persistable, SettingsStorage storage)
		{
			if (storage is null)
				return false;

			persistable.Load(storage);
			return true;
		}
	}
}