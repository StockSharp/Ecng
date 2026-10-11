namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal sealed class Av1FrameHeader
{
	public const int PrimaryRefNone = 7;

	public const int OnlyTx4x4 = 0;
	public const int TxModeLargest = 1;
	public const int TxModeSelect = 2;

	public int FrameWidth { get; init; }

	public int FrameHeight { get; init; }

	public int UpscaledWidth { get; init; }

	public int RenderWidth { get; init; }

	public int RenderHeight { get; init; }

	public int MiCols { get; init; }

	public int MiRows { get; init; }

	public bool AllowScreenContentTools { get; init; }

	public bool AllowIntrabc { get; init; }

	public int BaseQIdx { get; init; }

	public int DeltaQYDc { get; init; }

	public int DeltaQUDc { get; init; }

	public int DeltaQUAc { get; init; }

	public int DeltaQVDc { get; init; }

	public int DeltaQVAc { get; init; }

	public bool UsingQMatrix { get; init; }

	public int QmY { get; init; }

	public int QmU { get; init; }

	public int QmV { get; init; }

	public Av1SegmentationParams Segmentation { get; init; }

	public bool DeltaQPresent { get; init; }

	public int DeltaQRes { get; init; }

	public bool DeltaLfPresent { get; init; }

	public int DeltaLfRes { get; init; }

	public bool DeltaLfMulti { get; init; }

	public bool CodedLossless { get; init; }

	public bool AllLossless { get; init; }

	public Av1LoopFilterParams LoopFilter { get; init; }

	public Av1CdefParams Cdef { get; init; }

	public Av1LoopRestorationParams LoopRestoration { get; init; }

	public int TxMode { get; init; }

	public bool ReducedTxSet { get; init; }

	public Av1TileInfo TileInfo { get; init; }

	public bool DisableCdfUpdate { get; init; }

	public static Av1FrameHeader Parse(Av1BitReader reader, Av1SequenceHeader seq)
	{
		// reduced_still_picture_header == 1 forces: show_existing_frame=0, frame_type=KEY_FRAME,
		// FrameIsIntra=1, show_frame=1, showable_frame=0 -- no bits read for any of it.
		// Since frame_type==KEY_FRAME && show_frame: error_resilient_mode=1 (no bit read).
		bool disableCdfUpdate = reader.ReadFlag();

		// seq_force_screen_content_tools is always SELECT_SCREEN_CONTENT_TOOLS when reduced -- bit IS read.
		bool allowScreenContentTools = reader.ReadFlag();

		if (allowScreenContentTools)
		{
			// seq_force_integer_mv == SELECT_INTEGER_MV -- bit IS read, but FrameIsIntra unconditionally
			// forces force_integer_mv=1 afterward regardless of its value (spec order: the override happens
			// after the read, so the bit is consumed either way).
			reader.ReadFlag();
		}

		// frame_size_override_flag=0 when reduced (frame_type != SWITCH_FRAME) -- no bit read.
		// order_hint = f(OrderHintBits), OrderHintBits=0 when reduced -- f(0) reads nothing.
		// primary_ref_frame = PRIMARY_REF_NONE when FrameIsIntra -- no bit read.
		// refresh_frame_flags = allFrames when KEY_FRAME && show_frame -- no bit read.
		// FrameIsIntra=1 && refresh_frame_flags==allFrames -> the ref_order_hint loop is skipped entirely.

		// frame_size(): frame_size_override_flag==0 -> FrameWidth/FrameHeight come straight from the
		// sequence header's max_frame_width/height, no bits read for them here.
		int frameWidth = seq.MaxFrameWidth;
		int frameHeight = seq.MaxFrameHeight;

		// superres_params()
		bool useSuperres = seq.EnableSuperres && reader.ReadFlag();
		int upscaledWidth = frameWidth;
		if (useSuperres)
		{
			const int superresDenomBits = 3;
			const int superresDenomMin = 9;
			const int superresNum = 8;
			int codedDenom = (int)reader.ReadBits(superresDenomBits);
			int superresDenom = codedDenom + superresDenomMin;
			frameWidth = (upscaledWidth * superresNum + (superresDenom / 2)) / superresDenom;
		}

		int miCols = 2 * ((frameWidth + 7) >> 3);
		int miRows = 2 * ((frameHeight + 7) >> 3);

		// render_size()
		bool renderAndFrameSizeDifferent = reader.ReadFlag();
		int renderWidth, renderHeight;
		if (renderAndFrameSizeDifferent)
		{
			renderWidth = (int)reader.ReadBits(16) + 1;
			renderHeight = (int)reader.ReadBits(16) + 1;
		}
		else
		{
			renderWidth = upscaledWidth;
			renderHeight = frameHeight;
		}

		bool allowIntrabc = allowScreenContentTools && upscaledWidth == frameWidth && reader.ReadFlag();

		// Neither allow_screen_content_tools nor allow_intrabc is rejected here anymore:
		// palette_mode_info()/palette_tokens() and IntraBC's DV decode/prediction are both implemented (see
		// Av1TileDecoder's IntraFrameModeInfo/Residual and its ReadMv/FindMvStack/PredictIntrabc).

		// reduced_still_picture_header -> disable_frame_end_update_cdf=1, no bit read.
		// primary_ref_frame==PRIMARY_REF_NONE -> init_non_coeff_cdfs()+setup_past_independence(), no bits;
		// this is also where loop_filter_ref_deltas/mode_deltas get their defaults, applied below.

		var tileInfo = Av1TileInfo.Parse(reader, seq.Use128x128Superblock, miCols, miRows);

		int baseQIdx = (int)reader.ReadBits(8);
		int deltaQYDc = ReadDeltaQ(reader);
		int deltaQUDc = 0, deltaQUAc = 0, deltaQVDc = 0, deltaQVAc = 0;
		if (seq.NumPlanes > 1)
		{
			bool diffUvDelta = seq.SeparateUvDeltaQ && reader.ReadFlag();
			deltaQUDc = ReadDeltaQ(reader);
			deltaQUAc = ReadDeltaQ(reader);
			if (diffUvDelta)
			{
				deltaQVDc = ReadDeltaQ(reader);
				deltaQVAc = ReadDeltaQ(reader);
			}
			else
			{
				deltaQVDc = deltaQUDc;
				deltaQVAc = deltaQUAc;
			}
		}

		bool usingQMatrix = reader.ReadFlag();
		int qmY = 0, qmU = 0, qmV = 0;
		if (usingQMatrix)
		{
			qmY = (int)reader.ReadBits(4);
			qmU = (int)reader.ReadBits(4);
			qmV = seq.SeparateUvDeltaQ ? (int)reader.ReadBits(4) : qmU;

			// Quantizer matrices are an optional, rarely-enabled encoder feature (off by default in
			// libaom's own presets) that would otherwise require a large additional Quantizer_Matrix
			// table set for comparatively little real-world AVIF coverage -- rejected here, the earliest
			// point the bitstream confirms it's in use, once its own fields have been correctly consumed
			// to stay bitstream-synchronized.
			throw new AvifUnsupportedFeatureException("AV1 quantizer matrices (using_qmatrix) are not supported.");
		}

		var segmentation = ParseSegmentationParams(reader);

		bool deltaQPresent = baseQIdx > 0 && reader.ReadFlag();
		int deltaQRes = deltaQPresent ? (int)reader.ReadBits(2) : 0;

		bool deltaLfPresent = false;
		int deltaLfRes = 0;
		bool deltaLfMulti = false;
		if (deltaQPresent)
		{
			if (!allowIntrabc)
			{
				deltaLfPresent = reader.ReadFlag();
			}

			if (deltaLfPresent)
			{
				deltaLfRes = (int)reader.ReadBits(2);
				deltaLfMulti = reader.ReadFlag();
			}
		}

		// primary_ref_frame==PRIMARY_REF_NONE -> init_coeff_cdfs(), no bits read.

		bool codedLossless = true;
		for (int segmentId = 0; segmentId < Av1SegmentationParams.MaxSegments; segmentId++)
		{
			int qindex = GetQIndex(segmentation, segmentId, baseQIdx);
			bool lossless = qindex == 0 && deltaQYDc == 0 && deltaQUAc == 0 && deltaQUDc == 0 && deltaQVAc == 0 && deltaQVDc == 0;
			if (!lossless)
			{
				codedLossless = false;
			}
		}

		bool allLossless = codedLossless && frameWidth == upscaledWidth;

		var loopFilter = ParseLoopFilterParams(reader, codedLossless, allowIntrabc, seq.NumPlanes);
		var cdef = ParseCdefParams(reader, codedLossless, allowIntrabc, seq.EnableCdef);
		var loopRestoration = ParseLrParams(reader, allLossless, allowIntrabc, seq, cdef);

		int txMode;
		if (codedLossless)
		{
			txMode = OnlyTx4x4;
		}
		else
		{
			txMode = reader.ReadFlag() ? TxModeSelect : TxModeLargest;
		}

		// frame_reference_mode(): FrameIsIntra -> reference_select=0, no bit read.
		// skip_mode_params(): FrameIsIntra -> skipModeAllowed=0 -> skip_mode_present=0, no bit read.
		// FrameIsIntra -> allow_warped_motion=0, no bit read.
		bool reducedTxSet = reader.ReadFlag();

		// global_motion_params(): FrameIsIntra -> returns immediately after initializing GmType to IDENTITY
		// for every reference (no bits read).

		ParseFilmGrainApplyFlag(reader, seq.FilmGrainParamsPresent);

		return new Av1FrameHeader
		{
			FrameWidth = frameWidth,
			FrameHeight = frameHeight,
			UpscaledWidth = upscaledWidth,
			RenderWidth = renderWidth,
			RenderHeight = renderHeight,
			MiCols = miCols,
			MiRows = miRows,
			AllowScreenContentTools = allowScreenContentTools,
			AllowIntrabc = allowIntrabc,
			BaseQIdx = baseQIdx,
			DeltaQYDc = deltaQYDc,
			DeltaQUDc = deltaQUDc,
			DeltaQUAc = deltaQUAc,
			DeltaQVDc = deltaQVDc,
			DeltaQVAc = deltaQVAc,
			UsingQMatrix = usingQMatrix,
			QmY = qmY,
			QmU = qmU,
			QmV = qmV,
			Segmentation = segmentation,
			DeltaQPresent = deltaQPresent,
			DeltaQRes = deltaQRes,
			DeltaLfPresent = deltaLfPresent,
			DeltaLfRes = deltaLfRes,
			DeltaLfMulti = deltaLfMulti,
			CodedLossless = codedLossless,
			AllLossless = allLossless,
			LoopFilter = loopFilter,
			Cdef = cdef,
			LoopRestoration = loopRestoration,
			TxMode = txMode,
			ReducedTxSet = reducedTxSet,
			TileInfo = tileInfo,
			DisableCdfUpdate = disableCdfUpdate,
		};
	}
	private static void ParseFilmGrainApplyFlag(Av1BitReader reader, bool filmGrainParamsPresent)
	{
		if (!filmGrainParamsPresent)
		{
			return;
		}

		// show_frame=1 (always true here) so the "!show_frame && !showable_frame" early-out never applies.
		bool applyGrain = reader.ReadFlag();
		if (applyGrain)
		{
			throw new AvifUnsupportedFeatureException("AV1 film grain synthesis is not supported.");
		}
	}

	private static int ReadDeltaQ(Av1BitReader reader) => reader.ReadFlag() ? reader.ReadSu(7) : 0;
	private static int GetQIndex(Av1SegmentationParams segmentation, int segmentId, int baseQIdx)
	{
		if (segmentation.Enabled && segmentation.FeatureEnabled[segmentId, 0])
		{
			int data = segmentation.FeatureData[segmentId, 0];
			int qindex = baseQIdx + data;
			return Math.Clamp(qindex, 0, 255);
		}

		return baseQIdx;
	}

	private static Av1SegmentationParams ParseSegmentationParams(Av1BitReader reader)
	{
		var featureEnabled = new bool[Av1SegmentationParams.MaxSegments, Av1SegmentationParams.SegLvlMax];
		var featureData = new int[Av1SegmentationParams.MaxSegments, Av1SegmentationParams.SegLvlMax];

		ReadOnlySpan<int> featureBits = [8, 6, 6, 6, 6, 3, 0, 0];
		ReadOnlySpan<int> featureSigned = [1, 1, 1, 1, 1, 0, 0, 0];
		ReadOnlySpan<int> featureMax = [255, 63, 63, 63, 63, 7, 0, 0];

		bool enabled = reader.ReadFlag();
		if (enabled)
		{
			// primary_ref_frame==PRIMARY_REF_NONE (always true here) -> segmentation_update_data=1 forced,
			// no bits read for segmentation_update_map/segmentation_temporal_update/segmentation_update_data.
			for (int i = 0; i < Av1SegmentationParams.MaxSegments; i++)
			{
				for (int j = 0; j < Av1SegmentationParams.SegLvlMax; j++)
				{
					bool featureIsEnabled = reader.ReadFlag();
					featureEnabled[i, j] = featureIsEnabled;

					int clippedValue = 0;
					if (featureIsEnabled)
					{
						int bitsToRead = featureBits[j];
						int limit = featureMax[j];
						if (featureSigned[j] == 1)
						{
							int featureValue = reader.ReadSu(1 + bitsToRead);
							clippedValue = Math.Clamp(featureValue, -limit, limit);
						}
						else
						{
							int featureValue = (int)reader.ReadBits(bitsToRead);
							clippedValue = Math.Clamp(featureValue, 0, limit);
						}
					}

					featureData[i, j] = clippedValue;
				}
			}
		}

		bool segIdPreSkip = false;
		int lastActiveSegId = 0;
		for (int i = 0; i < Av1SegmentationParams.MaxSegments; i++)
		{
			for (int j = 0; j < Av1SegmentationParams.SegLvlMax; j++)
			{
				if (featureEnabled[i, j])
				{
					lastActiveSegId = i;
					if (j >= Av1SegmentationParams.SegLvlRefFrame)
					{
						segIdPreSkip = true;
					}
				}
			}
		}

		return new Av1SegmentationParams
		{
			Enabled = enabled,
			FeatureEnabled = featureEnabled,
			FeatureData = featureData,
			SegIdPreSkip = segIdPreSkip,
			LastActiveSegId = lastActiveSegId,
		};
	}

	private static Av1LoopFilterParams ParseLoopFilterParams(Av1BitReader reader, bool codedLossless, bool allowIntrabc, int numPlanes)
	{
		// setup_past_independence()'s defaults (always applied first: primary_ref_frame==PRIMARY_REF_NONE).
		int[] refDeltas = [1, 0, 0, 0, -1, 0, -1, -1]; // [INTRA, LAST, LAST2, LAST3, GOLDEN, BWDREF, ALTREF2, ALTREF]
		int[] modeDeltas = [0, 0];

		if (codedLossless || allowIntrabc)
		{
			return new Av1LoopFilterParams
			{
				Level = [0, 0, 0, 0],
				Sharpness = 0,
				DeltaEnabled = false,
				RefDeltas = refDeltas,
				ModeDeltas = modeDeltas,
			};
		}

		int level0 = (int)reader.ReadBits(6);
		int level1 = (int)reader.ReadBits(6);
		int level2 = 0, level3 = 0;
		if (numPlanes > 1 && (level0 != 0 || level1 != 0))
		{
			level2 = (int)reader.ReadBits(6);
			level3 = (int)reader.ReadBits(6);
		}

		int sharpness = (int)reader.ReadBits(3);
		bool deltaEnabled = reader.ReadFlag();
		if (deltaEnabled)
		{
			bool deltaUpdate = reader.ReadFlag();
			if (deltaUpdate)
			{
				for (int i = 0; i < 8; i++)
				{
					if (reader.ReadFlag())
					{
						refDeltas[i] = reader.ReadSu(7);
					}
				}

				for (int i = 0; i < 2; i++)
				{
					if (reader.ReadFlag())
					{
						modeDeltas[i] = reader.ReadSu(7);
					}
				}
			}
		}

		return new Av1LoopFilterParams
		{
			Level = [level0, level1, level2, level3],
			Sharpness = sharpness,
			DeltaEnabled = deltaEnabled,
			RefDeltas = refDeltas,
			ModeDeltas = modeDeltas,
		};
	}

	private static Av1CdefParams ParseCdefParams(Av1BitReader reader, bool codedLossless, bool allowIntrabc, bool enableCdef)
	{
		if (codedLossless || allowIntrabc || !enableCdef)
		{
			return new Av1CdefParams
			{
				Damping = 3,
				Bits = 0,
				YPriStrength = [0],
				YSecStrength = [0],
				UvPriStrength = [0],
				UvSecStrength = [0],
			};
		}

		int damping = (int)reader.ReadBits(2) + 3;
		int bits = (int)reader.ReadBits(2);
		int count = 1 << bits;

		var yPri = new int[count];
		var ySec = new int[count];
		var uvPri = new int[count];
		var uvSec = new int[count];

		for (int i = 0; i < count; i++)
		{
			yPri[i] = (int)reader.ReadBits(4);
			int ySecStrength = (int)reader.ReadBits(2);
			ySec[i] = ySecStrength == 3 ? ySecStrength + 1 : ySecStrength;

			uvPri[i] = (int)reader.ReadBits(4);
			int uvSecStrength = (int)reader.ReadBits(2);
			uvSec[i] = uvSecStrength == 3 ? uvSecStrength + 1 : uvSecStrength;
		}

		return new Av1CdefParams
		{
			Damping = damping,
			Bits = bits,
			YPriStrength = yPri,
			YSecStrength = ySec,
			UvPriStrength = uvPri,
			UvSecStrength = uvSec,
		};
	}

	private static Av1LoopRestorationParams ParseLrParams(Av1BitReader reader, bool allLossless, bool allowIntrabc, Av1SequenceHeader seq, Av1CdefParams cdef)
	{
		_ = cdef; // not needed here; kept as a parameter to make the call site's dependency ordering explicit

		if (allLossless || allowIntrabc || !seq.EnableRestoration)
		{
			return new Av1LoopRestorationParams
			{
				FrameRestorationType = [Av1LoopRestorationParams.RestoreNone, Av1LoopRestorationParams.RestoreNone, Av1LoopRestorationParams.RestoreNone],
				UsesLr = false,
				UnitSize = [0, 0, 0],
			};
		}

		ReadOnlySpan<int> remapLrType = [Av1LoopRestorationParams.RestoreNone, Av1LoopRestorationParams.RestoreSwitchable, Av1LoopRestorationParams.RestoreWiener, Av1LoopRestorationParams.RestoreSgrproj];

		var frameRestorationType = new int[3];
		bool usesLr = false;
		bool usesChromaLr = false;

		for (int i = 0; i < seq.NumPlanes; i++)
		{
			int lrType = (int)reader.ReadBits(2);
			frameRestorationType[i] = remapLrType[lrType];
			if (frameRestorationType[i] != Av1LoopRestorationParams.RestoreNone)
			{
				usesLr = true;
				if (i > 0)
				{
					usesChromaLr = true;
				}
			}
		}

		for (int i = seq.NumPlanes; i < 3; i++)
		{
			frameRestorationType[i] = Av1LoopRestorationParams.RestoreNone;
		}

		var unitSize = new int[3];
		if (usesLr)
		{
			const int restorationTileSizeMax = 256;
			int lrUnitShift;
			if (seq.Use128x128Superblock)
			{
				lrUnitShift = reader.ReadFlag() ? 1 : 0;
				lrUnitShift++;
			}
			else
			{
				lrUnitShift = reader.ReadFlag() ? 1 : 0;
				if (lrUnitShift != 0)
				{
					lrUnitShift += reader.ReadFlag() ? 1 : 0;
				}
			}

			unitSize[0] = restorationTileSizeMax >> (2 - lrUnitShift);

			int lrUvShift = 0;
			if (seq.SubsamplingX && seq.SubsamplingY && usesChromaLr)
			{
				lrUvShift = reader.ReadFlag() ? 1 : 0;
			}

			unitSize[1] = unitSize[0] >> lrUvShift;
			unitSize[2] = unitSize[0] >> lrUvShift;
		}

		return new Av1LoopRestorationParams
		{
			FrameRestorationType = frameRestorationType,
			UsesLr = usesLr,
			UnitSize = unitSize,
		};
	}
}
