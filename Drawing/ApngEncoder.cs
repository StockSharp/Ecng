namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;

/// <summary>
/// APNG writer using changed RGBA8 rectangles (blend SOURCE, disposal NONE), preserving every frame and its delay.
/// </summary>
internal static class ApngEncoder
{
	/// <param name="source">The animation.</param>
	/// <param name="colorChunks">The chunks that say what colors the samples are, as <see cref="PngCodec.Encode"/> takes them.</param>
	public static byte[] Encode(Animation source, byte[] colorChunks)
	{
		Animation.EnsureWithinBudget(source.FrameCount, (long)source.Width * source.Height * source.FrameCount, source.Limits);
		if (source.FrameCount == 0)
			throw new InvalidDataException("Cannot export zero frames as APNG.");

		using var png = new MemoryStream();
		png.Write(PngCodec.Signature);

		var header = new byte[13];
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0), source.Width);
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), source.Height);
		header[8] = 8; // bit depth
		header[9] = 6; // RGBA
		PngCodec.WriteChunk(png, "IHDR"u8, header);
		png.Write(colorChunks);

		var animation = new byte[8];
		BinaryPrimitives.WriteInt32BigEndian(animation.AsSpan(0), source.FrameCount);
		BinaryPrimitives.WriteInt32BigEndian(animation.AsSpan(4), source.Plays);
		PngCodec.WriteChunk(png, "acTL"u8, animation);

		var sequence = 0u;
		var index = 0;
		RasterImage previous = null;

		foreach (var frame in source.Frames)
		{
			if (frame.Image.Width != source.Width || frame.Image.Height != source.Height)
				throw new InvalidDataException("APNG frame dimensions must equal canvas.");

			var rectangle = previous is null ? new Rectangle(0, 0, source.Width, source.Height) : ChangedRectangle(previous, frame.Image);
			var image = frame.Image;
			if (rectangle.Width != source.Width || rectangle.Height != source.Height)
			{
				image = new(rectangle.Width, rectangle.Height, source.Limits);
				for (var y = 0; y < rectangle.Height; y++)
					frame.Image.Pixels.AsSpan(((rectangle.Y + y) * source.Width + rectangle.X) * 4, rectangle.Width * 4)
						.CopyTo(image.Pixels.AsSpan(y * rectangle.Width * 4));
			}
			// SOURCE replacement clears changed transparent pixels without disposing unchanged regions.
			var control = new byte[26];
			BinaryPrimitives.WriteUInt32BigEndian(control.AsSpan(0), sequence++);
			BinaryPrimitives.WriteInt32BigEndian(control.AsSpan(4), rectangle.Width);
			BinaryPrimitives.WriteInt32BigEndian(control.AsSpan(8), rectangle.Height);
			BinaryPrimitives.WriteInt32BigEndian(control.AsSpan(12), rectangle.X);
			BinaryPrimitives.WriteInt32BigEndian(control.AsSpan(16), rectangle.Y);
			BinaryPrimitives.WriteUInt16BigEndian(control.AsSpan(20), (ushort)Math.Min(ushort.MaxValue, frame.DelayNumerator));
			BinaryPrimitives.WriteUInt16BigEndian(control.AsSpan(22), (ushort)Math.Min(ushort.MaxValue, frame.DelayDenominator));
			PngCodec.WriteChunk(png, "fcTL"u8, control);

			var data = PngCodec.Compress(image);

			// The first frame is the picture a reader without animation shows.
			if (index == 0)
				PngCodec.WriteChunk(png, "IDAT"u8, data);
			else
			{
				var payload = new byte[4 + data.Length];
				BinaryPrimitives.WriteUInt32BigEndian(payload, sequence++);
				data.CopyTo(payload, 4);
				PngCodec.WriteChunk(png, "fdAT"u8, payload);
			}

			index++;
			previous = frame.Image;
		}

		if (index != source.FrameCount)
			throw new InvalidDataException("APNG frame count differs from the one announced.");

		PngCodec.WriteChunk(png, "IEND"u8, []);
		return png.ToArray();
	}

	private static Rectangle ChangedRectangle(RasterImage previous, RasterImage current)
	{
		var before = MemoryMarshal.Cast<byte, uint>(previous.Pixels);
		var after = MemoryMarshal.Cast<byte, uint>(current.Pixels);
		var left = current.Width;
		var top = current.Height;
		var right = 0;
		var bottom = 0;
		for (var y = 0; y < current.Height; y++)
			for (var x = 0; x < current.Width; x++)
				if (before[y * current.Width + x] != after[y * current.Width + x])
				{
					left = Math.Min(left, x);
					top = Math.Min(top, y);
					right = Math.Max(right, x + 1);
					bottom = Math.Max(bottom, y + 1);
				}
		return right == 0 ? new Rectangle(0, 0, 1, 1) : new Rectangle(left, top, right - left, bottom - top);
	}
}
