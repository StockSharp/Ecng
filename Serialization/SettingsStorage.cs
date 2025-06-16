namespace Ecng.Serialization;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Reflection;

using Newtonsoft.Json;

using Nito.AsyncEx;

/// <summary>
/// Provides a synchronized storage for application settings with support for XML serialization.
/// </summary>
public class SettingsStorage : SynchronizedDictionary<string, object>
{
	private readonly JsonReader _reader;
	private readonly Func<JsonReader, SettingsStorage, string, Type, CancellationToken, ValueTask<object>> _readJson;

	/// <summary>
	/// Initializes a new instance of the <see cref="SettingsStorage"/> class.
	/// </summary>
	public SettingsStorage()
		: base(StringComparer.InvariantCultureIgnoreCase)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SettingsStorage"/> class using the specified JSON reader and delegate for reading JSON.
	/// </summary>
	/// <param name="reader">The JSON reader used for deserialization.</param>
	/// <param name="readJson">The delegate to read JSON values.</param>
	internal SettingsStorage(JsonReader reader, Func<JsonReader, SettingsStorage, string, Type, CancellationToken, ValueTask<object>> readJson)
		: this()
	{
		_reader = reader ?? throw new ArgumentNullException(nameof(reader));
		_readJson = readJson ?? throw new ArgumentNullException(nameof(readJson));
	}

	/// <summary>
	/// Gets the names of all stored settings.
	/// </summary>
	public IEnumerable<string> Names => this.SyncGet(d => d.Keys.ToArray());

	/// <summary>
	/// Sets the value for a specified setting name.
	/// </summary>
	/// <typeparam name="T">The type of the value to set.</typeparam>
	/// <param name="name">The name of the setting.</param>
	/// <param name="value">The value to set.</param>
	/// <returns>The current instance of <see cref="SettingsStorage"/>.</returns>
	public SettingsStorage Set<T>(string name, T value)
	{
		SetValue(name, value);
		return this;
	}

	/// <summary>
	/// Ensures that the current instance is not used as a reader.
	/// </summary>
	private void EnsureIsNotReader()
	{
		if (_reader != null)
			throw new InvalidOperationException("_reader != null");
	}

	/// <summary>
	/// Sets the value for a specified setting name.
	/// </summary>
	/// <typeparam name="T">The type of the value being set.</typeparam>
	/// <param name="name">The name of the setting.</param>
	/// <param name="value">The value to set.</param>
	public void SetValue<T>(string name, T value)
	{
		EnsureIsNotReader();

		this[name.ThrowIfEmpty(nameof(name))] = value;
	}

	/// <summary>
	/// Determines whether the storage contains a setting with the specified name.
	/// </summary>
	/// <param name="name">The name of the setting.</param>
	/// <returns><c>true</c> if the setting exists; otherwise, <c>false</c>.</returns>
	public bool Contains(string name)
	{
		return ContainsKey(name.ThrowIfEmpty(nameof(name)));
	}

	/// <summary>
	/// Determines whether the storage contains a setting with the given key.
	/// </summary>
	/// <param name="key">The key of the setting.</param>
	/// <returns><c>true</c> if the key exists; otherwise, <c>false</c>.</returns>
	public override bool ContainsKey(string key)
	{
		EnsureIsNotReader();

		return base.ContainsKey(key);
	}

	/// <summary>
	/// Gets or sets the deep level of deserialization.
	/// </summary>
	internal int DeepLevel { get; set; }

	/// <summary>
	/// Gets the value of a setting with the specified name and converts it to the specified type.
	/// </summary>
	/// <typeparam name="T">The type of value expected.</typeparam>
	/// <param name="name">The name of the setting.</param>
	/// <param name="defaultValue">The default value if the setting is not found or is null.</param>
	/// <returns>The setting value converted to type <typeparamref name="T"/>.</returns>
	public T GetValue<T>(string name, T defaultValue = default)
		=> (T)GetValue(typeof(T), name, defaultValue);

	/// <summary>
	/// Gets the value of a setting with the specified name and converts it to the given type.
	/// </summary>
	/// <param name="type">The type of value expected.</param>
	/// <param name="name">The name of the setting.</param>
	/// <param name="defaultValue">The default value if the setting is not found or null.</param>
	/// <returns>The setting value converted to the specified type.</returns>
	public object GetValue(Type type, string name, object defaultValue = default)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		if (name.IsEmpty())
			throw new ArgumentNullException(nameof(name));

