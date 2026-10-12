namespace Ecng.Drawing.Codecs.Formats.Webp.Encoding.Vp8;

using System;
using System.IO;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.Dct;
using Ecng.Drawing.Codecs.Formats.Webp.Internal;

internal static class Vp8FrameEncoder
{
	private const int BPredSadThreshold = 16 * 16 * 6;

	public static void Encode(
		byte[] rgb, int width, int height,
		Vp8BoolEncoder partition0, Vp8BoolEncoder coefficientEncoder,
		Vp8QuantMatrix quant, bool useSkipProbability, int skipFalseProbability)
	{
		int mbCols = (width + 15) / 16;
		int mbRows = (height + 15) / 16;

		int yStride = (mbCols * 16) + 1;
		int yPaddedHeight = (mbRows * 16) + 1;
		int uvStride = (mbCols * 8) + 1;
		int uvPaddedHeight = (mbRows * 8) + 1;

		byte[] recY = WebpBufferPool.Shared.Rent(yStride * yPaddedHeight);
		byte[] recU = WebpBufferPool.Shared.Rent(uvStride * uvPaddedHeight);
		byte[] recV = WebpBufferPool.Shared.Rent(uvStride * uvPaddedHeight);

		int srcYStride = mbCols * 16;
		int srcYHeight = mbRows * 16;
		int srcUvStride = mbCols * 8;
		int srcUvHeight = mbRows * 8;

		byte[] srcY = WebpBufferPool.Shared.Rent(srcYStride * srcYHeight);
		byte[] srcU = WebpBufferPool.Shared.Rent(srcUvStride * srcUvHeight);
		byte[] srcV = WebpBufferPool.Shared.Rent(srcUvStride * srcUvHeight);

		try
		{
			InitializeBorders(recY, yStride, yPaddedHeight);
			InitializeBorders(recU, uvStride, uvPaddedHeight);
			InitializeBorders(recV, uvStride, uvPaddedHeight);

			int YOff(int x, int y) => ((y + 1) * yStride) + x + 1;
			int UOff(int x, int y) => ((y + 1) * uvStride) + x + 1;

			Vp8ForwardColorConverter.ConvertPlanes(rgb, width, height, srcY, srcYStride, srcU, srcV, srcUvStride);
			PadPlaneToGrid(srcY, width, height, srcYStride, srcYHeight);
			int chromaRealWidth = (width + 1) / 2;
			int chromaRealHeight = (height + 1) / 2;
			PadPlaneToGrid(srcU, chromaRealWidth, chromaRealHeight, srcUvStride, srcUvHeight);
			PadPlaneToGrid(srcV, chromaRealWidth, chromaRealHeight, srcUvStride, srcUvHeight);

			var modeWriter = new Vp8ModeWriter(mbCols);
			var coeffContext = new Vp8CoefficientContext(mbCols);
			byte[] coeffProbabilities = Vp8CoefficientProbabilities.DefaultFlat;

			Span<byte> aboveRight = stackalloc byte[4];

			for (int mbY = 0; mbY < mbRows; mbY++)
			{
				modeWriter.StartRow();
				coeffContext.StartRow();
				bool hasAbove = mbY > 0;

				for (int mbX = 0; mbX < mbCols; mbX++)
				{
					bool hasLeft = mbX > 0;

					ProcessMacroblock(
						recY, yStride, recU, recV, uvStride,
						srcY, srcYStride, srcU, srcV, srcUvStride,
						YOff, UOff, mbX, mbY, mbCols, hasAbove, hasLeft,
						quant, coeffContext, coeffProbabilities,
						modeWriter, partition0, coefficientEncoder,
						useSkipProbability, skipFalseProbability, aboveRight);
				}
			}
		}
		finally
		{
			WebpBufferPool.Shared.Return(recY);
			WebpBufferPool.Shared.Return(recU);
			WebpBufferPool.Shared.Return(recV);
			WebpBufferPool.Shared.Return(srcY);
			WebpBufferPool.Shared.Return(srcU);
			WebpBufferPool.Shared.Return(srcV);
		}
	}

