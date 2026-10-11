namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Av1CoeffTables
{
	public const int NumBaseLevels = 2;
	public const int CoeffBaseRange = 12;
	public const int BrCdfSize = 4;
	public const int SigCoefContextsEob = 4;
	public const int SigCoefContexts2D = 26;
	public const int SigCoefContexts = 42;
	public const int SigRefDiffOffsetNum = 5;
	public static readonly int[] TxSizeSqr =
	[
		Av1TxSize.Tx4x4, Av1TxSize.Tx8x8, Av1TxSize.Tx16x16, Av1TxSize.Tx32x32, Av1TxSize.Tx64x64,
		Av1TxSize.Tx4x4, Av1TxSize.Tx4x4, Av1TxSize.Tx8x8, Av1TxSize.Tx8x8, Av1TxSize.Tx16x16,
		Av1TxSize.Tx16x16, Av1TxSize.Tx32x32, Av1TxSize.Tx32x32, Av1TxSize.Tx4x4, Av1TxSize.Tx4x4,
		Av1TxSize.Tx8x8, Av1TxSize.Tx8x8, Av1TxSize.Tx16x16, Av1TxSize.Tx16x16,
	];
	public static readonly int[] TxSizeSqrUp =
	[
		Av1TxSize.Tx4x4, Av1TxSize.Tx8x8, Av1TxSize.Tx16x16, Av1TxSize.Tx32x32, Av1TxSize.Tx64x64,
		Av1TxSize.Tx8x8, Av1TxSize.Tx8x8, Av1TxSize.Tx16x16, Av1TxSize.Tx16x16, Av1TxSize.Tx32x32,
		Av1TxSize.Tx32x32, Av1TxSize.Tx64x64, Av1TxSize.Tx64x64, Av1TxSize.Tx16x16, Av1TxSize.Tx16x16,
		Av1TxSize.Tx32x32, Av1TxSize.Tx32x32, Av1TxSize.Tx64x64, Av1TxSize.Tx64x64,
	];
	public static readonly int[] AdjustedTxSize =
	[
		Av1TxSize.Tx4x4, Av1TxSize.Tx8x8, Av1TxSize.Tx16x16, Av1TxSize.Tx32x32, Av1TxSize.Tx32x32,
		Av1TxSize.Tx4x8, Av1TxSize.Tx8x4, Av1TxSize.Tx8x16, Av1TxSize.Tx16x8, Av1TxSize.Tx16x32,
		Av1TxSize.Tx32x16, Av1TxSize.Tx32x32, Av1TxSize.Tx32x32, Av1TxSize.Tx4x16, Av1TxSize.Tx16x4,
		Av1TxSize.Tx8x32, Av1TxSize.Tx32x8, Av1TxSize.Tx16x32, Av1TxSize.Tx32x16,
	];
	public static readonly int[][][] SigRefDiffOffset =
	[
		[[0, 1], [1, 0], [1, 1], [0, 2], [2, 0]], // TX_CLASS_2D
		[[0, 1], [1, 0], [0, 2], [0, 3], [0, 4]], // TX_CLASS_HORIZ
		[[0, 1], [1, 0], [2, 0], [3, 0], [4, 0]], // TX_CLASS_VERT
	];
	public static readonly int[][][] MagRefOffsetWithTxClass =
	[
		[[0, 1], [1, 0], [1, 1]], // TX_CLASS_2D
		[[0, 1], [1, 0], [0, 2]], // TX_CLASS_HORIZ
		[[0, 1], [1, 0], [2, 0]], // TX_CLASS_VERT
	];
	public static readonly int[] CoeffBasePosCtxOffset = [SigCoefContexts2D, SigCoefContexts2D + 5, SigCoefContexts2D + 10];
	public static readonly int[][][] CoeffBaseCtxOffset =
	[
		// TX_4X4
		[[0, 1, 6, 6, 0], [1, 6, 6, 21, 0], [6, 6, 21, 21, 0], [6, 21, 21, 21, 0], [0, 0, 0, 0, 0]],
		// TX_8X8
		[[0, 1, 6, 6, 21], [1, 6, 6, 21, 21], [6, 6, 21, 21, 21], [6, 21, 21, 21, 21], [21, 21, 21, 21, 21]],
		// TX_16X16
		[[0, 1, 6, 6, 21], [1, 6, 6, 21, 21], [6, 6, 21, 21, 21], [6, 21, 21, 21, 21], [21, 21, 21, 21, 21]],
		// TX_32X32
		[[0, 1, 6, 6, 21], [1, 6, 6, 21, 21], [6, 6, 21, 21, 21], [6, 21, 21, 21, 21], [21, 21, 21, 21, 21]],
		// TX_64X64
		[[0, 1, 6, 6, 21], [1, 6, 6, 21, 21], [6, 6, 21, 21, 21], [6, 21, 21, 21, 21], [21, 21, 21, 21, 21]],
		// TX_4X8
		[[0, 11, 11, 11, 0], [11, 11, 11, 11, 0], [6, 6, 21, 21, 0], [6, 21, 21, 21, 0], [21, 21, 21, 21, 0]],
		// TX_8X4
		[[0, 16, 6, 6, 21], [16, 16, 6, 21, 21], [16, 16, 21, 21, 21], [16, 16, 21, 21, 21], [0, 0, 0, 0, 0]],
		// TX_8X16
		[[0, 11, 11, 11, 11], [11, 11, 11, 11, 11], [6, 6, 21, 21, 21], [6, 21, 21, 21, 21], [21, 21, 21, 21, 21]],
		// TX_16X8
		[[0, 16, 6, 6, 21], [16, 16, 6, 21, 21], [16, 16, 21, 21, 21], [16, 16, 21, 21, 21], [16, 16, 21, 21, 21]],
		// TX_16X32
		[[0, 11, 11, 11, 11], [11, 11, 11, 11, 11], [6, 6, 21, 21, 21], [6, 21, 21, 21, 21], [21, 21, 21, 21, 21]],
		// TX_32X16
		[[0, 16, 6, 6, 21], [16, 16, 6, 21, 21], [16, 16, 21, 21, 21], [16, 16, 21, 21, 21], [16, 16, 21, 21, 21]],
		// TX_32X64
		[[0, 11, 11, 11, 11], [11, 11, 11, 11, 11], [6, 6, 21, 21, 21], [6, 21, 21, 21, 21], [21, 21, 21, 21, 21]],
		// TX_64X32
		[[0, 16, 6, 6, 21], [16, 16, 6, 21, 21], [16, 16, 21, 21, 21], [16, 16, 21, 21, 21], [16, 16, 21, 21, 21]],
		// TX_4X16
		[[0, 11, 11, 11, 0], [11, 11, 11, 11, 0], [6, 6, 21, 21, 0], [6, 21, 21, 21, 0], [21, 21, 21, 21, 0]],
		// TX_16X4
		[[0, 16, 6, 6, 21], [16, 16, 6, 21, 21], [16, 16, 21, 21, 21], [16, 16, 21, 21, 21], [0, 0, 0, 0, 0]],
		// TX_8X32
		[[0, 11, 11, 11, 11], [11, 11, 11, 11, 11], [6, 6, 21, 21, 21], [6, 21, 21, 21, 21], [21, 21, 21, 21, 21]],
		// TX_32X8
		[[0, 16, 6, 6, 21], [16, 16, 6, 21, 21], [16, 16, 21, 21, 21], [16, 16, 21, 21, 21], [16, 16, 21, 21, 21]],
		// TX_16X64
		[[0, 11, 11, 11, 11], [11, 11, 11, 11, 11], [6, 6, 21, 21, 21], [6, 21, 21, 21, 21], [21, 21, 21, 21, 21]],
		// TX_64X16
		[[0, 16, 6, 6, 21], [16, 16, 6, 21, 21], [16, 16, 21, 21, 21], [16, 16, 21, 21, 21], [16, 16, 21, 21, 21]],
	];
}
