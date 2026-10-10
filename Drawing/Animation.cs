namespace Ecng.Drawing;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// An animation: its canvas, how many times it plays and its frames. The frames are decoded one at a
/// time, as they are asked for, so that a long animation never has all its canvases in memory at once.
/// </summary>
internal sealed class Animation
{
	/// <summary>
	/// The most frames an animation may have to be decoded.
	/// </summary>
	public const int MaxFrames = 512;

	/// <summary>
	/// The most pixels an animation may take to decode, all frames together: every frame costs its
	/// canvas, whatever part of it the frame itself covers.
	/// </summary>
	public const long MaxPixels = 100_000_000;

	public Animation(int width, int height, int plays, int frameCount, IEnumerable<AnimationFrame> frames)
	{
		Width = width;
		Height = height;
		Plays = plays;
		FrameCount = frameCount;
		Frames = frames ?? throw new ArgumentNullException(nameof(frames));
	}

	public int Width { get; }
	public int Height { get; }

	/// <summary>
	/// How many times the animation plays; 0 - endlessly.
	/// </summary>
	public int Plays { get; }

	public int FrameCount { get; }

	/// <summary>
	/// The frames in order. Enumerating decodes them; each comes as a picture of its own that the caller may change.
	/// </summary>
	public IEnumerable<AnimationFrame> Frames { get; }

	/// <summary>
	/// The same animation with every frame replaced by what <paramref name="change"/> makes of it.
	/// </summary>
	public Animation Select(int width, int height, Func<RasterImage, RasterImage> change)
		=> new(width, height, Plays, FrameCount, Frames.Select(frame => frame.WithImage(change(frame.Image))));

	/// <summary>
	/// Refuses an animation that takes more work than the decoders do for one file.
	/// </summary>
	/// <param name="frames">Number of frames.</param>
	/// <param name="pixels">Pixels to decode and composite, all frames together.</param>
	/// <exception cref="InvalidDataException">The animation is beyond <see cref="MaxFrames"/> or <see cref="MaxPixels"/>.</exception>
	public static void EnsureWithinBudget(int frames, long pixels)
	{
		if (frames > MaxFrames)
			throw new InvalidDataException($"The animation has {frames} frames: more than the {MaxFrames} that are decoded.");

		if (pixels > MaxPixels)
			throw new InvalidDataException($"The animation takes {pixels} pixels to decode: more than the {MaxPixels} that are decoded.");
	}
}
