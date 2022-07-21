namespace Ecng.Net
{
	using System;
	using System.Net.Http;
	using System.Net.Http.Formatting;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;

	using Microsoft.Net.Http.Headers;

	using Newtonsoft.Json;

	public class MjmlResponse
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

	public class MjmlRestClient : RestBaseApiClient
	{
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
}