namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class AvifColrBox
{
	public static AvifColr Parse(byte[] data, AvifBox box)
	{
		if (box.PayloadLength < 4)
		{
			throw new AvifDecodingException("Truncated 'colr' box.");
		}

		int offset = box.PayloadOffset;
		string colourType = AvifBinaryReader.ReadFourCc(data, offset);
		offset += 4;

		if (colourType == "nclx")
		{
			if (box.PayloadOffset + box.PayloadLength < offset + 7)
			{
				throw new AvifDecodingException("Truncated 'colr' (nclx) box.");
			}

			int primaries = AvifBinaryReader.ReadUInt16(data, ref offset);
			int transfer = AvifBinaryReader.ReadUInt16(data, ref offset);
			int matrix = AvifBinaryReader.ReadUInt16(data, ref offset);
			bool fullRange = (data[offset] & 0x80) != 0;

			return new AvifColr
			{
				ColourType = colourType,
				ColourPrimaries = primaries,
				TransferCharacteristics = transfer,
				MatrixCoefficients = matrix,
				FullRange = fullRange,
			};
		}

		if (colourType is "rICC" or "prof")
		{
			int iccLength = box.PayloadOffset + box.PayloadLength - offset;
			byte[] icc = data.AsSpan(offset, iccLength).ToArray();
			return new AvifColr { ColourType = colourType, IccProfile = icc };
		}

		return new AvifColr { ColourType = colourType };
	}
}
