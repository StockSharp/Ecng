namespace Ecng.Net;

using System.Text;

using Newtonsoft.Json;

/// <summary>
/// A media type formatter for JSON content using Newtonsoft.Json.
/// </summary>
/// <remarks>
/// Companion to <see cref="JsonMediaTypeFormatter"/> (System.Text.Json). Use this
/// when the client must match a Newtonsoft-based server contract (e.g. integer
/// enum keys, Newtonsoft <c>JsonConverter</c>s such as
/// <see cref="Ecng.Serialization.JsonDateTimeMcsConverter"/> for unix-time wire).
/// </remarks>
public class NewtonsoftMediaTypeFormatter : IMediaTypeFormatter
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NewtonsoftMediaTypeFormatter"/> class
	/// with default serializer settings.
	/// </summary>
	public NewtonsoftMediaTypeFormatter()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NewtonsoftMediaTypeFormatter"/> class
	/// with the specified serializer settings.
	/// </summary>
	/// <param name="settings">The JSON serializer settings.</param>
	public NewtonsoftMediaTypeFormatter(JsonSerializerSettings settings)
	{
		Settings = settings ?? throw new ArgumentNullException(nameof(settings));
	}

	/// <summary>
	/// Gets or sets the JSON serializer settings.
	/// </summary>
	public JsonSerializerSettings Settings { get; set; } = new();

	/// <inheritdoc />
	public string MediaType => "application/json";

	/// <inheritdoc />
	public HttpContent Serialize(object value)
	{
		var json = JsonConvert.SerializeObject(value, Settings);
		return new StringContent(json, Encoding.UTF8, MediaType);
	}

	/// <inheritdoc />
	public async Task<T> DeserializeAsync<T>(HttpContent content, CancellationToken cancellationToken)
	{
		var json = await content.ReadAsStringAsync(
#if NET5_0_OR_GREATER
			cancellationToken
#endif
		).NoWait();

		return JsonConvert.DeserializeObject<T>(json, Settings);
	}
}
