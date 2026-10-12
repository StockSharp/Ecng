namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

using Ecng.Drawing.Codecs;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Alpha;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

internal static class WebpCodec
{
	public static bool IsWebp(ReadOnlySpan<byte> data)
		=> data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data.Slice(8, 4).SequenceEqual("WEBP"u8);

	public static (int width, int height) ReadSize(ReadOnlySpan<byte> data)
	{
		var file = Read(data.ToArray(), ImageProcessingLimits.Default);
		return (file.Width, file.Height);
	}

	public static RasterImage Decode(byte[] data)
		=> Decode(data, ImageProcessingLimits.Default);

	public static RasterImage Decode(byte[] data, ImageProcessingLimits limits)
	{
		var file = Read(data, limits);
		if (file.IsAnimated)
		{
			using var frames = Open(file, limits).Frames.GetEnumerator();
			if (!frames.MoveNext()) throw new InvalidDataException("WebP animation has no frames.");
			return frames.Current.Image;
		}
		return Decode(file.Bitstream, file.Alpha, file.IsLossless, file.Width, file.Height, limits);
	}

	public static RasterImage DecodeDownscaled(byte[] data, int width, int height, ImageProcessingLimits limits)
	{
		var file = Read(data, limits);
		if (file.IsAnimated) throw new NotSupportedException("Animated WebP uses the animation pipeline.");
		try
		{
			if (file.IsLossless) return Vp8LDecoder.DecodeDownscaled(file.Bitstream, width, height, limits);
			var reserved = Vp8ThumbnailBuffer.EstimateWorkingBytes(file.Width, width, height);
			var alpha = file.Alpha.IsEmpty ? null : new WebpAlphaRows(file.Alpha, file.Width, file.Height, limits, reserved);
			var frame = Vp8FrameDecoder.DecodeDownscaled(file.Bitstream, alpha, width, height, limits);
			using var decoded = CodecImage.FromBuffer(frame.Width, frame.Height, frame.PixelFormat, frame.Pixels, false, limits);
			return decoded.ToRaster();
		}
		catch (Exception error) when (error is ImageFormatException or ArgumentException or IndexOutOfRangeException or OverflowException)
		{
			throw new InvalidDataException("Invalid WebP thumbnail.", error);
		}
	}

	public static Animation Open(byte[] data)
		=> Open(data, ImageProcessingLimits.Default);

	public static Animation Open(byte[] data, ImageProcessingLimits limits)
	{
		var file = Read(data, limits);
		return file.IsAnimated ? Open(file, limits) : null;
	}

	public static int ReadPlays(byte[] data)
	{
		var file = Read(data, ImageProcessingLimits.Default);
		if (!file.IsAnimated) throw new NotSupportedException("WebP is not animated.");
		return file.Plays;
	}

	private static Animation Open(WebpFile file, ImageProcessingLimits limits)
	{
		RasterImage.EnsureDecodable(file.Width, file.Height, limits);
		Animation.EnsureWithinBudget(file.Frames.Count, (long)file.Width * file.Height * file.Frames.Count, limits);
		return new(file.Width, file.Height, file.Plays, file.Frames.Count, Frames(file, limits), limits);
	}

	private static IEnumerable<AnimationFrame> Frames(WebpFile file, ImageProcessingLimits limits)
	{
		var canvas = new RasterImage(file.Width, file.Height, limits);
		Fill(canvas, 0, 0, file.Width, file.Height, file.Background);
		foreach (var frame in file.Frames)
		{
			var image = Decode(frame.Bitstream, frame.Alpha, frame.IsLossless, frame.Width, frame.Height, limits);
			for (var y = 0; y < frame.Height; y++)
			{
				for (var x = 0; x < frame.Width; x++)
				{
					var pixel = (y * frame.Width + x) * 4;
					if (frame.IsNoBlend)
						image.Pixels.AsSpan(pixel, 4).CopyTo(canvas.Pixels.AsSpan(((y + frame.Y) * canvas.Width + x + frame.X) * 4, 4));
					else
						canvas.BlendPixel(x + frame.X, y + frame.Y, image.Pixels[pixel], image.Pixels[pixel + 1], image.Pixels[pixel + 2], image.Pixels[pixel + 3]);
				}
			}
			var result = new RasterImage(canvas.Width, canvas.Height, limits);
			canvas.Pixels.CopyTo(result.Pixels, 0);
			yield return new(result, frame.Delay, 1000);
			if (frame.IsDisposeToBackground)
				Fill(canvas, frame.X, frame.Y, frame.Width, frame.Height, file.Background);
		}
	}

