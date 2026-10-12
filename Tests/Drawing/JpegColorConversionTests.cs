namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

[TestClass]
public class JpegColorConversionTests : BaseTestClass
{
	[TestMethod]
	public void EveryYcbcrByteCombinationMatchesTheScalarFormula()
	{
		var components = new[]
		{
			new JpegComponent { H = 1, V = 1, BlocksWide = 32, Samples = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray() },
			new JpegComponent { H = 1, V = 1, BlocksWide = 32, Samples = new byte[256] },
			new JpegComponent { H = 1, V = 1, BlocksWide = 32, Samples = new byte[256] },
		};
		var converter = new JpegColorConverter(256, components, JpegColorModels.YCbCr);
		var output = new byte[256 * 4];
		for (var cr = 0; cr < 256; cr++)
		{
			components[2].Samples.AsSpan().Fill((byte)cr);
			for (var cb = 0; cb < 256; cb++)
			{
				components[1].Samples.AsSpan().Fill((byte)cb);
				converter.WriteRow(0, output);
				for (var y = 0; y < 256; y++)
				{
					var red = (byte)Math.Clamp((int)Math.Round(y + 1.402 * (cr - 128)), 0, 255);
					var green = (byte)Math.Clamp((int)Math.Round(y - 0.344136 * (cb - 128) - 0.714136 * (cr - 128)), 0, 255);
					var blue = (byte)Math.Clamp((int)Math.Round(y + 1.772 * (cb - 128)), 0, 255);
					var at = y * 4;
					if (output[at] != red || output[at + 1] != green || output[at + 2] != blue || output[at + 3] != 255)
						Fail($"YCbCr ({y},{cb},{cr}): expected ({red},{green},{blue},255), actual ({output[at]},{output[at + 1]},{output[at + 2]},{output[at + 3]}).");
				}
			}
		}
	}
}
