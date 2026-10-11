namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal sealed class AvifColr
{
	public string ColourType { get; init; }

	public int ColourPrimaries { get; init; }

	public int TransferCharacteristics { get; init; }

	public int MatrixCoefficients { get; init; }

	public bool FullRange { get; init; }

	public byte[] IccProfile { get; init; }
}