	private static void Fill(RasterImage image, int x, int y, int width, int height, uint color)
	{
		for (var row = y; row < y + height; row++)
		{
			for (var col = x; col < x + width; col++)
			{
				var offset = (row * image.Width + col) * 4;
				image.Pixels[offset] = (byte)(color >> 16);
				image.Pixels[offset + 1] = (byte)(color >> 8);
				image.Pixels[offset + 2] = (byte)color;
				image.Pixels[offset + 3] = (byte)(color >> 24);
			}
		}
	}

	private static RasterImage Decode(ReadOnlyMemory<byte> bitstream, ReadOnlyMemory<byte> alpha, bool isLossless, int width, int height, ImageProcessingLimits limits)
	{
		RasterImage.EnsureDecodable(width, height, limits);
		try
		{
			if (isLossless)
			{
				using var image = Vp8LDecoder.Decode(bitstream, limits);
				return image.ToRaster();
			}
			var frame = Vp8FrameDecoder.Instance.Decode(bitstream, alpha.IsEmpty ? null : WebpAlphaDecoder.Decode(alpha, width, height, limits), limits);
			using var decoded = CodecImage.FromBuffer(frame.Width, frame.Height, frame.PixelFormat, frame.Pixels, false, limits);
			return decoded.ToRaster();
		}
		catch (Exception error) when (error is ImageFormatException or ArgumentException or IndexOutOfRangeException or OverflowException)
		{
			throw new InvalidDataException("Invalid WebP bitstream.", error);
		}
	}

