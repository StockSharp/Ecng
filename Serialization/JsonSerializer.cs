namespace Ecng.Serialization
{
	using System;
	using System.Text;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;
	using Ecng.Reflection;

	using Newtonsoft.Json;

	public class JsonSerializer<T> : Serializer<T>
	{
		private const int _bufferSize = 1024;

		public bool Indent { get; set; }
		public Encoding Encoding { get; set; } = Encoding.UTF8;

		public override ISerializer GetSerializer(Type entityType)
		{
			var serializer = base.GetSerializer(entityType);
			serializer.SetValue(nameof(Indent), Indent);
			serializer.SetValue(nameof(Encoding), Encoding);
			return serializer;
		}

		public override string FileExtension => "json";

		public override void Deserialize(Stream stream, FieldList fields, SerializationItemCollection source)
			=> throw new NotSupportedException();

		public override void Serialize(FieldList fields, SerializationItemCollection source, Stream stream)
			=> throw new NotSupportedException();

		public override void Serialize(T graph, Stream stream)
			=> Task.Run(async () => await SerializeAsync(graph, stream, default)).Wait();

		public override T Deserialize(Stream stream)
			=> Task.Run(async () => await DeserializeAsync(stream, default)).Result;

		private static bool IsJsonPrimitive() => typeof(T).IsSerializablePrimitive() && typeof(T) != typeof(byte[]);

		public async Task SerializeAsync(T graph, Stream stream, CancellationToken cancellationToken)
		{
			var isPrimitive = IsJsonPrimitive();

			using var writer = new JsonTextWriter(new StreamWriter(stream, Encoding, _bufferSize, true));

			if (isPrimitive)
				await writer.WriteStartArrayAsync(cancellationToken);

			await WriteAsync(writer, graph, cancellationToken);

			if (isPrimitive)
				await writer.WriteEndArrayAsync(cancellationToken);
		}

		public async Task<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken)
		{
			var isPrimitive = IsJsonPrimitive();

			using var reader = new JsonTextReader(new StreamReader(stream, Encoding, true, _bufferSize, true));

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
			if (typeof(IPersistable).IsAssignableFrom(type) || typeof(IAsyncPersistable).IsAssignableFrom(type))
			{
				await reader.ReadWithCheckAsync(cancellationToken);

				if (reader.TokenType == JsonToken.EndArray || reader.TokenType == JsonToken.Null)
					return null;

				reader.ChechExpectedToken(JsonToken.StartObject);

				var per = GetSerializer(type).CreateObject(new SerializationItemCollection());

				var storage = new SettingsStorage(reader);

				if (per is IAsyncPersistable asyncPer)
					await asyncPer.LoadAsync(storage, default);
				else
					((IPersistable)per).Load(storage);

				await storage.TryClearDeepLevel(cancellationToken);

				await reader.ReadWithCheckAsync(cancellationToken);

				reader.ChechExpectedToken(JsonToken.EndObject);

				return per;
			}
			else if (type == typeof(SettingsStorage))
			{
				await reader.ReadWithCheckAsync(cancellationToken);

				if (reader.TokenType == JsonToken.EndArray || reader.TokenType == JsonToken.Null)
					return null;

				reader.ChechExpectedToken(JsonToken.StartObject);

				var storage = new SettingsStorage();

				while (await reader.ReadAsync(cancellationToken))
				{
					if (reader.TokenType == JsonToken.EndObject)
						break;

					var propName = (string)reader.Value;

					Type valueType;

					switch (reader.TokenType)
					{
						case JsonToken.StartObject:
							valueType = typeof(SettingsStorage);
							break;
						case JsonToken.PropertyName:
							valueType = typeof(string);
							break;
						case JsonToken.StartArray:
							valueType = typeof(string[]);
							break;
						default:
							throw new ArgumentOutOfRangeException(reader.TokenType.ToString());
					}

					var value = await ReadAsync(reader, valueType, cancellationToken);
					storage.SetValue(propName, value);
				}

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
			else
			{
				if (value is TimeZoneInfo tz)
					value = tz.To<string>();

				await writer.WriteValueAsync(value, cancellationToken);
			}
		}
	}
}