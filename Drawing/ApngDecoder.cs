namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Decodes APNG animation without discarding frames. PNG scanline decompression
/// and CRC validation are performed by the existing managed PNG decoder.
/// Supports full PNG color modes, frame subrectangles, SOURCE/OVER blending,
/// NONE/BACKGROUND/PREVIOUS disposal, arbitrary delays and loop counts.
/// </summary>
internal static class ApngDecoder
{
	private const int _maxInputLength = 128 * 1024 * 1024;

	private const int _disposeBackground = 1;
	private const int _disposePrevious = 2;
	private const int _blendSource = 0;

	// A frame as the file holds it: its fcTL and the compressed data of the chunks that follow.
	private sealed class RawFrame
	{
		public int X;
		public int Y;
		public int Width;
		public int Height;
		public int DelayNumerator;
		public int DelayDenominator;
		public int Disposal;
		public int Blend;

		public MemoryStream Compressed { get; } = new();
	}

	public static bool IsApng(ReadOnlySpan<byte> data)
	{
		if (!PngCodec.IsPng(data))
			return false;

		for (var position = 8; position + 12 <= data.Length;)
		{
			var length = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(position, 4));

			if (length > (uint)(data.Length - position - 12))
				return false;

			var tag = data.Slice(position + 4, 4);

			if (tag.SequenceEqual("acTL"u8))
				return true;

			// The animation control chunk comes before the image data or not at all.
			if (tag.SequenceEqual("IDAT"u8) || tag.SequenceEqual("IEND"u8))
				return false;

			position += (int)length + 12;
		}

