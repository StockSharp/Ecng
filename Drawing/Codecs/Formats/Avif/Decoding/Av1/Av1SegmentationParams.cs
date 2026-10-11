namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal sealed class Av1SegmentationParams
{
	public const int MaxSegments = 8;
	public const int SegLvlMax = 8;
	public const int SegLvlRefFrame = 5;

	public bool Enabled { get; init; }
	public bool[,] FeatureEnabled { get; init; }
	public int[,] FeatureData { get; init; }

	public bool SegIdPreSkip { get; init; }

	public int LastActiveSegId { get; init; }
}
