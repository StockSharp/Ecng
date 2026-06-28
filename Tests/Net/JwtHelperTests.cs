namespace Ecng.Tests.Net;

using System;

using Ecng.Common;
using Ecng.Net;

[TestClass]
public class JwtHelperTests : BaseTestClass
{
	private static string TokenWithExp(DateTimeOffset exp)
	{
		var payload = ("{\"exp\":" + exp.ToUnixTimeSeconds() + "}").UTF8().Base64Url();
		return "header." + payload + ".signature";
	}

	[TestMethod]
	public void GetExpiry_ReadsExp()
	{
		var exp = DateTimeOffset.UtcNow.AddHours(1);

		var got = JwtHelper.GetExpiry(TokenWithExp(exp));

		got.AssertNotNull();
		(Math.Abs((got.Value - exp.UtcDateTime).TotalSeconds) < 2).AssertTrue();
	}

	[TestMethod]
	public void IsExpiredOrExpiring_Logic()
	{
		var soon = TokenWithExp(DateTimeOffset.UtcNow.AddSeconds(30));

		JwtHelper.IsExpiredOrExpiring(soon, TimeSpan.FromMinutes(1)).AssertTrue();  // 30s away, 1min skew
		JwtHelper.IsExpiredOrExpiring(soon, TimeSpan.FromSeconds(5)).AssertFalse(); // 30s away, 5s skew
	}

	[TestMethod]
	public void Unreadable_ReturnsNullOrFalse()
	{
		JwtHelper.GetExpiry("not-a-jwt").AssertNull();
		JwtHelper.GetExpiry(null).AssertNull();
		JwtHelper.IsExpiredOrExpiring("garbage", TimeSpan.FromMinutes(1)).AssertFalse();
	}
}
