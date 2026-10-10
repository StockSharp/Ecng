namespace Ecng.Drawing;

using System;

/// <summary>
/// One frame of an animation: the whole canvas as it looks while the frame is shown, and for how long.
/// </summary>
internal sealed class AnimationFrame
{
	/// <param name="image">The composited canvas.</param>
	/// <param name="delayNumerator">Numerator of the time the frame is shown for, in seconds.</param>
	/// <param name="delayDenominator">Denominator of that time; greater than zero.</param>
	public AnimationFrame(RasterImage image, int delayNumerator, int delayDenominator)
	{
		Image = image ?? throw new ArgumentNullException(nameof(image));

		if (delayNumerator < 0)
			throw new ArgumentOutOfRangeException(nameof(delayNumerator));

		if (delayDenominator <= 0)
			throw new ArgumentOutOfRangeException(nameof(delayDenominator));

		DelayNumerator = delayNumerator;
		DelayDenominator = delayDenominator;
	}

	public RasterImage Image { get; }

	/// <summary>
	/// The frame is shown for <see cref="DelayNumerator"/> / <see cref="DelayDenominator"/> of a second:
	/// kept as the fraction the file states, so that writing it back changes nothing.
	/// </summary>
	public int DelayNumerator { get; }

	/// <inheritdoc cref="DelayNumerator"/>
	public int DelayDenominator { get; }

	public int DelayMilliseconds
		=> (int)Math.Min(int.MaxValue, Math.Round(DelayNumerator * 1000.0 / DelayDenominator, MidpointRounding.AwayFromZero));

	/// <summary>
	/// The same timing over another picture.
	/// </summary>
	public AnimationFrame WithImage(RasterImage image)
		=> new(image, DelayNumerator, DelayDenominator);
}
