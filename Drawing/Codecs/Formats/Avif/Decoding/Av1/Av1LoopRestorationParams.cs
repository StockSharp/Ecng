namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal sealed class Av1LoopRestorationParams
{
	public const int RestoreNone = 0;
	public const int RestoreWiener = 1;
	public const int RestoreSgrproj = 2;
	public const int RestoreSwitchable = 3;
	public IReadOnlyList<int> FrameRestorationType { get; init; }

	public bool UsesLr { get; init; }
	public IReadOnlyList<int> UnitSize { get; init; }
}
