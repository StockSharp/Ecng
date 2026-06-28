namespace Ecng.Tests.Net;

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Net;

[TestClass]
public class RetryDelegatingHandlerTests : BaseTestClass
{
	private sealed class StubHandler(Func<int, HttpResponseMessage> responder) : HttpMessageHandler
	{
		public int Calls;

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			=> Task.FromResult(responder(++Calls));
	}

	private static HttpClient Build(StubHandler inner)
		=> new(new RetryDelegatingHandler(maxRetries: 3, baseDelayMs: 1) { InnerHandler = inner });

	[TestMethod]
	public async Task Retries503ThenSucceeds()
	{
		var stub = new StubHandler(n => new(n < 3 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK));
		using var client = Build(stub);

		var resp = await client.GetAsync("https://x/", CancellationToken.None);

		resp.StatusCode.AssertEqual(HttpStatusCode.OK);
		stub.Calls.AssertEqual(3);
	}

	[TestMethod]
	public async Task Post500_NotRetried()
	{
		var stub = new StubHandler(_ => new(HttpStatusCode.InternalServerError));
		using var client = Build(stub);

		var resp = await client.PostAsync("https://x/", new StringContent(string.Empty), default);

		resp.StatusCode.AssertEqual(HttpStatusCode.InternalServerError);
		stub.Calls.AssertEqual(1); // non-idempotent 5xx is not retried
	}

	[TestMethod]
	public async Task Post429_Retried()
	{
		var stub = new StubHandler(n => new(n < 2 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK));
		using var client = Build(stub);

		var resp = await client.PostAsync("https://x/", new StringContent(string.Empty), default);

		resp.StatusCode.AssertEqual(HttpStatusCode.OK);
		stub.Calls.AssertEqual(2); // 429 is always retried, even for POST
	}
}
