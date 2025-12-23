namespace Ecng.Net;

/// <summary>
/// Contains constants for common HTTP header field names.
/// </summary>
public static class HttpHeaders
{
	/// <summary>
	/// Gets the header name for Authorization.
	/// </summary>
	public const string Authorization = "Authorization";

	/// <summary>
	/// Gets the header name for Accept-Encoding.
	/// </summary>
	public const string AcceptEncoding = "Accept-Encoding";

	/// <summary>
	/// Gets the header name for Accept-Language.
	/// </summary>
	public const string AcceptLanguage = "Accept-Language";

	/// <summary>
	/// Gets the header name for Cache-Control.
	/// </summary>
	public const string CacheControl = "Cache-Control";

	/// <summary>
	/// Gets the header name for Connection.
	/// </summary>
	public const string Connection = "Connection";

	/// <summary>
	/// Gets the header name for Keep-Alive.
	/// </summary>
	public const string KeepAlive = "Keep-Alive";

	/// <summary>
	/// Gets the header name for Last-Modified.
	/// </summary>
	public const string LastModified = "Last-Modified";

	/// <summary>
	/// Gets the header name for Proxy-Authenticate.
	/// </summary>
	public const string ProxyAuthenticate = "Proxy-Authenticate";

	/// <summary>
	/// Gets the header name for Proxy-Authorization.
	/// </summary>
	public const string ProxyAuthorization = "Proxy-Authorization";

	/// <summary>
	/// Gets the header name for Proxy-Connection.
	/// </summary>
	public const string ProxyConnection = "Proxy-Connection";

	/// <summary>
	/// Gets the header name for User-Agent.
	/// </summary>
	public const string UserAgent = "User-Agent";

	/// <summary>
	/// Gets the header name for Referer.
	/// </summary>
	public const string Referer = "Referer";

	/// <summary>
	/// Gets the header name for WWW-Authenticate.
	/// </summary>
	public const string WWWAuthenticate = "WWW-Authenticate";

	/// <summary>
	/// Gets the header name for CF-Connecting-IP (Cloudflare).
	/// </summary>
	public const string CFConnectingIP = "CF-Connecting-IP";

	/// <summary>
	/// Gets the header name for True-Client-IP (Cloudflare/Akamai).
	/// </summary>
	public const string TrueClientIP = "True-Client-IP";

	/// <summary>
	/// Gets the header name for X-Real-IP (nginx).
	/// </summary>
	public const string XRealIP = "X-Real-IP";

	/// <summary>
	/// Gets the header name for X-Forwarded-For (standard proxy header).
	/// </summary>
	public const string XForwardedFor = "X-Forwarded-For";

	/// <summary>
	/// Gets the header name for Forwarded (RFC 7239).
	/// </summary>
	public const string Forwarded = "Forwarded";

	/// <summary>
	/// Gets the list of headers used to determine the real client IP address behind proxies.
	/// </summary>
	public static readonly string[] ClientIpHeaders =
	[
		CFConnectingIP,
		TrueClientIP,
		XRealIP,
		XForwardedFor,
		Forwarded,
	];
}