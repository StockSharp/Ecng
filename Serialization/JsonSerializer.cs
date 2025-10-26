namespace Ecng.Serialization;

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

/// <summary>
/// Contains custom JSON conversion logic.
/// </summary>
static class JsonConversions
{
	static JsonConversions()
	{
		Converter.AddTypedConverter<object[], SecureString>(val => SecureStringEncryptor.Instance.Decrypt([.. val.Select(i => i.To<byte>())]));
	}
}

/// <summary>
/// Provides JSON serialization settings.
/// </summary>
public interface IJsonSerializer
{
	/// <summary>
	/// Gets or sets a value indicating whether the JSON output should be indented.
	/// </summary>
	bool Indent { get; set; }

	/// <summary>
	/// Gets or sets the text encoding used for serialization.
	/// </summary>
	Encoding Encoding { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the fill mode is enabled.
	/// </summary>
	bool FillMode { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether enums should be serialized as strings.
	/// </summary>
	bool EnumAsString { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether encrypted values are handled as byte arrays.
	/// </summary>
	bool EncryptedAsByteArray { get; set; }

	/// <summary>
	/// Gets or sets the buffer size used during serialization and deserialization.
	/// </summary>
	int BufferSize { get; set; }

	/// <summary>
	/// Gets or sets the null value handling option for JSON serialization.
	/// </summary>
	NullValueHandling NullValueHandling { get; set; }
}

/// <summary>
/// Provides JSON serialization and deserialization for a given type.
/// </summary>
/// <typeparam name="T">The type of the graph to serialize and deserialize.</typeparam>
public class JsonSerializer<T> : Serializer<T>, IJsonSerializer
{
	static JsonSerializer()
	{
		typeof(JsonConversions).EnsureRunClass();
	}

	/// <summary>
	/// Gets or sets a value indicating whether the JSON output should be indented.
	/// </summary>
	public bool Indent { get; set; }

	/// <summary>
	/// Gets or sets the text encoding used for serialization. Defaults to UTF8.
	/// </summary>
	public Encoding Encoding { get; set; } = Encoding.UTF8;

	/// <summary>
	/// Gets or sets a value indicating whether the fill mode is enabled.
	/// </summary>
	public bool FillMode { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether enums should be serialized as strings.
	/// </summary>
	public bool EnumAsString { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether encrypted values are handled as byte arrays.
	/// </summary>
	public bool EncryptedAsByteArray { get; set; }

	/// <summary>
	/// Gets or sets the buffer size used during serialization and deserialization.
	/// </summary>
	public int BufferSize { get; set; } = FileSizes.KB;

	/// <summary>
	/// Gets or sets the null value handling option for JSON serialization.
	/// </summary>
	public NullValueHandling NullValueHandling { get; set; } = NullValueHandling.Include;

	/// <summary>
	/// Gets the file extension associated with the JSON serializer.
	/// </summary>
	public override string FileExtension => "json";

	/// <summary>
	/// Creates a default instance of the JSON serializer with preconfigured settings.
	/// </summary>
	/// <returns>A default <see cref="JsonSerializer{T}"/> instance.</returns>
	public static JsonSerializer<T> CreateDefault()
		=> new()
		{
			Indent = true,
			EnumAsString = true,
			NullValueHandling = NullValueHandling.Ignore,
		};

	private static bool IsJsonPrimitive() => typeof(T).IsSerializablePrimitive() && typeof(T) != typeof(byte[]);

	/// <summary>
	/// Asynchronously serializes the specified object graph to the provided stream as JSON.
	/// </summary>
	/// <param name="graph">The object graph to serialize.</param>
	/// <param name="stream">The stream to which the graph is serialized.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the serialization operation.</param>
	/// <returns>A task representing the asynchronous serialization operation.</returns>
	public override async ValueTask SerializeAsync(T graph, Stream stream, CancellationToken cancellationToken)
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

	/// <summary>
	/// Asynchronously deserializes an object graph from the provided JSON stream.
	/// </summary>
	/// <param name="stream">The stream from which the object graph is deserialized.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the deserialization operation.</param>
	/// <returns>A task representing the asynchronous deserialization operation. The task result contains the deserialized object graph.</returns>
	public override async ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken)
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

	private async ValueTask<object> ReadAsync(JsonReader reader, Type type, CancellationToken cancellationToken)
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

				reader.CheckExpectedToken(JsonToken.StartObject);

				var per = type.CreateInstance();

				var storage = new SettingsStorage(reader, GetValueFromReaderAsync);

				if (per is IAsyncPersistable asyncPer)
					await asyncPer.LoadAsync(storage, default);
				else
					((IPersistable)per).Load(storage);

				await TryClearDeepLevel(reader, storage, cancellationToken);

				await reader.ReadWithCheckAsync(cancellationToken);

				reader.CheckExpectedToken(JsonToken.EndObject);

				return per;
			}
		}
		else if (type == typeof(SettingsStorage))
		{
			await reader.ReadWithCheckAsync(cancellationToken);

			if (reader.TokenType == JsonToken.EndArray || reader.TokenType == JsonToken.Null)
				return null;

			reader.CheckExpectedToken(JsonToken.StartObject);

			var storage = new SettingsStorage();
			await FillAsync(storage, reader, cancellationToken);

			//await reader.ReadWithCheckAsync(cancellationToken);

			reader.CheckExpectedToken(JsonToken.EndObject);

			return storage;
		}
		else if (type.Is<IEnumerable>() && type != typeof(string))
		{
			await reader.ReadWithCheckAsync(cancellationToken);

			if (reader.TokenType == JsonToken.EndArray || reader.TokenType == JsonToken.Null)
				return null;

			reader.CheckExpectedToken(JsonToken.StartArray);

			var itemType = type.GetItemType();

			var col = new List<object>();

			while (true)
			{
				var item = await ReadAsync(reader, itemType, cancellationToken);

				if (item is null && reader.TokenType == JsonToken.EndArray)
					break;

				col.Add(item);
			}

			reader.CheckExpectedToken(JsonToken.EndArray);

			if (!type.IsArray && type != typeof(IEnumerable<>).Make(itemType))
				return col;

			var arr = Array.CreateInstance(itemType, col.Count);
			var idx = 0;

			foreach (var item in col)
			{
				arr.SetValue(item, idx++);
			}

			return arr;
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
				var bytes = EncryptedAsByteArray
					? await reader.ReadAsBytesAsync(cancellationToken)
					: (await reader.ReadAsStringAsync(cancellationToken))?.Base64();

				value = bytes is null ? null : SecureStringEncryptor.Instance.Decrypt(bytes);
			}
			else if (type.TryGetAdapterType(out var adapterType))
			{
				value = await ReadAsync(reader, adapterType, cancellationToken);

				if (value is IPersistableAdapter adapter)
					value = adapter.UnderlyingValue;
			}
			else
				value = await reader.ReadAsStringAsync(cancellationToken);

			return value?.To(type);
		}
	}

