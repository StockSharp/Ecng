namespace Ecng.Net;

using System.Text;
using System.Text.Json;

/// <summary>
/// A media type formatter for JSON content using System.Text.Json.
/// </summary>
public class JsonMediaTypeFormatter : IMediaTypeFormatter
{
	/// <summary>
	/// Initializes a new instance of the <see cref="JsonMediaTypeFormatter"/> class
	/// with default serializer options.
	/// </summary>
	public JsonMediaTypeFormatter()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonMediaTypeFormatter"/> class
	/// with the specified serializer options.
	/// </summary>
	/// <param name="options">The JSON serializer options.</param>
	public JsonMediaTypeFormatter(JsonSerializerOptions options)
	{
		Options = options ?? throw new ArgumentNullException(nameof(options));
	}

	/// <summary>
	/// Gets or sets the JSON serializer options.
	/// </summary>
	public JsonSerializerOptions Options { get; set; } = new();

	/// <inheritdoc />
	public string MediaType => "application/json";

	/// <inheritdoc />
	public HttpContent Serialize(object value)
	{
		var json = JsonSerializer.Serialize(value, value?.GetType() ?? typeof(object), Options);
		return new StringContent(json, Encoding.UTF8, MediaType);
	}

	/// <inheritdoc />
	public async Task<T> DeserializeAsync<T>(HttpContent content, CancellationToken cancellationToken)
	{
		var stream = await content.ReadAsStreamAsync(
#if NET5_0_OR_GREATER
			cancellationToken
#endif
		).NoWait();

		return await JsonSerializer.DeserializeAsync<T>(stream, Options, cancellationToken).NoWait();
	}
}
