namespace Ecng.Net;

using System.Runtime.InteropServices;

/// <summary>
/// Represents a WebSocket message containing an encoding and a buffer of bytes.
/// Primary storage uses <see cref="ReadOnlyMemory{T}"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="WebSocketMessage"/> struct using <see cref="ReadOnlyMemory{T}"/>.
/// </remarks>
/// <param name="encoding">The text encoding.</param>
/// <param name="memory">The message bytes.</param>
public readonly struct WebSocketMessage(Encoding encoding, ReadOnlyMemory<byte> memory)
{
	/// <summary>
	/// Gets the encoding used to decode the message.
	/// </summary>
	public Encoding Encoding { get; } = encoding ?? throw new ArgumentNullException(nameof(encoding));

	/// <summary>
	/// Gets the message bytes as read-only memory (primary storage).
	/// </summary>
	public ReadOnlyMemory<byte> Memory { get; } = memory;

	/// <summary>
	/// Converts the message buffer into a string using the associated encoding.
	/// </summary>
	/// <returns>The message as a string.</returns>
	public string AsString()
		=> Encoding.GetString(Memory.Span);

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
	/// Creates a <see cref="JsonTextReader"/> to read the message as JSON.
	/// </summary>
	/// <returns>A <see cref="JsonTextReader"/> for the message.</returns>
	public JsonTextReader AsReader()
	{
		if (MemoryMarshal.TryGetArray(Memory, out var segment))
			return new JsonTextReader(new StreamReader(new MemoryStream(segment.Array, segment.Offset, segment.Count), Encoding));

		var arr = Memory.ToArray();
		return new JsonTextReader(new StreamReader(new MemoryStream(arr, 0, arr.Length), Encoding));
	}

	/// <summary>
	/// Gets the buffer containing the message data.
	/// </summary>
	[Obsolete("Use Memory property instead.")]
	public ArraySegment<byte> Buffer => Memory.ToArray();
}