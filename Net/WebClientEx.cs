namespace Ecng.Net
{
	using System;
	using System.Net;

	[Obsolete]
	public class WebClientEx : WebClient
	{
		public WebClientEx()
		{
			Timeout = TimeSpan.FromSeconds(60);
		}

		public TimeSpan Timeout { get; set; }

		public DecompressionMethods DecompressionMethods { get; set; }

		protected override WebRequest GetWebRequest(Uri address)
		{
			RequestedUri = address;
			ResponseUri = null;

			var request = base.GetWebRequest(address);

			if (request != null)
				request.Timeout = (int)Timeout.TotalMilliseconds;

			if (request is HttpWebRequest http)
				http.AutomaticDecompression = DecompressionMethods;

			return request;
		}

		public Uri RequestedUri { get; private set; }

		// http://stackoverflow.com/questions/690587/using-webclient-in-c-sharp-is-there-a-way-to-get-the-url-of-a-site-after-being-r
		public Uri ResponseUri { get; private set; }

		protected override WebResponse GetWebResponse(WebRequest request)
		{
			var response = base.GetWebResponse(request);

			if (response != null)
				ResponseUri = response.ResponseUri;

			return response;
		}

		public bool IsRedirected()
		{
			return RequestedUri != ResponseUri;
		}
	}
}