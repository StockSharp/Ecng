namespace Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class TiffTags
{
	public const ushort ImageWidth = 256;
	public const ushort ImageLength = 257;
	public const ushort BitsPerSample = 258;
	public const ushort Compression = 259;
	public const ushort PhotometricInterpretation = 262;
	public const ushort FillOrder = 266;
	public const ushort StripOffsets = 273;
	public const ushort Orientation = 274;
	public const ushort SamplesPerPixel = 277;
	public const ushort RowsPerStrip = 278;
	public const ushort StripByteCounts = 279;
	public const ushort PlanarConfiguration = 284;
	public const ushort Predictor = 317;
	public const ushort ColorMap = 320;
	public const ushort TileWidth = 322;
	public const ushort TileLength = 323;
	public const ushort TileOffsets = 324;
	public const ushort TileByteCounts = 325;
	public const ushort InkSet = 332;
	public const ushort ExtraSamples = 338;
	public const ushort SampleFormat = 339;
	public const ushort IccProfile = 34675;
}