	private async ValueTask WriteAsync(JsonWriter writer, object value, CancellationToken cancellationToken)
	{
		async Task WriteSettingsStorageAsync(SettingsStorage storage)
		{
			await writer.WriteStartObjectAsync(cancellationToken);

			foreach (var pair in storage)
			{
				if (pair.Value is null && NullValueHandling == NullValueHandling.Ignore)
					continue;

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
			else if (value != null && value.GetType().TryGetAdapterType(out var adapterType))
			{
				var adapter = adapterType.CreateInstance<IPersistableAdapter>();
				adapter.UnderlyingValue = value;
				await WriteAsync(writer, adapter, cancellationToken);
				return;
			}

			await writer.WriteValueAsync(value, cancellationToken);
		}
	}

	private async ValueTask FillAsync(SettingsStorage storage, JsonReader reader, CancellationToken cancellationToken)
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

			reader.CheckExpectedToken(JsonToken.PropertyName);

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

	private async ValueTask TryClearDeepLevel(JsonReader reader, SettingsStorage storage, CancellationToken cancellationToken)
	{
		var lvl = storage.DeepLevel;

		if (lvl == 0)
			return;

		for (var i = 1; i <= lvl; i++)
			await reader.ReadWithCheckAsync(cancellationToken);

		storage.DeepLevel = 0;
	}

	private async ValueTask<object> GetValueFromReaderAsync(JsonReader reader, SettingsStorage storage, string name, Type type, CancellationToken cancellationToken)
	{
		await TryClearDeepLevel(reader, storage, cancellationToken);

		await reader.ReadWithCheckAsync(cancellationToken);

		reader.CheckExpectedToken(JsonToken.PropertyName);

		if (!((string)reader.Value).EqualsIgnoreCase(name))
			throw new InvalidOperationException($"{reader.Value} != {name}");

		return await ReadAsync(reader, type, cancellationToken);
	}
}