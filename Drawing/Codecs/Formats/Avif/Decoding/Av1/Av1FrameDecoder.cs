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

internal static class Av1FrameDecoder
{
	public static Av1DecodedFrame Decode(byte[] tileBytes, ImageProcessingLimits limits)
		=> Decode(tileBytes, limits, false);

	public static Av1DecodedFrame Decode(byte[] tileBytes, ImageProcessingLimits limits, bool thumbnail)
	{
		var parsed = ParseFrameHeader(tileBytes);
		if (!thumbnail) RasterImage.EnsureDecodable(parsed.Frame.FrameWidth, parsed.Frame.FrameHeight, limits);
		limits.EnsureWorkingMemory(EstimateBuffers(parsed).working);
		return DecodeTileGroup(tileBytes, parsed.Sequence, parsed.Frame, parsed.TileGroupOffset, parsed.TileGroupLength);
	}

	public static (long planes, long working, long retained) EstimateBuffers(byte[] tileBytes) => EstimateBuffers(ParseFrameHeader(tileBytes));

	private static (long planes, long working, long retained) EstimateBuffers(Av1ParsedFrameHeader parsed)
	{
		var step = parsed.Sequence.Use128x128Superblock ? 128 : 64;
		var width = (parsed.Frame.MiCols * 4 + step - 1) / step * step;
		var height = (parsed.Frame.MiRows * 4 + step - 1) / step * step;
		long planeBytes = 0;
		for (var plane = 0; plane < parsed.Sequence.NumPlanes; plane++)
		{
			var pixels = (long)(plane > 0 && parsed.Sequence.SubsamplingX ? width / 2 : width) *
				(plane > 0 && parsed.Sequence.SubsamplingY ? height / 2 : height);
			long capacity = 16;
			while (capacity < pixels) capacity <<= 1;
			planeBytes += capacity * 4;
		}
		var maps = (long)parsed.Frame.MiRows * parsed.Frame.MiCols * 160;
		return (planeBytes, planeBytes * 3 + maps, planeBytes);
	}
	public static Av1ParsedFrameHeader ParseFrameHeader(byte[] tileBytes)
	{
		var obus = Av1ObuReader.ReadObus(tileBytes, 0, tileBytes.Length);

		Av1SequenceHeader sequence = null;

		for (int i = 0; i < obus.Count; i++)
		{
			var obu = obus[i];

			if (obu.Type == Av1ObuType.SequenceHeader)
			{
				sequence = Av1SequenceHeader.Parse(new Av1BitReader(tileBytes, obu.PayloadOffset, obu.PayloadLength));
				continue;
			}

			if (obu.Type is Av1ObuType.Frame or Av1ObuType.FrameHeader)
			{
				if (sequence is null)
				{
					throw new AvifDecodingException("AV1 frame header OBU appeared before any sequence header OBU.");
				}

				var reader = new Av1BitReader(tileBytes, obu.PayloadOffset, obu.PayloadLength);
				var frame = Av1FrameHeader.Parse(reader, sequence);
				reader.ByteAlign();

				if (obu.Type == Av1ObuType.Frame)
				{
					// frame_obu(): frame_header_obu() + byte_alignment() + tile_group_obu(sz), all within
					// this one OBU's payload -- the tile group data starts right where the header parse
					// (now byte-aligned) left off.
					int tileGroupOffset = reader.BytePosition;
					int tileGroupLength = obu.PayloadOffset + obu.PayloadLength - tileGroupOffset;
					return new Av1ParsedFrameHeader(sequence, frame, tileGroupOffset, tileGroupLength);
				}

				// OBU_FRAME_HEADER: the tile group is a separate, later OBU_TILE_GROUP.
				for (int j = i + 1; j < obus.Count; j++)
				{
					if (obus[j].Type == Av1ObuType.TileGroup)
					{
						return new Av1ParsedFrameHeader(sequence, frame, obus[j].PayloadOffset, obus[j].PayloadLength);
					}
				}

				throw new AvifDecodingException("AV1 bitstream has a frame header OBU but no following tile group OBU.");
			}
		}

		throw new AvifDecodingException("AV1 bitstream is missing a sequence header or frame header OBU.");
	}
	private static Av1DecodedFrame DecodeTileGroup(byte[] data, Av1SequenceHeader sequence, Av1FrameHeader frame, int tileGroupOffset, int tileGroupLength)
	{
		var tileRanges = Av1TileGroupObu.Parse(data, tileGroupOffset, tileGroupLength, frame.TileInfo);

		int frameSize = frame.MiRows * frame.MiCols;
		var yModes = new int[frameSize];
		var uvModes = new int[frameSize];
		var miSizes = new int[frameSize];
		var skips = new bool[frameSize];
		var segmentIds = new int[frameSize];
		var interTxSizes = new int[frameSize];
		var paletteSizesY = new int[frameSize];
		var paletteSizesUV = new int[frameSize];
		var paletteColorsYGrid = new int[frameSize * 8];
		var paletteColorsUGrid = new int[frameSize * 8];
		var isInters = new bool[frameSize];
		var mvRowsGrid = new int[frameSize];
		var mvColsGrid = new int[frameSize];
		var written = new bool[frameSize];
		var angleDeltaYGrid = new int[frameSize];
		var angleDeltaUvGrid = new int[frameSize];
		var useFilterIntraGrid = new bool[frameSize];
		var filterIntraModeGrid = new int[frameSize];

		// Reconstructed-plane buffers are allocated to the superblock-aligned canvas, not just
		// MiCols*4/MiRows*4: a coding/transform block chosen by decode_partition() is only guaranteed to
		// START within [0, MiCols)x[0, MiRows), never to fully fit within it -- its nominal pixel extent
		// routinely overhangs into the superblock's padding region at the frame's right/bottom edge (the
		// same reason real AV1 decoders always over-allocate to superblock granularity). MiCols*4/MiRows*4
		// remain the correct bound for every spec-level maxX/maxY computation (edge-sample clamping,
		// transform_block's own early return) -- only the backing storage needs the larger size.
		int sbSizePixels = sequence.Use128x128Superblock ? 128 : 64;
		int alignedLumaW = ((frame.MiCols * 4) + sbSizePixels - 1) / sbSizePixels * sbSizePixels;
		int alignedLumaH = ((frame.MiRows * 4) + sbSizePixels - 1) / sbSizePixels * sbSizePixels;

		int numPlanes = sequence.NumPlanes;
		var planeWidths = new int[3];
		var planeHeights = new int[3];
		var planes = new int[3][];
		for (int plane = 0; plane < 3; plane++)
		{
			int subX = plane > 0 && sequence.SubsamplingX ? 1 : 0;
			int subY = plane > 0 && sequence.SubsamplingY ? 1 : 0;
			planeWidths[plane] = plane < numPlanes ? alignedLumaW >> subX : 0;
			planeHeights[plane] = plane < numPlanes ? alignedLumaH >> subY : 0;

			// Rented, not `new`'d, and explicitly cleared to match `new int[]`'s zero-init guarantee --
			// ArrayPool.Rent does not provide one. Every sample within [0, MiCols*4) x [0, MiRows*4) is
			// unconditionally written during tile decode (prediction always runs, even for skip blocks --
			// only the residual add is skipped), but the array itself is sized to the superblock-*aligned*
			// canvas, usually larger than that; a source tile smaller than a grid's declared output size
			// (a mismatched/reused tile item) can make Av1TileComposer.CopyRegion legitimately read into
			// that aligned padding, so it can't be left uninitialized. Ownership passes down the filter
			// pipeline via the same swap-and-return convention Av1Cdef/Av1LoopRestoration use for their own
			// output buffers; whichever generation survives the whole pipeline is finally returned by
			// Av1TileComposer.Composite once it's done reading from it.
			planes[plane] = ArrayPool<int>.Shared.Rent(planeWidths[plane] * planeHeights[plane]);
			Array.Clear(planes[plane], 0, planeWidths[plane] * planeHeights[plane]);
		}

		var deltaLfs = new int[4][];
		for (int i = 0; i < 4; i++)
		{
			deltaLfs[i] = new int[frameSize];
		}

		var cdefIdx = new int[frameSize];

		// Sized to the superblock-aligned mi grid (matching alignedLumaW/H above), not just
		// MiCols/MiRows: transform_block() writes LoopfilterTxSizes at the same (row>>subY, col>>subX)
		// positions its own overhang can reach beyond the frame's mi bounds -- see the plane-buffer
		// comment above for why that overhang is legitimate, not a bug.
		int alignedMiCols = alignedLumaW / 4;
		int alignedMiRows = alignedLumaH / 4;

		var loopfilterTxSizes = new int[3][];
		var loopfilterTxSizeStrides = new int[3];
		for (int plane = 0; plane < 3; plane++)
		{
			int subX = plane > 0 && sequence.SubsamplingX ? 1 : 0;
			int subY = plane > 0 && sequence.SubsamplingY ? 1 : 0;
			int strideCols = alignedMiCols >> subX;
			int strideRows = alignedMiRows >> subY;
			loopfilterTxSizeStrides[plane] = strideCols;
			loopfilterTxSizes[plane] = plane < numPlanes ? new int[strideCols * strideRows] : [];
		}

		var restorationUnits = new Av1RestorationUnitGrid[3];
		for (int plane = 0; plane < numPlanes; plane++)
		{
			if (frame.LoopRestoration.FrameRestorationType[plane] == Av1LoopRestorationParams.RestoreNone)
			{
				continue;
			}

			int subX = plane > 0 && sequence.SubsamplingX ? 1 : 0;
			int subY = plane > 0 && sequence.SubsamplingY ? 1 : 0;
			int unitSize = frame.LoopRestoration.UnitSize[plane];
			int frameSizeForRows = Round2(frame.FrameHeight, subY);
			int frameSizeForCols = Round2(frame.UpscaledWidth, subX);
			restorationUnits[plane] = Av1RestorationUnitGrid.Create(unitSize, frameSizeForRows, frameSizeForCols);
		}

		int blocksDecoded = 0;
		int tilesStarted = 0;
		bool stoppedAtResidual = false;
		int minSymbolMaxBitsAtExit = int.MaxValue;

		foreach (var tileRange in tileRanges)
		{
			tilesStarted++;

			int miRowStart = frame.TileInfo.MiRowStarts[tileRange.TileRow];
			int miRowEnd = frame.TileInfo.MiRowStarts[tileRange.TileRow + 1];
			int miColStart = frame.TileInfo.MiColStarts[tileRange.TileCol];
			int miColEnd = frame.TileInfo.MiColStarts[tileRange.TileCol + 1];

			var cdf = new Av1CdfContext(frame.BaseQIdx);
			var symbols = new Av1SymbolDecoder(data, tileRange.Offset, tileRange.Length, frame.DisableCdfUpdate);
			var tileDecoder = new Av1TileDecoder(
				symbols, cdf, sequence, frame, miRowStart, miRowEnd, miColStart, miColEnd,
				yModes, uvModes, miSizes, skips, segmentIds, interTxSizes,
				paletteSizesY, paletteSizesUV, paletteColorsYGrid, paletteColorsUGrid,
				isInters, mvRowsGrid, mvColsGrid, written,
				planes, planeWidths, planeHeights,
				deltaLfs, cdefIdx, loopfilterTxSizes, loopfilterTxSizeStrides, restorationUnits,
				angleDeltaYGrid, angleDeltaUvGrid, useFilterIntraGrid, filterIntraModeGrid);

			tileDecoder.DecodeTile();

			blocksDecoded += tileDecoder.BlocksDecoded;
			stoppedAtResidual |= tileDecoder.StoppedAtResidual;
			minSymbolMaxBitsAtExit = Math.Min(minSymbolMaxBitsAtExit, tileDecoder.SymbolMaxBitsAtExit);
		}

		var result = new Av1FrameDecodeResult
		{
			Sequence = sequence,
			Frame = frame,
			BlocksDecoded = blocksDecoded,
			TilesStarted = tilesStarted,
			StoppedAtResidual = stoppedAtResidual,
			MinSymbolMaxBitsAtExit = minSymbolMaxBitsAtExit,
			Planes = planes,
			PlaneWidths = planeWidths,
			PlaneHeights = planeHeights,
			YModes = yModes,
			UvModes = uvModes,
			MiSizes = miSizes,
			Skips = skips,
			SegmentIds = segmentIds,
			DeltaLfs = deltaLfs,
			LoopfilterTxSizes = loopfilterTxSizes,
			LoopfilterTxSizeStrides = loopfilterTxSizeStrides,
			CdefIdx = cdefIdx,
			RestorationUnits = restorationUnits,
			PaletteSizesY = paletteSizesY,
			PaletteSizesUV = paletteSizesUV,
			PaletteColorsYGrid = paletteColorsYGrid,
			PaletteColorsUGrid = paletteColorsUGrid,
			IsInters = isInters,
			MvRowsGrid = mvRowsGrid,
			MvColsGrid = mvColsGrid,
			AngleDeltaYGrid = angleDeltaYGrid,
			AngleDeltaUvGrid = angleDeltaUvGrid,
			UseFilterIntraGrid = useFilterIntraGrid,
			FilterIntraModeGrid = filterIntraModeGrid,
		};

		Av1DeblockingFilter.Apply(result);

		// Only loop restoration ever reads this pre-CDEF snapshot (spec §7.17.1's stripe-boundary rule);
		// Av1LoopRestoration.Apply returns immediately when UsesLr is false without touching it, so
		// cloning three full planes (~12MB at 1080p) here would be pure waste for the (common) no-LR case.
		// Rented rather than Clone()'d (which would copy an oversized pooled source's full backing
		// Length); Av1LoopRestoration.Apply owns returning this once it's done reading from it.
		var deblockedPlanes = frame.LoopRestoration.UsesLr ? new int[3][] : [];
		if (frame.LoopRestoration.UsesLr)
		{
			for (int plane = 0; plane < numPlanes; plane++)
			{
				int length = planeWidths[plane] * planeHeights[plane];
				deblockedPlanes[plane] = ArrayPool<int>.Shared.Rent(length);
				Array.Copy(result.Planes[plane], deblockedPlanes[plane], length);
			}
		}

		Av1Cdef.Apply(result);
		Av1LoopRestoration.Apply(result, deblockedPlanes);

		return new Av1DecodedFrame(sequence, frame, result.Planes, planeWidths, planeHeights);
	}

	private static int Round2(int x, int n) => n == 0 ? x : (x + (1 << (n - 1))) >> n;
}
