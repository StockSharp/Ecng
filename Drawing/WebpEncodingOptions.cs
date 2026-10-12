namespace Ecng.Drawing;

using System;

/// <summary>Explicit compression mode and quality for WebP output.</summary>
public sealed class WebpEncodingOptions
{
	/// <summary>Default lossless writing.</summary>
	public static WebpEncodingOptions Lossless { get; } = new(WebpCompressionModes.Lossless, 100);

	/// <summary>Creates WebP output settings.</summary>
	/// <param name="mode">Lossless VP8L or lossy VP8.</param>
	/// <param name="quality">VP8 quality, 0 through 100. VP8L always retains exact pixels.</param>
	/// <exception cref="ArgumentOutOfRangeException">Mode or quality is outside its supported range.</exception>
	public WebpEncodingOptions(WebpCompressionModes mode, int quality)
	{
		if (mode is not (WebpCompressionModes.Lossless or WebpCompressionModes.Lossy)) throw new ArgumentOutOfRangeException(nameof(mode));
		if (quality is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(quality));
		Mode = mode;
		Quality = quality;
	}

	/// <summary>Compression mode.</summary>
	public WebpCompressionModes Mode { get; }
	/// <summary>VP8 color quality, 0 through 100.</summary>
	public int Quality { get; }
}
