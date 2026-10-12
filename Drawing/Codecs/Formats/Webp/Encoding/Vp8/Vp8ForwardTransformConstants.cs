namespace Ecng.Drawing.Codecs.Formats.Webp.Encoding.Vp8;

using System;
using System.IO;

internal static class Vp8ForwardTransformConstants
{
	public const int Sin = 2217;

	public const int Cos = 5352;

	public const int FirstPassBiasOdd1 = 1812;

	public const int FirstPassBiasOdd3 = 937;

	public const int SecondPassBiasOdd1 = 12000;

	public const int SecondPassBiasOdd3 = 51000;
}
