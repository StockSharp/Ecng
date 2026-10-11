namespace Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class TiffHeaderReader
{
	private const ushort ClassicTiffMagic = 42;
	private const ushort BigTiffMagic = 43;

	public static TiffHeader Read(byte[] data)
	{
		if (data.Length < 8)
		{
			throw new TiffDecodingException("Not a TIFF file: too short to contain an 8-byte header.");
		}

		TiffByteOrders byteOrder;
		if (data[0] == (byte)'I' && data[1] == (byte)'I')
		{
			byteOrder = TiffByteOrders.LittleEndian;
		}
		else if (data[0] == (byte)'M' && data[1] == (byte)'M')
		{
			byteOrder = TiffByteOrders.BigEndian;
		}
		else
		{
			throw new TiffDecodingException("Not a TIFF file: missing 'II'/'MM' byte-order mark.");
		}

		var reader = new TiffReader(data, byteOrder);
		ushort magic = reader.ReadUInt16(2);

		if (magic == BigTiffMagic)
		{
			throw new TiffUnsupportedFeatureException("BigTIFF is not supported.");
		}

		if (magic != ClassicTiffMagic)
		{
			throw new TiffDecodingException($"Not a TIFF file: unexpected magic number {magic} (expected 42).");
		}

		uint firstIfdOffset = reader.ReadUInt32(4);
		return new TiffHeader(byteOrder, firstIfdOffset);
	}
}
