namespace Ecng.Tests.Net;

using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Text;

using Ecng.Net;

using Nito.AsyncEx;

using SixLabors.ImageSharp;

[TestClass]
public class NetworkHelperTests : BaseTestClass
{
	[TestMethod]
	public void Gravatar()
	{
		"info@stocksharp.com".GetGravatarToken().GetGravatarUrl(100).AssertEqual("https://www.gravatar.com/avatar/cf4c4e682b9869e05c4cc4536b734828?size=100");
	}

	[TestMethod]
	public void Cache()
	{
		var correctUrl = "https://stocksharp.com/api/products?id=12".To<Uri>();
		var wrongUrl = "https://stocksharp.com/api/products?id=13".To<Uri>();

		var method = HttpMethod.Get;

		IRestApiClientCache cache = new InMemoryRestApiClientCache(TimeSpan.FromHours(1));
		cache.Set(method, correctUrl, default, new { });

		cache.TryGet<object>(method, correctUrl, default, out _).AssertTrue();
		cache.TryGet<object>(method, wrongUrl, default, out _).AssertFalse();

		cache.Remove(method, wrongUrl.To<string>());
		cache.Remove(method, correctUrl.To<string>());

		cache.TryGet<object>(method, correctUrl, default, out _).AssertFalse();
		cache.TryGet<object>(method, wrongUrl, default, out _).AssertFalse();

		cache.Set<object>(method, correctUrl, default, null);
		cache.TryGet<object>(method, correctUrl, default, out _).AssertFalse();

		cache.Set(method, correctUrl, default, 0);
		cache.TryGet<object>(method, correctUrl, default, out _).AssertTrue();

		cache.Remove();
		cache.TryGet<object>(method, correctUrl, default, out _).AssertFalse();

		cache.Set(method, correctUrl, default, 0);
		cache.Remove(method, "https://stocksharp.com/api/products");
		cache.TryGet<object>(method, correctUrl, default, out _).AssertFalse();
	}

	[TestMethod]
	public void IsImage()
	{
		"1.png".IsImage().AssertTrue();
		"C:\\1.png".IsImage().AssertTrue();

		"1.svg".IsImage().AssertTrue();
		"C:\\1.svg".IsImage().AssertTrue();

		"1.doc".IsImage().AssertFalse();
		"C:\\1.doc".IsImage().AssertFalse();

		"1.doc".IsImageVector().AssertFalse();
		"C:\\1.doc".IsImageVector().AssertFalse();

		"1.svg".IsImageVector().AssertTrue();
		"C:\\1.svg".IsImageVector().AssertTrue();

		".png".IsImage().AssertTrue();
		"C:\\.png".IsImage().AssertTrue();

		".svg".IsImageVector().AssertTrue();
		"C:\\.svg".IsImageVector().AssertTrue();
	}

	[TestMethod]
	public void IsInSubnet()
	{
		static bool IsInSubnet(string addr)
			=> addr.To<IPAddress>().IsInSubnet("95.31.0.0/16");

		IsInSubnet("95.31.174.147").AssertTrue();
		IsInSubnet("95.31.174.134").AssertTrue();
		IsInSubnet("95.31.174.112").AssertTrue();
		IsInSubnet("95.32.161.158").AssertFalse();
	}

	[TestMethod]
	public void ContentType2Encoding()
	{
		var result = ((string)null).TryExtractEncoding();
		result.AssertNull();

		string.Empty.TryExtractEncoding().AssertNull();
		"text/html".TryExtractEncoding().AssertNull();

		result = "text/html; charset=utf-8".TryExtractEncoding();
		result.AssertNotNull();
		result.WebName.AreEqual(Encoding.UTF8.WebName);

		result = "application/json; charset=\"ISO-8859-1\"".TryExtractEncoding();
		result.AssertNotNull();
		result.WebName.AreEqual(Encoding.GetEncoding("iso-8859-1").WebName);

		result = "text/plain; charset=windows-1252; format=flowed".TryExtractEncoding();
		result.AssertNotNull();
		result.WebName.AreEqual(Encoding.GetEncoding("windows-1252").WebName);

		result = "text/html; charset=invalid-charset".TryExtractEncoding();
		result.AssertNull();

		result = "charset=UTF-16; text/html".TryExtractEncoding();
		result.AssertNotNull();
		result.WebName.AreEqual(Encoding.Unicode.WebName);
	}

