namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal sealed class Av1CdfContext
{
	public Av1CdfContext(int baseQIdx)
	{
		// init_coeff_cdfs() (spec §6.8.21 semantics): coefficient CDFs are seeded from one of 4
		// quantizer-indexed default slices, chosen once per frame from base_q_idx (not the per-block,
		// delta-adjusted CurrentQIndex).
		int idx = baseQIdx <= 20 ? 0 : baseQIdx <= 60 ? 1 : baseQIdx <= 120 ? 2 : 3;

		TxbSkip = Clone(Av1CdfTables.DefaultTxbSkip[idx]);
		EobPt16 = Clone(Av1CdfTables.DefaultEobPt16[idx]);
		EobPt32 = Clone(Av1CdfTables.DefaultEobPt32[idx]);
		EobPt64 = Clone(Av1CdfTables.DefaultEobPt64[idx]);
		EobPt128 = Clone(Av1CdfTables.DefaultEobPt128[idx]);
		EobPt256 = Clone(Av1CdfTables.DefaultEobPt256[idx]);
		EobPt512 = Clone(Av1CdfTables.DefaultEobPt512[idx]);
		EobPt1024 = Clone(Av1CdfTables.DefaultEobPt1024[idx]);
		EobExtra = Clone(Av1CdfTables.DefaultEobExtra[idx]);
		DcSign = Clone(Av1CdfTables.DefaultDcSign[idx]);
		CoeffBaseEob = Clone(Av1CdfTables.DefaultCoeffBaseEob[idx]);
		CoeffBase = Clone(Av1CdfTables.DefaultCoeffBase[idx]);
		CoeffBr = Clone(Av1CdfTables.DefaultCoeffBr[idx]);
	}

	public readonly ushort[][][] TxbSkip;
	public readonly ushort[][][] EobPt16;
	public readonly ushort[][][] EobPt32;
	public readonly ushort[][][] EobPt64;
	public readonly ushort[][][] EobPt128;
	public readonly ushort[][][] EobPt256;
	public readonly ushort[][] EobPt512;
	public readonly ushort[][] EobPt1024;
	public readonly ushort[][][][] EobExtra;
	public readonly ushort[][][] DcSign;
	public readonly ushort[][][][] CoeffBaseEob;
	public readonly ushort[][][][] CoeffBase;
	public readonly ushort[][][][] CoeffBr;

	public readonly ushort[][][] IntraFrameYMode = Clone(Av1CdfTables.DefaultIntraFrameYMode);
	public readonly ushort[][] UvModeCflNotAllowed = Clone(Av1CdfTables.DefaultUvModeCflNotAllowed);
	public readonly ushort[][] UvModeCflAllowed = Clone(Av1CdfTables.DefaultUvModeCflAllowed);
	public readonly ushort[][] AngleDelta = Clone(Av1CdfTables.DefaultAngleDelta);
	public readonly ushort[][] PartitionW8 = Clone(Av1CdfTables.DefaultPartitionW8);
	public readonly ushort[][] PartitionW16 = Clone(Av1CdfTables.DefaultPartitionW16);
	public readonly ushort[][] PartitionW32 = Clone(Av1CdfTables.DefaultPartitionW32);
	public readonly ushort[][] PartitionW64 = Clone(Av1CdfTables.DefaultPartitionW64);
	public readonly ushort[][] PartitionW128 = Clone(Av1CdfTables.DefaultPartitionW128);
	public readonly ushort[][] SegmentId = Clone(Av1CdfTables.DefaultSegmentId);
	public readonly ushort[][] Tx8x8 = Clone(Av1CdfTables.DefaultTx8x8);
	public readonly ushort[][] Tx16x16 = Clone(Av1CdfTables.DefaultTx16x16);
	public readonly ushort[][] Tx32x32 = Clone(Av1CdfTables.DefaultTx32x32);
	public readonly ushort[][] Tx64x64 = Clone(Av1CdfTables.DefaultTx64x64);
	public readonly ushort[] FilterIntraMode = Clone(Av1CdfTables.DefaultFilterIntraMode);
	public readonly ushort[][] FilterIntra = Clone(Av1CdfTables.DefaultFilterIntra);
	public readonly ushort[][] Skip = Clone(Av1CdfTables.DefaultSkip);
	public readonly ushort[] DeltaQ = Clone(Av1CdfTables.DefaultDeltaQ);
	public readonly ushort[] DeltaLf = Clone(Av1CdfTables.DefaultDeltaLf);
	public readonly ushort[][] DeltaLfMulti = [Clone(Av1CdfTables.DefaultDeltaLf), Clone(Av1CdfTables.DefaultDeltaLf), Clone(Av1CdfTables.DefaultDeltaLf), Clone(Av1CdfTables.DefaultDeltaLf)];
	public readonly ushort[] CflSign = Clone(Av1CdfTables.DefaultCflSign);
	public readonly ushort[][] CflAlpha = Clone(Av1CdfTables.DefaultCflAlpha);
	public readonly ushort[][][] IntraTxTypeSet1 = Clone(Av1CdfTables.DefaultIntraTxTypeSet1);
	public readonly ushort[][][] IntraTxTypeSet2 = Clone(Av1CdfTables.DefaultIntraTxTypeSet2);
	public readonly ushort[][] InterTxTypeSet1 = Clone(Av1CdfTables.DefaultInterTxTypeSet1);
	public readonly ushort[][] InterTxTypeSet3 = Clone(Av1CdfTables.DefaultInterTxTypeSet3);
	public readonly ushort[] UseWiener = Clone(Av1CdfTables.DefaultUseWiener);
	public readonly ushort[] UseSgrproj = Clone(Av1CdfTables.DefaultUseSgrproj);
	public readonly ushort[] RestorationType = Clone(Av1CdfTables.DefaultRestorationType);

	public readonly ushort[][] PaletteYSize = Clone(Av1CdfTables.DefaultPaletteYSize);
	public readonly ushort[][] PaletteUvSize = Clone(Av1CdfTables.DefaultPaletteUvSize);
	public readonly ushort[][][] PaletteYMode = Clone(Av1CdfTables.DefaultPaletteYMode);
	public readonly ushort[][] PaletteUvMode = Clone(Av1CdfTables.DefaultPaletteUvMode);
	public readonly ushort[][][] PaletteYColorIndex = Clone(Av1CdfTables.DefaultPaletteYColorIndex);
	public readonly ushort[][][] PaletteUvColorIndex = Clone(Av1CdfTables.DefaultPaletteUvColorIndex);

	// MV/IntraBC CDFs. As explained on the Av1CdfTables default tables these come from, the spec's
	// MV_CONTEXTS context dimension is omitted (always MV_INTRABC_CONTEXT here); the comp dimension (0=row,
	// 1=col) is kept since read_mv_component(comp) adapts each independently even where the two start from
	// identical default values. mv_class0_fr/mv_class0_hp/mv_fr/mv_hp have no CDFs here at all: reduced
	// still_picture_header forces force_integer_mv=1 unconditionally for FrameIsIntra (spec order: the
	// override happens after seq_force_integer_mv's own bit is read, so this isn't a bitstream choice),
	// and read_mv_component's own syntax never reads those symbols when force_integer_mv is set -- so
	// unlike MvClass/MvClass0Bit/MvBit/MvSign, which the DV magnitude/sign always need, these four would be
	// allocated, cloned, and adapted for a code path that can provably never run. Mirrors the
	// IsAboveInter/IsLeftInter=false simplification elsewhere in this decoder.
	public readonly ushort[] Intrabc = Clone(Av1CdfTables.DefaultIntrabc);
	public readonly ushort[] MvJoint = Clone(Av1CdfTables.DefaultMvJoint);
	public readonly ushort[][] MvClass = Clone(Av1CdfTables.DefaultMvClass);
	public readonly ushort[][] MvClass0Bit = [Clone(Av1CdfTables.DefaultMvClass0Bit), Clone(Av1CdfTables.DefaultMvClass0Bit)];
	public readonly ushort[][] MvSign = [Clone(Av1CdfTables.DefaultMvSign), Clone(Av1CdfTables.DefaultMvSign)];
	public readonly ushort[][][] MvBit = [Clone(Av1CdfTables.DefaultMvBit), Clone(Av1CdfTables.DefaultMvBit)];
	public void CopyFrom(Av1CdfContext other)
	{
		Copy(TxbSkip, other.TxbSkip);
		Copy(EobPt16, other.EobPt16);
		Copy(EobPt32, other.EobPt32);
		Copy(EobPt64, other.EobPt64);
		Copy(EobPt128, other.EobPt128);
		Copy(EobPt256, other.EobPt256);
		Copy(EobPt512, other.EobPt512);
		Copy(EobPt1024, other.EobPt1024);
		Copy(EobExtra, other.EobExtra);
		Copy(DcSign, other.DcSign);
		Copy(CoeffBaseEob, other.CoeffBaseEob);
		Copy(CoeffBase, other.CoeffBase);
		Copy(CoeffBr, other.CoeffBr);
	}

	private static void Copy(ushort[] dest, ushort[] source) => Array.Copy(source, dest, source.Length);

	private static void Copy(ushort[][] dest, ushort[][] source)
	{
		for (int i = 0; i < source.Length; i++)
		{
			Copy(dest[i], source[i]);
		}
	}

	private static void Copy(ushort[][][] dest, ushort[][][] source)
	{
		for (int i = 0; i < source.Length; i++)
		{
			Copy(dest[i], source[i]);
		}
	}

	private static void Copy(ushort[][][][] dest, ushort[][][][] source)
	{
		for (int i = 0; i < source.Length; i++)
		{
			Copy(dest[i], source[i]);
		}
	}

	private static ushort[] Clone(ushort[] source) => (ushort[])source.Clone();

	private static ushort[][] Clone(ushort[][] source)
	{
		var result = new ushort[source.Length][];
		for (int i = 0; i < source.Length; i++)
		{
			result[i] = Clone(source[i]);
		}

		return result;
	}

	private static ushort[][][] Clone(ushort[][][] source)
	{
		var result = new ushort[source.Length][][];
		for (int i = 0; i < source.Length; i++)
		{
			result[i] = Clone(source[i]);
		}

		return result;
	}

	private static ushort[][][][] Clone(ushort[][][][] source)
	{
		var result = new ushort[source.Length][][][];
		for (int i = 0; i < source.Length; i++)
		{
			result[i] = Clone(source[i]);
		}

		return result;
	}
}
