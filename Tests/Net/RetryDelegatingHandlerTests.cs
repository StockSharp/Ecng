namespace Ecng.Tests.Net;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task RetryAfterCanBeCancelledBeforeAnotherAttempt(bool useDate)
	{
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		var stub = new StubHandler(n =>
		{
			var response = new HttpResponseMessage(n == 1 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK);
			if (n == 1)
			{
				response.Headers.RetryAfter = useDate
					? new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddMinutes(1))
					: new RetryConditionHeaderValue(TimeSpan.FromMinutes(1));
				cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));
			}
			return response;
		});
		using var client = Build(stub);

		await ThrowsAsync<OperationCanceledException>(() => client.GetAsync("https://example.test/", cancellation.Token));
		AreEqual(1, stub.Calls);
	}

	[TestMethod]
	public async Task DisposesRateLimitedResponseBeforeRetrying()
	{
		var content = new TrackedContent();
		var stub = new StubHandler(n =>
		{
			if (n == 1)
				return new(HttpStatusCode.TooManyRequests) { Content = content };
			IsTrue(content.Disposed);
			return new(HttpStatusCode.OK);
		});
		using var client = Build(stub);

		using var response = await client.GetAsync("https://example.test/", CancellationToken);
		AreEqual(HttpStatusCode.OK, response.StatusCode);
	}

	[TestMethod]
	[DataRow("POST")]
	[DataRow("PUT")]
	[DataRow("DELETE")]
	public async Task ReadOnlyRetriesNeverReplayWritesAfterNetworkFailure(string method)
	{
		var stub = new StubHandler(_ => throw new HttpRequestException("Connection lost after sending the request."));
		using var client = new HttpClient(new RetryDelegatingHandler(maxRetries: 3, baseDelayMs: 1)
		{
			InnerHandler = stub,
			RetryOnlyReadMethods = true,
		});
		using var request = new HttpRequestMessage(new HttpMethod(method), "https://example.test/");

		await ThrowsAsync<HttpRequestException>(() => client.SendAsync(request, CancellationToken));
		AreEqual(1, stub.Calls);
	}

	private sealed class TrackedContent() : StringContent("rate limited")
	{
		public bool Disposed { get; private set; }

		protected override void Dispose(bool disposing)
		{
			Disposed = true;
			base.Dispose(disposing);
		}
	}
}
