namespace Ecng.Serialization
{
	using System;
	using System.Text;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Security;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Reflection;

	using Newtonsoft.Json;

	static class JsonConversions
	{
		static JsonConversions()
		{
			Converter.AddTypedConverter<object[], SecureString>(val => SecureStringEncryptor.Instance.Decrypt(val.Select(i => i.To<byte>()).ToArray()));
		}
	}

	public class JsonSerializer<T> : Serializer<T>
	{
		static JsonSerializer()
		{
			typeof(JsonConversions).EnsureRunClass();
		}

		public bool Indent { get; set; }
		public Encoding Encoding { get; set; } = Encoding.UTF8;
		public bool FillMode { get; set; }
		public bool EnumAsString { get; set; }
		public bool EncryptedAsByteArray { get; set; }
		public int BufferSize { get; set; } = 1024;

		public override ISerializer GetSerializer(Type entityType)
		{
			var serializer = base.GetSerializer(entityType);

			serializer.SetValue(nameof(Indent), Indent);
			serializer.SetValue(nameof(Encoding), Encoding);
			serializer.SetValue(nameof(FillMode), FillMode);
			serializer.SetValue(nameof(EnumAsString), EnumAsString);
			serializer.SetValue(nameof(EncryptedAsByteArray), EncryptedAsByteArray);
			serializer.SetValue(nameof(BufferSize), BufferSize);

			return serializer;
		}

		public override string FileExtension => "json";

		public override void Serialize(T graph, Stream stream)
			=> SerializeAsync(graph, stream, default).Wait();

		public override T Deserialize(Stream stream)
			=> DeserializeAsync(stream, default).Result;

		private static bool IsJsonPrimitive() => typeof(T).IsSerializablePrimitive() && typeof(T) != typeof(byte[]);

		public async Task SerializeAsync(T graph, Stream stream, CancellationToken cancellationToken)
		{
			var isPrimitive = IsJsonPrimitive();

			using var writer = new JsonTextWriter(new StreamWriter(stream, Encoding, BufferSize, true))
			{
				Formatting = Indent ? Formatting.Indented : Formatting.None
			};

			if (isPrimitive)
				await writer.WriteStartArrayAsync(cancellationToken);

			await WriteAsync(writer, graph, cancellationToken);

			if (isPrimitive)
				await writer.WriteEndArrayAsync(cancellationToken);
		}

		public async Task<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken)
		{
			var isPrimitive = IsJsonPrimitive();

			using var reader = new JsonTextReader(new StreamReader(stream, Encoding, true, BufferSize, true))
			{
				FloatParseHandling = FloatParseHandling.Decimal
			};

			if (isPrimitive)
			{
				if (!await reader.ReadAsync(cancellationToken))
					return default;
			}

			var retVal = (T)await ReadAsync(reader, typeof(T), cancellationToken);

			if (isPrimitive)
				await reader.ReadAsync(cancellationToken);

			return retVal;
		}

		private async Task<object> ReadAsync(JsonReader reader, Type type, CancellationToken cancellationToken)
		{
			if (type.IsPersistable())
			{
				if (FillMode)
				{
					var storage = (SettingsStorage)await ReadAsync(reader, typeof(SettingsStorage), cancellationToken);

					if (storage is null)
						return null;

					var per = type.CreateInstance();

					if (per is IAsyncPersistable asyncPer)
						await asyncPer.LoadAsync(storage, default);
					else
						((IPersistable)per).Load(storage);

					return per;
				}
				else
				{
					await reader.ReadWithCheckAsync(cancellationToken);

					if (reader.TokenType == JsonToken.EndArray || reader.TokenType == JsonToken.Null)
						return null;

					reader.ChechExpectedToken(JsonToken.StartObject);

					var per = type.CreateInstance();

					var storage = new SettingsStorage(reader, GetValueFromReaderAsync);

					if (per is IAsyncPersistable asyncPer)
						await asyncPer.LoadAsync(storage, default);
					else
						((IPersistable)per).Load(storage);

					await TryClearDeepLevel(reader, storage, cancellationToken);

					await reader.ReadWithCheckAsync(cancellationToken);

					reader.ChechExpectedToken(JsonToken.EndObject);

					return per;
				}
			}
			else if (type == typeof(SettingsStorage))
			{
				await reader.ReadWithCheckAsync(cancellationToken);

				if (reader.TokenType == JsonToken.EndArray || reader.TokenType == JsonToken.Null)
					return null;

				reader.ChechExpectedToken(JsonToken.StartObject);

				var storage = new SettingsStorage();
				await FillAsync(storage, reader, cancellationToken);

				//await reader.ReadWithCheckAsync(cancellationToken);

				reader.ChechExpectedToken(JsonToken.EndObject);

				return storage;
			}
			else if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
			{
				await reader.ReadWithCheckAsync(cancellationToken);

				if (reader.TokenType == JsonToken.EndArray || reader.TokenType == JsonToken.Null)
					return null;

				reader.ChechExpectedToken(JsonToken.StartArray);

				var itemType = type.GetItemType();

				var col = new List<object>();

				while (true)
				{
					var item = await ReadAsync(reader, itemType, cancellationToken);

					if (item is null && reader.TokenType == JsonToken.EndArray)
						break;

					col.Add(item);
				}

				reader.ChechExpectedToken(JsonToken.EndArray);

				var arr = Array.CreateInstance(itemType, col.Count);
				var idx = 0;

				foreach (var item in col)
				{
					arr.SetValue(item, idx++);
				}

				return type.IsArray ? arr : col;
			}
			else
			{
				object value;

				if (type == typeof(DateTime))
					value = await reader.ReadAsDateTimeAsync(cancellationToken);
				else if (type == typeof(DateTimeOffset))
					value = await reader.ReadAsDateTimeOffsetAsync(cancellationToken);
				else if (type == typeof(byte[]))
					value = await reader.ReadAsBytesAsync(cancellationToken);
				else if (type == typeof(SecureString))
				{
					value = SecureStringEncryptor.Instance.Decrypt(EncryptedAsByteArray
						? await reader.ReadAsBytesAsync(cancellationToken)
						: (await reader.ReadAsStringAsync(cancellationToken))?.Base64());
				}
				else
					value = await reader.ReadAsStringAsync(cancellationToken);

				return value?.To(type);
			}
		}

