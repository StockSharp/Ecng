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
}
