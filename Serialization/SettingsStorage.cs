namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Collections;
	using Ecng.Common;

	using Newtonsoft.Json;

	/// <summary>
	/// Специальный класс для сохранения и загрузки настроек. Поддерживает еирархическую вложенность через метод <see cref="SetValue{T}"/>.
	/// </summary>
	public class SettingsStorage : SynchronizedDictionary<string, object>
	{
		private readonly JsonReader _reader;
		private readonly SettingsStorage _parent;

		/// <summary>
		/// Создать <see cref="SettingsStorage"/>.
		/// </summary>
		public SettingsStorage()
			: base(StringComparer.InvariantCultureIgnoreCase)
		{
		}

		internal SettingsStorage(SettingsStorage parent)
			: this(parent._reader)
		{
			_parent = parent ?? throw new ArgumentNullException(nameof(parent));
		}

		internal SettingsStorage(JsonReader reader)
			: this()
		{
			_reader = reader ?? throw new ArgumentNullException(nameof(reader));
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

		private int _deepLevel;

		public int DeepLevel
		{
			get => _deepLevel;
			private set
			{
				var diff = value - _deepLevel;
				
				_deepLevel = value;

				if (_parent != null)
					_parent.DeepLevel += diff;
			}
		}

		/// <summary>
		/// Получить значение из настроек.
		/// </summary>
		/// <typeparam name="T">Тип значения.</typeparam>
		/// <param name="name">Название значения.</param>
		/// <param name="defaultValue">Значения по умолчанию, если по названию <paramref name="name"/> не было найдено значения.</param>
		/// <returns>Значение. Если по названию <paramref name="name"/> не было найдено сохраненного значения, то будет возвращено <paramref name="defaultValue"/>.</returns>
		public T GetValue<T>(string name, T defaultValue = default)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			if (_reader != null)
				return GetValueFromReaderAsync(name, defaultValue).Result;

			return TryGetValue(name, out var value) ? value.To<T>() : defaultValue;
		}

		public T TryGet<T>(string name, T defaultValue = default)
			=> GetValue(name, defaultValue);

		public async Task<T> GetValueAsync<T>(string name, T defaultValue = default, CancellationToken cancellationToken = default)
		{
			if (_reader is null)
				return GetValue(name, defaultValue);
			else
				return await GetValueFromReaderAsync(name, defaultValue, cancellationToken);
		}

		internal async Task TryClearDeepLevel(CancellationToken cancellationToken)
		{
			var lvl = DeepLevel;

			if (lvl == 0)
				return;

			for (var i = 1; i <= lvl; i++)
				await _reader.ReadWithCheckAsync(cancellationToken);

			DeepLevel = 0;
		}

		private async Task<T> GetValueFromReaderAsync<T>(string name, T defaultValue = default, CancellationToken cancellationToken = default)
		{
			await TryClearDeepLevel(cancellationToken);

			await _reader.ReadWithCheckAsync(cancellationToken);

			_reader.ChechExpectedToken(JsonToken.PropertyName);
			
			if (!((string)_reader.Value).EqualsIgnoreCase(name))
				throw new InvalidOperationException($"{_reader.Value} != {name}");

			await _reader.ReadWithCheckAsync(cancellationToken);

			if (_reader.TokenType == JsonToken.Null)
				return defaultValue;

			object value;

			if (typeof(T) == typeof(SettingsStorage))
			{
				//await _reader.ReadWithCheckAsync(cancellationToken);

				DeepLevel++;

				value = new SettingsStorage(this);
			}
			else if (typeof(IEnumerable<SettingsStorage>).IsAssignableFrom(typeof(T)))
			{
				await _reader.ReadWithCheckAsync(cancellationToken);

				var list = new List<SettingsStorage>();

				while (_reader.TokenType != JsonToken.EndArray)
				{
					switch (_reader.TokenType)
					{
						case JsonToken.StartObject:
						{
							var inner = new SettingsStorage();
							await inner.FillAsync(_reader, cancellationToken);
							list.Add(inner);
							break;
						}
						case JsonToken.Null:
							list.Add(null);
							break;
						default:
							throw new InvalidOperationException($"{_reader.TokenType} is out of range.");
					}

					await _reader.ReadWithCheckAsync(cancellationToken);
				}

				value = list.ToArray();
			}
			else
			{
				if (!typeof(T).IsSerializablePrimitive())
					throw new InvalidOperationException($"Type {typeof(T)} is not primitive.");

				value = _reader.Value;
			}

			return value.To<T>() ?? defaultValue;
		}

		public async Task FillAsync(JsonReader reader, CancellationToken cancellationToken)
		{
			while (true)
			{
				await reader.ReadWithCheckAsync(cancellationToken);

				if (reader.TokenType == JsonToken.EndObject)
					break;

				reader.ChechExpectedToken(JsonToken.PropertyName);

				var propName = (string)reader.Value;

				await reader.ReadWithCheckAsync(cancellationToken);

				object value;

				switch (reader.TokenType)
				{
					case JsonToken.StartObject:
					{
						var inner = new SettingsStorage();
						await inner.FillAsync(reader, cancellationToken);
						//await reader.ReadWithCheckAsync(cancellationToken);
						value = inner;
						break;
					}
					case JsonToken.StartArray:
					{
						await reader.ReadWithCheckAsync(cancellationToken);

						var list = new List<object>();

						while (reader.TokenType != JsonToken.EndArray)
						{
							switch (reader.TokenType)
							{
								case JsonToken.StartObject:
								{
									var inner = new SettingsStorage();
									await inner.FillAsync(reader, cancellationToken);
									list.Add(inner);
									break;
								}
								default:
									list.Add(reader.Value);
									break;
							}

							await reader.ReadWithCheckAsync(cancellationToken);
						}

						value = list.ToArray();
						break;
					}
					default:
						value = reader.Value;
						break;
				}

				Set(propName, value);
			}
		}
	}
}