	[TestMethod]
	public void EndPoint_HostPort_Setters()
	{
		var ip = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
		ip.GetHost().AssertEqual("127.0.0.1");
		ip.GetPort().AssertEqual(8080);

		ip.SetHost("192.168.0.1");
		ip.GetHost().AssertEqual("192.168.0.1");

		ip.SetPort(9090);
		ip.GetPort().AssertEqual(9090);

		var dns = new DnsEndPoint("example.com", 80);
		dns.GetHost().AssertEqual("example.com");
		dns.GetPort().AssertEqual(80);

		var dns2 = (DnsEndPoint)dns.SetHost("host.test");
		dns2.Host.AssertEqual("host.test");

		var dns3 = (DnsEndPoint)dns.SetPort(1234);
		dns3.Port.AssertEqual(1234);
	}

	[TestMethod]
	public void Html_Url_Parsing_Basics()
	{
		"<b>ok</b>".EncodeToHtml().AssertEqual("&lt;b&gt;ok&lt;/b&gt;");
		"&lt;b&gt;ok&lt;/b&gt;".DecodeFromHtml().AssertEqual("<b>ok</b>");

		var col = "a=1&b=two".ParseUrl();
		var items = col.ExcludeEmpty().ToArray();
		items.Length.AssertEqual(2);
		items[0].key.AssertEqual("a");
		items[0].value.AssertEqual("1");

		// Format + ToQueryString
		var qs = new[] { new KeyValuePair<string, string>("q", "a b") }.ToQueryString(true);
		qs.AssertEqual("q=a+b");
	}

	[TestMethod]
	public void TryExtractEncoding_And_UrlEncodeUpper()
	{
		"text/html; charset=utf-8".TryExtractEncoding().WebName.AssertEqual(Encoding.UTF8.WebName);
		// current implementation uses WebUtility.UrlEncode, which encodes '%' to '%25'
		"q%ab%3f".EncodeUrlUpper().AssertEqual("q%25ab%253f");
	}

	[TestMethod]
	public void XmlEscape_ClearUrl_IsUrlSafeChar()
	{
		"<x>&".XmlEscape().AssertEqual("&lt;x&gt;&amp;");

		"a+.,%*b".ClearUrl().AssertEqual("ab");
		'a'.IsUrlSafeChar().AssertTrue();
		'+'.IsUrlSafeChar().AssertFalse();
	}

	[TestMethod]
	public void ImageAndUrlChecks()
	{
		"1.png".IsImage().AssertTrue();
		"1.svg".IsImageVector().AssertTrue();
		"no.doc".IsImage().AssertFalse();

		"click href=\"x\"".CheckContainsUrl().AssertTrue();
		new Uri("http://localhost/test").IsLocalhost().AssertTrue();
	}

	[TestMethod]
	public void GravatarMethods()
	{
		var token = "info@stocksharp.com".GetGravatarToken();
		token.AssertNotNull();
		var url = token.GetGravatarUrl(80);
		url.Contains(token).AssertTrue();
		url.Contains("size=80").AssertTrue();

		ThrowsExactly<ArgumentNullException>(() => "".GetGravatarToken());
		ThrowsExactly<ArgumentNullException>(() => "".GetGravatarUrl(10));
	}

