namespace Ecng.Serialization;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Reflection;

/// <summary>
/// Provides helper methods for persisting, cloning, loading and saving objects.
/// </summary>
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

	/// <summary>
	/// Registers the adapter type for the given type.
	/// </summary>
	/// <param name="type">The type for which the adapter is registered.</param>
	/// <param name="adapterType">The adapter type to register.</param>
	public static void RegisterAdapterType(this Type type, Type adapterType)
		=> _adapterTypes.Add(type, ValidateAdapterType(adapterType));

	/// <summary>
	/// Removes the registered adapter type for the given type.
	/// </summary>
	/// <param name="type">The type whose adapter registration is removed.</param>
	/// <returns><c>true</c> if the adapter was removed; otherwise, <c>false</c>.</returns>
	public static bool RemoveAdapterType(this Type type)
		=> _adapterTypes.Remove(type);

	/// <summary>
	/// Tries to get the registered adapter type for the given type.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <param name="adapterType">When this method returns, contains the adapter type if found.</param>
	/// <returns><c>true</c> if found; otherwise, <c>false</c>.</returns>
	public static bool TryGetAdapterType(this Type type, out Type adapterType)
		=> _adapterTypes.TryGetValue(type, out adapterType);

	/// <summary>
	/// Determines whether the specified type is persistable.
	/// </summary>
	/// <param name="type">The type to evaluate.</param>
	/// <returns><c>true</c> if the type implements IPersistable or IAsyncPersistable; otherwise, <c>false</c>.</returns>
	public static bool IsPersistable(this Type type)
		=> type.Is<IPersistable>() || type.Is<IAsyncPersistable>();

	private const string _typeKey = "type";
	private const string _valueKey = "value";
	private const string _settingsKey = "settings";

	/// <summary>
	/// Creates and initializes an object from the specified settings storage.
	/// </summary>
	/// <typeparam name="T">The type of the persistable object.</typeparam>
	/// <param name="storage">The settings storage used to create the object.</param>
	/// <returns>The created and initialized object.</returns>
	public static T LoadEntire<T>(this SettingsStorage storage)
		where T : IPersistable
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		var instance = storage.GetValue<Type>(_typeKey).CreateInstance<T>();
		instance.Load(storage, _settingsKey);
		return instance;
	}

	/// <summary>
	/// Saves the entire state of the persistable object into a new settings storage.
	/// </summary>
	/// <param name="persistable">The persistable object to save.</param>
	/// <param name="isAssemblyQualifiedName">A value indicating whether the type name should be assembly qualified.</param>
	/// <returns>A settings storage containing the saved state.</returns>
	public static SettingsStorage SaveEntire(this IPersistable persistable, bool isAssemblyQualifiedName)
	{
		if (persistable is null)
			throw new ArgumentNullException(nameof(persistable));

		return new SettingsStorage()
			.Set(_typeKey, persistable.GetType().GetTypeAsString(isAssemblyQualifiedName))
			.Set(_settingsKey, persistable.Save());
	}

	/// <summary>
	/// Clones the specified persistable object.
	/// </summary>
	/// <typeparam name="T">The type of the persistable object.</typeparam>
	/// <param name="obj">The object to clone.</param>
	/// <returns>A clone of the object.</returns>
	public static T Clone<T>(this T obj)
		where T : IPersistable
	{
		if (obj.IsNull())
			return default;

		var clone = obj.GetType().CreateInstance<T>();
		clone.Load(obj.Save());
		return clone;
	}

	/// <summary>
	/// Asynchronously clones the specified asynchronous persistable object.
	/// </summary>
	/// <typeparam name="T">The type of the asynchronous persistable object.</typeparam>
	/// <param name="obj">The object to clone.</param>
	/// <param name="cancellationToken">A token for cancellation.</param>
	/// <returns>A ValueTask with the cloned object.</returns>
	public static async ValueTask<T> CloneAsync<T>(this T obj, CancellationToken cancellationToken = default)
		where T : IAsyncPersistable
	{
		if (obj.IsNull())
			return default;

		var clone = obj.GetType().CreateInstance<T>();
		await clone.LoadAsync(await obj.SaveAsync(cancellationToken), cancellationToken);
		return clone;
	}

	/// <summary>
	/// Applies the state from the clone to the target persistable object.
	/// </summary>
	/// <typeparam name="T">The type of the persistable object.</typeparam>
	/// <param name="obj">The target object.</param>
	/// <param name="clone">The object from which to copy the state.</param>
	public static void Apply<T>(this T obj, T clone)
		where T : IPersistable
	{
		obj.Load(clone.Save());
	}

	/// <summary>
	/// Asynchronously applies the state from the clone to the target asynchronous persistable object.
	/// </summary>
	/// <typeparam name="T">The type of the asynchronous persistable object.</typeparam>
	/// <param name="obj">The target object.</param>
	/// <param name="clone">The object from which to copy the state.</param>
	/// <param name="cancellationToken">A token for cancellation.</param>
	/// <returns>A ValueTask representing the asynchronous operation.</returns>
	public static async ValueTask ApplyAsync<T>(this T obj, T clone, CancellationToken cancellationToken = default)
		where T : IAsyncPersistable
	{
		await obj.LoadAsync(await clone.SaveAsync(cancellationToken), cancellationToken);
	}

	/// <summary>
	/// Asynchronously saves the state of the asynchronous persistable object to a settings storage.
	/// </summary>
	/// <param name="persistable">The asynchronous persistable object.</param>
	/// <param name="cancellationToken">A token for cancellation.</param>
	/// <returns>A ValueTask with the settings storage containing the saved state.</returns>
	public static async ValueTask<SettingsStorage> SaveAsync(this IAsyncPersistable persistable, CancellationToken cancellationToken = default)
	{
		if (persistable is null)
			throw new ArgumentNullException(nameof(persistable));

		var storage = new SettingsStorage();
		await persistable.SaveAsync(storage, cancellationToken);
		return storage;
	}

	/// <summary>
	/// Asynchronously loads an asynchronous persistable object using the specified type.
	/// </summary>
	/// <param name="storage">The settings storage to load from.</param>
	/// <param name="type">The type of the asynchronous persistable object.</param>
	/// <param name="cancellationToken">A token for cancellation.</param>
	/// <returns>A ValueTask with the loaded asynchronous persistable object.</returns>
	public static async ValueTask<IAsyncPersistable> LoadAsync(this SettingsStorage storage, Type type, CancellationToken cancellationToken = default)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		var obj = type.CreateInstance<IAsyncPersistable>();
		await obj.LoadAsync(storage, cancellationToken);
		return obj;
	}

	/// <summary>
	/// Asynchronously loads an asynchronous persistable object of type T.
	/// </summary>
	/// <typeparam name="T">The type of the asynchronous persistable object.</typeparam>
	/// <param name="storage">The settings storage to load from.</param>
	/// <param name="cancellationToken">A token for cancellation.</param>
	/// <returns>A ValueTask with the loaded object of type T.</returns>
	public static async ValueTask<T> LoadAsync<T>(this SettingsStorage storage, CancellationToken cancellationToken = default)
		where T : IAsyncPersistable, new()
		=> (T)await storage.LoadAsync(typeof(T), cancellationToken);

	/// <summary>
	/// Saves the state of the persistable object to a settings storage.
	/// </summary>
	/// <param name="persistable">The persistable object to save.</param>
	/// <returns>A settings storage containing the saved state.</returns>
	public static SettingsStorage Save(this IPersistable persistable)
	{
		if (persistable is null)
			throw new ArgumentNullException(nameof(persistable));

		var storage = new SettingsStorage();
		persistable.Save(storage);
		return storage;
	}

	/// <summary>
	/// Loads an IPersistable object of the specified type from the settings storage.
	/// </summary>
	/// <param name="storage">The settings storage to load from.</param>
	/// <param name="type">The type of the persistable object.</param>
	/// <returns>The loaded persistable object.</returns>
	public static IPersistable Load(this SettingsStorage storage, Type type)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		var obj = type.CreateInstance<IPersistable>();
		obj.Load(storage);
		return obj;
	}

	/// <summary>
	/// Loads an IPersistable object of type T from the settings storage.
	/// </summary>
	/// <typeparam name="T">The type of the persistable object.</typeparam>
	/// <param name="storage">The settings storage to load from.</param>
	/// <returns>The loaded object of type T.</returns>
	public static T Load<T>(this SettingsStorage storage)
		where T : IPersistable
		=> (T)storage.Load(typeof(T));

	/// <summary>
	/// Forces the persistable object to load its state from the given settings storage.
	/// </summary>
	/// <typeparam name="T">The type of the persistable object.</typeparam>
	/// <param name="t">The target object.</param>
	/// <param name="storage">The settings storage to load from.</param>
	public static void ForceLoad<T>(this T t, SettingsStorage storage)
		where T : IPersistable
		=> t.Load(storage);

	/// <summary>
	/// Adds a persistable object's state as a value in the settings storage.
	/// </summary>
	/// <param name="storage">The settings storage to update.</param>
	/// <param name="name">The name of the setting.</param>
	/// <param name="persistable">The persistable object whose state is added.</param>
	public static void SetValue(this SettingsStorage storage, string name, IPersistable persistable)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		storage.SetValue(name, persistable.Save());
	}

	/// <summary>
	/// Loads the state of the persistable object from a string using the provided serializer.
	/// </summary>
	/// <param name="serializer">The serializer to use.</param>
	/// <param name="persistable">The persistable object to load.</param>
	/// <param name="value">The string representation of the state.</param>
	public static void LoadFromString(this ISerializer<SettingsStorage> serializer, IPersistable persistable, string value)
	{
		if (persistable is null)
			throw new ArgumentNullException(nameof(persistable));

		persistable.Load(serializer.LoadFromString(value));
	}

	/// <summary>
	/// Loads a value of type TValue from a string using the provided serializer.
	/// </summary>
	/// <typeparam name="TValue">The type of the value to load.</typeparam>
	/// <param name="serializer">The serializer to use.</param>
	/// <param name="value">The string representation of the value.</param>
	/// <returns>The deserialized value.</returns>
	public static TValue LoadFromString<TValue>(this ISerializer<TValue> serializer, string value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		return Do.Invariant(() => serializer.Deserialize(value.UTF8()));
	}

	/// <summary>
	/// Saves the state of the persistable object to a string using the provided serializer.
	/// </summary>
	/// <param name="serializer">The serializer to use.</param>
	/// <param name="persistable">The persistable object to save.</param>
	/// <returns>A string representing the saved state.</returns>
	public static string SaveToString(this ISerializer<SettingsStorage> serializer, IPersistable persistable)
	{
		if (persistable is null)
			throw new ArgumentNullException(nameof(persistable));

		return serializer.SaveToString(persistable.Save());
	}

	/// <summary>
	/// Saves the settings to a string using the provided serializer.
	/// </summary>
	/// <typeparam name="TValue">The type of the settings.</typeparam>
	/// <param name="serializer">The serializer to use.</param>
	/// <param name="settings">The settings to save.</param>
	/// <returns>A string representing the settings.</returns>
	public static string SaveToString<TValue>(this ISerializer<TValue> serializer, TValue settings)
	{
		if (serializer is null)
			throw new ArgumentNullException(nameof(serializer));

		if (settings is null)
			throw new ArgumentNullException(nameof(settings));

		return Do.Invariant(() => serializer.Serialize(settings).UTF8());
	}

	/// <summary>
	/// Determines whether the specified type is a serializable primitive.
	/// </summary>
	/// <param name="type">The type to examine.</param>
	/// <returns><c>true</c> if the type is a primitive or Uri; otherwise, <c>false</c>.</returns>
	public static bool IsSerializablePrimitive(this Type type)
		=> type.IsPrimitive() || type == typeof(Uri);

	/// <summary>
	/// Converts a tuple implementing IRefTuple to a settings storage.
	/// </summary>
	/// <param name="tuple">The tuple to convert.</param>
	/// <returns>A settings storage representing the tuple.</returns>
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

	/// <summary>
	/// Converts the settings storage to a RefPair of the specified types.
	/// </summary>
	/// <typeparam name="T1">The type of the first element.</typeparam>
	/// <typeparam name="T2">The type of the second element.</typeparam>
	/// <param name="storage">The settings storage to convert.</param>
	/// <returns>A RefPair containing the converted values.</returns>
	public static RefPair<T1, T2> ToRefPair<T1, T2>(this SettingsStorage storage)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		var tuple = new RefPair<T1, T2>();
		tuple.First = storage.GetValue<T1>(nameof(tuple.First));
		tuple.Second = storage.GetValue<T2>(nameof(tuple.Second));
		return tuple;
	}

	/// <summary>
	/// Converts the settings storage to a RefTriple of the specified types.
	/// </summary>
	/// <typeparam name="T1">The type of the first element.</typeparam>
	/// <typeparam name="T2">The type of the second element.</typeparam>
	/// <typeparam name="T3">The type of the third element.</typeparam>
	/// <param name="storage">The settings storage to convert.</param>
	/// <returns>A RefTriple containing the converted values.</returns>
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

	/// <summary>
	/// Converts the settings storage to a RefQuadruple of the specified types.
	/// </summary>
	/// <typeparam name="T1">The type of the first element.</typeparam>
	/// <typeparam name="T2">The type of the second element.</typeparam>
	/// <typeparam name="T3">The type of the third element.</typeparam>
	/// <typeparam name="T4">The type of the fourth element.</typeparam>
	/// <param name="storage">The settings storage to convert.</param>
	/// <returns>A RefQuadruple containing the converted values.</returns>
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

	/// <summary>
	/// Converts the settings storage to a RefFive tuple of the specified types.
	/// </summary>
	/// <typeparam name="T1">The type of the first element.</typeparam>
	/// <typeparam name="T2">The type of the second element.</typeparam>
	/// <typeparam name="T3">The type of the third element.</typeparam>
	/// <typeparam name="T4">The type of the fourth element.</typeparam>
	/// <typeparam name="T5">The type of the fifth element.</typeparam>
	/// <param name="storage">The settings storage to convert.</param>
	/// <returns>A RefFive tuple containing the converted values.</returns>
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

	/// <summary>
	/// Converts the settings storage to a MemberInfo.
	/// </summary>
	/// <param name="storage">The settings storage to convert.</param>
	/// <returns>The converted MemberInfo.</returns>
	public static MemberInfo ToMember(this SettingsStorage storage)
		=> storage.ToMember<MemberInfo>();

	/// <summary>
	/// Converts the settings storage to a member of type T.
	/// </summary>
	/// <typeparam name="T">The expected type of the member.</typeparam>
	/// <param name="storage">The settings storage to convert.</param>
	/// <returns>The converted member of type T.</returns>
	public static T ToMember<T>(this SettingsStorage storage)
		where T : MemberInfo
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		var type = storage.GetValue<Type>(_typeKey);
		var member = storage.GetValue(_valueKey, storage.GetValue("name", string.Empty));

		return member.IsEmpty() ? type.To<T>() : type.GetMember<T>(member);
	}

	/// <summary>
	/// Converts the specified member to a settings storage.
	/// </summary>
	/// <typeparam name="T">The type of the member.</typeparam>
	/// <param name="member">The member to convert.</param>
	/// <param name="isAssemblyQualifiedName">A value indicating whether the type name should be assembly qualified.</param>
	/// <returns>A settings storage representing the member.</returns>
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

	/// <summary>
	/// Loads the state of the persistable object from the specified settings storage using the given key.
	/// </summary>
	/// <param name="persistable">The persistable object to load.</param>
	/// <param name="settings">The settings storage.</param>
	/// <param name="name">The name of the value within the storage.</param>
	public static void Load(this IPersistable persistable, SettingsStorage settings, string name)
	{
		if (persistable is null)
			throw new ArgumentNullException(nameof(persistable));

		if (settings is null)
			throw new ArgumentNullException(nameof(settings));

		persistable.Load(settings.GetValue<SettingsStorage>(name));
	}

	/// <summary>
	/// Loads the state of the persistable object from the specified settings storage using the given key if it is not null.
	/// </summary>
	/// <param name="persistable">The persistable object to load.</param>
	/// <param name="settings">The settings storage.</param>
	/// <param name="name">The name of the value within the storage.</param>
	/// <returns><c>true</c> if the state was loaded; otherwise, <c>false</c>.</returns>
	public static bool LoadIfNotNull(this IPersistable persistable, SettingsStorage settings, string name)
	{
		if (settings is null)
			throw new ArgumentNullException(nameof(settings));

		return persistable.LoadIfNotNull(settings.GetValue<SettingsStorage>(name));
	}

	/// <summary>
	/// Loads the state of the persistable object from the specified settings storage if the storage is not null.
	/// </summary>
	/// <param name="persistable">The persistable object to load.</param>
	/// <param name="storage">The settings storage.</param>
	/// <returns><c>true</c> if the state was loaded; otherwise, <c>false</c>.</returns>
	public static bool LoadIfNotNull(this IPersistable persistable, SettingsStorage storage)
	{
		if (persistable is null)
			throw new ArgumentNullException(nameof(persistable));

		if (storage is null)
			return false;

		persistable.Load(storage);
		return true;
	}

	/// <summary>
	/// Converts an object to a settings storage with type and value.
	/// </summary>
	/// <param name="value">The object to convert.</param>
	/// <param name="isAssemblyQualifiedName">A value indicating whether the type name should be assembly qualified.</param>
	/// <returns>A settings storage representing the object.</returns>
	public static SettingsStorage ToStorage(this object value, bool isAssemblyQualifiedName = default)
		=> new SettingsStorage()
			.Set(_typeKey, value.CheckOnNull().GetType().GetTypeAsString(isAssemblyQualifiedName))
			.Set(_valueKey, value is IPersistable pv ? (object)pv.Save() : value.To<string>())
		;

	/// <summary>
	/// Converts the settings storage back to an object.
	/// </summary>
	/// <param name="storage">The settings storage.</param>
	/// <returns>The object represented by the settings storage.</returns>
	public static object FromStorage(this SettingsStorage storage)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		var valueType = storage.GetValue<Type>(_typeKey);

		if (valueType.Is<IPersistable>())
		{
			var value = valueType.CreateInstance<IPersistable>();
			value.Load(storage, _valueKey);
			return value;
		}
		else
		{
			var value = storage.GetValue<string>(_valueKey).To(valueType);

			if (value is DateTime dt)
				value = dt.ToUniversalTime();

			return value;
		}
	}

	private static readonly SynchronizedDictionary<Type, (Func<object, SettingsStorage> serialize, Func<SettingsStorage, object> deserialize)> _customSerializers = [];

	/// <summary>
	/// Registers a custom serializer for a specific type.
	/// </summary>
	/// <typeparam name="T">The type for which to register the serializer.</typeparam>
	/// <param name="serialize">A function to serialize the object to a settings storage.</param>
	/// <param name="deserialize">A function to deserialize from a settings storage to the object.</param>
	public static void RegisterCustomSerializer<T>(Func<T, SettingsStorage> serialize, Func<SettingsStorage, T> deserialize)
	{
		if (serialize is null)		throw new ArgumentNullException(nameof(serialize));
		if (deserialize is null)	throw new ArgumentNullException(nameof(deserialize));

		_customSerializers[typeof(T)] = (o => serialize((T)o), s => deserialize(s));
	}

	/// <summary>
	/// Unregisters the custom serializer for a specific type.
	/// </summary>
	/// <typeparam name="T">The type for which to unregister the serializer.</typeparam>
	public static void UnRegisterCustomSerializer<T>()
		=> _customSerializers.Remove(typeof(T));

	/// <summary>
	/// Tries to serialize an object using a registered custom serializer.
	/// </summary>
	/// <typeparam name="T">The type of the object to serialize.</typeparam>
	/// <param name="value">The object to serialize.</param>
	/// <param name="storage">When this method returns, contains the resulting settings storage if serialization was successful.</param>
	/// <returns><c>true</c> if serialization was successful; otherwise, <c>false</c>.</returns>
	public static bool TrySerialize<T>(this T value, out SettingsStorage storage)
	{
		storage = default;

		if (!_customSerializers.TryGetValue(typeof(T), out var serializer))
			return false;

		storage = serializer.serialize(value);
		return true;
	}

	/// <summary>
	/// Tries to deserialize a settings storage into an object of type T using a registered custom serializer.
	/// </summary>
	/// <typeparam name="T">The type to deserialize.</typeparam>
	/// <param name="storage">The settings storage containing serialized data.</param>
	/// <param name="value">When this method returns, contains the deserialized object if successful.</param>
	/// <returns><c>true</c> if deserialization was successful; otherwise, <c>false</c>.</returns>
	public static bool TryDeserialize<T>(this SettingsStorage storage, out T value)
	{
		value = default;

		if (!_customSerializers.TryGetValue(typeof(T), out var serializer))
			return false;

		value = (T)serializer.deserialize(storage);
		return true;
	}
}