namespace Ecng.Tests.Net;

using System.Net.Http.Headers;

using Ecng.Net;

[TestClass]
public class HttpRateLimitHandlerTests : BaseTestClass
{
	private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
	{
		private int _calls;

		public int Calls => Volatile.Read(ref _calls);

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref _calls);
			return respond(request, cancellationToken);
		}
	}

	private static Uri NewAddress() => new($"https://{Guid.NewGuid():N}.example.test/");

	private static HttpResponseMessage Limited(TimeSpan delay)
	{
		var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
		response.Headers.RetryAfter = new RetryConditionHeaderValue(delay);
		return response;
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[Timeout(15000, CooperativeCancellation = true)]
	public async Task AServerDelayLongerThanFiveSecondsIsFullyObserved(bool useDate)
	{
		var address = NewAddress();
		var calls = 0;
		var elapsed = new Stopwatch();
		var deadline = DateTime.MinValue;
		var stub = new StubHandler((_, _) =>
		{
			if (++calls != 1)
			{
				if (useDate)
					IsTrue(DateTime.UtcNow.AddMilliseconds(15) >= deadline, "The HTTP-date deadline has not passed.");
				else
					IsTrue(elapsed.Elapsed >= TimeSpan.FromSeconds(6), $"Retried after {elapsed.Elapsed} instead of six seconds.");
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
			}

			var response = Limited(TimeSpan.FromSeconds(6));
			if (useDate)
			{
				deadline = DateTime.UtcNow.AddSeconds(7).Truncate(TimeSpan.FromSeconds(1));
				response.Headers.RetryAfter = new RetryConditionHeaderValue(deadline);
			}
			elapsed.Start();
			return Task.FromResult(response);
		});
		using var client = new HttpClient(new HttpRateLimitHandler(stub) { DefaultDelay = TimeSpan.FromMilliseconds(1) });
		using var first = await client.GetAsync(address, CancellationToken);
		using var second = await client.GetAsync(address, CancellationToken);

		AreEqual(HttpStatusCode.OK, second.StatusCode);
		AreEqual(2, stub.Calls);
	}

	[TestMethod]
	[DataRow(HttpStatusCode.TooManyRequests, "0")]
	[DataRow(HttpStatusCode.TooManyRequests, "Fri, 31 Dec 1999 23:59:59 GMT")]
	[DataRow((HttpStatusCode)418, "0")]
	[DataRow(HttpStatusCode.ServiceUnavailable, "0")]
	[DataRow(HttpStatusCode.ServiceUnavailable, "Fri, 31 Dec 1999 23:59:59 GMT")]
	public async Task AnElapsedRetryAfterDoesNotApplyTheFallback(HttpStatusCode status, string header)
	{
		var address = NewAddress();
		var stub = new StubHandler((_, _) =>
		{
			var response = new HttpResponseMessage(status);
			response.Headers.TryAddWithoutValidation("Retry-After", header);
			return Task.FromResult(response);
		});
		using var client = new HttpClient(new HttpRateLimitHandler(stub));
		using var first = await client.GetAsync(address, CancellationToken);
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromSeconds(1));
		using var second = await client.GetAsync(address, cancellation.Token);

		AreEqual(2, stub.Calls, "A valid zero or past deadline must not become a thirty-second pause.");
	}

	[TestMethod]
	[DataRow(HttpStatusCode.TooManyRequests, null)]
	[DataRow(HttpStatusCode.TooManyRequests, "not-a-delay")]
	[DataRow(HttpStatusCode.TooManyRequests, "-1")]
	[DataRow((HttpStatusCode)418, null)]
	public async Task AMissingOrInvalidHeaderUsesTheConfiguredFallback(HttpStatusCode status, string header)
	{
		var address = NewAddress();
		var elapsed = new Stopwatch();
		var calls = 0;
		var delay = TimeSpan.FromMilliseconds(150);
		var stub = new StubHandler((_, _) =>
		{
			var response = new HttpResponseMessage(status);
			if (Interlocked.Increment(ref calls) == 1)
			{
				if (header is not null)
					response.Headers.TryAddWithoutValidation("Retry-After", header);
				elapsed.Start();
			}
			else
				IsTrue(elapsed.Elapsed >= delay, $"Fallback expired after {elapsed.Elapsed} instead of {delay}.");
			return Task.FromResult(response);
		});
		using var client = new HttpClient(new HttpRateLimitHandler(stub) { DefaultDelay = delay });
		using var first = await client.GetAsync(address, CancellationToken);
		using var second = await client.GetAsync(address, CancellationToken);

		AreEqual(2, stub.Calls);
	}

	[TestMethod]
	[DataRow(HttpStatusCode.OK, "60")]
	[DataRow(HttpStatusCode.BadRequest, "60")]
	[DataRow(HttpStatusCode.Forbidden, "60")]
	[DataRow(HttpStatusCode.NotFound, "60")]
	[DataRow(HttpStatusCode.InternalServerError, null)]
	[DataRow(HttpStatusCode.ServiceUnavailable, "invalid")]
	public async Task UnrelatedResponsesDoNotPauseOtherRequests(HttpStatusCode status, string header)
	{
		var address = NewAddress();
		var stub = new StubHandler((_, _) =>
		{
			var response = new HttpResponseMessage(status);
			if (header is not null)
				response.Headers.TryAddWithoutValidation("Retry-After", header);
			return Task.FromResult(response);
		});
		using var client = new HttpClient(new HttpRateLimitHandler(stub));
		using var first = await client.GetAsync(address, CancellationToken);
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromSeconds(1));
		using var second = await client.GetAsync(address, cancellation.Token);

		AreEqual(2, stub.Calls);
	}

	[TestMethod]
	[DataRow("GET", HttpStatusCode.TooManyRequests)]
	[DataRow("POST", HttpStatusCode.TooManyRequests)]
	[DataRow("DELETE", HttpStatusCode.ServiceUnavailable)]
	public async Task ThePauseHandlerReturnsTheOriginalResponseWithoutReplaying(string method, HttpStatusCode status)
	{
		using var expected = new HttpResponseMessage(status) { Content = new StringContent("original body") };
		expected.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(1));
		var stub = new StubHandler((_, _) => Task.FromResult(expected));
		using var client = new HttpClient(new HttpRateLimitHandler(stub));
		using var request = new HttpRequestMessage(new HttpMethod(method), NewAddress());
		using var actual = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, CancellationToken);

		AreSame(expected, actual);
		AreEqual("original body", await actual.Content.ReadAsStringAsync(CancellationToken));
		AreEqual(1, stub.Calls);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task ALongBanRemainsCancellableWithoutOverflowingTaskDelay(bool maximumFallback)
	{
		var address = NewAddress();
		var stub = new StubHandler((_, _) => Task.FromResult(maximumFallback
			? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
			: Limited(TimeSpan.FromDays(100))));
		using var client = new HttpClient(new HttpRateLimitHandler(stub)
		{
			DefaultDelay = maximumFallback ? TimeSpan.MaxValue : TimeSpan.FromSeconds(30),
		});
		using var first = await client.GetAsync(address, CancellationToken);
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

		await ThrowsAsync<OperationCanceledException>(() => client.GetAsync(address, cancellation.Token));
		IsTrue(cancellation.IsCancellationRequested);
		AreEqual(1, stub.Calls);
	}

	[TestMethod]
	public async Task AnInFlightResponseExtendsAnAlreadyWaitingRequest()
	{
		var address = NewAddress();
		var release = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
		var inFlightStub = new StubHandler((_, token) => release.Task.WaitAsync(token));
		using var inFlightClient = new HttpClient(new HttpRateLimitHandler(inFlightStub));
		var inFlight = inFlightClient.GetAsync(address, CancellationToken);
		var stub = new StubHandler((_, _) => Task.FromResult(Limited(TimeSpan.FromMilliseconds(150))));
		using var client = new HttpClient(new HttpRateLimitHandler(stub));
		using var first = await client.GetAsync(address, CancellationToken);
		var waiting = HttpRateLimitHandler.WaitAsync(address, CancellationToken);
		IsFalse(waiting.IsCompleted);

		var elapsed = Stopwatch.StartNew();
		release.SetResult(Limited(TimeSpan.FromSeconds(1)));
		using var extended = await inFlight;
		await waiting;

		IsTrue(elapsed.Elapsed >= TimeSpan.FromSeconds(1), "A waiter must recheck a deadline extended while it was asleep.");
		AreEqual(1, stub.Calls);
		AreEqual(1, inFlightStub.Calls);
	}

	[TestMethod]
	public async Task CancellingOneWaiterAndDisposingItsClientDoNotReleaseOthers()
	{
		var address = NewAddress();
		var elapsed = new Stopwatch();
		var stub = new StubHandler((_, _) =>
		{
			elapsed.Start();
			return Task.FromResult(Limited(TimeSpan.FromSeconds(1)));
		});
		using (var client = new HttpClient(new HttpRateLimitHandler(stub)))
		using (await client.GetAsync(address, CancellationToken))
		{
		}

		var survivor = HttpRateLimitHandler.WaitAsync(address, CancellationToken);
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		var cancelled = HttpRateLimitHandler.WaitAsync(address, cancellation.Token);
		cancellation.Cancel();
		await ThrowsAsync<OperationCanceledException>(() => cancelled);
		await survivor;

		IsTrue(elapsed.Elapsed >= TimeSpan.FromSeconds(1));
		AreEqual(1, stub.Calls);
	}

	[TestMethod]
	public async Task AnAlreadyCancelledRequestNeverReachesTheTransport()
	{
		var stub = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
		using var client = new HttpClient(new HttpRateLimitHandler(stub));
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		cancellation.Cancel();

		await ThrowsAsync<OperationCanceledException>(() => client.GetAsync(NewAddress(), cancellation.Token));
		AreEqual(0, stub.Calls);
	}

	[TestMethod]
	public void TheFallbackIsPositiveAndDefaultsToThirtySeconds()
	{
		using var handler = new HttpRateLimitHandler();
		AreEqual(TimeSpan.FromSeconds(30), handler.DefaultDelay);
		Throws<ArgumentOutOfRangeException>(() => handler.DefaultDelay = TimeSpan.Zero);
		Throws<ArgumentOutOfRangeException>(() => handler.DefaultDelay = TimeSpan.FromMilliseconds(-1));
	}
}