		if (_reader != null)
		{
			var res = AsyncHelper.Run(() => GetValueFromReaderAsync(type, name, default));

			if (res is null)
				return defaultValue;

			return res;
		}

		if (!TryGetValue(name, out var value))
			return defaultValue;

		if (value is SettingsStorage storage)
		{
			if (typeof(SettingsStorage).Is(type))
				return storage;

			var obj = Activator.CreateInstance(type);

			if (obj is IAsyncPersistable asyncPer)
				AsyncContext.Run(() => asyncPer.LoadAsync(storage, default));
			else if (obj is IPersistable per)
				per.Load(storage);
			else
				throw new ArgumentOutOfRangeException(type.To<string>());

			return obj;
		}
		else if (type.IsCollection() && type.GetItemType().IsPersistable())
		{
			if (value is null)
				return default;

			var elemType = type.GetItemType();

			var arr = ((IEnumerable)value)
				.Cast<SettingsStorage>()
				.Select(storage =>
				{
					if (storage is null)
						return null;

					var per = Activator.CreateInstance(elemType);

					if (per is IAsyncPersistable asyncPer)
						AsyncContext.Run(() => asyncPer.LoadAsync(storage, default));
					else
						((IPersistable)per).Load(storage);

					return per;
				})
				.ToArray();

			var typedArr = elemType.CreateArray(arr.Length);
			arr.CopyTo(typedArr, 0);
			return typedArr.To(type);
		}
		else if (type == typeof(SecureString) && value is string str)
		{
			value = SecureStringEncryptor.Instance.Decrypt(str.Base64());
		}

		return value.To(type);
	}

	/// <summary>
	/// Gets the value of a setting with the specified name and converts it to the specified type. Returns a default value if not found.
	/// </summary>
	/// <typeparam name="T">The type of the value expected.</typeparam>
	/// <param name="name">The name of the setting.</param>
	/// <param name="defaultValue">The default value if the setting is not found.</param>
	/// <returns>The setting value converted to type <typeparamref name="T"/>.</returns>
	public T TryGet<T>(string name, T defaultValue = default)
		=> (T)TryGet(typeof(T), name, defaultValue);

	/// <summary>
	/// Gets the value of a setting with the specified name and converts it to the given type. Returns a default value if not found.
	/// </summary>
	/// <param name="type">The expected type of the setting.</param>
	/// <param name="name">The name of the setting.</param>
	/// <param name="defaultValue">The default value if the setting is not found.</param>
	/// <returns>The setting value converted to the specified type.</returns>
	public object TryGet(Type type, string name, object defaultValue = default)
		=> GetValue(type, name, defaultValue);

	/// <summary>
	/// Asynchronously gets the value of a setting with the specified name and converts it to the specified type.
	/// </summary>
	/// <typeparam name="T">The expected type of the value.</typeparam>
	/// <param name="name">The name of the setting.</param>
	/// <param name="defaultValue">The default value if the setting is not found.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>A <see cref="ValueTask{T}"/> representing the asynchronous operation producing the value converted to type <typeparamref name="T"/>.</returns>
	public async ValueTask<T> GetValueAsync<T>(string name, T defaultValue = default, CancellationToken cancellationToken = default)
		=> (T)await GetValueAsync(typeof(T), name, defaultValue, cancellationToken);

	/// <summary>
	/// Asynchronously gets the value of a setting with the specified name and converts it to the given type.
	/// </summary>
	/// <param name="type">The expected type of the value.</param>
	/// <param name="name">The name of the setting.</param>
	/// <param name="defaultValue">The default value if the setting is not found.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous operation producing the setting value converted to the specified type.</returns>
	public async ValueTask<object> GetValueAsync(Type type, string name, object defaultValue = default, CancellationToken cancellationToken = default)
	{
		if (_reader is null)
			return GetValue(type, name, defaultValue);
		else
			return await GetValueFromReaderAsync(type, name, cancellationToken) ?? defaultValue;
	}

	/// <summary>
	/// Asynchronously gets the value from the JSON reader using the provided delegate.
	/// </summary>
	/// <param name="type">The expected type of the value.</param>
	/// <param name="name">The name of the setting.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous operation producing the value converted to the specified type.</returns>
	private async ValueTask<object> GetValueFromReaderAsync(Type type, string name, CancellationToken cancellationToken)
		=> await _readJson(_reader, this, name, type, cancellationToken);
}