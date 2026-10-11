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
	public static Image Decode(ReadOnlyMemory<byte> vp8LBytes)
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

		ValidateDimensions(width, height);

		var (argb, pixelCount, isPooled) = DecodeCore(reader, width, height, rentFromPool: true);
		try
		{
			var pixelFormat = alphaIsUsed ? PixelFormat.Rgba32 : PixelFormat.Rgb24;
			byte[] packed = PackPixels(argb.AsSpan(0, pixelCount), pixelFormat);
			return Image.FromBuffer(width, height, pixelFormat, packed, owned: true);
		}
		finally
		{
			if (isPooled)
			{
				WebpBufferPool.SharedUInt32.Return(argb);
			}
		}
	}
	public static uint[] DecodeAlphaSubstream(ReadOnlyMemory<byte> bytes, int width, int height, out bool isPooled)
	{
		ValidateDimensions(width, height);

		var (data, offset, end) = GetBounds(bytes);
		var reader = new Vp8LBitReader(data, offset, end);
		var (pixels, _, pooled) = DecodeCore(reader, width, height, rentFromPool: true);
		isPooled = pooled;
		return pixels;
	}
	private static (uint[] Pixels, int PixelCount, bool IsPooled) DecodeCore(Vp8LBitReader reader, int width, int height, bool rentFromPool)
	{
		int transformXsize = width;
		var transforms = Vp8LTransformReader.ReadTransforms(reader, ref transformXsize, height);

		uint[] pixels = Vp8LPixelDecoder.DecodeImageStream(reader, transformXsize, height, allowRecursion: true, rentFromPool);
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

	private static void ValidateDimensions(int width, int height)
	{
		if (width <= 0 || height <= 0 || width > WebpDecodingLimits.MaxDimension || height > WebpDecodingLimits.MaxDimension)
		{
			throw new WebpDecodingException($"Invalid VP8L image dimensions {width}x{height}.");
		}

		if ((long)width * height > WebpDecodingLimits.MaxPixelCount)
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
