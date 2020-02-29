namespace Ecng.Net
{
	using System.Text;
	using System.Web;
	using System.Collections.Specialized;

	public static class HttpHelper
	{
		public static string EncodeToHtml(this string text)
		{
			return HttpUtility.HtmlEncode(text);
		}

		public static string DecodeFromHtml(this string text)
		{
			return HttpUtility.HtmlDecode(text);
		}

		private static readonly Encoding _urlEncoding = Encoding.UTF8;

		public static string EncodeUrl(this string url)
		{
			return HttpUtility.UrlEncode(url, _urlEncoding);
		}

		public static string DecodeUrl(this string url)
		{
			return HttpUtility.UrlDecode(url, _urlEncoding);
		}

		public static NameValueCollection ParseUrl(this string url)
		{
			return HttpUtility.ParseQueryString(url, _urlEncoding);
		}
	}
}