	private static void ProcessMacroblock(
		byte[] recY, int yStride, byte[] recU, byte[] recV, int uvStride,
		byte[] srcY, int srcYStride, byte[] srcU, byte[] srcV, int srcUvStride,
		Func<int, int, int> yOff, Func<int, int, int> uOff,
		int mbX, int mbY, int mbCols, bool hasAbove, bool hasLeft,
		Vp8QuantMatrix quant, Vp8CoefficientContext coeffContext, byte[] coeffProbabilities,
		Vp8ModeWriter modeWriter, Vp8BoolEncoder partition0, Vp8BoolEncoder coefficientEncoder,
		bool useSkipProbability, int skipFalseProbability, Span<byte> aboveRight)
	{
		Span<short> quantizedBlocks = stackalloc short[24 * 16];
		Span<int> lastBlocks = stackalloc int[24];
		Span<short> y2Quantized = stackalloc short[16];
		int y2Last = 0;
		bool anyNonZero = false;

		int yOrigin = yOff(mbX * 16, mbY * 16);
		int srcYOrigin = (mbY * 16 * srcYStride) + (mbX * 16);

		int wholeMode = Vp8ModeDecision.SelectWholeBlockMode(recY, yOrigin, yStride, 16, hasAbove, hasLeft, srcY, srcYOrigin, srcYStride, out int wholeSad);
		bool isI4x4 = wholeSad > BPredSadThreshold;

		Span<int> subModes = stackalloc int[16];

		if (!isI4x4)
		{
			Span<short> dcValues = stackalloc short[16];
			Span<short> rawCoeffs = stackalloc short[16 * 16];

			for (int n = 0; n < 16; n++)
			{
				int row = n / 4;
				int col = n % 4;
				int blockOrigin = yOff((mbX * 16) + (col * 4), (mbY * 16) + (row * 4));
				int srcBlockOrigin = ((mbY * 16 + (row * 4)) * srcYStride) + (mbX * 16) + (col * 4);

				Span<short> raw = rawCoeffs.Slice(n * 16, 16);
				Vp8ForwardDct.Transform(srcY, srcBlockOrigin, srcYStride, recY, blockOrigin, yStride, raw);
				dcValues[n] = raw[0];
			}

			Span<short> y2Raw = stackalloc short[16];
			Vp8ForwardWht.Transform(dcValues, y2Raw);
			y2Last = Vp8ForwardQuantizer.Quantize(y2Raw, quant.Y2Dc, quant.Y2Ac, y2Quantized);
			if (y2Last > 0)
			{
				anyNonZero = true;
			}

			Span<short> y2Dequant = stackalloc short[16];
			Vp8ForwardQuantizer.Dequantize(y2Quantized, quant.Y2Dc, quant.Y2Ac, y2Dequant);
			Span<short> blockDc = stackalloc short[16];
			Vp8ScalarInverseWht.Transform(y2Dequant, blockDc);

			Span<short> dequant = stackalloc short[16];
			for (int n = 0; n < 16; n++)
			{
				Span<short> quantizedAc = quantizedBlocks.Slice(n * 16, 16);
				int last = Vp8ForwardQuantizer.Quantize(rawCoeffs.Slice(n * 16, 16), quant.Y1Dc, quant.Y1Ac, quantizedAc);
				lastBlocks[n] = last;
				if (last > 1)
				{
					anyNonZero = true;
				}

				Vp8ForwardQuantizer.Dequantize(quantizedAc, quant.Y1Dc, quant.Y1Ac, dequant);
				dequant[0] = blockDc[n]; // The Y2/WHT-derived DC replaces this block's own (unused) DC token.

				int row = n / 4;
				int col = n % 4;
				int blockOrigin = yOff((mbX * 16) + (col * 4), (mbY * 16) + (row * 4));
				Vp8ScalarInverseDct.TransformAndAdd(dequant, recY, blockOrigin, yStride);
			}
		}
		else
		{
			Span<short> raw = stackalloc short[16];
			Span<short> dequant = stackalloc short[16];
			for (int n = 0; n < 16; n++)
			{
				int row = n / 4;
				int col = n % 4;
				int blockOrigin = yOff((mbX * 16) + (col * 4), (mbY * 16) + (row * 4));
				int srcBlockOrigin = ((mbY * 16 + (row * 4)) * srcYStride) + (mbX * 16) + (col * 4);

				ReadOnlySpan<byte> ar = col == 3
					? ComputeAboveRight(recY, yOff, mbX, mbY, mbCols, hasAbove, aboveRight)
					: recY.AsSpan(blockOrigin - yStride + 4, 4);

				int mode = Vp8ModeDecision.SelectSubblockMode(recY, blockOrigin, yStride, ar, srcY, srcBlockOrigin, srcYStride, out _);
				subModes[n] = mode;

				Vp8ForwardDct.Transform(srcY, srcBlockOrigin, srcYStride, recY, blockOrigin, yStride, raw);

				Span<short> quantizedAc = quantizedBlocks.Slice(n * 16, 16);
				int last = Vp8ForwardQuantizer.Quantize(raw, quant.Y1Dc, quant.Y1Ac, quantizedAc);
				lastBlocks[n] = last;
				if (last > 0)
				{
					anyNonZero = true;
				}

				Vp8ForwardQuantizer.Dequantize(quantizedAc, quant.Y1Dc, quant.Y1Ac, dequant);
				Vp8ScalarInverseDct.TransformAndAdd(dequant, recY, blockOrigin, yStride);
			}
		}

		int uOrigin = uOff(mbX * 8, mbY * 8);
		int srcUOrigin = (mbY * 8 * srcUvStride) + (mbX * 8);

		int uvMode = Vp8ModeDecision.SelectWholeBlockMode(recU, uOrigin, uvStride, 8, hasAbove, hasLeft, srcU, srcUOrigin, srcUvStride, out _);

		Vp8IntraPredictionWholeBlock.PredictModeWholeBlock(uvMode, recV, uOrigin, uvStride, 8, hasAbove, hasLeft);

		for (int c = 0; c < 4; c++)
		{
			int row = c / 2;
			int col = c % 2;
			int uBlockOrigin = uOff((mbX * 8) + (col * 4), (mbY * 8) + (row * 4));
			int srcUBlockOrigin = ((mbY * 8 + (row * 4)) * srcUvStride) + (mbX * 8) + (col * 4);

			EncodeChromaBlock(srcU, recU, srcUBlockOrigin, uBlockOrigin, srcUvStride, uvStride, quant, quantizedBlocks, lastBlocks, 16 + c, ref anyNonZero);
			EncodeChromaBlock(srcV, recV, srcUBlockOrigin, uBlockOrigin, srcUvStride, uvStride, quant, quantizedBlocks, lastBlocks, 20 + c, ref anyNonZero);
		}

		bool skip = !anyNonZero;

		modeWriter.WriteMacroblockModes(partition0, mbX, skip, useSkipProbability, skipFalseProbability, isI4x4, wholeMode, subModes, uvMode);

		if (skip)
		{
			ResetSkippedContext(coeffContext, mbX, isI4x4);
		}
		else
		{
			EncodeMacroblockCoefficients(coefficientEncoder, coeffProbabilities, isI4x4, mbX, coeffContext, quantizedBlocks, lastBlocks, y2Quantized, y2Last);
		}
	}

