namespace Ecng.Serialization;

using System.Collections;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Contains custom JSON conversion logic.
/// </summary>
static class JsonConversions
{
	static JsonConversions()
	{
		Converter.AddTypedConverter<object[], SecureString>(val => SecureStringHelper.Decrypt([.. val.Select(i => i.To<byte>())]));
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
	/// Gets or sets the local <see cref="SecureString"/> encryptor used by this serializer.
	/// </summary>
	ISecureStringEncryptor SecureStringEncryptor { get; set; }

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
	/// Gets or sets the local <see cref="SecureString"/> encryptor used by this serializer.
	/// When set, it overrides <see cref="SecureStringHelper.Encryptor"/> only while this
	/// serializer is serializing or deserializing.
	/// </summary>
	public ISecureStringEncryptor SecureStringEncryptor { get; set; }

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
		using var encryptorScope = CreateSecureStringEncryptorScope();

		var isPrimitive = IsJsonPrimitive();

		using var buffer = new MemoryStream();
		using (var streamWriter = new StreamWriter(buffer, Encoding, BufferSize, true))
		using (var writer = new JsonTextWriter(streamWriter)
		{
			Formatting = Indent ? Formatting.Indented : Formatting.None,
			CloseOutput = false
		})
		{
			if (isPrimitive)
				await writer.WriteStartArrayAsync(cancellationToken).NoWait();

			await WriteAsync(writer, graph, cancellationToken).NoWait();

			if (isPrimitive)
				await writer.WriteEndArrayAsync(cancellationToken).NoWait();

			await writer.FlushAsync(cancellationToken).NoWait();
			await streamWriter.FlushAsync(cancellationToken).NoWait();
		}

		await WriteBufferedAsync(stream, buffer, cancellationToken).NoWait();
	}

