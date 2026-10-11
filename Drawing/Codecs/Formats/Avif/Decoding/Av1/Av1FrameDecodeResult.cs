namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Buffers;

internal sealed class Av1FrameDecodeResult
{
	public Av1SequenceHeader Sequence { get; init; }

	public Av1FrameHeader Frame { get; init; }
	public int BlocksDecoded { get; init; }
	public int TilesStarted { get; init; }
	public int[][] Planes { get; init; }

	public int[] PlaneWidths { get; init; }

	public int[] PlaneHeights { get; init; }
	public bool StoppedAtResidual { get; init; }
	public int[] YModes { get; init; }
	public int[] UvModes { get; init; }

	public int[] MiSizes { get; init; }

	public bool[] Skips { get; init; }

	public int[] SegmentIds { get; init; }
	public int[] PaletteSizesY { get; init; }

	public int[] PaletteSizesUV { get; init; }
	public int[] PaletteColorsYGrid { get; init; }

	public int[] PaletteColorsUGrid { get; init; }
	public bool[] IsInters { get; init; }

	public int[] MvRowsGrid { get; init; }

	public int[] MvColsGrid { get; init; }
	public int[] AngleDeltaYGrid { get; init; }

	public int[] AngleDeltaUvGrid { get; init; }
	public bool[] UseFilterIntraGrid { get; init; }

	public int[] FilterIntraModeGrid { get; init; }
	public int[][] DeltaLfs { get; init; }
	public int[][] LoopfilterTxSizes { get; init; }

	public int[] LoopfilterTxSizeStrides { get; init; }
	public int[] CdefIdx { get; init; }
	public Av1RestorationUnitGrid[] RestorationUnits { get; init; }
	public int MinSymbolMaxBitsAtExit { get; init; }
}
