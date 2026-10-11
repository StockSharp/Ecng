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

internal static class Av1LoopRestoration
{
	private const int FilterBits = 7;
	private const int SgrprojPrjBits = 7;
	private const int SgrprojRstBits = 4;
	private const int SgrprojMtableBits = 20;
	private const int SgrprojRecipBits = 12;
	private const int SgrprojSgrBits = 8;

	public static void Apply(Av1FrameDecodeResult result, int[][] deblockedPlanes)
	{
		var frame = result.Frame;

		if (!frame.LoopRestoration.UsesLr)
		{
			return;
		}

		// Rented (explicit width*height, not Clone()'s implicit "copy the source's whole backing Length",
		// since that source may itself be a larger-than-requested rented array) rather than `new`'d.
		var lrPlanes = new int[3][];
		for (int plane = 0; plane < result.Sequence.NumPlanes; plane++)
		{
			int length = result.PlaneWidths[plane] * result.PlaneHeights[plane];
			lrPlanes[plane] = ArrayPool<int>.Shared.Rent(length);
			Array.Copy(result.Planes[plane], lrPlanes[plane], length);
		}

		var state = new State(result, deblockedPlanes, lrPlanes);

		for (int y = 0; y < frame.FrameHeight; y += 4)
		{
			for (int x = 0; x < frame.UpscaledWidth; x += 4)
			{
				for (int plane = 0; plane < result.Sequence.NumPlanes; plane++)
				{
					if (frame.LoopRestoration.FrameRestorationType[plane] == Av1LoopRestorationParams.RestoreNone)
					{
						continue;
					}

					int row = y >> 2;
					int col = x >> 2;
					LoopRestoreBlock(state, plane, row, col);
				}
			}
		}

		// deblockedPlanes (state.CurrPlanes) is read only by GetSourceSample calls within the loop above --
		// its life ends here. The array being replaced below (whatever Av1Cdef left behind, or the
		// original tile-decode reconstruction target if CDEF didn't run) is likewise dead once superseded.
		for (int plane = 0; plane < result.Sequence.NumPlanes; plane++)
		{
			ArrayPool<int>.Shared.Return(deblockedPlanes[plane]);
			ArrayPool<int>.Shared.Return(result.Planes[plane]);
			result.Planes[plane] = lrPlanes[plane];
		}
	}

	private sealed class State(Av1FrameDecodeResult result, int[][] currPlanes, int[][] lrPlanes)
	{
		public Av1FrameDecodeResult Result { get; } = result;

		public int[][] CurrPlanes { get; } = currPlanes;

		public int[][] CdefPlanes { get; } = result.Planes;

		public int[][] LrPlanes { get; } = lrPlanes;

		public int PlaneEndX { get; set; }

		public int PlaneEndY { get; set; }

		public int StripeStartY { get; set; }

		public int StripeEndY { get; set; }
	}
	private static void LoopRestoreBlock(State state, int plane, int row, int col)
	{
		var result = state.Result;
		var seq = result.Sequence;
		var frame = result.Frame;
		var grid = result.RestorationUnits[plane];
		if (grid is null)
		{
			return;
		}

		int lumaY = row * 4;
		int stripeNum = (lumaY + 8) / 64;

		int subX = plane != 0 && seq.SubsamplingX ? 1 : 0;
		int subY = plane != 0 && seq.SubsamplingY ? 1 : 0;

		state.StripeStartY = (-8 + (stripeNum * 64)) >> subY;
		state.StripeEndY = state.StripeStartY + (64 >> subY) - 1;

		int unitSize = grid.UnitSize;
		int unitRow = Math.Min(grid.UnitRows - 1, (((row * 4) + 8) >> subY) / unitSize);
		int unitCol = Math.Min(grid.UnitCols - 1, ((col * 4) >> subX) / unitSize);

		state.PlaneEndX = Round2(frame.UpscaledWidth, subX) - 1;
		state.PlaneEndY = Round2(frame.FrameHeight, subY) - 1;

		int x = (col * 4) >> subX;
		int y = (row * 4) >> subY;
		int w = Math.Min(4 >> subX, state.PlaneEndX - x + 1);
		int h = Math.Min(4 >> subY, state.PlaneEndY - y + 1);

		if (w <= 0 || h <= 0)
		{
			return;
		}

		int rType = grid.LrType[(unitRow * grid.UnitCols) + unitCol];

		if (rType == Av1LoopRestorationParams.RestoreWiener)
		{
			WienerFilter(state, plane, grid, unitRow, unitCol, x, y, w, h);
		}
		else if (rType == Av1LoopRestorationParams.RestoreSgrproj)
		{
			SelfGuidedFilter(state, plane, grid, unitRow, unitCol, x, y, w, h);
		}
	}