	[TestMethod]
	[TestCategory("Integration")]
	public async Task GravatarDownload_Works_ForDifferentSizes()
	{
		// Arrange
		var token = "info@stocksharp.com".GetGravatarToken();
		using var http = new HttpClient();
		var sizes = new[] { 16, 80, 200 };
		var ct = CancellationToken;

		await sizes.Select(async size =>
		{
			var url = token.GetGravatarUrl(size);

			// Act
			var bytes = await http.GetByteArrayAsync(url, ct);

			// Assert
			bytes.Length.AssertGreater(0);

			using var image = Image.Load(bytes);
			image.Width.AssertEqual(size);
			image.Height.AssertEqual(size);
		}).WhenAll();
	}

	[TestMethod]
	public void TryGetStatusCode_FromMessage()
	{
		var ex1 = new HttpRequestException("404 Something");
		ex1.TryGetStatusCode().AssertEqual(HttpStatusCode.NotFound);

		var ex2 = new HttpRequestException("not found");
		ex2.TryGetStatusCode().AssertEqual(HttpStatusCode.NotFound);
	}

	[TestMethod]
	public void IsInSubnet_Ipv4_and_Ipv6()
	{
		"95.31.174.147".To<IPAddress>().IsInSubnet("95.31.0.0/16").AssertTrue();

		var addr6 = IPAddress.Parse("2001:db8::1");
		// mask with different family should return false
		addr6.IsInSubnet("192.168.0.0/16").AssertFalse();
	}

	[TestMethod]
	public void ChangeOrder_Reverses_When_Endianness_Differs()
	{
		var src = new byte[] { 1, 2, 3, 4 };
		var reversed = src.ToArray();
		// force isLittleEndian opposite to system
		var result = reversed.ChangeOrder(4, !BitConverter.IsLittleEndian);
		result.AssertEqual([4, 3, 2, 1]);
	}

	[TestMethod]
	public async Task GetDelay_And_TryRepeat_RetriesOnSocketError()
	{
		var policy = new RetryPolicyInfo { InitialDelay = TimeSpan.FromMilliseconds(1), MaxDelay = TimeSpan.FromMilliseconds(1000) };
		policy.Track.Clear();
		policy.Track.Add(SocketError.TimedOut);

		var delay = policy.GetDelay(3);
		(delay.TotalMilliseconds >= policy.InitialDelay.TotalMilliseconds / 1).AssertTrue();
		(delay.TotalMilliseconds <= policy.MaxDelay.TotalMilliseconds).AssertTrue();

		var attempts = 0;

		Task<string> Handler(CancellationToken ct)
		{
			attempts++;

			if (attempts < 3)
				throw new SocketException((int)SocketError.TimedOut);

			return "done".FromResult();
		}

		var res = await policy.TryRepeat(Handler, 5, CancellationToken);
		res.AssertEqual("done");
		attempts.AssertGreater(1);
	}

	[TestMethod]
	public void IsNetworkPath_UNCPath()
	{
		@"\\server\share".IsNetworkPath().AssertTrue();
		@"\\192.168.1.1\share".IsNetworkPath().AssertTrue();
		@"\\server\share\folder\file.txt".IsNetworkPath().AssertTrue();
		@"\\.\pipe\mypipe".IsNetworkPath().AssertTrue();
		@"//server/share".IsNetworkPath().AssertTrue();
		@"//192.168.1.1/share/folder".IsNetworkPath().AssertTrue();
	}

	[TestMethod]
	public void IsNetworkPath_LocalPath()
	{
		@"C:\Windows\System32".IsNetworkPath().AssertFalse();
		@"D:\Data\file.txt".IsNetworkPath().AssertFalse();
		@"E:\".IsNetworkPath().AssertFalse();
		@"Z:\NetworkShare".IsNetworkPath().AssertFalse();
		@"Y:\Data\file.txt".IsNetworkPath().AssertFalse();
	}

