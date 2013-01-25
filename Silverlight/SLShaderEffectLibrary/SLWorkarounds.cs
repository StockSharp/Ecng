using System;
using System.Windows.Media;

public static class Extensions
{
	public static Color FromScRgb(this Color c, double a, double r, double g, double b)
	{
		return Color.FromArgb((byte)Math.Ceiling(a * 255),
			(byte)Math.Ceiling(r * 255),
			(byte)Math.Ceiling(g * 255),
			(byte)Math.Ceiling(b * 255));
	}
}