namespace Ecng.Net
{
	using System;
	using System.Net;

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
			var request = base.GetWebRequest(address);
			request.Timeout = (int)Timeout.TotalMilliseconds;

			var http = request as HttpWebRequest;
			if (http != null)
				http.AutomaticDecompression = DecompressionMethods;

			return request;
		}
	}
}