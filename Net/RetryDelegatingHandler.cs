namespace Ecng.Net;

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// <see cref="DelegatingHandler"/> that retries transient HTTP failures with exponential
/// backoff. Rate-limit / unavailable responses (429 / 503) are always retried; other 5xx
/// responses are retried only for idempotent methods (GET / HEAD), as are network errors
/// and timeouts.
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

	/// <inheritdoc />
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var attempt = 0;

		while (true)
		{
			try
			{
				var response = await base.SendAsync(request, cancellationToken);

				if (!ShouldRetry(request.Method, response.StatusCode) || attempt >= _maxRetries)
					return response;
			}
			catch (HttpRequestException) when (attempt < _maxRetries)
			{
				// Network error — retry.
			}
			catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < _maxRetries)
			{
				// Timeout (not caller cancellation) — retry.
			}

			attempt++;

			var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
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
