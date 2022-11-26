namespace Ecng.Net;

using Microsoft.Net.Http.Headers;

public class MjmlRestClient : RestBaseApiClient
{
	public struct MjmlResponse
	{
		[JsonProperty("errors")]
		public object[] Errors { get; set; }

		[JsonProperty("html")]
		public string Html { get; set; }

		[JsonProperty("mjml")]
		public string Mjml { get; set; }

		[JsonProperty("mjml_version")]
		public string MjmlVersion { get; set; }

		[JsonProperty("message")]
		public string Message { get; set; }

		[JsonProperty("request_id")]
		public string RequestId { get; set; }

		[JsonProperty("started_at")]
		public string StartedAt { get; set; }
	}

	public MjmlRestClient(HttpMessageInvoker http, string userName, string password)
		: base(http, new JsonMediaTypeFormatter(), new JsonMediaTypeFormatter())
	{
		BaseAddress = "https://api.mjml.io/v1/".To<Uri>();
		PerRequestHeaders.Add(HeaderNames.Authorization, $"Basic {(userName + ":" + password).UTF8().Base64()}");
	}

	protected override bool PlainSingleArg => false;
	protected override bool ThrowIfNonSuccessStatusCode => false;

	public Task<MjmlResponse> RenderAsync(string mjml, CancellationToken cancellationToken)
		=> PostAsync<MjmlResponse>(GetCurrentMethod(), cancellationToken, mjml);
}