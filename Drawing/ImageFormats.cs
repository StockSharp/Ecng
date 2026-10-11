namespace Ecng.Drawing;

/// <summary>
/// Image formats identified from their file signatures.
/// </summary>
public enum ImageFormats
{
	/// <summary>Unrecognized image data.</summary>
	Unknown,
	/// <summary>PNG, including animated PNG.</summary>
	Png,
	/// <summary>JPEG.</summary>
	Jpeg,
	/// <summary>Windows or OS/2 bitmap.</summary>
	Bmp,
	/// <summary>GIF.</summary>
	Gif,
	/// <summary>WebP.</summary>
	Webp,
	/// <summary>Windows icon.</summary>
	Icon,
	/// <summary>TIFF.</summary>
	Tiff,
	/// <summary>AV1 image file.</summary>
	Avif,
}
