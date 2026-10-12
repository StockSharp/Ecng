namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Runtime.InteropServices;
using Ecng.Drawing.Codecs.Formats.Webp.Internal;
using Ecng.Drawing.Codecs.Internal;

internal static class Vp8LDecoder
{
	private const byte Signature = 0x2F;
	public static Image Decode(ReadOnlyMemory<byte> vp8LBytes, ImageProcessingLimits limits)
	{
		var (data, offset, end) = GetBounds(vp8LBytes);

		if (end - offset < 5 || data[offset] != Signature)
		{
			throw new WebpDecodingException("Malformed VP8L chunk: missing or invalid signature byte.");
		}

		var reader = new Vp8LBitReader(data, offset + 1, end);
		int width = (int)reader.ReadBits(14) + 1;
		int height = (int)reader.ReadBits(14) + 1;
		bool alphaIsUsed = reader.ReadBits(1) != 0;
		reader.ReadBits(3); // version_number -- always 0, nothing to branch on.

		ValidateDimensions(width, height, limits);

		var (argb, pixelCount, isPooled) = DecodeCore(reader, width, height, limits, rentFromPool: true);
		try
		{
			var pixelFormat = alphaIsUsed ? PixelFormat.Rgba32 : PixelFormat.Rgb24;
			byte[] packed = PackPixels(argb.AsSpan(0, pixelCount), pixelFormat);
			return Image.FromBuffer(width, height, pixelFormat, packed, owned: true, limits);
		}
		finally
		{
			if (isPooled)
			{
				WebpBufferPool.SharedUInt32.Return(argb);
			}
		}
	}
	public static uint[] DecodeAlphaSubstream(ReadOnlyMemory<byte> bytes, int width, int height, ImageProcessingLimits limits, out bool isPooled)
	{
		ValidateDimensions(width, height, limits, true);

		var (data, offset, end) = GetBounds(bytes);
		var reader = new Vp8LBitReader(data, offset, end);
		var (pixels, _, pooled) = DecodeCore(reader, width, height, limits, rentFromPool: true);
		isPooled = pooled;
		return pixels;
	}
	private static (uint[] Pixels, int PixelCount, bool IsPooled) DecodeCore(Vp8LBitReader reader, int width, int height, ImageProcessingLimits limits, bool rentFromPool)
	{
		int transformXsize = width;
		var transforms = Vp8LTransformReader.ReadTransforms(reader, ref transformXsize, height, limits);

		uint[] pixels = Vp8LPixelDecoder.DecodeImageStream(reader, transformXsize, height, allowRecursion: true, limits, rentFromPool);
		bool isPooled = rentFromPool;
		int pixelCount = transformXsize * height;

		for (int i = transforms.Count - 1; i >= 0; i--)
		{
			var transform = transforms[i];
			switch (transform.Type)
			{
				case Vp8LTransformTypes.SubtractGreen:
					Vp8LSubtractGreenTransform.ApplyInverse(pixels.AsSpan(0, pixelCount));
					break;

				case Vp8LTransformTypes.Predictor:
					Vp8LPredictorTransform.ApplyInverse(pixels.AsSpan(0, pixelCount), transform);
					break;

				case Vp8LTransformTypes.CrossColor:
					Vp8LColorTransform.ApplyInverse(pixels.AsSpan(0, pixelCount), transform);
					break;

				case Vp8LTransformTypes.ColorIndexing:
					// The expansion always replaces the buffer with a new, full-width one -- rented from the
					// same pool under the same rentFromPool flag the original buffer was, so this doesn't
					// regress to an unpooled allocation on the one path that used to be exactly that.
					uint[] expanded = Vp8LColorIndexingTransform.ApplyInverse(pixels.AsSpan(0, pixelCount), transform, rentFromPool);
					if (isPooled)
					{
						WebpBufferPool.SharedUInt32.Return(pixels);
					}

					pixels = expanded;
					isPooled = rentFromPool;
					pixelCount = transform.Xsize * transform.Ysize;
					break;
			}
		}

		return (pixels, pixelCount, isPooled);
	}

