namespace Ecng.Drawing;

using System;
using System.IO;

/// <summary>Limits on decoded image pixels, animation frames, total animation work and estimated decoder buffers.</summary>
public sealed class ImageProcessingLimits
{
	/// <summary>Default limits: 25 million pixels per image, 512 frames, 100 million animation pixels and 256 MiB of decoder buffers.</summary>
	public static ImageProcessingLimits Default { get; } = new(RasterImage.MaxPixels, Animation.MaxFrames, Animation.MaxPixels);

	/// <summary>Creates explicit limits for one image-processing operation.</summary>
	/// <param name="maxImagePixels">Maximum pixels in one allocated RGBA image, between 1 and 536870911.</param>
	/// <param name="maxAnimationFrames">Maximum number of animation frames, greater than zero.</param>
	/// <param name="maxAnimationPixels">Maximum total canvas pixels decoded across animation frames, greater than zero.</param>
	/// <exception cref="ArgumentOutOfRangeException">A limit is outside its supported range.</exception>
	public ImageProcessingLimits(int maxImagePixels, int maxAnimationFrames, long maxAnimationPixels)
		: this(maxImagePixels, maxAnimationFrames, maxAnimationPixels, 256L * 1024 * 1024)
	{
	}

	/// <summary>Creates explicit pixel, animation and working-memory limits.</summary>
	/// <param name="maxImagePixels">Maximum pixels in an allocated output image.</param>
	/// <param name="maxAnimationFrames">Maximum processed animation frames.</param>
	/// <param name="maxAnimationPixels">Maximum animation canvas pixels across all frames.</param>
	/// <param name="maxWorkingMemoryBytes">Positive maximum estimated decoder working bytes.</param>
	/// <exception cref="ArgumentOutOfRangeException">A limit is outside its supported range.</exception>
	public ImageProcessingLimits(int maxImagePixels, int maxAnimationFrames, long maxAnimationPixels, long maxWorkingMemoryBytes)
	{
		if (maxImagePixels is < 1 or > int.MaxValue / 4) throw new ArgumentOutOfRangeException(nameof(maxImagePixels));
		if (maxAnimationFrames < 1) throw new ArgumentOutOfRangeException(nameof(maxAnimationFrames));
		if (maxAnimationPixels < 1) throw new ArgumentOutOfRangeException(nameof(maxAnimationPixels));
		if (maxWorkingMemoryBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxWorkingMemoryBytes));
		MaxImagePixels = maxImagePixels;
		MaxAnimationFrames = maxAnimationFrames;
		MaxAnimationPixels = maxAnimationPixels;
		MaxWorkingMemoryBytes = maxWorkingMemoryBytes;
	}

	/// <summary>Maximum pixels in one allocated decoded image.</summary>
	public int MaxImagePixels { get; }
	/// <summary>Maximum frames processed from an animation.</summary>
	public int MaxAnimationFrames { get; }
	/// <summary>Maximum total canvas pixels decoded across animation frames.</summary>
	public long MaxAnimationPixels { get; }
	/// <summary>Maximum estimated decoder working bytes; 256 MiB by default.</summary>
	public long MaxWorkingMemoryBytes { get; }

	internal void EnsureWorkingMemory(long bytes)
	{
		if (bytes < 0 || bytes > MaxWorkingMemoryBytes) throw new InvalidDataException($"Image processing requires {bytes} working bytes, exceeding the {MaxWorkingMemoryBytes}-byte budget.");
	}
}
