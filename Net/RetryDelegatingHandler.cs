namespace Ecng.Net;

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// <see cref="DelegatingHandler"/> that retries transient HTTP failures with exponential
/// backoff and server Retry-After delays. By default, 429 / 503 responses and network
/// failures are retried for all methods; other 5xx responses are retried only for GET / HEAD.
/// Set <see cref="RetryOnlyReadMethods"/> to limit all retries to GET / HEAD.
/// </summary>
public class RetryDelegatingHandler : DelegatingHandler
{
	private readonly int _maxRetries;
	private readonly TimeSpan _baseDelay;

	/// <summary>
	/// Initializes a new instance of the <see cref="RetryDelegatingHandler"/> class.
	/// </summary>
	/// <param name="maxRetries">Maximum number of retries after the first attempt.</param>
	/// <param name="baseDelayMs">Base backoff delay in milliseconds (doubled each attempt).</param>
	public RetryDelegatingHandler(int maxRetries = 3, int baseDelayMs = 500)
	{
		if (maxRetries < 0)
			throw new ArgumentOutOfRangeException(nameof(maxRetries), maxRetries, "Invalid value.");

		_maxRetries = maxRetries;
		_baseDelay = TimeSpan.FromMilliseconds(baseDelayMs);
	}

	/// <summary>
	/// Gets or sets whether retries are limited to GET and HEAD requests.
	/// </summary>
	public bool RetryOnlyReadMethods { get; set; }

	/// <inheritdoc />
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var attempt = 0;
		var canRetry = !RetryOnlyReadMethods || request.Method == HttpMethod.Get || request.Method == HttpMethod.Head;

		while (true)
		{
			TimeSpan? retryAfter = null;
			try
			{
				var response = await base.SendAsync(request, cancellationToken);

				if (!canRetry || !ShouldRetry(request.Method, response.StatusCode) || attempt >= _maxRetries)
					return response;

				var header = response.Headers.RetryAfter;
				retryAfter = header?.Delta ?? (header?.Date - DateTimeOffset.UtcNow);
				response.Dispose();
			}
			catch (HttpRequestException) when (canRetry && attempt < _maxRetries)
			{
				// Network error — retry.
			}
			catch (TaskCanceledException) when (canRetry && !cancellationToken.IsCancellationRequested && attempt < _maxRetries)
			{
				// Timeout (not caller cancellation) — retry.
			}

			attempt++;

			var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
			if (retryAfter > delay)
				delay = retryAfter.Value;
			await Task.Delay(delay, cancellationToken);
		}
	}

	private static bool ShouldRetry(HttpMethod method, HttpStatusCode status)
	{
		// Always retry 429 (rate limited) and 503 (service unavailable).
		if (status is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
			return true;

		// Other server errors: only for idempotent methods.
		if (method == HttpMethod.Get || method == HttpMethod.Head)
			return (int)status >= 500;

		return false;
	}
}
