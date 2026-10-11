namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Av1BlockTables
{
	public static readonly int[] MiWidthLog2 = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 0, 2, 1, 3, 2, 4];

	public static readonly int[] MiHeightLog2 = [0, 1, 0, 1, 2, 1, 2, 3, 2, 3, 4, 3, 4, 5, 4, 5, 2, 0, 3, 1, 4, 2];

	public static readonly int[] Num4x4BlocksWide = [1, 1, 2, 2, 2, 4, 4, 4, 8, 8, 8, 16, 16, 16, 32, 32, 1, 4, 2, 8, 4, 16];

	public static readonly int[] Num4x4BlocksHigh = [1, 2, 1, 2, 4, 2, 4, 8, 4, 8, 16, 8, 16, 32, 16, 32, 4, 1, 8, 2, 16, 4];

	public static int BlockWidth(int blockSize) => 4 * Num4x4BlocksWide[blockSize];

	public static int BlockHeight(int blockSize) => 4 * Num4x4BlocksHigh[blockSize];
	public static readonly int[] SizeGroup = [0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 0, 0, 1, 1, 2, 2];
	public static readonly int[] MaxTxSizeRect =
	[
		Av1TxSize.Tx4x4, Av1TxSize.Tx4x8, Av1TxSize.Tx8x4, Av1TxSize.Tx8x8,
		Av1TxSize.Tx8x16, Av1TxSize.Tx16x8, Av1TxSize.Tx16x16, Av1TxSize.Tx16x32,
		Av1TxSize.Tx32x16, Av1TxSize.Tx32x32, Av1TxSize.Tx32x64, Av1TxSize.Tx64x32,
		Av1TxSize.Tx64x64, Av1TxSize.Tx64x64, Av1TxSize.Tx64x64, Av1TxSize.Tx64x64,
		Av1TxSize.Tx4x16, Av1TxSize.Tx16x4, Av1TxSize.Tx8x32, Av1TxSize.Tx32x8,
		Av1TxSize.Tx16x64, Av1TxSize.Tx64x16,
	];
	public static readonly int[] SplitTxSize =
	[
		Av1TxSize.Tx4x4, // TX_4X4
		Av1TxSize.Tx4x4, // TX_8X8
		Av1TxSize.Tx8x8, // TX_16X16
		Av1TxSize.Tx16x16, // TX_32X32
		Av1TxSize.Tx32x32, // TX_64X64
		Av1TxSize.Tx4x4, // TX_4X8
		Av1TxSize.Tx4x4, // TX_8X4
		Av1TxSize.Tx8x8, // TX_8X16
		Av1TxSize.Tx8x8, // TX_16X8
		Av1TxSize.Tx16x16, // TX_16X32
		Av1TxSize.Tx16x16, // TX_32X16
		Av1TxSize.Tx32x32, // TX_32X64
		Av1TxSize.Tx32x32, // TX_64X32
		Av1TxSize.Tx4x8, // TX_4X16
		Av1TxSize.Tx8x4, // TX_16X4
		Av1TxSize.Tx8x16, // TX_8X32
		Av1TxSize.Tx16x8, // TX_32X8
		Av1TxSize.Tx16x32, // TX_16X64
		Av1TxSize.Tx32x16, // TX_64X16
	];
	public static readonly int[][] PartitionSubsize =
	[
		// PARTITION_NONE
		[Av1BlockSize.Block4x4, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block8x8, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block16x16, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block32x32, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block64x64, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block128x128, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid],
		// PARTITION_HORZ
		[Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block8x4, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block16x8, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block32x16, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block64x32, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block128x64, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid],
		// PARTITION_VERT
		[Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block4x8, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block8x16, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block16x32, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block32x64, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block64x128, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid],
		// PARTITION_SPLIT
		[Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block4x4, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block8x8, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block16x16, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block32x32, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block64x64, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid],
		// PARTITION_HORZ_A
		[Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block8x4, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block16x8, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block32x16, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block64x32, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block128x64, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid],
		// PARTITION_HORZ_B
		[Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block8x4, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block16x8, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block32x16, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block64x32, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block128x64, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid],
		// PARTITION_VERT_A
		[Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block4x8, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block8x16, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block16x32, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block32x64, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block64x128, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid],
		// PARTITION_VERT_B
		[Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block4x8, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block8x16, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block16x32, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block32x64, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block64x128, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid],
		// PARTITION_HORZ_4
		[Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block16x4, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block32x8, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block64x16, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid],
		// PARTITION_VERT_4
		[Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block4x16, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block8x32, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Block16x64, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid, Av1BlockSize.Invalid],
	];
	public static readonly int[] IntraModeContext = [0, 1, 2, 3, 4, 4, 4, 4, 3, 0, 1, 2, 0];
	public static readonly int[][][] SubsampledSize =
	[
		[[Av1BlockSize.Block4x4, Av1BlockSize.Block4x4], [Av1BlockSize.Block4x4, Av1BlockSize.Block4x4]],
		[[Av1BlockSize.Block4x8, Av1BlockSize.Block4x4], [Av1BlockSize.Invalid, Av1BlockSize.Block4x4]],
		[[Av1BlockSize.Block8x4, Av1BlockSize.Invalid], [Av1BlockSize.Block4x4, Av1BlockSize.Block4x4]],
		[[Av1BlockSize.Block8x8, Av1BlockSize.Block8x4], [Av1BlockSize.Block4x8, Av1BlockSize.Block4x4]],
		[[Av1BlockSize.Block8x16, Av1BlockSize.Block8x8], [Av1BlockSize.Invalid, Av1BlockSize.Block4x8]],
		[[Av1BlockSize.Block16x8, Av1BlockSize.Invalid], [Av1BlockSize.Block8x8, Av1BlockSize.Block8x4]],
		[[Av1BlockSize.Block16x16, Av1BlockSize.Block16x8], [Av1BlockSize.Block8x16, Av1BlockSize.Block8x8]],
		[[Av1BlockSize.Block16x32, Av1BlockSize.Block16x16], [Av1BlockSize.Invalid, Av1BlockSize.Block8x16]],
		[[Av1BlockSize.Block32x16, Av1BlockSize.Invalid], [Av1BlockSize.Block16x16, Av1BlockSize.Block16x8]],
		[[Av1BlockSize.Block32x32, Av1BlockSize.Block32x16], [Av1BlockSize.Block16x32, Av1BlockSize.Block16x16]],
		[[Av1BlockSize.Block32x64, Av1BlockSize.Block32x32], [Av1BlockSize.Invalid, Av1BlockSize.Block16x32]],
		[[Av1BlockSize.Block64x32, Av1BlockSize.Invalid], [Av1BlockSize.Block32x32, Av1BlockSize.Block32x16]],
		[[Av1BlockSize.Block64x64, Av1BlockSize.Block64x32], [Av1BlockSize.Block32x64, Av1BlockSize.Block32x32]],
		[[Av1BlockSize.Block64x128, Av1BlockSize.Block64x64], [Av1BlockSize.Invalid, Av1BlockSize.Block32x64]],
		[[Av1BlockSize.Block128x64, Av1BlockSize.Invalid], [Av1BlockSize.Block64x64, Av1BlockSize.Block64x32]],
		[[Av1BlockSize.Block128x128, Av1BlockSize.Block128x64], [Av1BlockSize.Block64x128, Av1BlockSize.Block64x64]],
		[[Av1BlockSize.Block4x16, Av1BlockSize.Block4x8], [Av1BlockSize.Invalid, Av1BlockSize.Block4x8]],
		[[Av1BlockSize.Block16x4, Av1BlockSize.Invalid], [Av1BlockSize.Block8x4, Av1BlockSize.Block8x4]],
		[[Av1BlockSize.Block8x32, Av1BlockSize.Block8x16], [Av1BlockSize.Invalid, Av1BlockSize.Block4x16]],
		[[Av1BlockSize.Block32x8, Av1BlockSize.Invalid], [Av1BlockSize.Block16x8, Av1BlockSize.Block16x4]],
		[[Av1BlockSize.Block16x64, Av1BlockSize.Block16x32], [Av1BlockSize.Invalid, Av1BlockSize.Block8x32]],
		[[Av1BlockSize.Block64x16, Av1BlockSize.Invalid], [Av1BlockSize.Block32x16, Av1BlockSize.Block32x8]],
	];
	public static int GetPlaneResidualSize(int subsize, int plane, bool subsamplingX, bool subsamplingY)
	{
		int subX = plane > 0 && subsamplingX ? 1 : 0;
		int subY = plane > 0 && subsamplingY ? 1 : 0;
		return SubsampledSize[subsize][subX][subY];
	}
}
