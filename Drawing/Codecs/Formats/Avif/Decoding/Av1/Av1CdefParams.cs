namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal sealed class Av1CdefParams
{
	public int Damping { get; init; }

	public int Bits { get; init; }

	public IReadOnlyList<int> YPriStrength { get; init; }

	public IReadOnlyList<int> YSecStrength { get; init; }

	public IReadOnlyList<int> UvPriStrength { get; init; }

	public IReadOnlyList<int> UvSecStrength { get; init; }
}
