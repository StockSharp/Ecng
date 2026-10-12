namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

[TestClass]
public class JpegRasterSampleTests : BaseTestClass
{
	[TestMethod]
	public void RoundingAndClipping_MatchScalarValuesOnBothSidesOfEveryHalf()
	{
		var samples = new float[64];
		var pixels = new byte[24 * 16];
		for (var center = -256; center <= 384; center++)
		{
			for (var i = 0; i < samples.Length; i++) samples[i] = center + (i % 4 switch { 0 => -0.5f, 1 => -0.49999997f, 2 => 0.49999997f, _ => 0.5f });
			Array.Fill(pixels, (byte)23);
			JpegRasterSamples.Write(samples, pixels, 24, 3, 4);
			for (var y = 0; y < 16; y++)
				for (var x = 0; x < 24; x++)
				{
					var expected = y is >= 4 and < 12 && x is >= 3 and < 11
						? (byte)Math.Clamp((int)Math.Round((double)(samples[(y - 4) * 8 + x - 3] + 128)), 0, 255) : (byte)23;
					AreEqual(expected, pixels[y * 24 + x]);
				}
		}
	}
}
