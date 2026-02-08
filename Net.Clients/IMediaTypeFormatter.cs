namespace Ecng.Net;

/// <summary>
/// Abstracts HTTP content serialization and deserialization for REST API clients.
/// </summary>
public interface IMediaTypeFormatter
{
	/// <summary>
	/// Gets the media type this formatter handles (e.g. "application/json").
	/// </summary>
	string MediaType { get; }

	/// <summary>
	/// Serializes the specified value into <see cref="HttpContent"/>.
	/// </summary>
	/// <param name="value">The value to serialize.</param>
	/// <returns>The serialized HTTP content.</returns>
	HttpContent Serialize(object value);

	/// <summary>
	/// Deserializes the HTTP content into an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize to.</typeparam>
	/// <param name="content">The HTTP content to deserialize.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task representing the deserialized value.</returns>
	Task<T> DeserializeAsync<T>(HttpContent content, CancellationToken cancellationToken);
}
