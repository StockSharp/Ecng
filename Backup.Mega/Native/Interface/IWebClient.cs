namespace Ecng.Backup.Mega.Native;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public interface IWebClient
{
	int BufferSize { get; set; }

	Task<string> PostRequestJson(Uri url, string jsonData, CancellationToken cancellationToken);

	Task<string> PostRequestRaw(Uri url, Stream dataStream, CancellationToken cancellationToken);

	Task<Stream> PostRequestRawAsStream(Uri url, Stream dataStream, CancellationToken cancellationToken);

	Task<Stream> GetRequestRaw(Uri url, CancellationToken cancellationToken);
}