		private async Task WriteAsync(JsonWriter writer, object value, CancellationToken cancellationToken)
		{
			async Task WriteSettingsStorageAsync(SettingsStorage storage)
			{
				await writer.WriteStartObjectAsync(cancellationToken);

				foreach (var pair in storage)
				{
					await writer.WritePropertyNameAsync(pair.Key, cancellationToken);
					await WriteAsync(writer, pair.Value, cancellationToken);
				}

				await writer.WriteEndObjectAsync(cancellationToken);
			}

			if (value is IPersistable per)
			{
				await WriteSettingsStorageAsync(per.Save());
			}
			else if (value is IAsyncPersistable asyncPer)
			{
				await WriteSettingsStorageAsync(await asyncPer.SaveAsync(cancellationToken));
			}
			else if (value is SettingsStorage storage)
			{
				await WriteSettingsStorageAsync(storage);
			}
			else if (value is IEnumerable primCol && value is not string)
			{
				await writer.WriteStartArrayAsync(cancellationToken);

				foreach (var item in primCol)
					await WriteAsync(writer, item, cancellationToken);

				await writer.WriteEndArrayAsync(cancellationToken);
			}
			else if (value is SecureString secStr)
			{
				var encrypted = SecureStringEncryptor.Instance.Encrypt(secStr);
				await WriteAsync(writer, EncryptedAsByteArray ? encrypted : encrypted?.Base64(), cancellationToken);
			}
			else
			{
				if (value is TimeZoneInfo tz)
					value = tz.To<string>();
				else if (value is Enum && EnumAsString)
					value = value.To<string>();
				else if (value is Type t)
					value = t.GetTypeAsString(false);

				await writer.WriteValueAsync(value, cancellationToken);
			}
		}

		private async Task FillAsync(SettingsStorage storage, JsonReader reader, CancellationToken cancellationToken)
		{
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			if (reader is null)
				throw new ArgumentNullException(nameof(reader));

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
						await FillAsync(inner, reader, cancellationToken);
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
									await FillAsync(inner, reader, cancellationToken);
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

				storage.Set(propName, value);
			}
		}

		private async Task TryClearDeepLevel(JsonReader reader, SettingsStorage storage, CancellationToken cancellationToken)
		{
			var lvl = storage.DeepLevel;

			if (lvl == 0)
				return;

			for (var i = 1; i <= lvl; i++)
				await reader.ReadWithCheckAsync(cancellationToken);

			storage.DeepLevel = 0;
		}

		private async Task<object> GetValueFromReaderAsync(JsonReader reader, SettingsStorage storage, string name, Type type, CancellationToken cancellationToken)
		{
			await TryClearDeepLevel(reader, storage, cancellationToken);

			await reader.ReadWithCheckAsync(cancellationToken);

			reader.ChechExpectedToken(JsonToken.PropertyName);

			if (!((string)reader.Value).EqualsIgnoreCase(name))
				throw new InvalidOperationException($"{reader.Value} != {name}");

			return await ReadAsync(reader, type, cancellationToken);
		}
	}
}