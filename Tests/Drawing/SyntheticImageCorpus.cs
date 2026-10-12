namespace Ecng.Tests.Drawing;

using static ImageTestHelper;

internal static class SyntheticImageCorpus
{
	public static readonly string[] Patterns = ["bands", "gradient", "lines", "texture", "noise", "alpha"];

	public static byte[] Pixels(string pattern, int width, int height)
		=> Rgba(width, height, (x, y) =>
		{
			var fx = x / (double)Math.Max(1, width - 1);
			var fy = y / (double)Math.Max(1, height - 1);
			return pattern switch
			{
				"bands" => (x * 4 / width) switch { 0 => (240, 32, 16, 255), 1 => (24, 210, 72, 255), 2 => (16, 48, 240, 255), _ => ((byte)240, (byte)240, (byte)240, (byte)255) },
				"gradient" => (Byte(fx * 255), Byte(fy * 255), Byte((1 - fx) * fy * 255), (byte)255),
				"lines" => x % 31 == 0 || y % 23 == 0 ? ((byte)16, (byte)24, (byte)32, (byte)255) :
					x % 31 is >= 5 and <= 24 && y % 23 is >= 7 and <= 15 && (x + y) % 4 == 0 ? ((byte)32, (byte)80, (byte)192, (byte)255) : ((byte)245, (byte)246, (byte)248, (byte)255),
				"texture" => (Byte(115 + 65 * Math.Sin(x * 0.037) + 35 * Math.Cos(y * 0.023)),
					Byte(125 + 55 * Math.Sin((x + y) * 0.017) + Noise(x / 8, y / 8, 1) / 12.0),
					Byte(105 + 60 * Math.Cos(y * 0.041) + Noise(x / 3, y / 3, 2) / 10.0), (byte)255),
				"noise" => (Noise(x, y, 0), Noise(x, y, 1), Noise(x, y, 2), (byte)255),
				"alpha" => (Byte(fx * 255), Byte((1 - fy) * 255), (byte)190, Byte(Math.Clamp(1 - Math.Sqrt((fx - 0.5) * (fx - 0.5) + (fy - 0.5) * (fy - 0.5)) * 1.7, 0, 1) * 255)),
				_ => throw new ArgumentOutOfRangeException(nameof(pattern)),
			};
		});

	private static byte Byte(double value) => (byte)Math.Clamp((int)Math.Round(value), 0, 255);

	private static byte Noise(int x, int y, int channel)
	{
		var value = unchecked((uint)x * 0x9e3779b1u ^ (uint)y * 0x85ebca6bu ^ (uint)(channel + 1) * 0xc2b2ae35u);
		value ^= value >> 16;
		value *= 0x7feb352du;
		value ^= value >> 15;
		return (byte)value;
	}
}
