namespace Ecng.Tests.Net;

using Ecng.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using RestSharp;
using RestSharp.Authenticators;

[TestClass]
public class RestSharpRetryTests : BaseTestClass
{
	private sealed class CallbackAuthenticator(Action authenticate) : IAuthenticator
	{
		public ValueTask Authenticate(IRestClient client, RestRequest request)
		{
			authenticate();
			return default;
		}
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task GetRecoversFromHtmlRateLimitResponse()
	{
		var calls = 0;
		var elapsed = new Stopwatch();
		await using var app = await StartServerAsync(async context =>
		{
			var limited = Interlocked.Increment(ref calls) == 1;
			context.Response.StatusCode = limited ? StatusCodes.Status429TooManyRequests : StatusCodes.Status200OK;
			context.Response.ContentType = limited ? "text/html" : "application/json";
			if (limited)
			{
				context.Response.Headers.RetryAfter = "1";
				elapsed.Start();
			}
			else
				IsTrue(elapsed.Elapsed >= TimeSpan.FromSeconds(1), "The real HTTP retry arrived before Retry-After elapsed.");
			await context.Response.WriteAsync(limited ? "<html>429 Too Many Requests</html>" : "{\"ok\":true}", context.RequestAborted);
		});

		var result = await new RestRequest { Method = Method.Get }.InvokeAsync<Dictionary<string, bool>>(
			new Uri(app.Urls.Single()), this, null, CancellationToken);

		IsTrue(result["ok"]);
		AreEqual(2, calls);
	}

	[TestMethod]
	[DataRow(StatusCodes.Status429TooManyRequests)]
	[DataRow(StatusCodes.Status503ServiceUnavailable)]
	public async Task DoesNotReplayPostRequests(int status)
	{
		var calls = 0;
		await using var app = await StartServerAsync(async context =>
		{
			Interlocked.Increment(ref calls);
			context.Response.StatusCode = status;
			context.Response.ContentType = "text/html";
			await context.Response.WriteAsync("<html>Request rejected</html>", context.RequestAborted);
		});

		await ThrowsAsync<InvalidOperationException>(() => new RestRequest { Method = Method.Post }.InvokeAsync<object>(
			new Uri(app.Urls.Single()), this, null, CancellationToken));
		AreEqual(1, calls);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task PersistentRateLimitStillFailsAfterBoundedRetries()
	{
		var calls = 0;
		await using var app = await StartServerAsync(async context =>
		{
			Interlocked.Increment(ref calls);
			context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
			context.Response.ContentType = "text/html";
			context.Response.Headers.RetryAfter = "1";
			await context.Response.WriteAsync("<html>429 Too Many Requests</html>", context.RequestAborted);
		});

		await ThrowsAsync<InvalidOperationException>(() => new RestRequest { Method = Method.Get }.InvokeAsync<object>(
			new Uri(app.Urls.Single()), this, null, CancellationToken));
		AreEqual(4, calls);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task AServerPauseIsSharedAcrossCallerAssembliesAndPrecedesAuthentication(bool useDate)
	{
		var calls = 0;
		var elapsed = new Stopwatch();
		var deadline = DateTime.MinValue;
		await using var app = await StartServerAsync(async context =>
		{
			if (Interlocked.Increment(ref calls) == 1)
			{
				context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
				deadline = DateTime.UtcNow.AddSeconds(2).Truncate(TimeSpan.FromSeconds(1));
				context.Response.Headers.RetryAfter = useDate ? deadline.ToString("r", CultureInfo.InvariantCulture) : "1";
				elapsed.Start();
			}
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync("{\"ok\":true}", context.RequestAborted);
		});
		var address = new Uri(app.Urls.Single());
		await ThrowsAsync<InvalidOperationException>(() => new RestRequest { Method = Method.Post }.InvokeAsync<object>(
			address, this, null, CancellationToken));

		var authentications = 0;
		var auth = new CallbackAuthenticator(() =>
		{
			Interlocked.Increment(ref authentications);
			if (useDate)
				IsTrue(DateTime.UtcNow.AddMilliseconds(15) >= deadline, "The signature was generated before the HTTP-date deadline.");
			else
				IsTrue(elapsed.Elapsed >= TimeSpan.FromSeconds(1), "The signature was generated during the shared pause.");
		});
		// String callers use the Ecng.Net.SocketIO assembly's client, rather than the test assembly's client.
		var result = await new RestRequest { Method = Method.Get }.InvokeAsync2<Dictionary<string, bool>>(
			new Uri(address, "another-symbol"), "another-client", null, CancellationToken, auth: auth);

		IsTrue(result.Data["ok"]);
		AreEqual(1, authentications);
		AreEqual(2, calls);
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("60")]
	public async Task CancellationDuringASharedPausePreservesCancellationAndSkipsAuthentication(string retryAfter)
	{
		var calls = 0;
		await using var app = await StartServerAsync(async context =>
		{
			Interlocked.Increment(ref calls);
			context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
			context.Response.ContentType = "text/html";
			if (retryAfter is not null)
				context.Response.Headers.RetryAfter = retryAfter;
			await context.Response.WriteAsync("<html>rate limited</html>", context.RequestAborted);
		});
		var address = new Uri(app.Urls.Single());
		await ThrowsAsync<InvalidOperationException>(() => new RestRequest { Method = Method.Post }.InvokeAsync<object>(
			address, this, null, CancellationToken));

		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));
		var authentications = 0;
		var auth = new CallbackAuthenticator(() => Interlocked.Increment(ref authentications));
		await ThrowsAsync<OperationCanceledException>(() => new RestRequest { Method = Method.Get }.InvokeAsync2<object>(
			address, "another-client", null, cancellation.Token, auth: auth));

		AreEqual(0, authentications);
		AreEqual(1, calls);
	}

	[TestMethod]
	public async Task CancellationDuringTransportIsNotWrappedAsAnHttpFailureOrRetried()
	{
		var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = 0;
		await using var app = await StartServerAsync(async context =>
		{
			Interlocked.Increment(ref calls);
			received.TrySetResult();
			await Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted);
		});
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		var pending = new RestRequest { Method = Method.Get }.InvokeAsync<object>(
			new Uri(app.Urls.Single()), this, null, cancellation.Token);
		await received.Task.WaitAsync(CancellationToken);
		cancellation.Cancel();

		await ThrowsAsync<OperationCanceledException>(() => pending);
		AreEqual(1, calls);
	}

	[TestMethod]
	public async Task APostIsNotReplayedAfterTheServerDropsTheConnection()
	{
		var calls = 0;
		await using var app = await StartServerAsync(context =>
		{
			Interlocked.Increment(ref calls);
			context.Abort();
			return Task.CompletedTask;
		});

		await ThrowsAsync<InvalidOperationException>(() => new RestRequest { Method = Method.Post }.InvokeAsync<object>(
			new Uri(app.Urls.Single()), this, null, CancellationToken));
		AreEqual(1, calls);
	}

	private async Task<WebApplication> StartServerAsync(RequestDelegate handler)
	{
		var builder = WebApplication.CreateBuilder();
		builder.Logging.ClearProviders();
		builder.WebHost.UseUrls("http://127.0.0.1:0");
		var app = builder.Build();
		app.Run(handler);
		await app.StartAsync(CancellationToken);
		return app;
	}
}