	private static int Round2(int x, int n) => n == 0 ? x : (x + (1 << (n - 1))) >> n;
	private static int GetSourceSample(State state, int plane, int x, int y)
	{
		x = Math.Clamp(x, 0, state.PlaneEndX);
		y = Math.Clamp(y, 0, state.PlaneEndY);

		int stride = state.Result.PlaneWidths[plane];

		if (y < state.StripeStartY)
		{
			y = Math.Max(state.StripeStartY - 2, y);
			return state.CurrPlanes[plane][(y * stride) + x];
		}

		if (y > state.StripeEndY)
		{
			y = Math.Min(state.StripeEndY + 2, y);
			return state.CurrPlanes[plane][(y * stride) + x];
		}

		return state.CdefPlanes[plane][(y * stride) + x];
	}
	private static void WienerFilter(State state, int plane, Av1RestorationUnitGrid grid, int unitRow, int unitCol, int x, int y, int w, int h)
	{
		var result = state.Result;
		int bitDepth = result.Sequence.BitDepth;

		int interRound0 = bitDepth == 12 ? 5 : 3;
		int interRound1 = bitDepth == 12 ? 9 : 11;

		int unitIdx = (unitRow * grid.UnitCols) + unitCol;
		var vfilter = WienerCoefficients(grid, unitIdx, 0);
		var hfilter = WienerCoefficients(grid, unitIdx, 1);

		int offset = 1 << (bitDepth + FilterBits - interRound0 - 1);
		int limit = (1 << (bitDepth + 1 + FilterBits - interRound0)) - 1;

		var intermediate = new int[h + 6, w];
		for (int r = 0; r < h + 6; r++)
		{
			for (int c = 0; c < w; c++)
			{
				long s = 0;
				for (int t = 0; t < 7; t++)
				{
					s += hfilter[t] * GetSourceSample(state, plane, x + c + t - 3, y + r - 3);
				}

				int v = Round2(s, interRound0);
				intermediate[r, c] = Math.Clamp(v, -offset, limit - offset);
			}
		}

		var lr = state.LrPlanes[plane];
		int stride = result.PlaneWidths[plane];
		int maxSample = (1 << bitDepth) - 1;

		for (int r = 0; r < h; r++)
		{
			for (int c = 0; c < w; c++)
			{
				long s = 0;
				for (int t = 0; t < 7; t++)
				{
					s += vfilter[t] * intermediate[r + t, c];
				}

				int v = Round2(s, interRound1);
				lr[((y + r) * stride) + x + c] = Math.Clamp(v, 0, maxSample);
			}
		}
	}

