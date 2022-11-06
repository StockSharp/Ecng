namespace Ecng.Drawing
{
	using System;
	using System.Drawing;

	using Ecng.Common;

	public abstract class Brush
	{
		protected Brush()
		{
		}
	}

	public class SolidBrush : Brush
	{
		public SolidBrush(Color color)
		{
			Color = color;
		}

		public Color Color { get; }
	}

	public class LinearGradientBrush : Brush
	{
		public LinearGradientBrush(Point stop0, Point stop1, Color color0, Color color1)
			: this(new[] { color0, color1 }, new(stop0, new((stop1.X - stop0.X).Abs(), (stop1.Y - stop0.Y).Abs())))
		{
		}

		public LinearGradientBrush(Color[] linearColors, Rectangle rectangle)
		{
			LinearColors = linearColors ?? throw new ArgumentNullException(nameof(linearColors));
			Rectangle = rectangle;
		}

		public Color[] LinearColors { get; }

		public Rectangle Rectangle { get; }
	}
}