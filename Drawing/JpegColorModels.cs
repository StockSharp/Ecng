namespace Ecng.Drawing;

/// <summary>
/// What the components of a JPEG frame stand for.
/// </summary>
internal enum JpegColorModels
{
	/// <summary>
	/// One component: luminance.
	/// </summary>
	Gray,

	/// <summary>
	/// Luminance and two color differences.
	/// </summary>
	YCbCr,

	/// <summary>
	/// Red, green and blue as they are.
	/// </summary>
	Rgb,

	/// <summary>
	/// Cyan, magenta, yellow and black, stored inverted.
	/// </summary>
	Cmyk,

	/// <summary>
	/// <see cref="Cmyk"/> whose first three components went through the <see cref="YCbCr"/> transform.
	/// </summary>
	Ycck,
}
