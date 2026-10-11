namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal readonly struct AvifFtypBox
{
	public string MajorBrand { get; init; }

	public IReadOnlyList<string> CompatibleBrands { get; init; }
	public bool HasBrand(string brand) => MajorBrand == brand || CompatibleBrands.Contains(brand);

	public static AvifFtypBox Parse(byte[] data, AvifBox box)
	{
		if (box.PayloadLength < 8)
		{
			throw new AvifDecodingException("Truncated 'ftyp' box.");
		}

		string majorBrand = AvifBinaryReader.ReadFourCc(data, box.PayloadOffset);

		var compatible = new List<string>();
		int offset = box.PayloadOffset + 8; // skip major_brand(4) + minor_version(4)
		int end = box.PayloadOffset + box.PayloadLength;
		while (offset + 4 <= end)
		{
			compatible.Add(AvifBinaryReader.ReadFourCc(data, offset));
			offset += 4;
		}

		return new AvifFtypBox { MajorBrand = majorBrand, CompatibleBrands = compatible };
	}
}
