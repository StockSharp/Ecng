namespace Ecng.Net;

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Shares server-requested HTTP pauses between clients in the current process.
/// This handler never replays requests; it also records pauses on final responses and writes.
/// </summary>
public class HttpRateLimitHandler : DelegatingHandler
{
	private sealed class Pause
	{
		private long _until;

		public void Extend(TimeSpan delay)
		{
			if (delay <= TimeSpan.Zero)
				return;

			var now = Stopwatch.GetTimestamp();
			var ticks = Math.Ceiling(delay.TotalSeconds * Stopwatch.Frequency);
			var until = ticks >= long.MaxValue - now ? long.MaxValue : now + (long)ticks;
			long previous;
			do
			{
				previous = Interlocked.Read(ref _until);
				if (previous >= until)
					return;
			}
			while (Interlocked.CompareExchange(ref _until, until, previous) != previous);
		}

		public async Task WaitAsync(CancellationToken cancellationToken)
		{
			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var ticks = Interlocked.Read(ref _until) - Stopwatch.GetTimestamp();
				if (ticks <= 0)
					return;

				// Long IP bans must remain cancellable without exceeding Task.Delay's range.
				var milliseconds = Math.Ceiling(ticks * 1000.0 / Stopwatch.Frequency);
				await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(milliseconds, TimeSpan.FromDays(1).TotalMilliseconds)), cancellationToken).ConfigureAwait(false);
			}
		}
	}

	private static readonly ConcurrentDictionary<string, Pause> _pauses = new(StringComparer.OrdinalIgnoreCase);
	private TimeSpan _defaultDelay = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Initializes a handler. Assign <see cref="DelegatingHandler.InnerHandler"/> before use.
	/// </summary>
	public HttpRateLimitHandler()
	{
	}

	/// <summary>
	/// Initializes a handler around the supplied transport.
	/// </summary>
	/// <param name="innerHandler">The HTTP transport.</param>
	public HttpRateLimitHandler(HttpMessageHandler innerHandler) : base(innerHandler)
	{
	}

	/// <summary>
	/// Fallback pause for 429 and 418 responses without a usable Retry-After header.
	/// The default is thirty seconds. A valid server delay is never capped by this value.
	/// </summary>
	public TimeSpan DefaultDelay
	{
		get => _defaultDelay;
		set
		{
			if (value <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value));
			_defaultDelay = value;
		}
	}

	/// <summary>
	/// Waits for any pause announced by this server, including one recorded by another client.
	/// Call before generating time-sensitive request signatures.
	/// </summary>
	/// <param name="address">The absolute request address. Scheme, host and port identify the server.</param>
	/// <param name="cancellationToken">Cancels the wait.</param>
	public static Task WaitAsync(Uri address, CancellationToken cancellationToken)
		=> GetPause(address).WaitAsync(cancellationToken);

	private static Pause GetPause(Uri address)
	{
		if (address is null)
			throw new ArgumentNullException(nameof(address));

		return _pauses.GetOrAdd(address.GetLeftPart(UriPartial.Authority), static _ => new());
	}

	/// <inheritdoc />
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var pause = GetPause(request.RequestUri);
		await pause.WaitAsync(cancellationToken).ConfigureAwait(false);

		var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
		var isLimited = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode == 418;
		var retryAfter = response.Headers.RetryAfter;
		if (isLimited || ((int)response.StatusCode >= 500 && retryAfter is not null))
		{
			var delay = retryAfter?.Delta ?? (retryAfter?.Date?.UtcDateTime - DateTime.UtcNow);
			pause.Extend(delay ?? _defaultDelay);
		}

		return response;
	}
}
