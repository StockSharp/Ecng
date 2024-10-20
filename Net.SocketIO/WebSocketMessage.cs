namespace Ecng.Net;

public readonly struct WebSocketMessage(Encoding encoding, ArraySegment<byte> buffer)
{
	public Encoding Encoding { get; } = encoding ?? throw new ArgumentNullException(nameof(encoding));
	public ArraySegment<byte> Buffer { get; } = buffer;

	public string AsString()
		=> Encoding.GetString(Buffer);

	public dynamic AsObject()
		=> AsObject<object>();

	public dynamic AsObject<T>()
		=> AsString().DeserializeObject<T>();

	public JsonTextReader AsReader()
		=> new(new StreamReader(new MemoryStream(Buffer.Array, Buffer.Offset, Buffer.Count), Encoding));
}
