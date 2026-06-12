namespace Ecng.Tests.Licensing;

using System.Runtime.InteropServices;

using Ecng.Licensing;

[TestClass]
public class LicenseTests : BaseTestClass
{
	private const string _licPath = "../../../Resources/license_123.xml";

	private static byte[] GetLicenseBody() => File.ReadAllBytes(_licPath);

	[TestMethod]
	public void ParseLicense()
	{
		File.Exists(_licPath).AssertTrue();
		var body = GetLicenseBody();
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
		var body = GetLicenseBody();
		var lic = new License(body);
		var text = body.UTF8();
		text.Contains("<signature>").AssertTrue();
		var noSigText = lic.BodyWithoutSignature.UTF8();
		noSigText.Contains("<signature>").AssertFalse();
	}

	[TestMethod]
	public void ParseLicense_WithUtf8Bom()
	{
		var body = GetLicenseBody();
		var bom = Encoding.UTF8.GetPreamble();
		var bodyWithBom = bom.Concat(body).ToArray();

		var lic = new License(_licPath, bodyWithBom);

		lic.Id.AssertEqual(123L);
		lic.IssuedTo.AssertEqual("some@email.com");
		lic.Features.ContainsKey(OSPlatform.Windows).AssertTrue();
	}

	[TestMethod]
	public void ParseLicense_DuplicatePlatformNamesMergeFeatures()
	{
		var text = GetLicenseBody().UTF8();
		var duplicatePlatform =
"""
    <platform name="Windows">
      <feature name="feature3" expire="20301231 11:32:32" expireAction="PreventWork" hardwareId="SECOND-HW" account="ACC2" />
    </platform>
""";
		var body = text.Replace("  </platforms>", duplicatePlatform + Environment.NewLine + "  </platforms>").UTF8();

		var lic = new License(body);

		var features = lic.Features[OSPlatform.Windows].ToArray();
		features.Select(f => f.Name).AssertEqual(["feature1", "feature2", "feature3"]);
		features.Single(f => f.Name == "feature3").HardwareId.AssertEqual("SECOND-HW");
	}
}
