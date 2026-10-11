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
	public static Image Decode(Stream stream)
	{
		byte[] fileData = TiffStreamHelpers.BufferStream(stream);
		var header = TiffHeaderReader.Read(fileData);
		var reader = new TiffReader(fileData, header.ByteOrder);
		var ifd = TiffIfdReader.Read(reader, header.FirstIfdOffset);
		var descriptor = TiffValidation.Validate(ifd);

		byte[] palette = descriptor.Photometric == 3
			? TiffPalette.Resolve(descriptor.ColorMap, descriptor.BitsPerSample)
			: [];

		var image = Image.Create(descriptor.Width, descriptor.Height, descriptor.PixelFormat);
		DecodeStrips(reader, descriptor, palette, image);
		ReadIccProfile(ifd, image);
		return image;
	}

	private static void ReadIccProfile(TiffIfd ifd, Image image)
	{
		byte[] profileBytes = ifd.TryGetBytes(TiffTags.IccProfile);
		if (profileBytes is not null)
		{
			image.Metadata.Profiles.Add(new RawMetadataProfile { Kind = MetadataProfileKind.Icc, Data = profileBytes });
		}
	}

	private static void DecodeStrips(TiffReader reader, TiffImageDescriptor descriptor, byte[] palette, Image image)
	{
		int width = descriptor.Width;
		int height = descriptor.Height;
		int samplesPerPixel = descriptor.SamplesPerPixel;
		int bitDepth = descriptor.BitsPerSample;
		int rowsPerStrip = (int)Math.Min(descriptor.RowsPerStrip, (uint)height);

		int paddedRowByteCount = GetPaddedRowByteCount(width, samplesPerPixel, bitDepth);
		int sampleCount = width * samplesPerPixel;
		var samples = new ushort[sampleCount];

		int rowsDecoded = 0;
		for (int stripIndex = 0; rowsDecoded < height; stripIndex++)
		{
			if (stripIndex >= descriptor.StripOffsets.Length)
			{
				throw new TiffDecodingException("TIFF strip metadata does not cover the full image height.");
			}

			int rowsInThisStrip = Math.Min(rowsPerStrip, height - rowsDecoded);
			int stripByteLength = paddedRowByteCount * rowsInThisStrip;

			uint declaredByteCount = descriptor.StripByteCounts[stripIndex];
			if (declaredByteCount > TiffDecodingLimits.MaxDeclaredStripByteCount)
			{
				throw new TiffDecodingException($"TIFF strip {stripIndex} declares {declaredByteCount} bytes, exceeding the supported maximum.");
			}

			var compressed = reader.ReadSpan(CheckedInt(descriptor.StripOffsets[stripIndex], "strip offset"), CheckedInt(declaredByteCount, "strip byte count"));
			byte[] decompressed = DecompressStrip(compressed, descriptor.Compression, stripByteLength);
			var decompressedSpan = decompressed.AsSpan(0, stripByteLength);

			if (descriptor.Predictor == 2)
			{
				UndoPredictor(decompressedSpan, paddedRowByteCount, rowsInThisStrip, samplesPerPixel, bitDepth, reader.ByteOrder);
			}

			for (int row = 0; row < rowsInThisStrip; row++)
			{
				var rowBytes = decompressedSpan.Slice(row * paddedRowByteCount, paddedRowByteCount);
				TiffBitUnpacker.Unpack(rowBytes, bitDepth, sampleCount, reader.ByteOrder, samples);

				var destRow = image.GetRowSpan(rowsDecoded + row);
				TiffSampleWriter.WriteRow(samples, descriptor, palette, destRow);
			}

			rowsDecoded += rowsInThisStrip;
		}
	}

	private static byte[] DecompressStrip(ReadOnlySpan<byte> compressed, int compression, int expectedLength)
	{
		var output = new byte[expectedLength];

		switch (compression)
		{
			case 1: // None.
				if (compressed.Length < expectedLength)
					throw new TiffDecodingException("Truncated uncompressed TIFF strip.");
				compressed[..expectedLength].CopyTo(output);
				return output;

			case 5: // LZW.
				TiffLzwDecoder.Decode(compressed, output);
				return output;

			case 32773: // PackBits.
				TiffPackBitsDecoder.Decode(compressed, output);
				return output;

			default:
				throw new TiffDecodingException($"Unreachable: compression {compression} should have been rejected during validation.");
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
