namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

using System;

using Ecng.Drawing.Codecs.Formats.Webp.Internal;

internal static class Vp8LPixelDecoder
{
	public static uint[] DecodeImageStream(Vp8LBitReader reader, int width, int height, bool allowRecursion, ImageProcessingLimits limits, bool rentFromPool = false)
	{
		limits.EnsureWorkingMemory((long)width * height * 8);
		var stream = new Vp8LPixelStream(reader, width, height, allowRecursion, limits);
		limits.EnsureWorkingMemory((long)stream.PixelCount * 8 + stream.WorkingBytes);
		var cache = stream.CacheBits == 0 ? null : new Vp8LColorCache(stream.CacheBits);
		var pixels = rentFromPool ? WebpBufferPool.SharedUInt32.Rent(stream.PixelCount) : new uint[stream.PixelCount];
		var position = 0;
		while (stream.TryRead(position, out var command))
		{
			if (command.Distance > 0)
			{
				Vp8LBackwardCopier.CopyPixels(pixels, position, command.Distance, command.Length);
				if (cache is not null)
					for (var i = 0; i < command.Length; i++) cache.Insert(pixels[position + i]);
			}
			else
			{
				var pixel = command.CacheIndex >= 0 ? cache.Lookup(command.CacheIndex) : command.Color;
				pixels[position] = pixel;
				cache?.Insert(pixel);
			}
			position += command.Length;
		}
		// A truncated stream must never expose pixels left in a pooled buffer by another image.
		pixels.AsSpan(position, stream.PixelCount - position).Clear();
		return pixels;
	}
}
