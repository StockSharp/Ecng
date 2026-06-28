namespace Ecng.Tests.Net;

using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Net;

[TestClass]
public class PwnedPasswordsClientTests : BaseTestClass
{
	private sealed class StubHandler(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			=> Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
	}

	private static string Suffix(string password)
	{
		var hex = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password))).ToUpperInvariant();
		return hex[5..];
	}

	[TestMethod]
	public async Task Breached_WhenSuffixPresent()
	{
		const string pwd = "password123";
		// The range API returns "SUFFIX:count" lines; include ours with a non-zero count.
		var body = "0000000000000000000000000000000000000:1\r\n" + Suffix(pwd) + ":42\r\n";

		var client = new PwnedPasswordsClient(new HttpClient(new StubHandler(body)));

		(await client.IsBreachedAsync(pwd, default)).AssertTrue();
	}

	[TestMethod]
	public async Task NotBreached_WhenSuffixAbsent()
	{
		var client = new PwnedPasswordsClient(new HttpClient(new StubHandler("ABCDEF0000000000000000000000000000000:9\r\n")));

		(await client.IsBreachedAsync("some-unique-passphrase", default)).AssertFalse();
	}

	[TestMethod]
	public async Task FailsOpen_OnHttpError()
	{
		var client = new PwnedPasswordsClient(new HttpClient(new StubHandler(string.Empty, HttpStatusCode.InternalServerError)));

		(await client.IsBreachedAsync("whatever", default)).AssertFalse();
	}
}
