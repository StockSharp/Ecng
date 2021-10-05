namespace Ecng.Net
{
	using System;
	using System.Net.Http;
	using System.Net.Http.Formatting;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Collections.Generic;
	using System.Linq;
	using System.IO;
	using System.Net;
	using System.Text;

	using Ecng.Common;

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
}