	private static void EncodeChromaBlock(
		byte[] srcPlane, byte[] recPlane, int srcOrigin, int recOrigin, int srcStride, int recStride,
		Vp8QuantMatrix quant, Span<short> quantizedBlocks, Span<int> lastBlocks, int blockIndex, ref bool anyNonZero)
	{
		Span<short> raw = stackalloc short[16];
		Vp8ForwardDct.Transform(srcPlane, srcOrigin, srcStride, recPlane, recOrigin, recStride, raw);

		Span<short> quantized = quantizedBlocks.Slice(blockIndex * 16, 16);
		int last = Vp8ForwardQuantizer.Quantize(raw, quant.UvDc, quant.UvAc, quantized);
		lastBlocks[blockIndex] = last;
		if (last > 0)
		{
			anyNonZero = true;
		}

		Span<short> dequant = stackalloc short[16];
		Vp8ForwardQuantizer.Dequantize(quantized, quant.UvDc, quant.UvAc, dequant);
		Vp8ScalarInverseDct.TransformAndAdd(dequant, recPlane, recOrigin, recStride);
	}

	private static void EncodeMacroblockCoefficients(
		Vp8BoolEncoder tokenBw, byte[] probabilities, bool isI4x4, int mbX,
		Vp8CoefficientContext context, ReadOnlySpan<short> quantizedBlocks, ReadOnlySpan<int> lastBlocks, ReadOnlySpan<short> y2Quantized, int y2Last)
	{
		if (!isI4x4)
		{
			int dcCtx = (context.LeftDc ? 1 : 0) + (context.AboveDc[mbX] ? 1 : 0);
			Vp8CoefficientEncoder.EncodeBlock(tokenBw, probabilities, 1, dcCtx, 0, y2Quantized, y2Last);
			bool dcNonZero = y2Last > 0;
			context.LeftDc = dcNonZero;
			context.AboveDc[mbX] = dcNonZero;
		}

		int yPlaneType = isI4x4 ? 3 : 0;
		int yFirst = isI4x4 ? 0 : 1;

		Span<bool> colY = stackalloc bool[4];
		for (int i = 0; i < 4; i++)
		{
			colY[i] = context.AboveY[(mbX * 4) + i];
		}

		for (int row = 0; row < 4; row++)
		{
			bool rowLeft = context.LeftY[row];
			for (int col = 0; col < 4; col++)
			{
				int idx = (row * 4) + col;
				int ctx = (rowLeft ? 1 : 0) + (colY[col] ? 1 : 0);
				Vp8CoefficientEncoder.EncodeBlock(tokenBw, probabilities, yPlaneType, ctx, yFirst, quantizedBlocks.Slice(idx * 16, 16), lastBlocks[idx]);
				bool nz = lastBlocks[idx] > yFirst;
				rowLeft = nz;
				colY[col] = nz;
			}

			context.LeftY[row] = rowLeft;
		}

		for (int i = 0; i < 4; i++)
		{
			context.AboveY[(mbX * 4) + i] = colY[i];
		}

		EncodeChromaPlaneTokens(tokenBw, probabilities, mbX, context.AboveU, context.LeftU, quantizedBlocks, lastBlocks, blockOffset: 16);
		EncodeChromaPlaneTokens(tokenBw, probabilities, mbX, context.AboveV, context.LeftV, quantizedBlocks, lastBlocks, blockOffset: 20);
	}

