namespace Ecng.Net.Captcha
{
	using System;
	using System.Net.Http.Formatting;
	using System.Security;
	using System.Threading;
	using System.Threading.Tasks;

	using Newtonsoft.Json;

	using Ecng.Collections;
	using Ecng.Common;

	public class ReCaptcha3Validator : Disposable, ICaptchaValidator<float>
	{
		private class ReCaptcha3Response
		{
			[JsonProperty("challenge_ts")]
			public DateTime Timestamp { get; set; }

			[JsonProperty("score")]
			public float Score { get; set; }

			[JsonProperty("success")]
			public bool Success { get; set; }

			[JsonProperty("hostname")]
			public string Hostname { get; set; }

			[JsonProperty("error-codes")]
			public string[] ErrorCodes { get; set; }
		}

		private class ReCaptcha3Client : RestBaseApiClient
		{
			public ReCaptcha3Client()
				: base(new RestApiFormUrlEncodedMediaTypeFormatter(), new JsonMediaTypeFormatter())
			{
				BaseAddress = new("https://www.google.com/recaptcha/api/");
			}

			public Task<ReCaptcha3Response> SiteVerifyAsync(string secret, string response, string remoteip, CancellationToken cancellationToken)
				=> GetAsync<ReCaptcha3Response>(GetCurrentMethod(), cancellationToken, secret, response, remoteip);
		}

		private readonly SecureString _secret;
		private readonly ReCaptcha3Client _client = new();

		public ReCaptcha3Validator(SecureString secret)
		{
			if (secret.IsEmpty())
				throw new ArgumentNullException(nameof(secret));

			_secret = secret;
		}

		protected override void DisposeManaged()
		{
			_client.Dispose();
			base.DisposeManaged();
		}

		async Task<float> ICaptchaValidator<float>.ValidateAsync(string response, string address, CancellationToken cancellationToken)
		{
			var result = await _client.SiteVerifyAsync(_secret.UnSecure(), response, address, cancellationToken);

			if (result.Success)
				return result.Score;

			throw new InvalidOperationException(result.ErrorCodes.Join(Environment.NewLine));
		}
	}
}