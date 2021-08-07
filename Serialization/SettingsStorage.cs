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

		/// <summary>
		/// Создать <see cref="SettingsStorage"/>.
		/// </summary>
		public SettingsStorage()
			: base(StringComparer.InvariantCultureIgnoreCase)
		{
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

			if (_reader != null)
				throw new InvalidOperationException("_reader != null");

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
			{
				if (!_reader.Read())
					throw new InvalidOperationException("EOF");

				if (_reader.TokenType != JsonToken.PropertyName)
					throw new InvalidOperationException($"{_reader.TokenType} != {JsonToken.PropertyName}");

				if (!((string)_reader.Value).CompareIgnoreCase(name))
					throw new InvalidOperationException($"{_reader.Value} != {name}");

				if (!_reader.Read())
					throw new InvalidOperationException("EOF");

				return _reader.Value.To<T>();
			}

			return TryGetValue(name, out var value) ? value.To<T>() : default;
		}

		public T TryGet<T>(string name, T defaultValue = default)
			=> GetValue(name, defaultValue);

		public async Task<T> GetValueAsync<T>(string name, T defaultValue = default, CancellationToken cancellationToken = default)
		{
			if (_reader is null)
				return GetValue(name, defaultValue);
			else
			{
				if (!await _reader.ReadAsync(cancellationToken))
					throw new InvalidOperationException("EOF");

				if (_reader.TokenType != JsonToken.PropertyName)
					throw new InvalidOperationException($"{_reader.TokenType} != {JsonToken.PropertyName}");

				if (!((string)_reader.Value).CompareIgnoreCase(name))
					throw new InvalidOperationException($"{_reader.Value} != {name}");

				if (!await _reader.ReadAsync(cancellationToken))
					throw new InvalidOperationException("EOF");

				return _reader.Value.To<T>();
			}
		}
	}
}