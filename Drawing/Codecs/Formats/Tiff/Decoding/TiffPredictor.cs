namespace Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Buffers.Binary;

internal static class TiffPredictor
{
	public static void UndoHorizontalDifferencing8(Span<byte> row, int samplesPerPixel)
	{
		for (int i = samplesPerPixel; i < row.Length; i++)
		{
			row[i] = (byte)(row[i] + row[i - samplesPerPixel]);
		}
	}
	public static void UndoHorizontalDifferencing16(Span<byte> row, int samplesPerPixel, TiffByteOrders byteOrder)
	{
		int sampleStrideBytes = samplesPerPixel * 2;

		for (int offset = sampleStrideBytes; offset + 2 <= row.Length; offset += 2)
		{
			var current = row.Slice(offset, 2);
			var previous = row.Slice(offset - sampleStrideBytes, 2);

			ushort currentValue = byteOrder == TiffByteOrders.LittleEndian
				? BinaryPrimitives.ReadUInt16LittleEndian(current)
				: BinaryPrimitives.ReadUInt16BigEndian(current);
			ushort previousValue = byteOrder == TiffByteOrders.LittleEndian
				? BinaryPrimitives.ReadUInt16LittleEndian(previous)
				: BinaryPrimitives.ReadUInt16BigEndian(previous);

			ushort reconstructed = (ushort)(currentValue + previousValue);

			if (byteOrder == TiffByteOrders.LittleEndian)
			{
				BinaryPrimitives.WriteUInt16LittleEndian(current, reconstructed);
			}
			else
			{
				BinaryPrimitives.WriteUInt16BigEndian(current, reconstructed);
			}
		}
	}
}