	private static int Round2(long x, int n) => n == 0 ? (int)x : (int)((x + (1L << (n - 1))) >> n);
	private static int[] WienerCoefficients(Av1RestorationUnitGrid grid, int unitIdx, int pass)
	{
		var filter = new int[7];
		filter[3] = 128;
		for (int i = 0; i < 3; i++)
		{
			int c = grid.LrWiener[(((unitIdx * 2) + pass) * 3) + i];
			filter[i] = c;
			filter[6 - i] = c;
			filter[3] -= 2 * c;
		}

		return filter;
	}
	private static void SelfGuidedFilter(State state, int plane, Av1RestorationUnitGrid grid, int unitRow, int unitCol, int x, int y, int w, int h)
	{
		var result = state.Result;
		int bitDepth = result.Sequence.BitDepth;
		int unitIdx = (unitRow * grid.UnitCols) + unitCol;

		int set = grid.LrSgrSet[unitIdx];
		var flt0 = BoxFilter(state, plane, x, y, w, h, set, 0);
		var flt1 = BoxFilter(state, plane, x, y, w, h, set, 1);

		int w0 = grid.LrSgrXqd[(unitIdx * 2) + 0];
		int w1 = grid.LrSgrXqd[(unitIdx * 2) + 1];
		int w2 = (1 << SgrprojPrjBits) - w0 - w1;
		int r0 = Av1SgrParams.Table[set][0];
		int r1 = Av1SgrParams.Table[set][2];

		var lr = state.LrPlanes[plane];
		int stride = result.PlaneWidths[plane];
		int maxSample = (1 << bitDepth) - 1;
		var cdef = state.CdefPlanes[plane];

		for (int i = 0; i < h; i++)
		{
			for (int j = 0; j < w; j++)
			{
				long u = (long)cdef[((y + i) * stride) + x + j] << SgrprojRstBits;
				long v = w1 * u;
				v += r0 != 0 ? (long)w0 * flt0![i, j] : w0 * u;
				v += r1 != 0 ? (long)w2 * flt1![i, j] : w2 * u;
				int s = Round2(v, SgrprojRstBits + SgrprojPrjBits);
				lr[((y + i) * stride) + x + j] = Math.Clamp(s, 0, maxSample);
			}
		}
	}
	private static int[,] BoxFilter(State state, int plane, int x, int y, int w, int h, int set, int pass)
	{
		int r = Av1SgrParams.Table[set][(pass * 2) + 0];
		if (r == 0)
		{
			return null;
		}

		int eps = Av1SgrParams.Table[set][(pass * 2) + 1];
		int bitDepth = state.Result.Sequence.BitDepth;

		int n = ((2 * r) + 1) * ((2 * r) + 1);
		long n2e = (long)n * n * eps;
		long s = ((1L << SgrprojMtableBits) + (n2e / 2)) / n2e;

		var a = new int[h + 2, w + 2];
		var b = new int[h + 2, w + 2];

		for (int i = -1; i < h + 1; i++)
		{
			for (int j = -1; j < w + 1; j++)
			{
				long aSum = 0;
				long bSum = 0;
				for (int dy = -r; dy <= r; dy++)
				{
					for (int dx = -r; dx <= r; dx++)
					{
						int c = GetSourceSample(state, plane, x + j + dx, y + i + dy);
						aSum += (long)c * c;
						bSum += c;
					}
				}

				long aRounded = Round2(aSum, 2 * (bitDepth - 8));
				long d = Round2(bSum, bitDepth - 8);
				long p = Math.Max(0, (aRounded * n) - (d * d));
				long z = Round2(p * s, SgrprojMtableBits);

				int a2;
				if (z >= 255)
				{
					a2 = 256;
				}
				else if (z == 0)
				{
					a2 = 1;
				}
				else
				{
					a2 = (int)((((z << SgrprojSgrBits) + (z / 2)) / (z + 1)));
				}

				long oneOverN = ((1L << SgrprojRecipBits) + (n / 2)) / n;
				long b2 = ((1L << SgrprojSgrBits) - a2) * bSum * oneOverN;

				a[i + 1, j + 1] = a2;
				b[i + 1, j + 1] = Round2(b2, SgrprojRecipBits);
			}
		}

		var f = new int[h, w];
		var cdef = state.CdefPlanes[plane];
		int stride = state.Result.PlaneWidths[plane];

		for (int i = 0; i < h; i++)
		{
			int shift = 5;
			if (pass == 0 && (i & 1) != 0)
			{
				shift = 4;
			}

			for (int j = 0; j < w; j++)
			{
				long aAcc = 0;
				long bAcc = 0;
				for (int dy = -1; dy <= 1; dy++)
				{
					for (int dx = -1; dx <= 1; dx++)
					{
						int weight;
						if (pass == 0)
						{
							weight = (i + dy) % 2 != 0 ? dx == 0 ? 6 : 5 : 0;
						}
						else
						{
							weight = dx == 0 || dy == 0 ? 4 : 3;
						}

						aAcc += weight * a[i + 1 + dy, j + 1 + dx];
						bAcc += weight * b[i + 1 + dy, j + 1 + dx];
					}
				}

				long v = (aAcc * cdef[((y + i) * stride) + x + j]) + bAcc;
				f[i, j] = Round2(v, SgrprojSgrBits + shift - SgrprojRstBits);
			}
		}

		return f;
	}
}