	[TestMethod]
	public void IsNetworkPath_UnixPath()
	{
		@"/usr/bin/bash".IsNetworkPath().AssertFalse();
		@"/home/user/file.txt".IsNetworkPath().AssertFalse();
	}

	[TestMethod]
	public void IsNetworkPath_EmptyOrNull()
	{
		ThrowsExactly<ArgumentNullException>(() => string.Empty.IsNetworkPath());
		ThrowsExactly<ArgumentNullException>(() => ((string)null).IsNetworkPath());
	}

	[TestMethod]
	public void IsNetworkPath_RelativePath()
	{
		@"folder\file.txt".IsNetworkPath().AssertFalse();
		@".\file.txt".IsNetworkPath().AssertFalse();
		@"..\file.txt".IsNetworkPath().AssertFalse();
		@"Folder1".IsNetworkPath().AssertFalse();
		@"temp".IsNetworkPath().AssertFalse();
	}

	[TestMethod]
	public void IsNetworkPath_HttpPath()
	{
		@"http://example.com/file.txt".IsNetworkPath().AssertTrue();
		@"https://example.com/folder/".IsNetworkPath().AssertTrue();
		@"ftp://ftp.example.com/data".IsNetworkPath().AssertTrue();
	}

	[TestMethod]
	public void IsNetworkPath_ShortPath()
	{
		// Short paths should return false, not throw
		"C:".IsNetworkPath().AssertFalse();
		"ab".IsNetworkPath().AssertFalse();
		"a".IsNetworkPath().AssertFalse();
	}

	[TestMethod]
	public void IsNetworkPath_HostPort()
	{
		// IP:port addresses
		"127.0.0.1:5001".IsNetworkPath().AssertTrue();
		"192.168.1.1:8080".IsNetworkPath().AssertTrue();
		"10.0.0.1:443".IsNetworkPath().AssertTrue();

		// hostname:port addresses
		"localhost:5001".IsNetworkPath().AssertTrue();
		"example.com:80".IsNetworkPath().AssertTrue();
		"my-server:9000".IsNetworkPath().AssertTrue();
	}

	[TestMethod]
	public void IsUncPath()
	{
		// Valid UNC paths
		@"\\server\share".IsUncPath().AssertTrue();
		@"\\192.168.1.1\share".IsUncPath().AssertTrue();
		@"//server/share".IsUncPath().AssertTrue();

		// Not UNC paths
		@"C:\folder".IsUncPath().AssertFalse();
		"http://example.com".IsUncPath().AssertFalse();
		"localhost:5001".IsUncPath().AssertFalse();
		string.Empty.IsUncPath().AssertFalse();
		((string)null).IsUncPath().AssertFalse();
	}

	[TestMethod]
	public void IsUrlPath()
	{
		// Valid URL paths
		"http://example.com".IsUrlPath().AssertTrue();
		"https://example.com/path".IsUrlPath().AssertTrue();
		"ftp://ftp.example.com".IsUrlPath().AssertTrue();
		"HTTP://EXAMPLE.COM".IsUrlPath().AssertTrue();

		// Not URL paths
		@"\\server\share".IsUrlPath().AssertFalse();
		@"C:\folder".IsUrlPath().AssertFalse();
		"localhost:5001".IsUrlPath().AssertFalse();
		string.Empty.IsUrlPath().AssertFalse();
		((string)null).IsUrlPath().AssertFalse();
	}

