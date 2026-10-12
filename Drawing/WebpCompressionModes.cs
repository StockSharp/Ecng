namespace Ecng.Drawing;

/// <summary>WebP bitstream compression modes.</summary>
public enum WebpCompressionModes
{
	/// <summary>VP8L retains every RGBA byte.</summary>
	Lossless,
	/// <summary>VP8 uses quality-controlled color compression and retains alpha exactly.</summary>
	Lossy,
}
