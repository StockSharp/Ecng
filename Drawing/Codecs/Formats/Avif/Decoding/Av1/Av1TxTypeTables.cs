namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Av1TxTypeTables
{
	public static readonly int[] TxTypeIntraInvSet1 = [Av1TxType.Idtx, Av1TxType.DctDct, Av1TxType.VDct, Av1TxType.HDct, Av1TxType.AdstAdst, Av1TxType.AdstDct, Av1TxType.DctAdst];
	public static readonly int[] TxTypeIntraInvSet2 = [Av1TxType.Idtx, Av1TxType.DctDct, Av1TxType.AdstAdst, Av1TxType.AdstDct, Av1TxType.DctAdst];
	public static readonly int[] TxTypeInterInvSet1 =
		[
			Av1TxType.Idtx, Av1TxType.VDct, Av1TxType.HDct, Av1TxType.VAdst, Av1TxType.HAdst, Av1TxType.VFlipadst, Av1TxType.HFlipadst,
			Av1TxType.DctDct, Av1TxType.AdstDct, Av1TxType.DctAdst, Av1TxType.FlipadstDct, Av1TxType.DctFlipadst, Av1TxType.AdstAdst,
			Av1TxType.FlipadstFlipadst, Av1TxType.AdstFlipadst, Av1TxType.FlipadstAdst,
		];
	public static readonly int[] TxTypeInterInvSet3 = [Av1TxType.Idtx, Av1TxType.DctDct];
	public static readonly bool[][] TxTypeInSetIntra =
	[
		[true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false],
		[true, true, true, true, false, false, false, false, false, true, true, true, false, false, false, false],
		[true, true, true, true, false, false, false, false, false, true, false, false, false, false, false, false],
	];
	public static readonly int[] FilterIntraModeToIntraDir = [Av1IntraMode.DcPred, Av1IntraMode.VPred, Av1IntraMode.HPred, Av1IntraMode.D157Pred, Av1IntraMode.DcPred];
	public static readonly int[] ModeToTxfm =
	[
		Av1TxType.DctDct, // DC_PRED
		Av1TxType.AdstDct, // V_PRED
		Av1TxType.DctAdst, // H_PRED
		Av1TxType.DctDct, // D45_PRED
		Av1TxType.AdstAdst, // D135_PRED
		Av1TxType.AdstDct, // D113_PRED
		Av1TxType.DctAdst, // D157_PRED
		Av1TxType.DctAdst, // D203_PRED
		Av1TxType.AdstDct, // D67_PRED
		Av1TxType.AdstAdst, // SMOOTH_PRED
		Av1TxType.AdstDct, // SMOOTH_V_PRED
		Av1TxType.DctAdst, // SMOOTH_H_PRED
		Av1TxType.AdstAdst, // PAETH_PRED
		Av1TxType.DctDct, // UV_CFL_PRED
	];
}
