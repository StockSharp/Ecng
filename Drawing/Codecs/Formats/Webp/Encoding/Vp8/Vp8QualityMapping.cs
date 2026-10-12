namespace Ecng.Drawing.Codecs.Formats.Webp.Encoding.Vp8;

using System;
using System.IO;

internal static class Vp8QualityMapping
{
	public static int QualityToBaseQIndex(int quality)
	{
		int clampedQuality = Math.Clamp(quality, 0, 100);
		int qIndex = 127 - (int)Math.Round(clampedQuality * 127 / 100.0);
		return Math.Clamp(qIndex, 0, 127);
	}

	public static int QualityToFilterLevel(int quality)
	{
		int clampedQuality = Math.Clamp(quality, 0, 100);
		int level = (int)Math.Round((100 - clampedQuality) * 0.5);
		return Math.Clamp(level, 0, 63);
	}
}