	/// <summary>
	/// Asynchronously deserializes an object graph from the provided JSON stream.
	/// </summary>
	/// <param name="stream">The stream from which the object graph is deserialized.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the deserialization operation.</param>
	/// <returns>A task representing the asynchronous deserialization operation. The task result contains the deserialized object graph.</returns>
	public override async ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken)
	{
		using var encryptorScope = CreateSecureStringEncryptorScope();

		var isPrimitive = IsJsonPrimitive();

		using var reader = new JsonTextReader(new StreamReader(stream, Encoding, true, BufferSize, true))
		{
			DateParseHandling = DateParseHandling.None,
			FloatParseHandling = FloatParseHandling.Decimal
		};

		if (isPrimitive)
		{
			if (!await reader.ReadAsync(cancellationToken).NoWait())
				return default;
		}

		var retVal = (T)await ReadAsync(reader, typeof(T), cancellationToken).NoWait();

		if (isPrimitive)
			await reader.ReadAsync(cancellationToken).NoWait();

		return retVal;
	}

	private Scope<ISecureStringEncryptor> CreateSecureStringEncryptorScope()
		=> SecureStringEncryptor is null ? null : new Scope<ISecureStringEncryptor>(SecureStringEncryptor, false);

	private static async ValueTask WriteBufferedAsync(Stream stream, MemoryStream buffer, CancellationToken cancellationToken)
	{
		if (!buffer.TryGetBuffer(out var source))
			source = new ArraySegment<byte>(buffer.ToArray());

		if (source.Count == 0)
			return;

		if (stream is MemoryStream memoryStream && TryWriteToMemoryStream(memoryStream, source))
			return;

		await stream.WriteAsync(source.Array.AsMemory(source.Offset, source.Count), cancellationToken).NoWait();
	}

	private static bool TryWriteToMemoryStream(MemoryStream stream, ArraySegment<byte> source)
	{
		if (!stream.TryGetBuffer(out _))
			return false;

		var position = stream.Position;
		var end = position + source.Count;

		if (position < 0 || end > int.MaxValue)
			return false;

		var newLength = Math.Max(stream.Length, end);
		stream.SetLength(newLength);

		if (!stream.TryGetBuffer(out var destination))
			return false;

		Buffer.BlockCopy(source.Array, source.Offset, destination.Array, destination.Offset + (int)position, source.Count);
		stream.Position = end;

		return true;
	}

	private static DateTime ParseDateTime(string value)
	{
		if (value is null)
			return default;

		return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
	}

	private static DateTimeOffset ParseDateTimeOffset(string value)
		=> value is null ? default : DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

	private async ValueTask<object> ReadAsync(JsonReader reader, Type type, CancellationToken cancellationToken)
	{
		if (type.IsPersistable())
		{
			if (FillMode)
			{
				var storage = (SettingsStorage)await ReadAsync(reader, typeof(SettingsStorage), cancellationToken).NoWait();

				if (storage is null)
					return null;

				var per = type.CreateInstance();

				if (per is IAsyncPersistable asyncPer)
					await asyncPer.LoadAsync(storage, cancellationToken).NoWait();
				else
					((IPersistable)per).Load(storage);

				return per;
			}
			else
			{
				await reader.ReadWithCheckAsync(cancellationToken).NoWait();

				if (reader.TokenType == JsonToken.EndArray || reader.TokenType == JsonToken.Null)
					return null;

				reader.CheckExpectedToken(JsonToken.StartObject);

				var per = type.CreateInstance();

				var storage = new SettingsStorage(reader, GetValueFromReaderAsync);

				if (per is IAsyncPersistable asyncPer)
					await asyncPer.LoadAsync(storage, cancellationToken).NoWait();
				else
					((IPersistable)per).Load(storage);

				await TryClearDeepLevel(reader, storage, cancellationToken).NoWait();

				await SkipToEndObjectAsync(reader, cancellationToken).NoWait();

				return per;
			}
		}
		else if (type == typeof(SettingsStorage))
		{
			await reader.ReadWithCheckAsync(cancellationToken).NoWait();

			if (reader.TokenType == JsonToken.EndArray || reader.TokenType == JsonToken.Null)
				return null;

			reader.CheckExpectedToken(JsonToken.StartObject);

			var storage = new SettingsStorage();
			await FillAsync(storage, reader, cancellationToken).NoWait();

			//await reader.ReadWithCheckAsync(cancellationToken).NoWait();

			reader.CheckExpectedToken(JsonToken.EndObject);

			return storage;
		}
		else if (type.Is<IEnumerable>() && type != typeof(string))
		{
			await reader.ReadWithCheckAsync(cancellationToken).NoWait();

			if (reader.TokenType == JsonToken.EndArray || reader.TokenType == JsonToken.Null)
				return null;

			reader.CheckExpectedToken(JsonToken.StartArray);

			var itemType = type.GetItemType();

			var col = (IList)Activator.CreateInstance(typeof(List<>).Make(itemType));

			while (true)
			{
				var item = await ReadAsync(reader, itemType, cancellationToken).NoWait();

				if (item is null && reader.TokenType == JsonToken.EndArray)
					break;

				col.Add(item);
			}

			reader.CheckExpectedToken(JsonToken.EndArray);

			if (!type.IsArray)
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
			{
				var str = await reader.ReadAsStringAsync(cancellationToken).NoWait();
				value = str is null ? null : ParseDateTime(str);
			}
			else if (type == typeof(DateTimeOffset))
			{
				var str = await reader.ReadAsStringAsync(cancellationToken).NoWait();
				value = str is null ? null : ParseDateTimeOffset(str);
			}
			else if (type == typeof(byte[]))
				value = await reader.ReadAsBytesAsync(cancellationToken).NoWait();
			else if (type == typeof(SecureString))
			{
				var bytes = EncryptedAsByteArray
					? await reader.ReadAsBytesAsync(cancellationToken).NoWait()
					: (await reader.ReadAsStringAsync(cancellationToken).NoWait())?.Base64();

				value = bytes is null ? null : SecureStringHelper.Decrypt(bytes);
			}
			else if (type.TryGetAdapterType(out var adapterType))
			{
				value = await ReadAsync(reader, adapterType, cancellationToken).NoWait();

				if (value is IPersistableAdapter adapter)
					value = adapter.UnderlyingValue;
			}
			else
				value = await reader.ReadAsStringAsync(cancellationToken).NoWait();

			return value?.To(type);
		}
	}

	private async ValueTask WriteAsync(JsonWriter writer, object value, CancellationToken cancellationToken)
	{
		async Task WriteSettingsStorageAsync(SettingsStorage storage)
		{
			await writer.WriteStartObjectAsync(cancellationToken).NoWait();

			foreach (var pair in storage)
			{
				if (pair.Value is null && NullValueHandling == NullValueHandling.Ignore)
					continue;

				await writer.WritePropertyNameAsync(pair.Key, cancellationToken).NoWait();
				await WriteAsync(writer, pair.Value, cancellationToken).NoWait();
			}

			await writer.WriteEndObjectAsync(cancellationToken).NoWait();
		}

		if (value is IPersistable per)
		{
			await WriteSettingsStorageAsync(per.Save()).NoWait();
		}
		else if (value is IAsyncPersistable asyncPer)
		{
			await WriteSettingsStorageAsync(await asyncPer.SaveAsync(cancellationToken)).NoWait();
		}
		else if (value is SettingsStorage storage)
		{
			await WriteSettingsStorageAsync(storage).NoWait();
		}
		else if (value is IEnumerable primCol && value is not string)
		{
			await writer.WriteStartArrayAsync(cancellationToken).NoWait();

			foreach (var item in primCol)
				await WriteAsync(writer, item, cancellationToken).NoWait();

			await writer.WriteEndArrayAsync(cancellationToken).NoWait();
		}
		else if (value is SecureString secStr)
		{
			var encrypted = SecureStringHelper.Encrypt(secStr);
			await WriteAsync(writer, EncryptedAsByteArray ? encrypted : encrypted?.Base64(), cancellationToken).NoWait();
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
				await WriteAsync(writer, adapter, cancellationToken).NoWait();
				return;
			}

			await writer.WriteValueAsync(value, cancellationToken).NoWait();
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
			await reader.ReadWithCheckAsync(cancellationToken).NoWait();

			if (reader.TokenType == JsonToken.EndObject)
				break;

			reader.CheckExpectedToken(JsonToken.PropertyName);

			var propName = (string)reader.Value;

			await reader.ReadWithCheckAsync(cancellationToken).NoWait();

			object value;

			switch (reader.TokenType)
			{
				case JsonToken.StartObject:
				{
					var inner = new SettingsStorage();
					await FillAsync(inner, reader, cancellationToken).NoWait();
					value = inner;
					break;
				}
				case JsonToken.StartArray:
				{
					value = await ReadArrayAsync(reader, cancellationToken).NoWait();
					break;
				}
				default:
					value = reader.Value;
					break;
			}

			storage.Set(propName, value);
		}
	}

	private async ValueTask<object[]> ReadArrayAsync(JsonReader reader, CancellationToken cancellationToken)
	{
		await reader.ReadWithCheckAsync(cancellationToken).NoWait();

		var list = new List<object>();

		while (reader.TokenType != JsonToken.EndArray)
		{
			switch (reader.TokenType)
			{
				case JsonToken.StartObject:
				{
					var inner = new SettingsStorage();
					await FillAsync(inner, reader, cancellationToken).NoWait();
					list.Add(inner);
					break;
				}
				case JsonToken.StartArray:
				{
					list.Add(await ReadArrayAsync(reader, cancellationToken).NoWait());
					break;
				}
				default:
					list.Add(reader.Value);
					break;
			}

			await reader.ReadWithCheckAsync(cancellationToken).NoWait();
		}

		return list.ToArray();
	}

	private async ValueTask TryClearDeepLevel(JsonReader reader, SettingsStorage storage, CancellationToken cancellationToken)
	{
		var lvl = storage.DeepLevel;

		if (lvl == 0)
			return;

		for (var i = 1; i <= lvl; i++)
			await reader.ReadWithCheckAsync(cancellationToken).NoWait();

		storage.DeepLevel = 0;
	}

	private static async ValueTask SkipToEndObjectAsync(JsonReader reader, CancellationToken cancellationToken)
	{
		while (reader.TokenType != JsonToken.EndObject)
		{
			await reader.ReadWithCheckAsync(cancellationToken).NoWait();

			if (reader.TokenType == JsonToken.PropertyName)
			{
				await reader.ReadWithCheckAsync(cancellationToken).NoWait();
				await reader.SkipAsync(cancellationToken).NoWait();
			}
		}
	}

	private async ValueTask<object> GetValueFromReaderAsync(JsonReader reader, SettingsStorage storage, string name, Type type, CancellationToken cancellationToken)
	{
		await TryClearDeepLevel(reader, storage, cancellationToken).NoWait();

		await reader.ReadWithCheckAsync(cancellationToken).NoWait();

		reader.CheckExpectedToken(JsonToken.PropertyName);

		if (!((string)reader.Value).EqualsIgnoreCase(name))
			throw new InvalidOperationException($"{reader.Value} != {name}");

		return await ReadAsync(reader, type, cancellationToken).NoWait();
	}
}
