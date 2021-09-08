namespace Ecng.Serialization
{
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

	/// <summary>
	/// Специальный класс для сохранения и загрузки настроек. Поддерживает еирархическую вложенность через метод <see cref="SetValue{T}"/>.
	/// </summary>
	public class SettingsStorage : SynchronizedDictionary<string, object>
	{
		private readonly JsonReader _reader;
		private readonly Func<JsonReader, SettingsStorage, string, Type, CancellationToken, Task<object>> _readJson;

		/// <summary>
		/// Создать <see cref="SettingsStorage"/>.
		/// </summary>
		public SettingsStorage()
			: base(StringComparer.InvariantCultureIgnoreCase)
		{
		}

		internal SettingsStorage(JsonReader reader, Func<JsonReader, SettingsStorage, string, Type, CancellationToken, Task<object>> readJson)
			: this()
		{
			_reader = reader ?? throw new ArgumentNullException(nameof(reader));
			_readJson = readJson ?? throw new ArgumentNullException(nameof(readJson));
		}

		/// <summary>
		/// Все названия значений, хранящиеся в настройках.
		/// </summary>
		public IEnumerable<string> Names => this.SyncGet(d => d.Keys.ToArray());

		public SettingsStorage Set<T>(string name, T value)
		{
			SetValue(name, value);
			return this;
		}

		private void EnsureIsNotReader()
		{
			if (_reader != null)
				throw new InvalidOperationException("_reader != null");
		}

		/// <summary>
		/// Добавить значение в настройки.
		/// </summary>
		/// <typeparam name="T">Тип значения. Если тип значения равен <see cref="SettingsStorage"/>, то образуется иерархия.</typeparam>
		/// <param name="name">Название значения.</param>
		/// <param name="value">Значение.</param>
		public void SetValue<T>(string name, T value)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			EnsureIsNotReader();

			this[name] = value;
		}

		/// <summary>
		/// Проверить, содержится ли значение в настройках.
		/// </summary>
		/// <param name="name">Название значения.</param>
		/// <returns>True, если значение содержится, иначе, false.</returns>
		public bool Contains(string name)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			return ContainsKey(name);
		}

		public override bool ContainsKey(string key)
		{
			EnsureIsNotReader();

			return base.ContainsKey(key);
		}

		internal int DeepLevel { get; set; }

		/// <summary>
		/// Получить значение из настроек.
		/// </summary>
		/// <typeparam name="T">Тип значения.</typeparam>
		/// <param name="name">Название значения.</param>
		/// <param name="defaultValue">Значения по умолчанию, если по названию <paramref name="name"/> не было найдено значения.</param>
		/// <returns>Значение. Если по названию <paramref name="name"/> не было найдено сохраненного значения, то будет возвращено <paramref name="defaultValue"/>.</returns>
		public T GetValue<T>(string name, T defaultValue = default)
			=> (T)GetValue(typeof(T), name, defaultValue);

		/// <summary>
		/// Получить значение из настроек.
		/// </summary>
		/// <typeparam name="T">Тип значения.</typeparam>
		/// <param name="name">Название значения.</param>
		/// <param name="defaultValue">Значения по умолчанию, если по названию <paramref name="name"/> не было найдено значения.</param>
		/// <returns>Значение. Если по названию <paramref name="name"/> не было найдено сохраненного значения, то будет возвращено <paramref name="defaultValue"/>.</returns>
		public object GetValue(Type type, string name, object defaultValue = default)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			if (_reader != null)
			{
				var res = GetValueFromReaderAsync(type, name, default).Result;

				if (res is null)
					return defaultValue;

				return res;
			}

			if (!TryGetValue(name, out var value))
				return defaultValue;

			if (value is SettingsStorage storage)
			{
				if (type == typeof(SettingsStorage))
					return value;

				var per = Activator.CreateInstance(type);

				if (per is IAsyncPersistable asyncPer)
					asyncPer.LoadAsync(storage, default).Wait();
				else
					((IPersistable)per).Load(storage);

				return per;
			}
			else if (type.IsCollection() && type.GetElementType().IsPersistable())
			{
				if (value is null)
					return default;

				var elemType = type.GetElementType();

				var arr = ((IEnumerable)value)
					.Cast<SettingsStorage>()
					.Select(storage =>
					{
						if (storage is null)
							return null;

						var per = Activator.CreateInstance(elemType);

						if (per is IAsyncPersistable asyncPer)
							asyncPer.LoadAsync(storage, default).Wait();
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

		public T TryGet<T>(string name, T defaultValue = default)
			=> GetValue(name, defaultValue);

		public object TryGet(Type type, string name, object defaultValue = default)
			=> GetValue(type, name, defaultValue);

		public async Task<T> GetValueAsync<T>(string name, T defaultValue = default, CancellationToken cancellationToken = default)
			=> (T)await GetValueAsync(typeof(T), name, defaultValue, cancellationToken);

		public async Task<object> GetValueAsync(Type type, string name, object defaultValue = default, CancellationToken cancellationToken = default)
		{
			if (_reader is null)
				return GetValue(type, name, defaultValue);
			else
				return await GetValueFromReaderAsync(type, name, cancellationToken) ?? defaultValue;
		}

		private async Task<object> GetValueFromReaderAsync(Type type, string name, CancellationToken cancellationToken)
			=> await _readJson(_reader, this, name, type, cancellationToken);
	}
}