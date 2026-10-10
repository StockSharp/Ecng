namespace Ecng.Drawing;

/// <summary>
/// A line of text rendered for a canvas: how opaque the text is at every pixel of the box it takes.
/// </summary>
internal sealed class TextMask
{
	private readonly int _left, _top, _width;
	private readonly byte[] _opacity;

	/// <param name="left">The column of the canvas the box starts at.</param>
	/// <param name="top">The row of the canvas the box starts at.</param>
	/// <param name="width">Width of the box in pixels.</param>
	/// <param name="opacity">Opacity of the text, 0..255, for every pixel of the box row by row.</param>
	public TextMask(int left, int top, int width, byte[] opacity)
	{
		_left = left;
		_top = top;
		_width = width;
		_opacity = opacity;
	}

	/// <summary>
	/// Draws the line in white onto a picture of the size it was rendered for.
	/// </summary>
	public void Draw(RasterImage image)
	{
		for (int at = 0, y = _top; at < _opacity.Length; y++)
			for (var x = _left; x < _left + _width; x++, at++)
			{
				if (_opacity[at] != 0)
					image.BlendPixel(x, y, 255, 255, 255, _opacity[at]);
			}
	}
}
