namespace Ecng.Drawing;

using System;
using System.Drawing;

using Ecng.Common;

public abstract class Brush
{
	protected Brush()
	{
	}
}

public class SolidBrush(Color color) : Brush
{
	public Color Color { get; } = color;
}

public class LinearGradientBrush(Color[] linearColors, Rectangle rectangle) : Brush
{
	public LinearGradientBrush(Point stop0, Point stop1, Color color0, Color color1)
		: this([color0, color1], new(stop0, new((stop1.X - stop0.X).Abs(), (stop1.Y - stop0.Y).Abs())))
	{
	}

	public Color[] LinearColors { get; } = linearColors ?? throw new ArgumentNullException(nameof(linearColors));

	public Rectangle Rectangle { get; } = rectangle;
}
