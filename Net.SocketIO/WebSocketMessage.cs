namespace Ecng.Net;

/// <summary>
/// Represents a WebSocket message containing an encoding and a buffer of bytes.
/// </summary>
public readonly struct WebSocketMessage(Encoding encoding, ArraySegment<byte> buffer)
{
	/// <summary>
	/// Gets the encoding used to decode the message.
	/// </summary>
	public Encoding Encoding { get; } = encoding ?? throw new ArgumentNullException(nameof(encoding));

	/// <summary>
	/// Gets the buffer containing the message data.
	/// </summary>
	public ArraySegment<byte> Buffer { get; } = buffer;

	/// <summary>
	/// Converts the message buffer into a string using the associated encoding.
	/// </summary>
	/// <returns>The message as a string.</returns>
	public string AsString()
		=> Encoding.GetString(Buffer);

	/// <summary>
	/// Deserializes the message string into a dynamic object.
	/// </summary>
	/// <returns>The deserialized object.</returns>
	public dynamic AsObject()
		=> AsObject<object>();

	/// <summary>
	/// Deserializes the message string into an object of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize the message into.</typeparam>
	/// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
	public T AsObject<T>()
		=> AsString().DeserializeObject<T>();

	/// <summary>
	/// Creates a <see cref="Newtonsoft.Json.JsonTextReader"/> to read the message as JSON.
	/// </summary>
	/// <returns>A <see cref="Newtonsoft.Json.JsonTextReader"/> for the message.</returns>
	public JsonTextReader AsReader()
		=> new(new StreamReader(new MemoryStream(Buffer.Array, Buffer.Offset, Buffer.Count), Encoding));
}