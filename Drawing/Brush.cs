namespace Ecng.Drawing;

using System;
using System.Drawing;

using Ecng.Common;

/// <summary>
/// Represents a base class for drawing brushes.
/// </summary>
public abstract class Brush
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Brush"/> class.
	/// </summary>
	protected Brush()
	{
	}
}

/// <summary>
/// Represents a brush that paints a solid color.
/// </summary>
/// <param name="color">The solid color to use for painting.</param>
public class SolidBrush(Color color) : Brush
{
	/// <summary>
	/// Gets the solid <see cref="Color"/> used by the brush.
	/// </summary>
	public Color Color { get; } = color;
}

/// <summary>
/// Represents a brush that paints a gradient between multiple colors.
/// </summary>
/// <param name="linearColors">An array of colors defining the gradient stops.</param>
/// <param name="rectangle">The rectangle that defines the bounds of the gradient.</param>
public class LinearGradientBrush(Color[] linearColors, Rectangle rectangle) : Brush
{
	/// <summary>
	/// Initializes a new instance of the <see cref="LinearGradientBrush"/> class using two points and two colors.
	/// </summary>
	/// <param name="stop0">The first point of the gradient.</param>
	/// <param name="stop1">The second point of the gradient.</param>
	/// <param name="color0">The color at the first point.</param>
	/// <param name="color1">The color at the second point.</param>
	public LinearGradientBrush(Point stop0, Point stop1, Color color0, Color color1)
		// Anchor the rectangle at the top-left of the two stops, not at stop0: when stop1 is left
		// of or above stop0 the old rectangle was offset into the wrong quadrant and didn't even
		// contain stop1.
		: this([color0, color1], new(
			new(stop0.X.Min(stop1.X), stop0.Y.Min(stop1.Y)),
			new((stop1.X - stop0.X).Abs(), (stop1.Y - stop0.Y).Abs())))
	{
	}

	/// <summary>
	/// Gets the array of colors defining the gradient stops.
	/// </summary>
	public Color[] LinearColors { get; } = linearColors ?? throw new ArgumentNullException(nameof(linearColors));

	/// <summary>
	/// Gets the rectangle that defines the bounds of the gradient.
	/// </summary>
	public Rectangle Rectangle { get; } = rectangle;
}