	[TestMethod]
	public void IsHostPortAddress()
	{
		// Valid host:port
		"127.0.0.1:5001".IsHostPortAddress().AssertTrue();
		"localhost:8080".IsHostPortAddress().AssertTrue();
		"example.com:443".IsHostPortAddress().AssertTrue();
		"my-server:9000".IsHostPortAddress().AssertTrue();
		"192.168.1.1:1".IsHostPortAddress().AssertTrue();
		"host:65535".IsHostPortAddress().AssertTrue();

		// Invalid - Windows paths
		@"C:\folder".IsHostPortAddress().AssertFalse();
		@"D:\file.txt".IsHostPortAddress().AssertFalse();

		// Invalid - no port or invalid port
		"localhost".IsHostPortAddress().AssertFalse();
		"localhost:".IsHostPortAddress().AssertFalse();
		":5001".IsHostPortAddress().AssertFalse();
		"localhost:0".IsHostPortAddress().AssertFalse();
		"localhost:99999".IsHostPortAddress().AssertFalse();
		"localhost:abc".IsHostPortAddress().AssertFalse();

		// Invalid - empty/null
		string.Empty.IsHostPortAddress().AssertFalse();
		((string)null).IsHostPortAddress().AssertFalse();
	}

	[TestMethod]
	public void IsFileUriPath()
	{
		// Valid file:// URIs
		"file://server/share".IsFileUriPath().AssertTrue();
		"file:///C:/folder/file.txt".IsFileUriPath().AssertTrue();
		"FILE://SERVER/SHARE".IsFileUriPath().AssertTrue();

		// Not file:// URIs
		@"\\server\share".IsFileUriPath().AssertFalse();
		"http://example.com".IsFileUriPath().AssertFalse();
		@"C:\folder".IsFileUriPath().AssertFalse();
		string.Empty.IsFileUriPath().AssertFalse();
		((string)null).IsFileUriPath().AssertFalse();
	}

	[TestMethod]
	public void IsWebDavPath()
	{
		// Valid WebDAV paths
		"dav://server/folder".IsWebDavPath().AssertTrue();
		"davs://server/secure/folder".IsWebDavPath().AssertTrue();
		"DAV://SERVER/FOLDER".IsWebDavPath().AssertTrue();
		"DAVS://SERVER/FOLDER".IsWebDavPath().AssertTrue();

		// Not WebDAV paths
		"http://webdav.example.com".IsWebDavPath().AssertFalse();
		"https://webdav.example.com".IsWebDavPath().AssertFalse();
		@"\\server\share".IsWebDavPath().AssertFalse();
		string.Empty.IsWebDavPath().AssertFalse();
		((string)null).IsWebDavPath().AssertFalse();
	}

	[TestMethod]
	public void IsNetworkPath_FileUri()
	{
		"file://server/share".IsNetworkPath().AssertTrue();
		"file:///C:/folder/file.txt".IsNetworkPath().AssertTrue();
	}

	[TestMethod]
	public void IsNetworkPath_WebDav()
	{
		"dav://server/folder".IsNetworkPath().AssertTrue();
		"davs://server/secure/folder".IsNetworkPath().AssertTrue();
	}

	[TestMethod]
	public void TryParseClientIpHeader_SimpleIPv4()
	{
		NetworkHelper.TryParseClientIpHeader("192.168.1.1", out var ip).AssertTrue();
		ip.AssertEqual(IPAddress.Parse("192.168.1.1"));
	}

	[TestMethod]
	public void TryParseClientIpHeader_IPv4WithPort()
	{
		NetworkHelper.TryParseClientIpHeader("192.168.1.1:8080", out var ip).AssertTrue();
		ip.AssertEqual(IPAddress.Parse("192.168.1.1"));
	}

	[TestMethod]
	public void TryParseClientIpHeader_SimpleIPv6()
	{
		NetworkHelper.TryParseClientIpHeader("::1", out var ip).AssertTrue();
		ip.AssertEqual(IPAddress.Parse("::1"));

		NetworkHelper.TryParseClientIpHeader("2001:db8::1", out ip).AssertTrue();
		ip.AssertEqual(IPAddress.Parse("2001:db8::1"));
	}

	[TestMethod]
	public void TryParseClientIpHeader_IPv6WithBracketsAndPort()
	{
		NetworkHelper.TryParseClientIpHeader("[::1]:8080", out var ip).AssertTrue();
		ip.AssertEqual(IPAddress.Parse("::1"));

		NetworkHelper.TryParseClientIpHeader("[2001:db8::1]:443", out ip).AssertTrue();
		ip.AssertEqual(IPAddress.Parse("2001:db8::1"));
	}

