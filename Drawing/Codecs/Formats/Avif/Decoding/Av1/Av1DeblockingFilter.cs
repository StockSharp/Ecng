namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Av1DeblockingFilter
{
	private const int MaxLoopFilter = 63;
	private const int SegLvlAltLfYV = 1;

	public static void Apply(Av1FrameDecodeResult result)
	{
		var seq = result.Sequence;
		var frame = result.Frame;
		var level = frame.LoopFilter.Level;

		for (int plane = 0; plane < seq.NumPlanes; plane++)
		{
			if (plane != 0 && level[1 + plane] == 0)
			{
				continue;
			}

			for (int pass = 0; pass < 2; pass++)
			{
				bool chroma = plane != 0;
				int subX = chroma && seq.SubsamplingX ? 1 : 0;
				int subY = chroma && seq.SubsamplingY ? 1 : 0;
				int rowStep = plane == 0 ? 1 : 1 << subY;
				int colStep = plane == 0 ? 1 : 1 << subX;

				for (int row = 0; row < frame.MiRows; row += rowStep)
				{
					for (int col = 0; col < frame.MiCols; col += colStep)
					{
						LoopFilterEdge(result, plane, pass, row, col);
					}
				}
			}
		}
	}
	private static void LoopFilterEdge(Av1FrameDecodeResult result, int plane, int pass, int row, int col)
	{
		var seq = result.Sequence;
		var frame = result.Frame;

		int subX = plane != 0 && seq.SubsamplingX ? 1 : 0;
		int subY = plane != 0 && seq.SubsamplingY ? 1 : 0;

		int dx = pass == 0 ? 1 : 0;
		int dy = pass == 0 ? 0 : 1;

		int x = col * 4;
		int y = row * 4;

		row |= subY;
		col |= subX;

		bool onScreen;
		if (x >= frame.FrameWidth)
		{
			onScreen = false;
		}
		else if (y >= frame.FrameHeight)
		{
			onScreen = false;
		}
		else if (pass == 0 && x == 0)
		{
			onScreen = false;
		}
		else if (pass == 1 && y == 0)
		{
			onScreen = false;
		}
		else
		{
			onScreen = true;
		}

		if (!onScreen)
		{
			return;
		}

		int xP = x >> subX;
		int yP = y >> subY;

		int prevRow = row - (dy << subY);
		int prevCol = col - (dx << subX);

		int miCols = frame.MiCols;
		int lfStride = result.LoopfilterTxSizeStrides[plane];
		int txSz = result.LoopfilterTxSizes[plane][((row >> subY) * lfStride) + (col >> subX)];
		int prevTxSz = result.LoopfilterTxSizes[plane][((prevRow >> subY) * lfStride) + (prevCol >> subX)];

		// applyFilter (spec §7.14.2) reduces to isTxEdge here: isIntra is always true for this
		// intra-only decoder, and applyFilter is 1 whenever isTxEdge && (isBlockEdge || !skip ||
		// isIntra) -- with isIntra always true, the isBlockEdge/skip terms never change the result, so
		// isBlockEdge (and the MiSizes/Skips reads it would need) is never computed.
		bool isTxEdge;
		if (pass == 0 && xP % Av1TxDimensions.Width[txSz] == 0)
		{
			isTxEdge = true;
		}
		else if (pass == 1 && yP % Av1TxDimensions.Height[txSz] == 0)
		{
			isTxEdge = true;
		}
		else
		{
			isTxEdge = false;
		}

		// isIntra is always true (intra-only decoder), so applyFilter reduces to isTxEdge && (isBlockEdge || !skip || true) == isTxEdge.
		bool applyFilter = isTxEdge;
		if (!applyFilter)
		{
			return;
		}

		int filterSize = FilterSize(txSz, prevTxSz, pass, plane);

		var (lvl, limit, blimit, thresh) = AdaptiveFilterStrength(result, row, col, plane, pass);
		if (lvl == 0)
		{
			(lvl, limit, blimit, thresh) = AdaptiveFilterStrength(result, prevRow, prevCol, plane, pass);
		}

		if (lvl == 0)
		{
			return;
		}

		for (int i = 0; i < 4; i++)
		{
			SampleFilter(result, plane, xP + (dy * i), yP + (dx * i), limit, blimit, thresh, dx, dy, filterSize);
		}
	}
	private static int FilterSize(int txSz, int prevTxSz, int pass, int plane)
	{
		int baseSize = pass == 0
			? Math.Min(Av1TxDimensions.Width[prevTxSz], Av1TxDimensions.Width[txSz])
			: Math.Min(Av1TxDimensions.Height[prevTxSz], Av1TxDimensions.Height[txSz]);

		return plane == 0 ? Math.Min(16, baseSize) : Math.Min(8, baseSize);
	}
	private static (int Lvl, int Limit, int Blimit, int Thresh) AdaptiveFilterStrength(Av1FrameDecodeResult result, int row, int col, int plane, int pass)
	{
		var frame = result.Frame;
		int idx = (row * frame.MiCols) + col;
		int segment = result.SegmentIds[idx];
		int i = plane == 0 ? pass : plane + 1;
		int deltaLf = frame.DeltaLfMulti ? result.DeltaLfs[i][idx] : result.DeltaLfs[0][idx];

		int baseFilterLevel = Math.Clamp(deltaLf + frame.LoopFilter.Level[i], 0, MaxLoopFilter);
		int lvlSeg = baseFilterLevel;
		int feature = SegLvlAltLfYV + i;
		if (frame.Segmentation.Enabled && frame.Segmentation.FeatureEnabled[segment, feature])
		{
			lvlSeg = Math.Clamp(frame.Segmentation.FeatureData[segment, feature] + lvlSeg, 0, MaxLoopFilter);
		}

		if (frame.LoopFilter.DeltaEnabled)
		{
			int nShift = lvlSeg >> 5;
			lvlSeg = Math.Clamp(lvlSeg + (frame.LoopFilter.RefDeltas[0] << nShift), 0, MaxLoopFilter);
		}

		int lvl = lvlSeg;
		int sharpness = frame.LoopFilter.Sharpness;
		int shift = sharpness > 4 ? 2 : sharpness > 0 ? 1 : 0;
		int limit = sharpness > 0 ? Math.Clamp(lvl >> shift, 1, 9 - sharpness) : Math.Max(1, lvl >> shift);
		int blimit = (2 * (lvl + 2)) + limit;
		int thresh = lvl >> 4;

		return (lvl, limit, blimit, thresh);
	}
	private static void SampleFilter(Av1FrameDecodeResult result, int plane, int x, int y, int limit, int blimit, int thresh, int dx, int dy, int filterSize)
	{
		var (hevMask, filterMask, flatMask, flatMask2) = FilterMask(result, plane, x, y, limit, blimit, thresh, dx, dy, filterSize);
		if (!filterMask)
		{
			return;
		}

		if (filterSize == 4 || !flatMask)
		{
			NarrowFilter(result, hevMask, x, y, plane, dx, dy);
		}
		else if (filterSize == 8 || !flatMask2)
		{
			WideFilter(result, x, y, plane, dx, dy, 3);
		}
		else
		{
			WideFilter(result, x, y, plane, dx, dy, 4);
		}
	}
	private static (bool HevMask, bool FilterMask, bool FlatMask, bool FlatMask2) FilterMask(Av1FrameDecodeResult result, int plane, int x, int y, int limit, int blimit, int thresh, int dx, int dy, int filterSize)
	{
		int bitDepth = result.Sequence.BitDepth;
		var buf = result.Planes[plane];
		int stride = result.PlaneWidths[plane];

		int Sample(int offset) => buf[((y + (dy * offset)) * stride) + x + (dx * offset)];

		int q0 = Sample(0);
		int q1 = Sample(1);
		int q2 = Sample(2);
		int q3 = Sample(3);
		int p0 = Sample(-1);
		int p1 = Sample(-2);
		int p2 = Sample(-3);
		int p3 = Sample(-4);
		int q4 = 0, q5 = 0, q6 = 0, p4 = 0, p5 = 0, p6 = 0;
		if (filterSize == 16)
		{
			q4 = Sample(4);
			q5 = Sample(5);
			q6 = Sample(6);
			p4 = Sample(-5);
			p5 = Sample(-6);
			p6 = Sample(-7);
		}

		int threshBd = thresh << (bitDepth - 8);
		bool hevMask = Math.Abs(p1 - p0) > threshBd || Math.Abs(q1 - q0) > threshBd;

		int filterLen = filterSize == 4 ? 4 : plane != 0 ? 6 : filterSize == 8 ? 8 : 16;

		int limitBd = limit << (bitDepth - 8);
		int blimitBd = blimit << (bitDepth - 8);
		bool mask = Math.Abs(p1 - p0) > limitBd
			|| Math.Abs(q1 - q0) > limitBd
			|| (Math.Abs(p0 - q0) * 2) + (Math.Abs(p1 - q1) / 2) > blimitBd;

		if (filterLen >= 6)
		{
			mask = mask || Math.Abs(p2 - p1) > limitBd || Math.Abs(q2 - q1) > limitBd;
		}

		if (filterLen >= 8)
		{
			mask = mask || Math.Abs(p3 - p2) > limitBd || Math.Abs(q3 - q2) > limitBd;
		}

		bool filterMask = !mask;

		bool flatMask = false;
		if (filterSize >= 8)
		{
			int thresholdBd = 1 << (bitDepth - 8);
			bool m = Math.Abs(p1 - p0) > thresholdBd
				|| Math.Abs(q1 - q0) > thresholdBd
				|| Math.Abs(p2 - p0) > thresholdBd
				|| Math.Abs(q2 - q0) > thresholdBd;

			if (filterLen >= 8)
			{
				m = m || Math.Abs(p3 - p0) > thresholdBd || Math.Abs(q3 - q0) > thresholdBd;
			}

			flatMask = !m;
		}

		bool flatMask2 = false;
		if (filterSize >= 16)
		{
			int thresholdBd = 1 << (bitDepth - 8);
			bool m = Math.Abs(p6 - p0) > thresholdBd
				|| Math.Abs(q6 - q0) > thresholdBd
				|| Math.Abs(p5 - p0) > thresholdBd
				|| Math.Abs(q5 - q0) > thresholdBd
				|| Math.Abs(p4 - p0) > thresholdBd
				|| Math.Abs(q4 - q0) > thresholdBd;
			flatMask2 = !m;
		}

		return (hevMask, filterMask, flatMask, flatMask2);
	}
	private static void NarrowFilter(Av1FrameDecodeResult result, bool hevMask, int x, int y, int plane, int dx, int dy)
	{
		int bitDepth = result.Sequence.BitDepth;
		var buf = result.Planes[plane];
		int stride = result.PlaneWidths[plane];

		int Idx(int offset) => ((y + (dy * offset)) * stride) + x + (dx * offset);

		int q0 = buf[Idx(0)];
		int q1 = buf[Idx(1)];
		int p0 = buf[Idx(-1)];
		int p1 = buf[Idx(-2)];

		int bound = 1 << (bitDepth - 1);
		int Clamp(int v) => Math.Clamp(v, -bound, bound - 1);

		int bias = 0x80 << (bitDepth - 8);
		int ps1 = p1 - bias;
		int ps0 = p0 - bias;
		int qs0 = q0 - bias;
		int qs1 = q1 - bias;

		int filter = hevMask ? Clamp(ps1 - qs1) : 0;
		filter = Clamp(filter + (3 * (qs0 - ps0)));
		int filter1 = Clamp(filter + 4) >> 3;
		int filter2 = Clamp(filter + 3) >> 3;

		buf[Idx(0)] = Clamp(qs0 - filter1) + bias;
		buf[Idx(-1)] = Clamp(ps0 + filter2) + bias;

		if (!hevMask)
		{
			int f = Round2(filter1, 1);
			buf[Idx(1)] = Clamp(qs1 - f) + bias;
			buf[Idx(-2)] = Clamp(ps1 + f) + bias;
		}
	}
	private static void WideFilter(Av1FrameDecodeResult result, int x, int y, int plane, int dx, int dy, int log2Size)
	{
		var buf = result.Planes[plane];
		int stride = result.PlaneWidths[plane];
		int Idx(int offset) => ((y + (dy * offset)) * stride) + x + (dx * offset);

		int n = log2Size == 4 ? 6 : plane == 0 ? 3 : 2;
		int n2 = log2Size == 3 && plane == 0 ? 0 : 1;

		var f = new int[2 * n];
		for (int i = -n; i < n; i++)
		{
			long t = 0;
			for (int j = -n; j <= n; j++)
			{
				int p = Math.Clamp(i + j, -(n + 1), n);
				int tap = Math.Abs(j) <= n2 ? 2 : 1;
				t += (long)buf[Idx(p)] * tap;
			}

			f[i + n] = Round2(t, log2Size);
		}

		for (int i = -n; i < n; i++)
		{
			buf[Idx(i)] = f[i + n];
		}
	}

	private static int Round2(long x, int n) => n == 0 ? (int)x : (int)((x + (1L << (n - 1))) >> n);
}
