namespace Ecng.Tests.Net;

using System.Net;
using System.Text;

using Ecng.Net;

[TestClass]
public class RestBaseApiClientTests : BaseTestClass
{
	private class UrlCapturingHandler : HttpMessageHandler
	{
		public Uri LastRequestUri { get; private set; }
		public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
		public string ResponseBody { get; set; } = "null";

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			LastRequestUri = request.RequestUri;

			var response = new HttpResponseMessage(StatusCode);

			if (StatusCode == HttpStatusCode.NoContent)
				response.Content = new StringContent("", Encoding.UTF8, "application/json");
			else
				response.Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json");

			return Task.FromResult(response);
		}
	}

	private sealed class TestFormatter(string mediaType) : IMediaTypeFormatter
	{
		public string MediaType { get; } = mediaType;

		public HttpContent Serialize(object value)
			=> new StringContent(value?.To<string>() ?? string.Empty, Encoding.UTF8, MediaType);

		public Task<T> DeserializeAsync<T>(HttpContent content, CancellationToken cancellationToken)
			=> Task.FromResult(default(T));
	}

	private sealed class HeaderRestClient(IMediaTypeFormatter requestFormatter, IMediaTypeFormatter responseFormatter)
		: RestBaseApiClient(new HttpClient(new UrlCapturingHandler()), requestFormatter, responseFormatter)
	{
		public HttpRequestMessage CreateGet(Uri uri)
			=> CreateRequest(HttpMethod.Get, uri);
	}

	private class TestRestClient : RestBaseApiClient
	{
		public TestRestClient(HttpMessageHandler handler)
			: base(new HttpClient(handler), new JsonMediaTypeFormatter(), new JsonMediaTypeFormatter())
		{
			BaseAddress = new Uri("https://example.com/api/");
		}

		public Task<string> GetItemsAsync(long[] ids, CancellationToken cancellationToken)
			=> GetAsync<string>(GetCurrentMethod(), cancellationToken, ids);

		public Task<string> DeleteItemsAsync(long[] ids, CancellationToken cancellationToken)
			=> DeleteAsync<string>(GetCurrentMethod(), cancellationToken, ids);

		public Task<string> GetByNameAsync(string name, CancellationToken cancellationToken)
			=> GetAsync<string>(GetCurrentMethod(), cancellationToken, name);

		public Task<string> GetByPriceAsync(decimal price, CancellationToken cancellationToken)
			=> GetAsync<string>(GetCurrentMethod(), cancellationToken, price);

		public Task<string> GetFilteredAsync(long[] ids, string filter, CancellationToken cancellationToken)
			=> GetAsync<string>(GetCurrentMethod(), cancellationToken, ids, filter);

		public Task<string> GetWithIntListAsync(List<int> values, CancellationToken cancellationToken)
			=> GetAsync<string>(GetCurrentMethod(), cancellationToken, values);

		public Task<string> GetWithStringsAsync(string[] tags, CancellationToken cancellationToken)
			=> GetAsync<string>(GetCurrentMethod(), cancellationToken, (object)tags);

		public Task<string> TryGetByNameAsync(string name, CancellationToken cancellationToken)
			=> GetAsync<string>(GetCurrentMethod(), cancellationToken, name);

		public Task<int> GetCountAsync(string name, CancellationToken cancellationToken)
			=> GetAsync<int>(GetCurrentMethod(), cancellationToken, name);

		// Method signature carries an extra parameter, but the caller passes
		// only `itemId` — exercises the args/params count-mismatch path.
		public Task<string> GetWithMissingArgAsync(long tenantId, long itemId, CancellationToken cancellationToken)
			=> GetAsync<string>(GetCurrentMethod(), cancellationToken, itemId);

		// Server-bound parameter: kept on the C# signature, excluded from
		// the wire via [Rest(Ignore = true)]; caller passes only `itemId`.
		public Task<string> GetWithIgnoredTenantAsync([Rest(Ignore = true)] long tenantId, long itemId, CancellationToken cancellationToken)
			=> GetAsync<string>(GetCurrentMethod(), cancellationToken, itemId);

		public Task<string> GetOverloadedAsync(string name, CancellationToken cancellationToken)
			=> GetAsync<string>(GetCurrentMethod(), cancellationToken, name);

		public Task<string> GetOverloadedAsync(string name, int page, CancellationToken cancellationToken)
			=> GetAsync<string>(GetCurrentMethod(), cancellationToken, name, page);
	}

	/// <summary>
	/// Verifies that array parameters in GET requests are comma-aggregated, not ToString'd.
	/// </summary>
	[TestMethod]
	public async Task GetAsync_ArrayParameter_ShouldAggregateWithComma()
	{
		var handler = new UrlCapturingHandler();
		var client = new TestRestClient(handler);

		await client.GetItemsAsync([1151, 1153, 1154], CancellationToken);

		var query = handler.LastRequestUri.Query;

		// should contain all values, NOT "System.Int64[]"
		query.Contains("1151").AssertTrue();
		query.Contains("1153").AssertTrue();
		query.Contains("1154").AssertTrue();
		query.Contains("Int64").AssertFalse();
	}

	[TestMethod]
	public async Task GetAsync_OverloadsWithSameNameUseMatchingSignature()
	{
		var handler = new UrlCapturingHandler();
		var client = new TestRestClient(handler);

		await client.GetOverloadedAsync("alpha", CancellationToken);
		await client.GetOverloadedAsync("beta", 2, CancellationToken);

		var query = handler.LastRequestUri.Query;

		query.Contains("beta").AssertTrue();
		query.Contains("2").AssertTrue();
	}

	/// <summary>
	/// Verifies that array parameters in DELETE requests are comma-aggregated.
	/// </summary>
	[TestMethod]
	public async Task DeleteAsync_ArrayParameter_ShouldAggregateWithComma()
	{
		var handler = new UrlCapturingHandler();
		var client = new TestRestClient(handler);

		await client.DeleteItemsAsync([10, 20, 30], CancellationToken);

		var query = handler.LastRequestUri.Query;

		query.Contains("10").AssertTrue();
		query.Contains("20").AssertTrue();
		query.Contains("30").AssertTrue();
		query.Contains("Int64").AssertFalse();
	}

	/// <summary>
	/// Verifies that string parameters are NOT expanded as enumerable.
	/// </summary>
	[TestMethod]
	public async Task GetAsync_StringParameter_ShouldNotBeExpanded()
	{
		var handler = new UrlCapturingHandler();
		var client = new TestRestClient(handler);

		await client.GetByNameAsync("hello", CancellationToken);

		var query = handler.LastRequestUri.Query;

		query.Contains("hello").AssertTrue();
		// should NOT contain individual chars
		query.Contains("h%2c").AssertFalse();
	}

	/// <summary>
	/// Verifies that array and scalar parameters work together.
	/// </summary>
	[TestMethod]
	public async Task GetAsync_ArrayAndScalarParameters_ShouldWorkTogether()
	{
		var handler = new UrlCapturingHandler();
		var client = new TestRestClient(handler);

		await client.GetFilteredAsync([1, 2], "active", CancellationToken);

		var query = handler.LastRequestUri.Query;

		query.Contains("1").AssertTrue();
		query.Contains("2").AssertTrue();
		query.Contains("active").AssertTrue();
		query.Contains("Int64").AssertFalse();
	}

	/// <summary>
	/// Verifies that null elements in array are skipped without exception.
	/// </summary>
	[TestMethod]
	public async Task GetAsync_ArrayWithNullElements_ShouldNotThrow()
	{
		var handler = new UrlCapturingHandler();
		var client = new TestRestClient(handler);

		await client.GetWithStringsAsync(["a", null, "b"], CancellationToken);

		var query = handler.LastRequestUri.Query;

		query.Contains("a").AssertTrue();
		query.Contains("b").AssertTrue();
	}

	/// <summary>
	/// Verifies that List parameters are also expanded like arrays.
	/// </summary>
	[TestMethod]
	public async Task GetAsync_ListParameter_ShouldAggregateWithComma()
	{
		var handler = new UrlCapturingHandler();
		var client = new TestRestClient(handler);

		await client.GetWithIntListAsync([100, 200], CancellationToken);

		var query = handler.LastRequestUri.Query;

		query.Contains("100").AssertTrue();
		query.Contains("200").AssertTrue();
		query.Contains("Int32").AssertFalse();
	}

	/// <summary>
	/// Verifies that 204 No Content returns default instead of deserialization error.
	/// </summary>
	[TestMethod]
	public async Task GetAsync_NoContent_ReturnsDefault_ReferenceType()
	{
		var handler = new UrlCapturingHandler { StatusCode = HttpStatusCode.NoContent };
		var client = new TestRestClient(handler);

		var result = await client.TryGetByNameAsync("missing", CancellationToken);

		result.AssertNull();
	}

	/// <summary>
	/// Verifies that 204 No Content returns default for value types.
	/// </summary>
	[TestMethod]
	public async Task GetAsync_NoContent_ReturnsDefault_ValueType()
	{
		var handler = new UrlCapturingHandler { StatusCode = HttpStatusCode.NoContent };
		var client = new TestRestClient(handler);

		var result = await client.GetCountAsync("missing", CancellationToken);

		0.AssertEqual(result);
	}

	/// <summary>
	/// Verifies that empty content body returns default instead of deserialization error.
	/// </summary>
	[TestMethod]
	public async Task GetAsync_EmptyBody_ReturnsDefault()
	{
		var handler = new UrlCapturingHandler { ResponseBody = "" };
		var client = new TestRestClient(handler);

		var result = await client.TryGetByNameAsync("empty", CancellationToken);

		result.AssertNull();
	}

	/// <summary>
	/// Verifies that the args/params count-mismatch exception names the
	/// [Rest(Ignore = true)] escape hatch so callers can discover it without
	/// reading GetInfo source.
	/// </summary>
	[TestMethod]
	public async Task GetInfo_ArgCountMismatch_MentionsRestIgnoreEscapeHatch()
	{
		var handler = new UrlCapturingHandler();
		var client = new TestRestClient(handler);

		var ex = await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
			() => client.GetWithMissingArgAsync(42L, 7L, CancellationToken));

		ex.Message.Contains("Rest(Ignore").AssertTrue(
			$"Expected mismatch message to mention the [Rest(Ignore = true)] " +
			$"escape hatch, got: {ex.Message}");
	}

	/// <summary>
	/// Verifies that a [Rest(Ignore = true)] parameter is filtered out of
	/// the wire shape: the call succeeds with one fewer arg, and the
	/// parameter name never appears in the request URL.
	/// </summary>
	[TestMethod]
	public async Task GetAsync_RestIgnoreParameter_IsFilteredFromWire()
	{
		var handler = new UrlCapturingHandler();
		var client = new TestRestClient(handler);

		await client.GetWithIgnoredTenantAsync(42L, 7L, CancellationToken);

		var query = handler.LastRequestUri.Query;

		query.ContainsIgnoreCase("tenantId").AssertFalse(
			$"[Rest(Ignore=true)] tenantId must not appear on the wire; got: {query}");
		query.Contains("7").AssertTrue($"Expected itemId=7 on the wire; got: {query}");
	}

	[TestMethod]
	[DataRow("application/request", "application/response")]
	[DataRow("application/json", "text/plain")]
	public void CreateRequest_AcceptHeaderUsesResponseFormatter(string requestMediaType, string responseMediaType)
	{
		var client = new HeaderRestClient(
			new TestFormatter(requestMediaType),
			new TestFormatter(responseMediaType));
		using var request = client.CreateGet(new Uri("https://example.com/api"));

		request.Headers.Accept.Single().MediaType.AssertEqual(responseMediaType);
	}

	/// <summary>
	/// Extracts the value of the named query parameter from <paramref name="uri"/> and
	/// URL-decodes it back to the raw value the server would observe.
	/// </summary>
	private static string GetDecodedQueryValue(Uri uri, string name)
	{
		// Query starts with '?'; strip it and split on '&'.
		var query = uri.Query.TrimStart('?');

		foreach (var pair in query.Split('&'))
		{
			var idx = pair.IndexOf('=');

			if (idx < 0)
				continue;

			var key = WebUtility.UrlDecode(pair.Substring(0, idx));

			if (key == name)
				return WebUtility.UrlDecode(pair.Substring(idx + 1));
		}

		return null;
	}

	/// <summary>
	/// Regression test for query-value encoding: ensures GET/DELETE query values are passed
	/// through verbatim (no HTML entity escaping), so the server receives the original raw
	/// value after URL-decoding. (Was: FormatQueryValue HTML-encoded values via
	/// value?.ToString().EncodeToHtml(), corrupting "a&b" into "a&amp;b",
	/// Net.Clients\RestBaseApiClient.cs:557.)
	/// </summary>
	[TestMethod]
	public async Task FormatQueryValue_SpecialChars_NotHtmlEncoded()
	{
		var handler = new UrlCapturingHandler();
		var client = new TestRestClient(handler);

		const string raw = "a&b<é>";

		await client.GetByNameAsync(raw, CancellationToken);

		var decoded = GetDecodedQueryValue(handler.LastRequestUri, "name");

		// The server must receive the original value, with no HTML entity escaping.
		decoded.AssertEqual(raw);
	}

	/// <summary>
	/// Regression test for numeric query formatting: ensures decimal/double GET args are
	/// formatted with the invariant culture ("1.5"), independent of the current thread
	/// culture. (Was: FormatQueryValue used value?.ToString() with the current thread culture,
	/// producing "1,5" on ru-RU, Net.Clients\RestBaseApiClient.cs:557.)
	/// </summary>
	[TestMethod]
	public async Task FormatQueryValue_Decimal_InvariantCulture()
	{
		var oldCulture = Thread.CurrentThread.CurrentCulture;

		try
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");

			var handler = new UrlCapturingHandler();
			var client = new TestRestClient(handler);

			await client.GetByPriceAsync(1.5m, CancellationToken);

			var decoded = GetDecodedQueryValue(handler.LastRequestUri, "price");

			decoded.AssertEqual("1.5");
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = oldCulture;
		}
	}
}
