namespace Ecng.Tests.Net;

using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Text;

using Ecng.Net;

[TestClass]
public class NetworkHelperTests : BaseTestClass
{
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

		Assert.ThrowsExactly<ArgumentNullException>(() => "".GetGravatarToken());
		Assert.ThrowsExactly<ArgumentNullException>(() => "".GetGravatarUrl(10));
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
}