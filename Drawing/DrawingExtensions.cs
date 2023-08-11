namespace Ecng.Drawing;

using System.Drawing;

public static class DrawingExtensions
{
	public static Color ToColor(this int argb)
		=> Color.FromArgb(argb);
}