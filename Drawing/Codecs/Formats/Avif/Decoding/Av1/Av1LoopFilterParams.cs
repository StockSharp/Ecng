namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal sealed class Av1LoopFilterParams
{
	public IReadOnlyList<int> Level { get; init; }

	public int Sharpness { get; init; }

	public bool DeltaEnabled { get; init; }
	public IReadOnlyList<int> RefDeltas { get; init; }

	public IReadOnlyList<int> ModeDeltas { get; init; }
}
