namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;

/// <summary>
/// Minimal standards-compliant APNG writer. Every frame is emitted as an entire
/// RGBA8 source-composited frame (blend SOURCE, disposal NONE), with the delay it had.
/// </summary>
internal static class ApngEncoder
{
	/// <param name="source">The animation.</param>
	/// <param name="colorChunks">The chunks that say what colors the samples are, as <see cref="PngCodec.Encode"/> takes them.</param>
	public static byte[] Encode(Animation source, byte[] colorChunks)
	{
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

		foreach (var frame in source.Frames)
		{
			if (frame.Image.Width != source.Width || frame.Image.Height != source.Height)
				throw new InvalidDataException("APNG frame dimensions must equal canvas.");

			// A full-canvas frame at offset (0,0), neither disposed of nor blended.
			var control = new byte[26];
			BinaryPrimitives.WriteUInt32BigEndian(control.AsSpan(0), sequence++);
			BinaryPrimitives.WriteInt32BigEndian(control.AsSpan(4), source.Width);
			BinaryPrimitives.WriteInt32BigEndian(control.AsSpan(8), source.Height);
			BinaryPrimitives.WriteUInt16BigEndian(control.AsSpan(20), (ushort)Math.Min(ushort.MaxValue, frame.DelayNumerator));
			BinaryPrimitives.WriteUInt16BigEndian(control.AsSpan(22), (ushort)Math.Min(ushort.MaxValue, frame.DelayDenominator));
			PngCodec.WriteChunk(png, "fcTL"u8, control);

			var data = PngCodec.Compress(frame.Image);

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
		}

		if (index != source.FrameCount)
			throw new InvalidDataException("APNG frame count differs from the one announced.");

		PngCodec.WriteChunk(png, "IEND"u8, []);
		return png.ToArray();
	}
}
