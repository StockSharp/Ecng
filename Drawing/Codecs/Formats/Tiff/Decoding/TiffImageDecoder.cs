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

internal static class TiffImageDecoder
{
	public static Image Decode(Stream stream, ImageProcessingLimits limits)
		=> Decode(TiffStreamHelpers.BufferStream(stream), limits);

	public static Image Decode(byte[] fileData, ImageProcessingLimits limits)
	{
		var header = TiffHeaderReader.Read(fileData);
		var reader = new TiffReader(fileData, header.ByteOrder);
		var ifd = TiffIfdReader.Read(reader, header.FirstIfdOffset);
		var descriptor = TiffValidation.Validate(ifd, limits);

		byte[] palette = descriptor.Photometric == 3
			? TiffPalette.Resolve(descriptor.ColorMap, descriptor.BitsPerSample)
			: [];

		var image = Image.Create(descriptor.Width, descriptor.Height, descriptor.PixelFormat, limits);
		DecodeStrips(reader, descriptor, palette, image, limits);
		ReadIccProfile(ifd, image);
		return image;
	}

	public static RasterImage DecodeDownscaled(byte[] fileData, int width, int height, ImageProcessingLimits limits)
	{
		var header = TiffHeaderReader.Read(fileData);
		var reader = new TiffReader(fileData, header.ByteOrder);
		var ifd = TiffIfdReader.Read(reader, header.FirstIfdOffset);
		var descriptor = TiffValidation.Validate(ifd, limits, true);
		var palette = descriptor.Photometric == 3 ? TiffPalette.Resolve(descriptor.ColorMap, descriptor.BitsPerSample) : [];
		var downscaler = new ImageDownscaler(descriptor.Width, descriptor.Height, width, height, limits);
		var rgba = new byte[checked(descriptor.Width * 4)];
		ReadRows(reader, descriptor, palette, limits, (_, native) =>
		{
			Image.WriteRgba(native, descriptor.PixelFormat, rgba);
			downscaler.AddRow(rgba);
		});
		return downscaler.Image;
	}

	private static void ReadIccProfile(TiffIfd ifd, Image image)
	{
		byte[] profileBytes = ifd.TryGetBytes(TiffTags.IccProfile);
		if (profileBytes is not null)
		{
			image.Metadata.Profiles.Add(new RawMetadataProfile { Kind = MetadataProfileKind.Icc, Data = profileBytes });
		}
	}

	private static void DecodeStrips(TiffReader reader, TiffImageDescriptor descriptor, byte[] palette, Image image, ImageProcessingLimits limits)
		=> ReadRows(reader, descriptor, palette, limits, (y, row) => row.AsSpan().CopyTo(image.GetRowSpan(y)));

	private static void ReadRows(TiffReader reader, TiffImageDescriptor descriptor, byte[] palette, ImageProcessingLimits limits, Action<int, byte[]> consume)
	{
		int width = descriptor.Width;
		int height = descriptor.Height;
		int samplesPerPixel = descriptor.SamplesPerPixel;
		int bitDepth = descriptor.BitsPerSample;
		int rowsPerStrip = (int)Math.Min(descriptor.RowsPerStrip, (uint)height);

		int paddedRowByteCount = GetPaddedRowByteCount(width, samplesPerPixel, bitDepth);
		int sampleCount = width * samplesPerPixel;
		limits.EnsureWorkingMemory((long)paddedRowByteCount + (long)sampleCount * 2 +
			(long)width * (Image.BytesPerPixel(descriptor.PixelFormat) + 4) +
			(descriptor.Compression == 5 ? 4096L * 4 : 0));
		var samples = new ushort[sampleCount];
		var packedRow = new byte[paddedRowByteCount];
		var nativeRow = new byte[checked(width * Image.BytesPerPixel(descriptor.PixelFormat))];

		int rowsDecoded = 0;
		for (int stripIndex = 0; rowsDecoded < height; stripIndex++)
		{
			if (stripIndex >= descriptor.StripOffsets.Length)
			{
				throw new TiffDecodingException("TIFF strip metadata does not cover the full image height.");
			}

			int rowsInThisStrip = Math.Min(rowsPerStrip, height - rowsDecoded);
			long stripByteLength = (long)paddedRowByteCount * rowsInThisStrip;

			uint declaredByteCount = descriptor.StripByteCounts[stripIndex];
			if (declaredByteCount > TiffDecodingLimits.MaxDeclaredStripByteCount)
			{
				throw new TiffDecodingException($"TIFF strip {stripIndex} declares {declaredByteCount} bytes, exceeding the supported maximum.");
			}

			var offset = CheckedInt(descriptor.StripOffsets[stripIndex], "strip offset");
			var count = CheckedInt(declaredByteCount, "strip byte count");
			_ = reader.ReadSpan(offset, count);
			var decompressor = new TiffStripReader(reader.Data.AsMemory(offset, count), descriptor.Compression, stripByteLength);

			for (int row = 0; row < rowsInThisStrip; row++)
			{
				decompressor.Read(packedRow);
				if (descriptor.Predictor == 2) UndoPredictor(packedRow, paddedRowByteCount, 1, samplesPerPixel, bitDepth, reader.ByteOrder);
				TiffBitUnpacker.Unpack(packedRow, bitDepth, sampleCount, reader.ByteOrder, samples);
				TiffSampleWriter.WriteRow(samples, descriptor, palette, nativeRow);
				consume(rowsDecoded + row, nativeRow);
			}

			rowsDecoded += rowsInThisStrip;
		}
	}

	private static void UndoPredictor(Span<byte> stripBytes, int paddedRowByteCount, int rowCount, int samplesPerPixel, int bitDepth, TiffByteOrders byteOrder)
	{
		for (int row = 0; row < rowCount; row++)
		{
			var rowSpan = stripBytes.Slice(row * paddedRowByteCount, paddedRowByteCount);
			if (bitDepth == 16)
			{
				TiffPredictor.UndoHorizontalDifferencing16(rowSpan, samplesPerPixel, byteOrder);
			}
			else
			{
				TiffPredictor.UndoHorizontalDifferencing8(rowSpan, samplesPerPixel);
			}
		}
	}

	private static int GetPaddedRowByteCount(int width, int samplesPerPixel, int bitDepth)
	{
		long bits = (long)width * samplesPerPixel * bitDepth;
		return (int)((bits + 7) / 8);
	}

	private static int CheckedInt(uint value, string what)
	{
		if (value > int.MaxValue)
		{
			throw new TiffDecodingException($"TIFF {what} is out of range.");
		}

		return (int)value;
	}
}
