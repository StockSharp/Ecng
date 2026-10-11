namespace Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal sealed record TiffImageDescriptor
{
	public int Width { get; init; }

	public int Height { get; init; }
	public int BitsPerSample { get; init; }

	public int SamplesPerPixel { get; init; }
	public int Compression { get; init; }
	public int Photometric { get; init; }
	public int Predictor { get; init; }

	public uint RowsPerStrip { get; init; }

	public uint[] StripOffsets { get; init; }

	public uint[] StripByteCounts { get; init; }
	public bool HasAlpha { get; init; }
	public bool AlphaIsPremultiplied { get; init; }
	public uint[] ColorMap { get; init; }
	public PixelFormat PixelFormat { get; init; }
}
