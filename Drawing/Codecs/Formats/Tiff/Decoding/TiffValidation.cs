namespace Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using Ecng.Drawing.Codecs.Formats.Tiff.Internal;

internal static class TiffValidation
{
	public static TiffImageDescriptor Validate(TiffIfd ifd)
	{
		int width = RequireDimension(ifd.RequireUInt32(TiffTags.ImageWidth), "ImageWidth");
		int height = RequireDimension(ifd.RequireUInt32(TiffTags.ImageLength), "ImageLength");

		if ((long)width * height > TiffDecodingLimits.MaxPixelCount)
		{
			throw new TiffDecodingException($"TIFF dimensions {width}x{height} exceed the maximum supported pixel count.");
		}

		if (ifd.HasTag(TiffTags.TileWidth) || ifd.HasTag(TiffTags.TileOffsets))
		{
			throw new TiffUnsupportedFeatureException("Tiled TIFF organization is not supported.");
		}

		int compression = (int)ifd.GetUInt32(TiffTags.Compression, 1);
		if (compression is not (1 or 5 or 32773))
		{
			throw new TiffUnsupportedFeatureException($"TIFF compression {compression} ({DescribeCompression(compression)}) is not supported.");
		}

		int planarConfiguration = (int)ifd.GetUInt32(TiffTags.PlanarConfiguration, 1);
		if (planarConfiguration != 1)
		{
			throw new TiffUnsupportedFeatureException($"TIFF PlanarConfiguration {planarConfiguration} (planar/separate storage) is not supported.");
		}

		int fillOrder = (int)ifd.GetUInt32(TiffTags.FillOrder, 1);
		if (fillOrder != 1)
		{
			throw new TiffUnsupportedFeatureException($"TIFF FillOrder {fillOrder} (LSB-first bit packing) is not supported.");
		}

		int sampleFormat = (int)ifd.GetUInt32(TiffTags.SampleFormat, 1);
		if (sampleFormat != 1)
		{
			throw new TiffUnsupportedFeatureException($"TIFF SampleFormat {sampleFormat} is not supported (only unsigned integer samples are).");
		}

		int samplesPerPixel = (int)ifd.GetUInt32(TiffTags.SamplesPerPixel, 1);
		if (samplesPerPixel is < 1 or > 8)
		{
			throw new TiffUnsupportedFeatureException($"TIFF SamplesPerPixel {samplesPerPixel} is not supported.");
		}

		int bitsPerSample = ReadUniformBitsPerSample(ifd);
		if (bitsPerSample is not (1 or 2 or 4 or 8 or 16))
		{
			throw new TiffUnsupportedFeatureException($"TIFF bit depth {bitsPerSample} is not supported.");
		}

		int predictor = (int)ifd.GetUInt32(TiffTags.Predictor, 1);
		if (predictor is not (1 or 2))
		{
			throw new TiffUnsupportedFeatureException($"TIFF Predictor {predictor} is not supported.");
		}

		if (predictor == 2 && bitsPerSample is not (8 or 16))
		{
			throw new TiffUnsupportedFeatureException($"TIFF Predictor 2 (horizontal differencing) with {bitsPerSample}-bit samples is not supported.");
		}

		int photometric = (int)ifd.RequireUInt32(TiffTags.PhotometricInterpretation);
		uint[] extraSamples = ifd.GetUInt32Array(TiffTags.ExtraSamples);

		bool hasAlpha = false;
		bool alphaIsPremultiplied = false;
		uint[] colorMap = [];
		PixelFormat pixelFormat;

		switch (photometric)
		{
			case 0 or 1: // WhiteIsZero / BlackIsZero
				if (samplesPerPixel != 1)
				{
					throw new TiffUnsupportedFeatureException($"Grayscale TIFF with SamplesPerPixel {samplesPerPixel} is not supported.");
				}

				pixelFormat = bitsPerSample == 16 ? PixelFormat.Gray16 : PixelFormat.Gray8;
				break;

			case 2: // RGB
				if (samplesPerPixel == 4)
				{
					hasAlpha = true;
					alphaIsPremultiplied = extraSamples.Length > 0 && extraSamples[0] == 1;
				}
				else if (samplesPerPixel != 3)
				{
					throw new TiffUnsupportedFeatureException($"RGB TIFF with SamplesPerPixel {samplesPerPixel} is not supported.");
				}

				pixelFormat = (bitsPerSample == 16, hasAlpha) switch
				{
					(false, false) => PixelFormat.Rgb24,
					(false, true) => PixelFormat.Rgba32,
					(true, false) => PixelFormat.Rgb48,
					(true, true) => PixelFormat.Rgba64,
				};
				break;

			case 3: // Palette
				if (samplesPerPixel != 1)
				{
					throw new TiffUnsupportedFeatureException($"Palette TIFF with SamplesPerPixel {samplesPerPixel} is not supported.");
				}

				if (bitsPerSample == 16)
				{
					throw new TiffUnsupportedFeatureException("16-bit-indexed palette TIFF is not supported.");
				}

				colorMap = ifd.RequireUInt32Array(TiffTags.ColorMap);
				int expectedColorMapCount = 3 * (1 << bitsPerSample);
				if (colorMap.Length < expectedColorMapCount)
				{
					throw new TiffDecodingException($"TIFF ColorMap has {colorMap.Length} entries; expected at least {expectedColorMapCount} for a {bitsPerSample}-bit palette.");
				}

				pixelFormat = PixelFormat.Rgb24;
				break;

			case 5: // Separated (CMYK)
				int inkSet = (int)ifd.GetUInt32(TiffTags.InkSet, 1);
				if (inkSet != 1)
				{
					throw new TiffUnsupportedFeatureException($"TIFF InkSet {inkSet} (non-standard ink set) is not supported.");
				}

				if (samplesPerPixel != 4)
				{
					throw new TiffUnsupportedFeatureException($"CMYK TIFF with SamplesPerPixel {samplesPerPixel} is not supported.");
				}

				if (bitsPerSample != 8)
				{
					throw new TiffUnsupportedFeatureException($"{bitsPerSample}-bit CMYK TIFF is not supported.");
				}

				pixelFormat = PixelFormat.Cmyk32;
				break;

			default:
				throw new TiffUnsupportedFeatureException($"TIFF PhotometricInterpretation {photometric} is not supported.");
		}

		uint rowsPerStrip = ifd.GetUInt32(TiffTags.RowsPerStrip, (uint)height);
		if (rowsPerStrip == 0)
		{
			rowsPerStrip = (uint)height;
		}

		uint[] stripOffsets = ifd.RequireUInt32Array(TiffTags.StripOffsets);
		uint[] stripByteCounts = ifd.RequireUInt32Array(TiffTags.StripByteCounts);

		long expectedStripCount = ((long)height + rowsPerStrip - 1) / rowsPerStrip;
		if (stripOffsets.Length != stripByteCounts.Length || stripOffsets.Length < expectedStripCount)
		{
			throw new TiffDecodingException(
				$"TIFF strip metadata is inconsistent: {stripOffsets.Length} offset(s), {stripByteCounts.Length} byte count(s), expected {expectedStripCount} strip(s).");
		}

		return new TiffImageDescriptor
		{
			Width = width,
			Height = height,
			BitsPerSample = bitsPerSample,
			SamplesPerPixel = samplesPerPixel,
			Compression = compression,
			Photometric = photometric,
			Predictor = predictor,
			RowsPerStrip = rowsPerStrip,
			StripOffsets = stripOffsets,
			StripByteCounts = stripByteCounts,
			HasAlpha = hasAlpha,
			AlphaIsPremultiplied = alphaIsPremultiplied,
			ColorMap = colorMap,
			PixelFormat = pixelFormat,
		};
	}

	private static int RequireDimension(uint value, string name)
	{
		if (value == 0 || value > int.MaxValue)
		{
			throw new TiffDecodingException($"Invalid TIFF {name}: {value}.");
		}

		return (int)value;
	}
	private static int ReadUniformBitsPerSample(TiffIfd ifd)
	{
		if (!ifd.HasTag(TiffTags.BitsPerSample))
		{
			return 1;
		}

		uint[] values = ifd.GetUInt32Array(TiffTags.BitsPerSample);
		if (values.Length == 0)
		{
			return 1;
		}

		uint first = values[0];
		for (int i = 1; i < values.Length; i++)
		{
			if (values[i] != first)
			{
				throw new TiffUnsupportedFeatureException("TIFF files with a different bit depth per channel are not supported.");
			}
		}

		return (int)first;
	}

	private static string DescribeCompression(int code) => code switch
	{
		2 or 3 or 4 => "CCITT fax",
		6 => "old-style JPEG",
		7 => "JPEG",
		8 or 32946 => "Deflate",
		34712 => "JPEG2000",
		50000 => "ZSTD",
		50001 => "WebP",
		_ => "unrecognized",
	};
}
