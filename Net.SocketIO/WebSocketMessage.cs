namespace Ecng.Net;

public readonly struct WebSocketMessage(WebSocketClient client, ArraySegment<byte> buffer)
{
	private readonly WebSocketClient _client = client ?? throw new ArgumentNullException(nameof(client));

	public ArraySegment<byte> Buffer { get; } = buffer;

	public string AsString()
		=> _client.Encoding.GetString(Buffer);

	public dynamic AsObject()
		=> AsString().DeserializeObject<object>();

	public JsonTextReader AsReader()
		=> new(new StreamReader(new MemoryStream(Buffer.Array, Buffer.Offset, Buffer.Count), _client.Encoding));
}
