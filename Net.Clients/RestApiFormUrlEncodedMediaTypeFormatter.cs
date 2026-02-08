namespace Ecng.Net;

using System.Text;

/// <summary>
/// Represents a formatter for REST API form URL encoded media types.
/// </summary>
public class RestApiFormUrlEncodedMediaTypeFormatter : IMediaTypeFormatter
{
	/// <inheritdoc />
	public string MediaType => "application/x-www-form-urlencoded";

	/// <inheritdoc />
	public HttpContent Serialize(object value)
	{
		if (value is IDictionary<string, object> args)
			value = args.Select(p => (p.Key.EncodeUrl(), p.Value?.ToString().EncodeUrl())).ToQueryString();

		if (value is string str)
			return new StringContent(str, Encoding.UTF8, MediaType);

		throw new NotSupportedException($"Cannot serialize {value?.GetType()}");
	}

	/// <inheritdoc />
	public Task<T> DeserializeAsync<T>(HttpContent content, CancellationToken cancellationToken)
		=> throw new NotSupportedException();
}