	[TestMethod]
	public void TryParseClientIpHeader_CommaSeparatedList()
	{
		// Should take the first IP
		NetworkHelper.TryParseClientIpHeader("192.168.1.1, 10.0.0.1, 172.16.0.1", out var ip).AssertTrue();
		ip.AssertEqual(IPAddress.Parse("192.168.1.1"));

		NetworkHelper.TryParseClientIpHeader("  203.0.113.50  ,  70.41.3.18  ", out ip).AssertTrue();
		ip.AssertEqual(IPAddress.Parse("203.0.113.50"));
	}

	[TestMethod]
	public void TryParseClientIpHeader_ForwardedFormat()
	{
		// RFC 7239 Forwarded header format
		NetworkHelper.TryParseClientIpHeader("for=192.168.1.1", out var ip).AssertTrue();
		ip.AssertEqual(IPAddress.Parse("192.168.1.1"));

		NetworkHelper.TryParseClientIpHeader("for=192.168.1.1;proto=https", out ip).AssertTrue();
		ip.AssertEqual(IPAddress.Parse("192.168.1.1"));

		NetworkHelper.TryParseClientIpHeader("for=192.168.1.1;proto=https;by=10.0.0.1", out ip).AssertTrue();
		ip.AssertEqual(IPAddress.Parse("192.168.1.1"));
	}

	[TestMethod]
	public void TryParseClientIpHeader_QuotedValues()
	{
		NetworkHelper.TryParseClientIpHeader("\"192.168.1.1\"", out var ip).AssertTrue();
		ip.AssertEqual(IPAddress.Parse("192.168.1.1"));

		NetworkHelper.TryParseClientIpHeader("for=\"192.168.1.1\"", out ip).AssertTrue();
		ip.AssertEqual(IPAddress.Parse("192.168.1.1"));
	}

	[TestMethod]
	public void TryParseClientIpHeader_Unknown()
	{
		NetworkHelper.TryParseClientIpHeader("unknown", out _).AssertFalse();
		NetworkHelper.TryParseClientIpHeader("Unknown", out _).AssertFalse();
		NetworkHelper.TryParseClientIpHeader("UNKNOWN", out _).AssertFalse();
	}

	[TestMethod]
	public void TryParseClientIpHeader_EmptyOrNull()
	{
		NetworkHelper.TryParseClientIpHeader(null, out _).AssertFalse();
		NetworkHelper.TryParseClientIpHeader("", out _).AssertFalse();
		NetworkHelper.TryParseClientIpHeader("   ", out _).AssertFalse();
	}

	[TestMethod]
	public void TryParseClientIpHeader_InvalidValues()
	{
		NetworkHelper.TryParseClientIpHeader("not-an-ip", out _).AssertFalse();
		NetworkHelper.TryParseClientIpHeader("999.999.999.999", out _).AssertFalse();
	}

	[TestMethod]
	public void HttpHeaders_ClientIpHeaders_ContainsExpectedHeaders()
	{
		HttpHeaders.ClientIpHeaders.Length.AssertEqual(5);
		HttpHeaders.ClientIpHeaders.Contains(HttpHeaders.CFConnectingIP).AssertTrue();
		HttpHeaders.ClientIpHeaders.Contains(HttpHeaders.TrueClientIP).AssertTrue();
		HttpHeaders.ClientIpHeaders.Contains(HttpHeaders.XRealIP).AssertTrue();
		HttpHeaders.ClientIpHeaders.Contains(HttpHeaders.XForwardedFor).AssertTrue();
		HttpHeaders.ClientIpHeaders.Contains(HttpHeaders.Forwarded).AssertTrue();
	}
}
