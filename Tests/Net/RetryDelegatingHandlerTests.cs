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
			=> Task.FromResult(responder(Interlocked.Increment(ref Calls)));
	}

	private static HttpClient Build(StubHandler inner)
		=> new(new RetryDelegatingHandler(maxRetries: 3, baseDelayMs: 1)
		{
			InnerHandler = inner,
			DefaultDelay = TimeSpan.FromMilliseconds(1),
		});

	[TestMethod]
	public async Task Retries503ThenSucceeds()
	{
		var stub = new StubHandler(n => new(n < 3 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK));
		using var client = Build(stub);

		using var resp = await client.GetAsync($"https://{Guid.NewGuid():N}.example.test/", CancellationToken);

		AreEqual(HttpStatusCode.OK, resp.StatusCode);
		AreEqual(3, stub.Calls);
	}

	[TestMethod]
	public async Task Post500_NotRetried()
	{
		var stub = new StubHandler(_ => new(HttpStatusCode.InternalServerError));
		using var client = Build(stub);

		using var body = new StringContent(string.Empty);
		using var resp = await client.PostAsync($"https://{Guid.NewGuid():N}.example.test/", body, CancellationToken);

		AreEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
		AreEqual(1, stub.Calls);
	}

	[TestMethod]
	public async Task Post429_Retried()
	{
		var stub = new StubHandler(n => new(n < 2 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK));
		using var client = Build(stub);

		using var body = new StringContent(string.Empty);
		using var resp = await client.PostAsync($"https://{Guid.NewGuid():N}.example.test/", body, CancellationToken);

		AreEqual(HttpStatusCode.OK, resp.StatusCode);
		AreEqual(2, stub.Calls);
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
					? new RetryConditionHeaderValue(DateTime.UtcNow.AddMinutes(1))
					: new RetryConditionHeaderValue(TimeSpan.FromMinutes(1));
				cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));
			}
			return response;
		});
		using var client = Build(stub);

		await ThrowsAsync<OperationCanceledException>(() => client.GetAsync($"https://{Guid.NewGuid():N}.example.test/", cancellation.Token));
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

		using var response = await client.GetAsync($"https://{Guid.NewGuid():N}.example.test/", CancellationToken);
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
		using var request = new HttpRequestMessage(new HttpMethod(method), $"https://{Guid.NewGuid():N}.example.test/");

		await ThrowsAsync<HttpRequestException>(() => client.SendAsync(request, CancellationToken));
		AreEqual(1, stub.Calls);
	}

	[TestMethod]
	[DataRow(429, "seconds")]
	[DataRow(429, "date")]
	[DataRow(418, "seconds")]
	[DataRow(503, "seconds")]
	[DataRow(429, "missing")]
	[DataRow(429, "invalid")]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task AFinalResponsePausesOtherClientsForTheSameServer(int status, string header)
	{
		var address = new Uri($"https://{Guid.NewGuid():N}.example.test/");
		var first = new StubHandler(_ =>
		{
			var response = new HttpResponseMessage((HttpStatusCode)status);
			if (header == "seconds")
				response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(1));
			else if (header == "date")
				response.Headers.RetryAfter = new RetryConditionHeaderValue(DateTime.UtcNow.AddMinutes(1));
			else if (header == "invalid")
				response.Headers.TryAddWithoutValidation("Retry-After", "not-a-delay");
			return response;
		});
		var second = new StubHandler(_ => new(HttpStatusCode.OK));
		using var firstClient = new HttpClient(new RetryDelegatingHandler(maxRetries: 0) { InnerHandler = first });
		using var secondClient = new HttpClient(new RetryDelegatingHandler(maxRetries: 0) { InnerHandler = second });
		using var response = await firstClient.GetAsync(address, CancellationToken);
		AreEqual((HttpStatusCode)status, response.StatusCode);

		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));
		await ThrowsAsync<OperationCanceledException>(() => secondClient.GetAsync(new Uri(address, "another-symbol"), cancellation.Token));
		AreEqual(1, first.Calls);
		AreEqual(0, second.Calls, "Another adapter must not bypass the server's pause.");
	}

	[TestMethod]
	[DataRow("POST")]
	[DataRow("DELETE")]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ARejectedWritePausesReadsWithoutReplayingTheWrite(string method)
	{
		var address = new Uri($"https://{Guid.NewGuid():N}.example.test/");
		var stub = new StubHandler(_ =>
		{
			var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
			response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(1));
			return response;
		});
		using var client = new HttpClient(new RetryDelegatingHandler(maxRetries: 3)
		{
			InnerHandler = stub,
			RetryOnlyReadMethods = true,
		});
		using var request = new HttpRequestMessage(new HttpMethod(method), address);
		using var response = await client.SendAsync(request, CancellationToken);
		AreEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
		AreEqual(1, stub.Calls);

		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));
		await ThrowsAsync<OperationCanceledException>(() => client.GetAsync(address, cancellation.Token));
		AreEqual(1, stub.Calls);
	}

	[TestMethod]
	public async Task AServerPauseDoesNotBlockAnotherAuthority()
	{
		var host = $"{Guid.NewGuid():N}.example.test";
		var stub = new StubHandler(n =>
		{
			var response = new HttpResponseMessage(n == 1 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK);
			if (n == 1)
				response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(1));
			return response;
		});
		using var client = new HttpClient(new RetryDelegatingHandler(maxRetries: 0) { InnerHandler = stub });
		using var first = await client.GetAsync($"https://{host}:8001/", CancellationToken);
		using var second = await client.GetAsync($"https://{host}:8002/", CancellationToken);
		AreEqual(HttpStatusCode.OK, second.StatusCode);
		AreEqual(2, stub.Calls);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ARequestCanDisableReplayWhileKeepingTheSharedPause()
	{
		var address = new Uri($"https://{Guid.NewGuid():N}.example.test/");
		var stub = new StubHandler(_ =>
		{
			var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
			response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(1));
			return response;
		});
		using var client = Build(stub);
		using var request = new HttpRequestMessage(HttpMethod.Get, address);
		request.Options.Set(RetryDelegatingHandler.IsRetryAllowed, false);
		using var response = await client.SendAsync(request, CancellationToken);
		AreEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
		AreEqual(1, stub.Calls);
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));
		await ThrowsAsync<OperationCanceledException>(() => client.GetAsync(address, cancellation.Token));
		AreEqual(1, stub.Calls);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ASharedPauseExpiresAndRequestsResume()
	{
		var address = new Uri($"https://{Guid.NewGuid():N}.example.test/");
		var stub = new StubHandler(n =>
		{
			var response = new HttpResponseMessage(n == 1 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK);
			if (n == 1)
				response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMilliseconds(150));
			return response;
		});
		using var client = new HttpClient(new RetryDelegatingHandler(maxRetries: 0) { InnerHandler = stub });
		var started = System.Diagnostics.Stopwatch.StartNew();
		using var first = await client.GetAsync(address, CancellationToken);
		using var second = await client.GetAsync(address, CancellationToken);
		IsTrue(started.Elapsed >= TimeSpan.FromMilliseconds(150));
		AreEqual(HttpStatusCode.OK, second.StatusCode);
		AreEqual(2, stub.Calls);
	}

	[TestMethod]
	[DataRow(HttpStatusCode.OK)]
	[DataRow(HttpStatusCode.TooManyRequests)]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task AnInFlightResponseCannotShortenAnExistingPause(HttpStatusCode status)
	{
		var address = new Uri($"https://{Guid.NewGuid():N}.example.test/");
		var releaseShort = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var shortClient = new HttpClient(new HttpRateLimitHandler(new DeferredHandler(releaseShort.Task)));
		var shortRequest = shortClient.GetAsync(address, CancellationToken);
		var stub = new StubHandler(_ =>
		{
			var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
			response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(1));
			return response;
		});
		using var longClient = new HttpClient(new RetryDelegatingHandler(maxRetries: 0) { InnerHandler = stub });
		using var longResponse = await longClient.GetAsync(address, CancellationToken);
		var shortResponse = new HttpResponseMessage(status);
		shortResponse.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMilliseconds(1));
		releaseShort.SetResult(shortResponse);
		using var returnedShort = await shortRequest;

		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));
		await ThrowsAsync<OperationCanceledException>(() => longClient.GetAsync(address, cancellation.Token));
		AreEqual(1, stub.Calls);
	}

	[TestMethod]
	[DataRow(HttpStatusCode.TooManyRequests)]
	[DataRow(HttpStatusCode.ServiceUnavailable)]
	[DataRow(HttpStatusCode.InternalServerError)]
	public async Task ExhaustingTheBudgetDisposesOnlyIntermediateResponsesAndStillPausesOtherClients(HttpStatusCode status)
	{
		var address = new Uri($"https://{Guid.NewGuid():N}.example.test/");
		var contents = new List<TrackedContent>();
		var stub = new StubHandler(n =>
		{
			var content = new TrackedContent();
			contents.Add(content);
			var response = new HttpResponseMessage(status) { Content = content };
			response.Headers.RetryAfter = new RetryConditionHeaderValue(n == 3 ? TimeSpan.FromMinutes(1) : TimeSpan.Zero);
			return response;
		});
		using var client = new HttpClient(new RetryDelegatingHandler(maxRetries: 2, baseDelayMs: 1)
		{
			InnerHandler = stub,
			DefaultDelay = TimeSpan.FromMilliseconds(1),
		});
		using (var response = await client.GetAsync(address, CancellationToken))
		{
			AreEqual(status, response.StatusCode);
			AreEqual(3, stub.Calls);
			IsTrue(contents[0].Disposed);
			IsTrue(contents[1].Disposed);
			IsFalse(contents[2].Disposed, "The caller owns the final response.");
			AreEqual("rate limited", await response.Content.ReadAsStringAsync(CancellationToken));
		}
		IsTrue(contents[2].Disposed);

		var next = new StubHandler(_ => new(HttpStatusCode.OK));
		using var nextClient = Build(next);
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));
		await ThrowsAsync<OperationCanceledException>(() => nextClient.GetAsync(address, cancellation.Token));
		AreEqual(0, next.Calls);
	}

	[TestMethod]
	[DataRow(false, false)]
	[DataRow(true, false)]
	[DataRow(false, true)]
	[DataRow(true, true)]
	public async Task TransportFailuresRespectTheRetryBudget(bool timeout, bool recover)
	{
		var stub = new StubHandler(n =>
		{
			if (recover && n == 3)
				return new(HttpStatusCode.OK);
			if (timeout)
				throw new TaskCanceledException("Transport timeout while the caller is still active.");
			throw new HttpRequestException("Connection reset.");
		});
		using var client = new HttpClient(new RetryDelegatingHandler(maxRetries: 2, baseDelayMs: 1) { InnerHandler = stub });
		Task<HttpResponseMessage> Send() => client.GetAsync($"https://{Guid.NewGuid():N}.example.test/", CancellationToken);

		if (recover)
		{
			using var response = await Send();
			AreEqual(HttpStatusCode.OK, response.StatusCode);
		}
		else if (timeout)
			await ThrowsAsync<TaskCanceledException>(Send);
		else
			await ThrowsAsync<HttpRequestException>(Send);

		AreEqual(3, stub.Calls);
		IsFalse(CancellationToken.IsCancellationRequested);
	}

	[TestMethod]
	[DataRow("POST", HttpStatusCode.TooManyRequests)]
	[DataRow("POST", HttpStatusCode.ServiceUnavailable)]
	[DataRow("PUT", HttpStatusCode.TooManyRequests)]
	[DataRow("DELETE", HttpStatusCode.ServiceUnavailable)]
	[DataRow("PATCH", HttpStatusCode.InternalServerError)]
	public async Task ReadOnlyRetriesNeverReplayRejectedWrites(string method, HttpStatusCode status)
	{
		var stub = new StubHandler(_ => new(status));
		using var client = new HttpClient(new RetryDelegatingHandler(maxRetries: 3, baseDelayMs: 1)
		{
			InnerHandler = stub,
			RetryOnlyReadMethods = true,
			DefaultDelay = TimeSpan.FromMilliseconds(1),
		});
		using var request = new HttpRequestMessage(new HttpMethod(method), $"https://{Guid.NewGuid():N}.example.test/");
		using var response = await client.SendAsync(request, CancellationToken);
		AreEqual(status, response.StatusCode);
		AreEqual(1, stub.Calls);
	}

	[TestMethod]
	public async Task AReadCanDisableReplayAfterATransportFailure()
	{
		var stub = new StubHandler(_ => throw new HttpRequestException("A signed request must be regenerated by its caller."));
		using var client = Build(stub);
		using var request = new HttpRequestMessage(HttpMethod.Get, $"https://{Guid.NewGuid():N}.example.test/");
		request.Options.Set(RetryDelegatingHandler.IsRetryAllowed, false);

		await ThrowsAsync<HttpRequestException>(() => client.SendAsync(request, CancellationToken));
		AreEqual(1, stub.Calls);
	}

	[TestMethod]
	public async Task CallerCancellationIsNotTreatedAsATransportTimeout()
	{
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		var stub = new StubHandler(_ =>
		{
			cancellation.Cancel();
			throw new TaskCanceledException("Cancelled by caller.");
		});
		using var client = Build(stub);

		await ThrowsAsync<OperationCanceledException>(() => client.GetAsync($"https://{Guid.NewGuid():N}.example.test/", cancellation.Token));
		AreEqual(1, stub.Calls);
	}

	[TestMethod]
	public async Task CancellationDuringExponentialBackoffPreventsTheNextAttempt()
	{
		var stub = new StubHandler(_ => new(HttpStatusCode.InternalServerError));
		using var client = new HttpClient(new RetryDelegatingHandler(baseDelayMs: 60000) { InnerHandler = stub });
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		var pending = client.GetAsync($"https://{Guid.NewGuid():N}.example.test/", cancellation.Token);
		AreEqual(1, stub.Calls);
		cancellation.Cancel();

		await ThrowsAsync<OperationCanceledException>(() => pending);
		AreEqual(1, stub.Calls);
	}

	[TestMethod]
	[DataRow("GET", HttpStatusCode.InternalServerError, true)]
	[DataRow("HEAD", HttpStatusCode.BadGateway, true)]
	[DataRow("HEAD", HttpStatusCode.ServiceUnavailable, true)]
	[DataRow("GET", HttpStatusCode.TooManyRequests, true)]
	[DataRow("GET", HttpStatusCode.NotFound, false)]
	[DataRow("GET", (HttpStatusCode)418, false)]
	public async Task ReadRetriesDistinguishTransientFailuresFromPermanentRejections(string method, HttpStatusCode status, bool retries)
	{
		var stub = new StubHandler(n => new(n == 1 ? status : HttpStatusCode.OK));
		using var client = new HttpClient(new RetryDelegatingHandler(baseDelayMs: 1)
		{
			InnerHandler = stub,
			RetryOnlyReadMethods = true,
			DefaultDelay = TimeSpan.FromMilliseconds(1),
		});
		using var request = new HttpRequestMessage(new HttpMethod(method), $"https://{Guid.NewGuid():N}.example.test/");
		using var response = await client.SendAsync(request, CancellationToken);

		AreEqual(retries ? HttpStatusCode.OK : status, response.StatusCode);
		AreEqual(retries ? 2 : 1, stub.Calls);
	}

	[TestMethod]
	public async Task EachBackoffDoublesAfterAnotherTransientFailure()
	{
		var sentAt = new List<long>();
		var stub = new StubHandler(n =>
		{
			sentAt.Add(Stopwatch.GetTimestamp());
			return new(n == 4 ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
		});
		using var client = new HttpClient(new RetryDelegatingHandler(maxRetries: 3, baseDelayMs: 80) { InnerHandler = stub });
		using var response = await client.GetAsync($"https://{Guid.NewGuid():N}.example.test/", CancellationToken);

		AreEqual(HttpStatusCode.OK, response.StatusCode);
		AreEqual(4, stub.Calls);
		for (var i = 1; i < sentAt.Count; i++)
		{
			var milliseconds = (sentAt[i] - sentAt[i - 1]) * 1000.0 / Stopwatch.Frequency;
			var expected = 80 * (1 << (i - 1));
			IsTrue(milliseconds >= expected - 10, $"Attempt {i + 1} waited {milliseconds} ms instead of {expected} ms.");
		}
	}

	[TestMethod]
	[DataRow(-1)]
	[DataRow(-2)]
	public void NegativeBackoffIsRejectedAtConstruction(int delay)
		=> Throws<ArgumentOutOfRangeException>(() => new RetryDelegatingHandler(baseDelayMs: delay));

	private sealed class DeferredHandler(Task<HttpResponseMessage> response) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			=> response.WaitAsync(cancellationToken);
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
