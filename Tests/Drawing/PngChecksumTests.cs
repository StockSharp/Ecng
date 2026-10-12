namespace Ecng.Tests.Drawing;

using System.Text;

using Ecng.Drawing;

[TestClass]
public class PngChecksumTests : BaseTestClass
{
	[TestMethod]
	public void Checksum_MatchesTheIeeeKnownAnswer()
		=> AreEqual(0xcbf43926u, PngCodec.Crc(Encoding.ASCII.GetBytes("123456789")));

	[TestMethod]
	public void Checksum_MatchesBitwisePolynomialAcrossLengthsAndOffsets()
	{
		var bytes = new byte[2056];
		var random = new Random(23);
		random.NextBytes(bytes);
		for (var offset = 0; offset < 8; offset++)
			for (var length = 0; length <= 2048; length++)
			{
				var expected = 0xffffffffu;
				for (var i = 0; i < length; i++)
				{
					expected ^= bytes[offset + i];
					for (var bit = 0; bit < 8; bit++) expected = expected >> 1 ^ ((expected & 1) == 0 ? 0u : 0xedb88320u);
				}
				AreEqual(~expected, PngCodec.Crc(bytes.AsSpan(offset, length)));
			}
	}
}
