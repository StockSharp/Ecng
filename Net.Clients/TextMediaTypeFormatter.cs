namespace Ecng.Net;

using Ecng.Serialization;

/// <summary>
/// A media type formatter for processing text-based content.
/// </summary>
public class TextMediaTypeFormatter : IMediaTypeFormatter
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TextMediaTypeFormatter"/> class with the specified media types.
	/// </summary>
	/// <param name="mediaTypes">A collection of media type strings.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="mediaTypes"/> is null.</exception>
	public TextMediaTypeFormatter(IEnumerable<string> mediaTypes)
	{
		if (mediaTypes is null)
			throw new ArgumentNullException(nameof(mediaTypes));

		MediaType = mediaTypes.First();
	}

	/// <inheritdoc />
	public string MediaType { get; }

	/// <inheritdoc />
	public HttpContent Serialize(object value)
		=> throw new NotSupportedException();

	/// <inheritdoc />
	public async Task<T> DeserializeAsync<T>(HttpContent content, CancellationToken cancellationToken)
	{
		var str = await content.ReadAsStringAsync(
#if NET5_0_OR_GREATER
			cancellationToken
#endif
		).NoWait();

		if (typeof(T) == typeof(string))
			return (T)(object)str;

		return (T)str.DeserializeObject(typeof(T));
	}
}
