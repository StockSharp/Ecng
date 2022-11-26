namespace Ecng.Net;

using System.IO;
using System.Text;

public class RestApiFormUrlEncodedMediaTypeFormatter : FormUrlEncodedMediaTypeFormatter
{
	public RestApiFormUrlEncodedMediaTypeFormatter()
	{
		SupportedEncodings.Add(Encoding.UTF8);
		SupportedEncodings.Add(Encoding.Unicode);
	}

	public override bool CanWriteType(Type type)
		=> true;

	public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext, CancellationToken cancellationToken)
	{
		var encoding = SelectCharacterEncoding(content?.Headers);

		if (value is IDictionary<string, object> args)
		{
			value = args.Select(p => $"{p.Key}={p.Value?.ToString().EncodeToHtml()}").JoinAnd();
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