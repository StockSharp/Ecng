namespace Ecng.Tests.Net.Mocks;

using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;

using Ecng.Common;
using Ecng.Net;

/// <summary>
/// Exact copy of the old RestApiFormUrlEncodedMediaTypeFormatter (pre-refactor)
/// that inherited from Microsoft's FormUrlEncodedMediaTypeFormatter.
/// Used for side-by-side comparison tests.
/// </summary>
internal class LegacyFormUrlEncodedFormatter : FormUrlEncodedMediaTypeFormatter
{
	public LegacyFormUrlEncodedFormatter()
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