	private static void EncodeChromaPlaneTokens(
		Vp8BoolEncoder tokenBw, byte[] probabilities, int mbX, bool[] above, bool[] left,
		ReadOnlySpan<short> quantizedBlocks, ReadOnlySpan<int> lastBlocks, int blockOffset)
	{
		Span<bool> col = stackalloc bool[2];
		col[0] = above[(mbX * 2) + 0];
		col[1] = above[(mbX * 2) + 1];

		for (int row = 0; row < 2; row++)
		{
			bool rowLeft = left[row];
			for (int c = 0; c < 2; c++)
			{
				int idx = blockOffset + (row * 2) + c;
				int ctx = (rowLeft ? 1 : 0) + (col[c] ? 1 : 0);
				Vp8CoefficientEncoder.EncodeBlock(tokenBw, probabilities, 2, ctx, 0, quantizedBlocks.Slice(idx * 16, 16), lastBlocks[idx]);
				bool nz = lastBlocks[idx] > 0;
				rowLeft = nz;
				col[c] = nz;
			}

			left[row] = rowLeft;
		}

		above[(mbX * 2) + 0] = col[0];
		above[(mbX * 2) + 1] = col[1];
	}

	private static void ResetSkippedContext(Vp8CoefficientContext context, int mbX, bool isI4x4)
	{
		for (int i = 0; i < 4; i++)
		{
			context.AboveY[(mbX * 4) + i] = false;
			context.LeftY[i] = false;
		}

		for (int i = 0; i < 2; i++)
		{
			context.AboveU[(mbX * 2) + i] = false;
			context.LeftU[i] = false;
			context.AboveV[(mbX * 2) + i] = false;
			context.LeftV[i] = false;
		}

		if (!isI4x4)
		{
			context.AboveDc[mbX] = false;
			context.LeftDc = false;
		}
	}

	private static ReadOnlySpan<byte> ComputeAboveRight(byte[] yPlane, Func<int, int, int> yOff, int mbX, int mbY, int mbCols, bool hasAbove, Span<byte> aboveRight)
	{
		if (!hasAbove)
		{
			aboveRight.Fill(127);
			return aboveRight;
		}

		if (mbX == mbCols - 1)
		{
			byte value = yPlane[yOff((mbX * 16) + 15, (mbY * 16) - 1)];
			aboveRight.Fill(value);
			return aboveRight;
		}

		int baseOffset = yOff((mbX * 16) + 16, (mbY * 16) - 1);
		for (int k = 0; k < 4; k++)
		{
			aboveRight[k] = yPlane[baseOffset + k];
		}

		return aboveRight;
	}

	private static void InitializeBorders(byte[] plane, int stride, int paddedHeight)
	{
		plane.AsSpan(0, stride).Fill(127);
		for (int row = 1; row < paddedHeight; row++)
		{
			plane[row * stride] = 129;
		}
	}

	private static void PadPlaneToGrid(byte[] plane, int realWidth, int realHeight, int paddedWidth, int paddedHeight)
	{
		for (int y = 0; y < realHeight; y++)
		{
			int rowBase = y * paddedWidth;
			byte edge = plane[rowBase + realWidth - 1];
			for (int x = realWidth; x < paddedWidth; x++)
			{
				plane[rowBase + x] = edge;
			}
		}

		for (int y = realHeight; y < paddedHeight; y++)
		{
			Array.Copy(plane, (realHeight - 1) * paddedWidth, plane, y * paddedWidth, paddedWidth);
		}
	}
}
