namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal sealed class Vp8LHuffmanGroup
{
	public Vp8LHuffmanTable Green { get; init; }

	public Vp8LHuffmanTable Red { get; init; }

	public Vp8LHuffmanTable Blue { get; init; }

	public Vp8LHuffmanTable Alpha { get; init; }

	public Vp8LHuffmanTable Distance { get; init; }
	public static Vp8LHuffmanGroup Read(Vp8LBitReader reader, int colorCacheBits)
	{
		int cacheSize = colorCacheBits > 0 ? 1 << colorCacheBits : 0;
		int greenAlphabetSize = 256 + 24 + cacheSize;

		var green = Vp8LCodeLengthReader.ReadHuffmanCode(reader, greenAlphabetSize);
		var red = Vp8LCodeLengthReader.ReadHuffmanCode(reader, 256);
		var blue = Vp8LCodeLengthReader.ReadHuffmanCode(reader, 256);
		var alpha = Vp8LCodeLengthReader.ReadHuffmanCode(reader, 256);
		var distance = Vp8LCodeLengthReader.ReadHuffmanCode(reader, Internal.WebpDecodingLimits.DistanceAlphabetSize);

		return new Vp8LHuffmanGroup { Green = green, Red = red, Blue = blue, Alpha = alpha, Distance = distance };
	}
}