	public static RasterImage DecodeDownscaled(ReadOnlyMemory<byte> bytes, int outputWidth, int outputHeight, ImageProcessingLimits limits)
	{
		var (data, offset, end) = GetBounds(bytes);
		if (end - offset < 5 || data[offset] != Signature) throw new WebpDecodingException("Invalid VP8L signature.");
		var reader = new Vp8LBitReader(data, offset + 1, end);
		var width = (int)reader.ReadBits(14) + 1;
		var height = (int)reader.ReadBits(14) + 1;
		reader.ReadBits(1);
		if (reader.ReadBits(3) != 0) throw new WebpDecodingException("Unsupported VP8L version.");
		ValidateDimensions(width, height, limits, true);
		var reserved = (long)outputWidth * outputHeight * 4 + (long)outputWidth * 80 + (long)width * 4;
		var pixels = new Vp8LRowDecoder(reader, width, height, limits, reserved);
		var downscaler = new ImageDownscaler(width, height, outputWidth, outputHeight, limits);
		var row = new byte[checked(width * 4)];
		for (var y = 0; y < height; y++)
		{
			var source = pixels.ReadRow();
			for (var x = 0; x < width; x++)
			{
				var argb = source[x];
				row[x * 4] = (byte)(argb >> 16);
				row[x * 4 + 1] = (byte)(argb >> 8);
				row[x * 4 + 2] = (byte)argb;
				row[x * 4 + 3] = (byte)(argb >> 24);
			}
			downscaler.AddRow(row);
		}
		return downscaler.Image;
	}

	public static Vp8LRowDecoder OpenAlphaRows(ReadOnlyMemory<byte> bytes, int width, int height, ImageProcessingLimits limits, long reservedBytes)
	{
		ValidateDimensions(width, height, limits, true);
		var (data, offset, end) = GetBounds(bytes);
		return new Vp8LRowDecoder(new Vp8LBitReader(data, offset, end), width, height, limits, reservedBytes);
	}

	private static void ValidateDimensions(int width, int height, ImageProcessingLimits limits)
		=> ValidateDimensions(width, height, limits, false);

	private static void ValidateDimensions(int width, int height, ImageProcessingLimits limits, bool thumbnail)
	{
		if (width <= 0 || height <= 0 || width > WebpDecodingLimits.MaxDimension || height > WebpDecodingLimits.MaxDimension)
		{
			throw new WebpDecodingException($"Invalid VP8L image dimensions {width}x{height}.");
		}

		if (!thumbnail && (long)width * height > limits.MaxImagePixels)
		{
			throw new WebpDecodingException($"VP8L image {width}x{height} exceeds the maximum supported pixel count.");
		}
	}

	private static byte[] PackPixels(ReadOnlySpan<uint> argb, PixelFormat format)
	{
		int bytesPerPixel = format == PixelFormat.Rgba32 ? 4 : 3;
		int byteCount = checked((int)((long)argb.Length * bytesPerPixel));
		var buffer = ImageBufferPool.Shared.Rent(byteCount);

		int o = 0;
		for (int i = 0; i < argb.Length; i++)
		{
			uint pixel = argb[i];
			buffer[o++] = (byte)(pixel >> 16); // red
			buffer[o++] = (byte)(pixel >> 8);  // green
			buffer[o++] = (byte)pixel;         // blue

			if (bytesPerPixel == 4)
			{
				buffer[o++] = (byte)(pixel >> 24); // alpha
			}
		}

		return buffer;
	}
	private static (byte[] Data, int Offset, int End) GetBounds(ReadOnlyMemory<byte> memory)
	{
		if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment) && segment.Array is not null)
		{
			return (segment.Array, segment.Offset, segment.Offset + segment.Count);
		}

		byte[] copy = memory.ToArray();
		return (copy, 0, copy.Length);
	}
}
