namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Av1IntraPrediction
{
	private const int IntraEdgeTaps = 5;
	private const int IntraFilterScaleBits = 4;

	private static readonly int[] SmWeights4 = [255, 149, 85, 64];
	private static readonly int[] SmWeights8 = [255, 197, 146, 105, 73, 50, 37, 32];
	private static readonly int[] SmWeights16 = [255, 225, 196, 170, 145, 123, 102, 84, 68, 54, 43, 33, 26, 20, 17, 16];
	private static readonly int[] SmWeights32 = [255, 240, 225, 210, 196, 182, 169, 157, 145, 133, 122, 111, 101, 92, 83, 74, 66, 59, 52, 45, 39, 34, 29, 25, 21, 17, 14, 12, 10, 9, 8, 8];
	private static readonly int[] SmWeights64 = [255, 248, 240, 233, 225, 218, 210, 203, 196, 189, 182, 176, 169, 163, 156, 150, 144, 138, 133, 127, 121, 116, 111, 106, 101, 96, 91, 86, 82, 77, 73, 69, 65, 61, 57, 54, 50, 47, 44, 41, 38, 35, 32, 29, 27, 25, 22, 20, 18, 16, 15, 13, 12, 10, 9, 8, 7, 6, 6, 5, 5, 4, 4, 4];
	private static readonly int[] ModeToAngle = [0, 90, 180, 45, 135, 113, 157, 203, 67, 0, 0, 0, 0];
	private static readonly int[] DrIntraDerivative =
	[
		0, 0, 0, 1023, 0, 0, 547, 0, 0, 372, 0, 0, 0, 0,
		273, 0, 0, 215, 0, 0, 178, 0, 0, 151, 0, 0, 132, 0, 0,
		116, 0, 0, 102, 0, 0, 0, 90, 0, 0, 80, 0, 0, 71, 0, 0,
		64, 0, 0, 57, 0, 0, 51, 0, 0, 45, 0, 0, 0, 40, 0, 0,
		35, 0, 0, 31, 0, 0, 27, 0, 0, 23, 0, 0, 19, 0, 0,
		15, 0, 0, 0, 0, 11, 0, 0, 7, 0, 0, 3, 0, 0,
	];
	private static readonly int[][] IntraEdgeKernel =
	[
		[0, 4, 8, 4, 0],
		[0, 5, 6, 5, 0],
		[2, 4, 4, 4, 2],
	];
	private static readonly int[][][] IntraFilterTaps =
	[
		[
			[-6, 10, 0, 0, 0, 12, 0],
			[-5, 2, 10, 0, 0, 9, 0],
			[-3, 1, 1, 10, 0, 7, 0],
			[-3, 1, 1, 2, 10, 5, 0],
			[-4, 6, 0, 0, 0, 2, 12],
			[-3, 2, 6, 0, 0, 2, 9],
			[-3, 2, 2, 6, 0, 2, 7],
			[-3, 1, 2, 2, 6, 3, 5],
		],
		[
			[-10, 16, 0, 0, 0, 10, 0],
			[-6, 0, 16, 0, 0, 6, 0],
			[-4, 0, 0, 16, 0, 4, 0],
			[-2, 0, 0, 0, 16, 2, 0],
			[-10, 16, 0, 0, 0, 0, 10],
			[-6, 0, 16, 0, 0, 0, 6],
			[-4, 0, 0, 16, 0, 0, 4],
			[-2, 0, 0, 0, 16, 0, 2],
		],
		[
			[-8, 8, 0, 0, 0, 16, 0],
			[-8, 0, 8, 0, 0, 16, 0],
			[-8, 0, 0, 8, 0, 16, 0],
			[-8, 0, 0, 0, 8, 16, 0],
			[-4, 4, 0, 0, 0, 0, 16],
			[-4, 0, 4, 0, 0, 0, 16],
			[-4, 0, 0, 4, 0, 0, 16],
			[-4, 0, 0, 0, 4, 0, 16],
		],
		[
			[-2, 8, 0, 0, 0, 10, 0],
			[-1, 3, 8, 0, 0, 6, 0],
			[-1, 2, 3, 8, 0, 4, 0],
			[0, 1, 2, 3, 8, 2, 0],
			[-1, 4, 0, 0, 0, 3, 10],
			[-1, 3, 4, 0, 0, 4, 6],
			[-1, 2, 3, 4, 0, 4, 4],
			[-1, 2, 2, 3, 4, 3, 3],
		],
		[
			[-12, 14, 0, 0, 0, 14, 0],
			[-10, 0, 14, 0, 0, 12, 0],
			[-9, 0, 0, 14, 0, 11, 0],
			[-8, 0, 0, 0, 14, 10, 0],
			[-10, 12, 0, 0, 0, 0, 14],
			[-9, 1, 12, 0, 0, 0, 12],
			[-8, 0, 0, 12, 0, 1, 11],
			[-7, 0, 0, 1, 12, 1, 9],
		],
	];

	private static int[] SmWeightsForLog2(int log2) => log2 switch
	{
		2 => SmWeights4,
		3 => SmWeights8,
		4 => SmWeights16,
		5 => SmWeights32,
		_ => SmWeights64,
	};

	private static int Round2(long x, int n) => n == 0 ? (int)x : (int)((x + (1L << (n - 1))) >> n);

	private static int Round2Signed(long x, int n) => x >= 0 ? Round2(x, n) : -Round2(-x, n);

	private static int Clip1(int x, int bitDepth) => Math.Clamp(x, 0, (1 << bitDepth) - 1);
	public static void BuildEdges(
		Av1EdgeArray above,
		Av1EdgeArray left,
		int[] plane,
		int planeStride,
		int x,
		int y,
		int w,
		int h,
		bool haveLeft,
		bool haveAbove,
		bool haveAboveRight,
		bool haveBelowLeft,
		int maxX,
		int maxY,
		int bitDepth)
	{
		if (!haveAbove && haveLeft)
		{
			int v = plane[(y * planeStride) + x - 1];
			for (int i = 0; i < w + h; i++)
			{
				above[i] = v;
			}
		}
		else if (!haveAbove)
		{
			int v = (1 << (bitDepth - 1)) - 1;
			for (int i = 0; i < w + h; i++)
			{
				above[i] = v;
			}
		}
		else
		{
			int aboveLimit = Math.Min(maxX, x + (haveAboveRight ? 2 * w : w) - 1);
			for (int i = 0; i < w + h; i++)
			{
				above[i] = plane[((y - 1) * planeStride) + Math.Min(aboveLimit, x + i)];
			}
		}

		if (!haveLeft && haveAbove)
		{
			int v = plane[((y - 1) * planeStride) + x];
			for (int i = 0; i < w + h; i++)
			{
				left[i] = v;
			}
		}
		else if (!haveLeft)
		{
			int v = (1 << (bitDepth - 1)) + 1;
			for (int i = 0; i < w + h; i++)
			{
				left[i] = v;
			}
		}
		else
		{
			int leftLimit = Math.Min(maxY, y + (haveBelowLeft ? 2 * h : h) - 1);
			for (int i = 0; i < w + h; i++)
			{
				left[i] = plane[(Math.Min(leftLimit, y + i) * planeStride) + x - 1];
			}
		}

		int corner;
		if (haveAbove && haveLeft)
		{
			corner = plane[((y - 1) * planeStride) + x - 1];
		}
		else if (haveAbove)
		{
			corner = plane[((y - 1) * planeStride) + x];
		}
		else if (haveLeft)
		{
			corner = plane[(y * planeStride) + x - 1];
		}
		else
		{
			corner = 1 << (bitDepth - 1);
		}

		above[-1] = corner;
		left[-1] = corner;
	}
	public static void Predict(
		int[] pred,
		int w,
		int h,
		int log2W,
		int log2H,
		Av1EdgeArray aboveRow,
		Av1EdgeArray leftCol,
		int mode,
		bool haveLeft,
		bool haveAbove,
		bool useFilterIntra,
		int filterIntraMode,
		int angleDelta,
		bool enableIntraEdgeFilter,
		bool filterTypeSmooth,
		int maxX,
		int maxY,
		int x,
		int y,
		int bitDepth)
	{
		if (useFilterIntra)
		{
			PredictRecursive(pred, w, h, aboveRow, leftCol, filterIntraMode, bitDepth);
		}
		else if (Av1IntraMode.IsDirectional(mode))
		{
			PredictDirectional(pred, w, h, aboveRow, leftCol, mode, angleDelta, haveLeft, haveAbove, enableIntraEdgeFilter, filterTypeSmooth, maxX, maxY, x, y, bitDepth);
		}
		else if (mode is Av1IntraMode.SmoothPred or Av1IntraMode.SmoothVPred or Av1IntraMode.SmoothHPred)
		{
			PredictSmooth(pred, w, h, log2W, log2H, aboveRow, leftCol, mode);
		}
		else if (mode == Av1IntraMode.DcPred)
		{
			PredictDc(pred, w, h, log2W, log2H, aboveRow, leftCol, haveLeft, haveAbove, bitDepth);
		}
		else
		{
			PredictPaeth(pred, w, h, aboveRow, leftCol);
		}
	}
	private static void PredictPaeth(int[] pred, int w, int h, Av1EdgeArray aboveRow, Av1EdgeArray leftCol)
	{
		for (int i = 0; i < h; i++)
		{
			for (int j = 0; j < w; j++)
			{
				int baseVal = aboveRow[j] + leftCol[i] - aboveRow[-1];
				int pLeft = Math.Abs(baseVal - leftCol[i]);
				int pTop = Math.Abs(baseVal - aboveRow[j]);
				int pTopLeft = Math.Abs(baseVal - aboveRow[-1]);

				pred[(i * w) + j] = pLeft <= pTop && pLeft <= pTopLeft ? leftCol[i] : pTop <= pTopLeft ? aboveRow[j] : aboveRow[-1];
			}
		}
	}
	private static void PredictRecursive(int[] pred, int w, int h, Av1EdgeArray aboveRow, Av1EdgeArray leftCol, int filterIntraMode, int bitDepth)
	{
		int w4 = w >> 2;
		int h2 = h >> 1;
		Span<int> p = stackalloc int[7];

		for (int i2 = 0; i2 < h2; i2++)
		{
			for (int j4 = 0; j4 < w4; j4++)
			{
				for (int i = 0; i < 7; i++)
				{
					if (i < 5)
					{
						if (i2 == 0)
						{
							p[i] = aboveRow[(j4 << 2) + i - 1];
						}
						else if (j4 == 0 && i == 0)
						{
							p[i] = leftCol[(i2 << 1) - 1];
						}
						else
						{
							p[i] = pred[(((i2 << 1) - 1) * w) + (j4 << 2) + i - 1];
						}
					}
					else if (j4 == 0)
					{
						p[i] = leftCol[(i2 << 1) + i - 5];
					}
					else
					{
						p[i] = pred[(((i2 << 1) + i - 5) * w) + (j4 << 2) - 1];
					}
				}

				var taps = IntraFilterTaps[filterIntraMode];
				for (int i1 = 0; i1 <= 1; i1++)
				{
					for (int j1 = 0; j1 <= 3; j1++)
					{
						long pr = 0;
						var row = taps[(i1 << 2) + j1];
						for (int i = 0; i < 7; i++)
						{
							pr += (long)row[i] * p[i];
						}

						pred[(((i2 << 1) + i1) * w) + (j4 << 2) + j1] = Clip1(Round2Signed(pr, IntraFilterScaleBits), bitDepth);
					}
				}
			}
		}
	}
	private static void PredictDc(int[] pred, int w, int h, int log2W, int log2H, Av1EdgeArray aboveRow, Av1EdgeArray leftCol, bool haveLeft, bool haveAbove, int bitDepth)
	{
		int avg;
		if (haveLeft && haveAbove)
		{
			long sum = 0;
			for (int k = 0; k < h; k++)
			{
				sum += leftCol[k];
			}

			for (int k = 0; k < w; k++)
			{
				sum += aboveRow[k];
			}

			sum += (w + h) >> 1;
			avg = (int)(sum / (w + h));
		}
		else if (haveLeft)
		{
			long sum = 0;
			for (int k = 0; k < h; k++)
			{
				sum += leftCol[k];
			}

			avg = Clip1(Round2(sum, log2H), bitDepth);
		}
		else if (haveAbove)
		{
			long sum = 0;
			for (int k = 0; k < w; k++)
			{
				sum += aboveRow[k];
			}

			avg = Clip1(Round2(sum, log2W), bitDepth);
		}
		else
		{
			avg = 1 << (bitDepth - 1);
		}

		for (int i = 0; i < h; i++)
		{
			for (int j = 0; j < w; j++)
			{
				pred[(i * w) + j] = avg;
			}
		}
	}
	private static void PredictSmooth(int[] pred, int w, int h, int log2W, int log2H, Av1EdgeArray aboveRow, Av1EdgeArray leftCol, int mode)
	{
		if (mode == Av1IntraMode.SmoothPred)
		{
			var smWeightsX = SmWeightsForLog2(log2W);
			var smWeightsY = SmWeightsForLog2(log2H);
			for (int i = 0; i < h; i++)
			{
				for (int j = 0; j < w; j++)
				{
					long smoothPred =
						((long)smWeightsY[i] * aboveRow[j]) +
						((long)(256 - smWeightsY[i]) * leftCol[h - 1]) +
						((long)smWeightsX[j] * leftCol[i]) +
						((long)(256 - smWeightsX[j]) * aboveRow[w - 1]);
					pred[(i * w) + j] = Round2(smoothPred, 9);
				}
			}
		}
		else if (mode == Av1IntraMode.SmoothVPred)
		{
			var smWeights = SmWeightsForLog2(log2H);
			for (int i = 0; i < h; i++)
			{
				for (int j = 0; j < w; j++)
				{
					long smoothPred = ((long)smWeights[i] * aboveRow[j]) + ((long)(256 - smWeights[i]) * leftCol[h - 1]);
					pred[(i * w) + j] = Round2(smoothPred, 8);
				}
			}
		}
		else
		{
			var smWeights = SmWeightsForLog2(log2W);
			for (int i = 0; i < h; i++)
			{
				for (int j = 0; j < w; j++)
				{
					long smoothPred = ((long)smWeights[j] * leftCol[i]) + ((long)(256 - smWeights[j]) * aboveRow[w - 1]);
					pred[(i * w) + j] = Round2(smoothPred, 8);
				}
			}
		}
	}
	private static void PredictDirectional(
		int[] pred,
		int w,
		int h,
		Av1EdgeArray aboveRow,
		Av1EdgeArray leftCol,
		int mode,
		int angleDelta,
		bool haveLeft,
		bool haveAbove,
		bool enableIntraEdgeFilter,
		bool filterTypeSmooth,
		int maxX,
		int maxY,
		int x,
		int y,
		int bitDepth)
	{
		const int angleStep = 3;
		int pAngle = ModeToAngle[mode] + (angleDelta * angleStep);

		int upsampleAbove = 0;
		int upsampleLeft = 0;

		if (enableIntraEdgeFilter && pAngle != 90 && pAngle != 180)
		{
			if (pAngle > 90 && pAngle < 180 && w + h >= 24)
			{
				int corner = FilterCorner(aboveRow, leftCol);
				aboveRow[-1] = corner;
				leftCol[-1] = corner;
			}

			int filterType = filterTypeSmooth ? 1 : 0;

			if (haveAbove)
			{
				int strength = EdgeFilterStrength(w, h, filterType, pAngle - 90);
				int numPx = Math.Min(w, maxX - x + 1) + (pAngle < 90 ? h : 0) + 1;
				EdgeFilter(aboveRow, numPx, strength);
			}

			if (haveLeft)
			{
				int strength = EdgeFilterStrength(w, h, filterType, pAngle - 180);
				int numPx = Math.Min(h, maxY - y + 1) + (pAngle > 180 ? w : 0) + 1;
				EdgeFilter(leftCol, numPx, strength);
			}

			upsampleAbove = EdgeUpsampleSelect(w, h, filterType, pAngle - 90) ? 1 : 0;
			int numPxAbove = w + (pAngle < 90 ? h : 0);
			if (upsampleAbove == 1)
			{
				EdgeUpsample(aboveRow, numPxAbove, bitDepth);
			}

			upsampleLeft = EdgeUpsampleSelect(w, h, filterType, pAngle - 180) ? 1 : 0;
			int numPxLeft = h + (pAngle > 180 ? w : 0);
			if (upsampleLeft == 1)
			{
				EdgeUpsample(leftCol, numPxLeft, bitDepth);
			}
		}

		int dx = 0;
		int dy = 0;
		if (pAngle < 90)
		{
			dx = DrIntraDerivative[pAngle];
		}
		else if (pAngle > 90 && pAngle < 180)
		{
			dx = DrIntraDerivative[180 - pAngle];
			dy = DrIntraDerivative[pAngle - 90];
		}
		else if (pAngle > 180)
		{
			dy = DrIntraDerivative[270 - pAngle];
		}

		if (pAngle < 90)
		{
			int maxBaseX = (w + h - 1) << upsampleAbove;
			for (int i = 0; i < h; i++)
			{
				int idx = (i + 1) * dx;
				for (int j = 0; j < w; j++)
				{
					int baseIdx = (idx >> (6 - upsampleAbove)) + (j << upsampleAbove);
					int shift = (idx << upsampleAbove) >> 1 & 0x1F;
					pred[(i * w) + j] = baseIdx < maxBaseX
						? Round2((long)(aboveRow[baseIdx] * (32 - shift)) + (aboveRow[baseIdx + 1] * shift), 5)
						: aboveRow[maxBaseX];
				}
			}
		}
		else if (pAngle > 90 && pAngle < 180)
		{
			for (int i = 0; i < h; i++)
			{
				for (int j = 0; j < w; j++)
				{
					int idx = (j << 6) - ((i + 1) * dx);
					int baseIdx = idx >> (6 - upsampleAbove);
					if (baseIdx >= -(1 << upsampleAbove))
					{
						int shift = (idx << upsampleAbove) >> 1 & 0x1F;
						pred[(i * w) + j] = Round2((long)(aboveRow[baseIdx] * (32 - shift)) + (aboveRow[baseIdx + 1] * shift), 5);
					}
					else
					{
						int idx2 = (i << 6) - ((j + 1) * dy);
						int baseIdx2 = idx2 >> (6 - upsampleLeft);
						int shift2 = (idx2 << upsampleLeft) >> 1 & 0x1F;
						pred[(i * w) + j] = Round2((long)(leftCol[baseIdx2] * (32 - shift2)) + (leftCol[baseIdx2 + 1] * shift2), 5);
					}
				}
			}
		}
		else if (pAngle > 180)
		{
			int maxBaseY = (w + h - 1) << upsampleLeft;
			for (int i = 0; i < h; i++)
			{
				for (int j = 0; j < w; j++)
				{
					int idx = (j + 1) * dy;
					int baseIdx = (idx >> (6 - upsampleLeft)) + (i << upsampleLeft);
					int shift = (idx << upsampleLeft) >> 1 & 0x1F;
					pred[(i * w) + j] = baseIdx < maxBaseY
						? Round2((long)(leftCol[baseIdx] * (32 - shift)) + (leftCol[baseIdx + 1] * shift), 5)
						: leftCol[maxBaseY];
				}
			}
		}
		else if (pAngle == 90)
		{
			for (int i = 0; i < h; i++)
			{
				for (int j = 0; j < w; j++)
				{
					pred[(i * w) + j] = aboveRow[j];
				}
			}
		}
		else
		{
			for (int i = 0; i < h; i++)
			{
				for (int j = 0; j < w; j++)
				{
					pred[(i * w) + j] = leftCol[i];
				}
			}
		}
	}
	private static int FilterCorner(Av1EdgeArray aboveRow, Av1EdgeArray leftCol)
	{
		int s = (leftCol[0] * 5) + (aboveRow[-1] * 6) + (aboveRow[0] * 5);
		return Round2(s, 4);
	}
	private static int EdgeFilterStrength(int w, int h, int filterType, int delta)
	{
		int d = Math.Abs(delta);
		int blkWh = w + h;
		int strength = 0;

		if (filterType == 0)
		{
			if (blkWh <= 8)
			{
				if (d >= 56)
				{
					strength = 1;
				}
			}
			else if (blkWh <= 12)
			{
				if (d >= 40)
				{
					strength = 1;
				}
			}
			else if (blkWh <= 16)
			{
				if (d >= 40)
				{
					strength = 1;
				}
			}
			else if (blkWh <= 24)
			{
				if (d >= 8)
				{
					strength = 1;
				}

				if (d >= 16)
				{
					strength = 2;
				}

				if (d >= 32)
				{
					strength = 3;
				}
			}
			else if (blkWh <= 32)
			{
				strength = 1;
				if (d >= 4)
				{
					strength = 2;
				}

				if (d >= 32)
				{
					strength = 3;
				}
			}
			else
			{
				strength = 3;
			}
		}
		else
		{
			if (blkWh <= 8)
			{
				if (d >= 40)
				{
					strength = 1;
				}

				if (d >= 64)
				{
					strength = 2;
				}
			}
			else if (blkWh <= 16)
			{
				if (d >= 20)
				{
					strength = 1;
				}

				if (d >= 48)
				{
					strength = 2;
				}
			}
			else if (blkWh <= 24)
			{
				if (d >= 4)
				{
					strength = 3;
				}
			}
			else
			{
				strength = 3;
			}
		}

		return strength;
	}
	private static bool EdgeUpsampleSelect(int w, int h, int filterType, int delta)
	{
		int d = Math.Abs(delta);
		int blkWh = w + h;

		if (d <= 0 || d >= 40)
		{
			return false;
		}

		return filterType == 0 ? blkWh <= 16 : blkWh <= 8;
	}
	private static void EdgeUpsample(Av1EdgeArray buf, int numPx, int bitDepth)
	{
		// numPx maxes out around w+h (largest transform side sum, 128), so this is always well within a
		// safe stackalloc bound.
		Span<int> dup = stackalloc int[numPx + 3];
		dup[0] = buf[-1];
		for (int i = -1; i < numPx; i++)
		{
			dup[i + 2] = buf[i];
		}

		dup[numPx + 2] = buf[numPx - 1];

		buf[-2] = dup[0];
		for (int i = 0; i < numPx; i++)
		{
			int s = -dup[i] + (9 * dup[i + 1]) + (9 * dup[i + 2]) - dup[i + 3];
			s = Clip1(Round2(s, 4), bitDepth);
			buf[(2 * i) - 1] = s;
			buf[2 * i] = dup[i + 2];
		}
	}
	private static void EdgeFilter(Av1EdgeArray buf, int sz, int strength)
	{
		if (strength == 0)
		{
			return;
		}

		// sz is spec-bounded to <= 129 (§7.11.2.12's own note), so this is always a safe stackalloc.
		Span<int> edge = stackalloc int[sz];
		for (int i = 0; i < sz; i++)
		{
			edge[i] = buf[i - 1];
		}

		var kernel = IntraEdgeKernel[strength - 1];
		for (int i = 1; i < sz; i++)
		{
			int s = 0;
			for (int j = 0; j < IntraEdgeTaps; j++)
			{
				int k = Math.Clamp(i - 2 + j, 0, sz - 1);
				s += kernel[j] * edge[k];
			}

			buf[i - 1] = (s + 8) >> 4;
		}
	}
	public static void PredictChromaFromLuma(
		int[] chromaPlane,
		int chromaStride,
		int[] lumaPlane,
		int lumaStride,
		int startX,
		int startY,
		int w,
		int h,
		int log2W,
		int log2H,
		int subX,
		int subY,
		int alpha,
		int maxLumaW,
		int maxLumaH,
		int bitDepth)
	{
		// h*w maxes out at 32*32 (the largest chroma transform size) = 4KB, a safe stackalloc bound.
		Span<int> l = stackalloc int[h * w];
		long lumaAvg = 0;

		for (int i = 0; i < h; i++)
		{
			int lumaY = (startY + i) << subY;
			lumaY = Math.Min(lumaY, maxLumaH - (1 << subY));
			for (int j = 0; j < w; j++)
			{
				int lumaX = (startX + j) << subX;
				lumaX = Math.Min(lumaX, maxLumaW - (1 << subX));

				int t = 0;
				for (int dy = 0; dy <= subY; dy++)
				{
					for (int dx = 0; dx <= subX; dx++)
					{
						t += lumaPlane[((lumaY + dy) * lumaStride) + lumaX + dx];
					}
				}

				int v = t << (3 - subX - subY);
				l[(i * w) + j] = v;
				lumaAvg += v;
			}
		}

		lumaAvg = Round2(lumaAvg, log2W + log2H);

		for (int i = 0; i < h; i++)
		{
			for (int j = 0; j < w; j++)
			{
				int dc = chromaPlane[((startY + i) * chromaStride) + startX + j];
				int scaledLuma = Round2Signed((long)alpha * (l[(i * w) + j] - lumaAvg), 6);
				chromaPlane[((startY + i) * chromaStride) + startX + j] = Clip1(dc + scaledLuma, bitDepth);
			}
		}
	}
}
