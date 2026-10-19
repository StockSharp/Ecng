namespace Ecng.Tests.Net;

using Ecng.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using RestSharp;

[TestClass]
public class RestSharpRetryTests : BaseTestClass
{
	[TestMethod]
	public async Task GetRecoversFromHtmlRateLimitResponse()
	{
		var calls = 0;
		await using var app = await StartServerAsync(async context =>
		{
			var limited = Interlocked.Increment(ref calls) == 1;
			context.Response.StatusCode = limited ? StatusCodes.Status429TooManyRequests : StatusCodes.Status200OK;
			context.Response.ContentType = limited ? "text/html" : "application/json";
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
	public async Task PersistentRateLimitStillFailsAfterBoundedRetries()
	{
		var calls = 0;
		await using var app = await StartServerAsync(async context =>
		{
			Interlocked.Increment(ref calls);
			context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
			context.Response.ContentType = "text/html";
			await context.Response.WriteAsync("<html>429 Too Many Requests</html>", context.RequestAborted);
		});

		await ThrowsAsync<InvalidOperationException>(() => new RestRequest { Method = Method.Get }.InvokeAsync<object>(
			new Uri(app.Urls.Single()), this, null, CancellationToken));
		AreEqual(4, calls);
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
