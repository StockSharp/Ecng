namespace Ecng.Tests.Security;

using Ecng.Security;

[TestClass]
public class CryptoHelperTests : BaseTestClass
{
	[TestMethod]
	public void HashesOfEmptyInputAreTheWellKnownOnes()
	{
		// An empty payload is a legitimate thing to hash - an empty file has a hash like any other.
		var empty = Array.Empty<byte>();

		empty.Md5().AssertEqual("D41D8CD98F00B204E9800998ECF8427E");
		empty.Sha256().AssertEqual("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
		empty.Sha512().AssertEqual("CF83E1357EEFB8BDF1542850D66D8007D620E4050B5715DC83F4A921D36CE9CE47D0D13C5D85F2B0FF8318D2877EEC2F63B931BD47417A81A538327AF927DA3E");
	}

	[TestMethod]
	public void HashingNothingAtAllIsStillRefused()
	{
		byte[] missing = null;

		ThrowsExactly<ArgumentNullException>(() => missing.Md5());
		ThrowsExactly<ArgumentNullException>(() => missing.Sha256());
		ThrowsExactly<ArgumentNullException>(() => missing.Sha512());
	}
}
