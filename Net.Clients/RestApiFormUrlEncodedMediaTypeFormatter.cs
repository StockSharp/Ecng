namespace Ecng.Net;

using System.IO;
using System.Text;

/// <summary>
/// Represents a formatter for REST API form URL encoded media types.
/// </summary>
public class RestApiFormUrlEncodedMediaTypeFormatter : FormUrlEncodedMediaTypeFormatter
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RestApiFormUrlEncodedMediaTypeFormatter"/> class.
	/// </summary>
	public RestApiFormUrlEncodedMediaTypeFormatter()
	{
		SupportedEncodings.Add(Encoding.UTF8);
		SupportedEncodings.Add(Encoding.Unicode);
	}

	/// <summary>
	/// Determines whether this instance can write objects of the specified type.
	/// </summary>
	/// <param name="type">The type of the object to write.</param>
	/// <returns>true if the type can be written; otherwise, false.</returns>
	public override bool CanWriteType(Type type)
		=> true;

	/// <summary>
	/// Asynchronously writes the specified value to the given stream.
	/// </summary>
	/// <param name="type">The type of the value to write.</param>
	/// <param name="value">The value to write.</param>
	/// <param name="writeStream">The stream to which the content is written.</param>
	/// <param name="content">The HTTP content.</param>
	/// <param name="transportContext">The transport context.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext, CancellationToken cancellationToken)
	{
		var encoding = SelectCharacterEncoding(content?.Headers);

		if (value is IDictionary<string, object> args)
		{
			value = args.Select(p => (p.Key.EncodeUrl(), p.Value?.ToString().EncodeUrl())).ToQueryString();
		}

		if (value is string str)
		{
			var bytes = encoding.GetBytes(str);
			return writeStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
		}
		else
			return base.WriteToStreamAsync(type, value, writeStream, content, transportContext, cancellationToken);
	}
}