		return false;
	}

	/// <summary>
	/// Reads how many times the animation plays, 0 for endlessly, off its acTL chunk.
	/// </summary>
	public static int ReadPlays(ReadOnlySpan<byte> data)
	{
		if (!PngCodec.IsPng(data))
			throw new InvalidDataException("Invalid APNG input.");

		for (var position = 8; position + 12 <= data.Length;)
		{
			var length = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(position, 4));

			if (length > (uint)(data.Length - position - 12))
				break;

			if (data.Slice(position + 4, 4).SequenceEqual("acTL"u8))
			{
				if (length != 8)
					break;

				var plays = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(position + 12, 4));

				if (plays > int.MaxValue)
					break;

				return (int)plays;
			}

			position += (int)length + 12;
		}

		throw new InvalidDataException("Invalid APNG animation control.");
	}

	/// <summary>
	/// Reads the chunks of the file - everything but the pixels - and prepares its frames for decoding.
	/// </summary>
	public static Animation Open(byte[] data)
	{
		if (!PngCodec.IsPng(data) || data.Length > _maxInputLength)
			throw new InvalidDataException("Invalid APNG input.");

		var bytes = data.AsSpan();
		var position = 8;
		var header = Array.Empty<byte>();
		var palette = Array.Empty<byte>();
		var transparency = Array.Empty<byte>();
		var width = 0;
		var height = 0;
		var expectedCount = 0;
		var plays = 1;
		var expectedSequence = 0u;
		var seenIdat = false;
		var seenActl = false;
		var ended = false;
		var frames = new List<RawFrame>();
		RawFrame active = null;

		while (position + 12 <= data.Length)
		{
			var size = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(position, 4));

			if (size > (uint)(data.Length - position - 12))
				throw new InvalidDataException("APNG chunk length exceeds file.");

			var chunk = bytes.Slice(position + 4, 4);
			var content = bytes.Slice(position + 8, (int)size);
			var crc = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(position + 8 + (int)size, 4));

			if (PngCodec.Crc(bytes.Slice(position + 4, (int)size + 4)) != crc)
				throw new InvalidDataException("APNG chunk CRC mismatch.");

			if (chunk.SequenceEqual("IHDR"u8))
			{
				if (header.Length > 0 || position != 8 || content.Length != 13)
					throw new InvalidDataException("Invalid APNG IHDR.");

				header = content.ToArray();
				width = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(0, 4));
				height = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(4, 4));

				if (width < 1 || height < 1)
					throw new InvalidDataException("APNG has no size.");

				RasterImage.EnsureDecodable(width, height);
			}
			else if (chunk.SequenceEqual("PLTE"u8))
				palette = content.ToArray();
			else if (chunk.SequenceEqual("tRNS"u8))
				transparency = content.ToArray();
			else if (chunk.SequenceEqual("acTL"u8))
			{
				if (seenActl || seenIdat || header.Length == 0 || content.Length != 8)
					throw new InvalidDataException("Invalid APNG animation control.");

				seenActl = true;
				var count = BinaryPrimitives.ReadUInt32BigEndian(content[..4]);
				var loops = BinaryPrimitives.ReadUInt32BigEndian(content[4..]);

				if (count < 1 || count > int.MaxValue || loops > int.MaxValue)
					throw new InvalidDataException("Invalid APNG frame or loop count.");

				// Every frame is composited onto the whole canvas, whatever rectangle it covers itself.
				Animation.EnsureWithinBudget((int)count, (long)count * width * height);

				expectedCount = (int)count;
				plays = (int)loops;
			}
			else if (chunk.SequenceEqual("fcTL"u8))
			{
				if (!seenActl || content.Length != 26 || frames.Count == expectedCount)
					throw new InvalidDataException("Invalid APNG frame control.");

				var sequence = BinaryPrimitives.ReadUInt32BigEndian(content[..4]);

				if (sequence != expectedSequence++)
					throw new InvalidDataException("APNG frame sequence out of order.");

				var w = BinaryPrimitives.ReadUInt32BigEndian(content.Slice(4, 4));
				var h = BinaryPrimitives.ReadUInt32BigEndian(content.Slice(8, 4));
				var x = BinaryPrimitives.ReadUInt32BigEndian(content.Slice(12, 4));
				var y = BinaryPrimitives.ReadUInt32BigEndian(content.Slice(16, 4));

				if (w == 0 || h == 0 || w > width || h > height || x > width - w || y > height - h)
					throw new InvalidDataException("APNG frame rectangle exceeds canvas.");

				var numerator = BinaryPrimitives.ReadUInt16BigEndian(content.Slice(20, 2));
				var denominator = BinaryPrimitives.ReadUInt16BigEndian(content.Slice(22, 2));
				var disposal = content[24];
				var blend = content[25];

				if (disposal > 2 || blend > 1)
					throw new InvalidDataException("Unsupported APNG disposal/blend operation.");

				active = new()
				{
					Width = (int)w,
					Height = (int)h,
					X = (int)x,
					Y = (int)y,
					DelayNumerator = numerator,
					// A zero denominator stands for hundredths of a second.
					DelayDenominator = denominator == 0 ? 100 : denominator,
					Disposal = disposal,
					Blend = blend,
				};

				frames.Add(active);
			}
			else if (chunk.SequenceEqual("IDAT"u8))
			{
				if (!seenActl)
					throw new InvalidDataException("APNG control must precede IDAT.");

				seenIdat = true;

				// With fcTL preceding IDAT the default image is frame zero.
				// Otherwise IDAT is a static poster, not part of the animation.
				if (active != null && frames.Count == 1)
					active.Compressed.Write(content);
			}
			else if (chunk.SequenceEqual("fdAT"u8))
			{
				if (!seenActl || active == null || content.Length < 4)
					throw new InvalidDataException("Orphaned APNG frame data.");

				var sequence = BinaryPrimitives.ReadUInt32BigEndian(content[..4]);

				if (sequence != expectedSequence++)
					throw new InvalidDataException("APNG frame data sequence out of order.");

				active.Compressed.Write(content[4..]);
			}
			else if (chunk.SequenceEqual("IEND"u8))
			{
				if (content.Length != 0 || position + 12 != data.Length)
					throw new InvalidDataException("Invalid APNG end.");

				ended = true;
				break;
			}
			else if ((chunk[0] & 0x20) == 0)
				throw new NotSupportedException("Unexpected critical APNG chunk.");

			position += 12 + (int)size;
		}

		if (!ended || !seenActl || frames.Count != expectedCount || header.Length != 13)
			throw new InvalidDataException("Incomplete APNG animation.");

		foreach (var frame in frames)
		{
			if (frame.Compressed.Length == 0)
				throw new InvalidDataException("APNG frame data is missing.");
		}

		return new(width, height, plays, frames.Count, Compose(width, height, header, palette, transparency, frames));
	}

	private static IEnumerable<AnimationFrame> Compose(int width, int height, byte[] header, byte[] palette, byte[] transparency, List<RawFrame> frames)
	{
		var canvas = new RasterImage(width, height);
		RawFrame previous = null;

		// The canvas as it was before the previous frame, if that frame is to be taken back.
		byte[] saved = null;

		foreach (var frame in frames)
		{
			if (previous?.Disposal == _disposeBackground)
			{
				for (var y = previous.Y; y < previous.Y + previous.Height; y++)
					canvas.Pixels.AsSpan((y * width + previous.X) * 4, previous.Width * 4).Clear();
			}
			else if (previous?.Disposal == _disposePrevious)
				saved.CopyTo(canvas.Pixels, 0);

			saved = frame.Disposal == _disposePrevious ? (byte[])canvas.Pixels.Clone() : null;

			Draw(canvas, DecodeFrame(header, palette, transparency, frame), frame);
			previous = frame;

			var composited = new RasterImage(width, height);
			canvas.Pixels.CopyTo(composited.Pixels, 0);

			yield return new(composited, frame.DelayNumerator, frame.DelayDenominator);
		}
	}

	private static void Draw(RasterImage canvas, RasterImage image, RawFrame frame)
	{
		for (var y = 0; y < frame.Height; y++)
			for (var x = 0; x < frame.Width; x++)
			{
				var source = (y * frame.Width + x) * 4;

				if (frame.Blend == _blendSource)
					image.Pixels.AsSpan(source, 4).CopyTo(canvas.Pixels.AsSpan(((y + frame.Y) * canvas.Width + x + frame.X) * 4));
				else
					canvas.BlendPixel(x + frame.X, y + frame.Y,
						image.Pixels[source], image.Pixels[source + 1],
						image.Pixels[source + 2], image.Pixels[source + 3]);
			}
	}

	// A frame is a PNG of its own once it gets the header of the file with its own size in it.
	private static RasterImage DecodeFrame(byte[] originalHeader, byte[] palette, byte[] transparency, RawFrame frame)
	{
		using var output = new MemoryStream();
		output.Write(PngCodec.Signature);

		var header = (byte[])originalHeader.Clone();
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), frame.Width);
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), frame.Height);
		PngCodec.WriteChunk(output, "IHDR"u8, header);

		if (palette.Length != 0)
			PngCodec.WriteChunk(output, "PLTE"u8, palette);

		if (transparency.Length != 0)
			PngCodec.WriteChunk(output, "tRNS"u8, transparency);

		PngCodec.WriteChunk(output, "IDAT"u8, frame.Compressed.GetBuffer().AsSpan(0, (int)frame.Compressed.Length));
		PngCodec.WriteChunk(output, "IEND"u8, []);

		return PngCodec.Decode(output.ToArray());
	}
}
