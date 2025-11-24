namespace Ecng.Tests.Licensing;

using System.Runtime.InteropServices;
using System.Globalization;

using Ecng.Licensing;

[TestClass]
public class LicenseTests : BaseTestClass
{
	private const string _licPath = "../../../Resources/license_123.xml";

	[TestMethod]
	public void ParseLicense()
	{
		File.Exists(_licPath).AssertTrue();
		var body = File.ReadAllBytes(_licPath);
		var lic = new License(_licPath, body);

		lic.Version.AssertEqual(new Version(2,0));
		lic.Id.AssertEqual(123L);
		lic.IssuedTo.AssertEqual("some@email.com");
		lic.Features.Count.AssertEqual(1);
		var winPlatform = OSPlatform.Windows;
		lic.Features.ContainsKey(winPlatform).AssertTrue();
		var feats = lic.Features[winPlatform].ToArray();
		feats.Length.AssertEqual(2);
		feats[0].Name.AssertEqual("feature1");
		feats[1].Name.AssertEqual("feature2");
		feats.All(f => f.HardwareId == "BFEBFBFF000306D4C07529701BLG926AF").AssertTrue();

		var expected1 = new DateTime(638683653467270000, DateTimeKind.Utc);
		var expected2 = DateTime.ParseExact("20301231 11:32:32", "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture).UtcKind();
		feats[0].ExpirationDate.AssertEqual(expected1);
		feats[1].ExpirationDate.AssertEqual(expected2);
	}

	[TestMethod]
	public void BodyWithoutSignature()
	{
		var body = File.ReadAllBytes(_licPath);
		var lic = new License(body);
		var text = body.UTF8();
		text.Contains("<signature>").AssertTrue();
		var noSigText = lic.BodyWithoutSignature.UTF8();
		noSigText.Contains("<signature>").AssertFalse();
	}
}