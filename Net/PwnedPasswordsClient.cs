namespace Ecng.Net;

using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// <see cref="IPasswordBreachChecker"/> backed by the Have I Been Pwned
/// (https://haveibeenpwned.com/API/v3#PwnedPasswords) range API using k-anonymity:
/// only the first 5 chars of the SHA-1 hex hash are sent, the API returns every full
/// hash suffix sharing that prefix, and the match is done locally — the password
/// itself never leaves the process. No API key is required.
/// </summary>
public sealed class PwnedPasswordsClient : IPasswordBreachChecker
{
	private const string _apiBase = "https://api.pwnedpasswords.com/range/";
	private readonly HttpClient _http;

	/// <summary>
	/// Minimum number of recorded breach occurrences for a password to be considered
	/// unsafe. Defaults to 1 (any appearance).
	/// </summary>
	public int MinOccurrences { get; init; } = 1;

	/// <summary>
	/// Initializes a new instance of the <see cref="PwnedPasswordsClient"/> class.
	/// </summary>
	/// <param name="http">The HTTP client to use. A 5s timeout is applied when none is set.</param>
	public PwnedPasswordsClient(HttpClient http)
	{
		_http = http ?? throw new ArgumentNullException(nameof(http));

		if (_http.Timeout == Timeout.InfiniteTimeSpan)
			_http.Timeout = TimeSpan.FromSeconds(5);
	}

	/// <inheritdoc />
	public async Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken = default)
	{
		if (password.IsEmpty())
			return false;

		var hash = ComputeSha1(password).ToUpperInvariant();
		var prefix = hash[..5];
		var suffix = hash[5..];

		try
		{
			using var resp = await _http.GetAsync(_apiBase + prefix, cancellationToken);

			if (!resp.IsSuccessStatusCode)
				return false; // fail open

			using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
			using var reader = new StreamReader(stream, Encoding.ASCII);

			string line;
			while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
			{
				var sep = line.IndexOf(':');
				if (sep <= 0)
					continue;

				if (line.AsSpan(0, sep).Equals(suffix.AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					var rest = line[(sep + 1)..].Trim();
					return int.TryParse(rest, out var count) && count >= MinOccurrences;
				}
			}
		}
		catch (HttpRequestException) { /* fail open */ }
		catch (TaskCanceledException) { /* fail open — timeout */ }

		return false;
	}

	private static string ComputeSha1(string text)
	{
		var bytes = Encoding.UTF8.GetBytes(text);
		var hash = SHA1.HashData(bytes);
		return Convert.ToHexString(hash);
	}
}