	private static WebpFile Read(byte[] data, ImageProcessingLimits limits)
	{
		if (!IsWebp(data)) throw new InvalidDataException("Expected a WebP RIFF container.");
		var length = (long)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4)) + 8;
		if (length < 12 || length > data.Length || length > 128 * 1024 * 1024)
			throw new InvalidDataException("Invalid WebP container length.");
		var file = new WebpFile();
		var hasExtended = false;
		var hasAnimationHeader = false;
		for (var offset = 12; offset < length;)
		{
			var (type, payload, next) = ReadChunk(data, offset, (int)length);
			var bytes = payload.Span;
			switch (type)
			{
				case 0x58385056: // VP8X.
					if (hasExtended || offset != 12 || bytes.Length != 10 || (bytes[0] & 0xc1) != 0 || bytes[1] != 0 || bytes[2] != 0 || bytes[3] != 0)
						throw new InvalidDataException("Invalid WebP extended header.");
					hasExtended = true;
					file.IsAnimated = (bytes[0] & 2) != 0;
					file.Width = Read24(bytes[4..]) + 1;
					file.Height = Read24(bytes[7..]) + 1;
					break;
				case 0x4d494e41: // ANIM.
					if (!file.IsAnimated || hasAnimationHeader || bytes.Length != 6 || file.Frames.Count != 0)
						throw new InvalidDataException("Invalid WebP animation header.");
					hasAnimationHeader = true;
					file.Background = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
					file.Plays = BinaryPrimitives.ReadUInt16LittleEndian(bytes[4..]);
					break;
				case 0x464d4e41: // ANMF.
					if (!hasAnimationHeader || bytes.Length < 16 || (bytes[15] & ~3) != 0)
						throw new InvalidDataException("Invalid WebP animation frame.");
					var x = Read24(bytes) * 2;
					var y = Read24(bytes[3..]) * 2;
					var width = Read24(bytes[6..]) + 1;
					var height = Read24(bytes[9..]) + 1;
					if ((long)x + width > file.Width || (long)y + height > file.Height)
						throw new InvalidDataException("WebP frame lies outside its canvas.");
					var (stream, alpha, lossless) = ReadFrame(data, offset + 8 + 16, offset + 8 + payload.Length, width, height);
					file.Frames.Add(new(x, y, width, height, Read24(bytes[12..]), (bytes[15] & 2) != 0, (bytes[15] & 1) != 0, lossless, stream, alpha));
					Animation.EnsureWithinBudget(file.Frames.Count, (long)file.Width * file.Height * file.Frames.Count, limits);
					break;
				case 0x48504c41: // ALPH.
					if (file.IsAnimated || !file.Alpha.IsEmpty || !file.Bitstream.IsEmpty || payload.IsEmpty)
						throw new InvalidDataException("Invalid WebP alpha chunk.");
					file.Alpha = payload;
					break;
				case 0x20385056: // VP8.
				case 0x4c385056: // VP8L.
					if (file.IsAnimated || !file.Bitstream.IsEmpty) throw new InvalidDataException("Repeated WebP image data.");
					file.IsLossless = type == 0x4c385056;
					var size = BitstreamSize(payload.Span, file.IsLossless);
					if (hasExtended && size != (file.Width, file.Height)) throw new InvalidDataException("WebP canvas and bitstream sizes differ.");
					(file.Width, file.Height) = size;
					file.Bitstream = payload;
					break;
			}
			offset = next;
		}
		if (file.IsAnimated ? !hasAnimationHeader || file.Frames.Count == 0 : file.Bitstream.IsEmpty || file.IsLossless && !file.Alpha.IsEmpty)
			throw new InvalidDataException("WebP image data is missing or inconsistent.");
		return file;
	}

	private static (ReadOnlyMemory<byte> stream, ReadOnlyMemory<byte> alpha, bool isLossless) ReadFrame(byte[] data, int offset, int end, int width, int height)
	{
		ReadOnlyMemory<byte> stream = default, alpha = default;
		var isLossless = false;
		while (offset < end)
		{
			var (type, payload, next) = ReadChunk(data, offset, end);
			if (type == 0x48504c41 && alpha.IsEmpty && stream.IsEmpty && !payload.IsEmpty)
				alpha = payload;
			else if ((type == 0x20385056 || type == 0x4c385056) && stream.IsEmpty)
			{
				isLossless = type == 0x4c385056;
				if (BitstreamSize(payload.Span, isLossless) != (width, height)) throw new InvalidDataException("WebP frame dimensions differ from its bitstream.");
				stream = payload;
			}
			else
				throw new InvalidDataException("Unexpected WebP frame chunk.");
			offset = next;
		}
		if (stream.IsEmpty || isLossless && !alpha.IsEmpty) throw new InvalidDataException("Invalid WebP frame data.");
		return (stream, alpha, isLossless);
	}

	private static (uint type, ReadOnlyMemory<byte> payload, int next) ReadChunk(byte[] data, int offset, int end)
	{
		if (end - offset < 8) throw new InvalidDataException("Truncated WebP chunk.");
		var type = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset));
		var size = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 4));
		var next = (long)offset + 8 + size + (size & 1);
		if (next > end) throw new InvalidDataException("Truncated WebP chunk payload.");
		return (type, data.AsMemory(offset + 8, (int)size), (int)next);
	}

	private static (int width, int height) BitstreamSize(ReadOnlySpan<byte> data, bool isLossless)
	{
		int width, height;
		var valid = isLossless
			? WebpBitstreamHeaderPeek.TryPeekVp8L(data, out width, out height, out _) && (data[4] & 0xe0) == 0
			: WebpBitstreamHeaderPeek.TryPeekVp8(data, out width, out height) && (data[0] & 1) == 0;
		if (!valid || width <= 0 || height <= 0) throw new InvalidDataException("Invalid WebP bitstream header.");
		return (width, height);
	}

	private static int Read24(ReadOnlySpan<byte> data) => data[0] | data[1] << 8 | data[2] << 16;
}
