namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using Ecng.Drawing.Codecs.Formats.Avif.Internal;

internal sealed class AvifResolvedImageItem
{
	public int Width { get; init; }

	public int Height { get; init; }

	public int BitDepth { get; init; }

	public bool Monochrome { get; init; }

	public bool ChromaSubsamplingX { get; init; }

	public bool ChromaSubsamplingY { get; init; }

	public IReadOnlyList<byte[]> Tiles { get; init; }

	public int GridRows { get; init; }

	public int GridColumns { get; init; }

	public AvifColr ColorInfo { get; init; }
}
