namespace Ecng.Backup.Mega.Native;

using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class WebClient : IWebClient
{
	private const int _defaultResponseTimeout = Timeout.Infinite;

	private static readonly HttpClient _sharedHttpClient = CreateHttpClient(_defaultResponseTimeout, GenerateUserAgent());

	private readonly HttpClient _httpClient;

	public WebClient(int responseTimeout = _defaultResponseTimeout, ProductInfoHeaderValue userAgent = null)
	{
		if (responseTimeout == _defaultResponseTimeout && userAgent == null)
		{
			_httpClient = _sharedHttpClient;
		}
		else
		{
			_httpClient = CreateHttpClient(responseTimeout, userAgent ?? GenerateUserAgent());
		}
	}

	public int BufferSize { get; set; } = Options.DefaultBufferSize;

	public async Task<string> PostRequestJson(Uri url, string jsonData, CancellationToken cancellationToken)
	{
		using var jsonStream = new MemoryStream(jsonData.ToBytes());
		using var responseStream = await PostRequest(url, jsonStream, "application/json", cancellationToken);
		return StreamToString(responseStream);
	}

	public async Task<string> PostRequestRaw(Uri url, Stream dataStream, CancellationToken cancellationToken)
	{
		using var responseStream = await PostRequest(url, dataStream, "application/json", cancellationToken);
		return StreamToString(responseStream);
	}

	public Task<Stream> PostRequestRawAsStream(Uri url, Stream dataStream, CancellationToken cancellationToken)
	{
		return PostRequest(url, dataStream, "application/octet-stream", cancellationToken);
	}

	public Task<Stream> GetRequestRaw(Uri url, CancellationToken cancellationToken)
	{
		return _httpClient.GetStreamAsync(url, cancellationToken);
	}

	private async Task<Stream> PostRequest(Uri url, Stream dataStream, string contentType, CancellationToken cancellationToken)
	{
		using var content = new StreamContent(dataStream, BufferSize);

		content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

		var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
		{
			Content = content
		};

		var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		if (!response.IsSuccessStatusCode
			&& response.StatusCode == HttpStatusCode.InternalServerError
			&& response.ReasonPhrase == "Server Too Busy")
		{
			return new MemoryStream(Encoding.UTF8.GetBytes(((long)ApiResultCode.RequestFailedRetry).ToString()));
		}

		response.EnsureSuccessStatusCode();

		return await response.Content.ReadAsStreamAsync(cancellationToken);
	}

	private static string StreamToString(Stream stream)
	{
		using var streamReader = new StreamReader(stream, Encoding.UTF8);
		return streamReader.ReadToEnd();
	}

	private static HttpClient CreateHttpClient(int timeout, ProductInfoHeaderValue userAgent)
	{
		var httpClient = new HttpClient(
			new HttpClientHandler
			{
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
			})
		{
			Timeout = TimeSpan.FromMilliseconds(timeout)
		};

		httpClient.DefaultRequestHeaders.UserAgent.Add(userAgent);

		return httpClient;
	}

	private static ProductInfoHeaderValue GenerateUserAgent()
	{
		var assemblyName = Assembly.GetEntryAssembly().GetName();
		return new ProductInfoHeaderValue(assemblyName.Name, assemblyName.Version.ToString(2));
	}
}