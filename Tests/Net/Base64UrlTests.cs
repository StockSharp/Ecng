namespace Ecng.Tests.Net;

using System.Linq;

using Ecng.Common;
using Ecng.Net;

[TestClass]
public class Base64UrlTests : BaseTestClass
{
	[TestMethod]
	public void Roundtrip()
	{
		var bytes = new byte[] { 0, 1, 2, 3, 250, 251, 252, 253, 254, 255 };

		var encoded = bytes.Base64Url();

		encoded.Contains('+').AssertFalse();
		encoded.Contains('/').AssertFalse();
		encoded.Contains('=').AssertFalse();

		encoded.Base64Url().SequenceEqual(bytes).AssertTrue();
	}

	[TestMethod]
	public void DecodesUnpadded()
	{
		// "{}" -> standard base64 "e30=" -> base64url "e30" (no padding).
		"e30".Base64Url().UTF8().AssertEqual("{}");
	}
}
