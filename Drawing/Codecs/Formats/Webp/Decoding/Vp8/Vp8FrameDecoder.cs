namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Runtime.InteropServices;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Alpha;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.ColorConversion;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.Dct;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.LoopFilter;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.Upsampling;
using Ecng.Drawing.Codecs.Formats.Webp.Internal;

internal sealed class Vp8FrameDecoder : IWebpLossyBitstreamDecoder
{
	public static IWebpLossyBitstreamDecoder Instance { get; } = new Vp8FrameDecoder();

	public Vp8DecodedFrame Decode(ReadOnlyMemory<byte> vp8Bytes, byte[] alphaPlane, ImageProcessingLimits limits)
		=> DecodeCore(vp8Bytes, alphaPlane, null, limits, 0, 0);

	public static Vp8DecodedFrame DecodeDownscaled(ReadOnlyMemory<byte> bytes, WebpAlphaRows alpha, int width, int height, ImageProcessingLimits limits)
		=> ((Vp8FrameDecoder)Instance).DecodeCore(bytes, null, alpha, limits, width, height);

	private Vp8DecodedFrame DecodeCore(ReadOnlyMemory<byte> vp8Bytes, byte[] alphaPlane, WebpAlphaRows alphaRows, ImageProcessingLimits limits, int outputWidth, int outputHeight)
	{
		var isArray = MemoryMarshal.TryGetArray(vp8Bytes, out ArraySegment<byte> segment) && segment.Array is not null;
		var data = isArray ? segment.Array : vp8Bytes.ToArray();
		var start = isArray ? segment.Offset : 0;
		var end = start + vp8Bytes.Length;
		Vp8FrameHeader frameHeader = Vp8FrameHeader.Parse(data.AsSpan(start, end - start), limits, outputWidth > 0);
		var buffered = outputWidth > 0;
		if (buffered) limits.EnsureWorkingMemory(Vp8ThumbnailBuffer.EstimateWorkingBytes(frameHeader.Width, outputWidth, outputHeight) + (alphaRows?.WorkingBytes ?? 0));

		var partition0 = new Vp8BoolDecoder(data, start + frameHeader.HeaderByteLength, frameHeader.FirstPartitionLength);

		partition0.GetFlag(); // color_space - ignored, always treated as standard studio-range YUV.
		partition0.GetFlag(); // clamping_type - ignored, reconstructed pixels are always clamped to [0,255].

		Vp8SegmentHeader segmentHeader = Vp8SegmentHeader.Parse(partition0);
		Vp8LoopFilterHeader loopFilterHeader = Vp8LoopFilterHeader.Parse(partition0);

		int numPartitions = 1 << (int)partition0.GetValue(2);
		int partition0End = start + frameHeader.HeaderByteLength + frameHeader.FirstPartitionLength;
		Vp8BoolDecoder[] coefficientPartitions = Vp8Partitions.Build(data, partition0End, numPartitions, end);

		Vp8QuantIndices quantIndices = Vp8QuantIndices.Parse(partition0);
		Vp8QuantMatrix[] quantMatrices = Vp8Dequantizer.Resolve(quantIndices, segmentHeader);

		partition0.GetFlag(); // refresh_entropy_probs - irrelevant, a WebP `VP8 ` chunk is always a single lone keyframe.
		byte[] coeffProbabilities = Vp8CoefficientDecoder.ParseProbabilityUpdates(partition0);
		bool useSkipProbability = partition0.GetFlag();
		int skipFalseProbability = useSkipProbability ? (int)partition0.GetValue(8) : 0;

		var filterStrengths = Vp8LoopFilterLevelResolver.Resolve(loopFilterHeader, segmentHeader);

		int mbCols = (frameHeader.Width + 15) / 16;
		int mbRows = (frameHeader.Height + 15) / 16;

		int yStride = (mbCols * 16) + 1;
		int yPaddedHeight = buffered ? 33 : (mbRows * 16) + 1;
		int uvStride = (mbCols * 8) + 1;
		int uvPaddedHeight = buffered ? 17 : (mbRows * 8) + 1;

		byte[] yPlane = WebpBufferPool.Shared.Rent(yStride * yPaddedHeight);
		byte[] uPlane = WebpBufferPool.Shared.Rent(uvStride * uvPaddedHeight);
		byte[] vPlane = WebpBufferPool.Shared.Rent(uvStride * uvPaddedHeight);

		try
		{
			InitializeBorders(yPlane, yStride, yPaddedHeight);
			InitializeBorders(uPlane, uvStride, uvPaddedHeight);
			InitializeBorders(vPlane, uvStride, uvPaddedHeight);

			var yBase = 0;
			var uvBase = 0;
			int YOff(int x, int y) => ((y + yBase + 1) * yStride) + x + 1;
			int UOff(int x, int y) => ((y + uvBase + 1) * uvStride) + x + 1;
			var thumbnail = buffered ? new Vp8ThumbnailBuffer(yPlane, uPlane, vPlane, yStride, uvStride,
				frameHeader.Width, frameHeader.Height, outputWidth, outputHeight, alphaRows, limits) : null;

			var modeDecoder = new Vp8ModeDecoder(mbCols, segmentHeader, useSkipProbability, skipFalseProbability);
			var coeffContext = new Vp8CoefficientContext(mbCols);
			var modes = new Vp8MacroblockModes();

			var segmentAtMb = new int[mbCols * (buffered ? 1 : mbRows)];
			var isI4x4AtMb = new bool[segmentAtMb.Length];
			var innerFilterAtMb = new bool[segmentAtMb.Length];

			Span<short> coeffs = stackalloc short[25 * 16];
			Span<short> y2Coeffs = stackalloc short[16];
			Span<short> blockDc = stackalloc short[16];
			Span<byte> aboveRight = stackalloc byte[4];
			Span<bool> colY = stackalloc bool[4];
			Span<bool> colU = stackalloc bool[2];
			Span<bool> colV = stackalloc bool[2];

			for (int mbY = 0; mbY < mbRows; mbY++)
			{
				if (buffered)
				{
					yBase = 16 - mbY * 16;
					uvBase = 8 - mbY * 8;
					thumbnail.BeginRow(mbY);
				}
				modeDecoder.StartRow();
				coeffContext.StartRow();
				Vp8BoolDecoder tokenBr = coefficientPartitions[mbY % numPartitions];
				bool hasAbove = mbY > 0;

				for (int mbX = 0; mbX < mbCols; mbX++)
				{
					modeDecoder.DecodeMacroblock(partition0, mbX, modes);
					bool hasLeft = mbX > 0;

					Vp8QuantMatrix quant = quantMatrices[modes.Segment];

					coeffs.Clear();
					bool anyNonZero;

					uint nonZeroBlocks;
					uint acBlocks;

					if (modes.Skip)
					{
						ResetSkippedContext(coeffContext, mbX, modes.IsI4x4);
						anyNonZero = false;
						nonZeroBlocks = 0;
						acBlocks = 0;
					}
					else
					{
						anyNonZero = DecodeMacroblockCoefficients(
							tokenBr, coeffProbabilities, quant, modes, mbX, coeffContext,
							coeffs, y2Coeffs, blockDc, colY, colU, colV, out nonZeroBlocks, out acBlocks);
					}

					int mbIndex = buffered ? mbX : (mbY * mbCols) + mbX;
					segmentAtMb[mbIndex] = modes.Segment;
					isI4x4AtMb[mbIndex] = modes.IsI4x4;

					// f_inner = is_i4x4 || !skip, where "skip" is the *effective* skip (no residual at all).
					bool effectiveSkip = !anyNonZero;
					innerFilterAtMb[mbIndex] = modes.IsI4x4 || !effectiveSkip;

					int yOrigin = YOff(mbX * 16, mbY * 16);

					if (!modes.IsI4x4)
					{
						Vp8IntraPredictionWholeBlock.PredictModeWholeBlock(modes.YMode, yPlane, yOrigin, yStride, 16, hasAbove, hasLeft);

						for (int n = 0; n < 16; n++)
						{
							if ((nonZeroBlocks & (1u << n)) == 0)
							{
								continue;
							}

							int row = n / 4;
							int col = n % 4;
							int blockOrigin = YOff((mbX * 16) + (col * 4), (mbY * 16) + (row * 4));
							AddResidual(coeffs, n, acBlocks, yPlane, blockOrigin, yStride);
						}
					}
					else
					{
						ComputeAboveRight(yPlane, yStride, YOff, mbX, mbY, mbCols, hasAbove, aboveRight);

						for (int n = 0; n < 16; n++)
						{
							int row = n / 4;
							int col = n % 4;
							int blockOrigin = YOff((mbX * 16) + (col * 4), (mbY * 16) + (row * 4));

							ReadOnlySpan<byte> ar = col == 3
								? aboveRight
								: yPlane.AsSpan(blockOrigin - yStride + 4, 4);

							// Prediction runs for every subblock regardless — each one's reconstructed pixels
							// are what the next subblock predicts from. Only the residual can be skipped.
							Vp8IntraPrediction4x4.Predict(modes.SubModes[n], yPlane, blockOrigin, yStride, ar);

							if ((nonZeroBlocks & (1u << n)) != 0)
							{
								AddResidual(coeffs, n, acBlocks, yPlane, blockOrigin, yStride);
							}
						}
					}

					int uOrigin = UOff(mbX * 8, mbY * 8);
					Vp8IntraPredictionWholeBlock.PredictModeWholeBlock(modes.UvMode, uPlane, uOrigin, uvStride, 8, hasAbove, hasLeft);
					Vp8IntraPredictionWholeBlock.PredictModeWholeBlock(modes.UvMode, vPlane, uOrigin, uvStride, 8, hasAbove, hasLeft);

					for (int c = 0; c < 4; c++)
					{
						int row = c / 2;
						int col = c % 2;
						int uBlockOrigin = UOff((mbX * 8) + (col * 4), (mbY * 8) + (row * 4));

						if ((nonZeroBlocks & (1u << (16 + c))) != 0)
						{
							AddResidual(coeffs, 16 + c, acBlocks, uPlane, uBlockOrigin, uvStride);
						}

						if ((nonZeroBlocks & (1u << (20 + c))) != 0)
						{
							AddResidual(coeffs, 20 + c, acBlocks, vPlane, uBlockOrigin, uvStride);
						}
					}
				}
				if (buffered)
				{
					thumbnail.EndPrediction(mbY);
					ApplyLoopFilter(yPlane, yStride, uPlane, vPlane, uvStride, mbCols, mbY, mbY + 1, true,
						loopFilterHeader, filterStrengths, segmentAtMb, isI4x4AtMb, innerFilterAtMb, YOff, UOff);
					thumbnail.CommitRow(mbY);
				}
			}
			if (buffered) return thumbnail.Complete();

			ApplyLoopFilter(
				yPlane, yStride, uPlane, vPlane, uvStride, mbCols, 0, mbRows, false,
				loopFilterHeader, filterStrengths, segmentAtMb, isI4x4AtMb, innerFilterAtMb, YOff, UOff);

			return ProduceRgbFrame(yPlane, yStride, uPlane, vPlane, uvStride, mbCols, mbRows, frameHeader.Width, frameHeader.Height, YOff, UOff, alphaPlane);
		}
		finally
		{
			WebpBufferPool.Shared.Return(yPlane);
			WebpBufferPool.Shared.Return(uPlane);
			WebpBufferPool.Shared.Return(vPlane);
		}
	}
	private static void InitializeBorders(byte[] plane, int stride, int paddedHeight)
	{
		plane.AsSpan(0, stride).Fill(127);
		for (int row = 1; row < paddedHeight; row++)
		{
			plane[row * stride] = 129;
		}
	}
	private static void AddResidual(Span<short> coeffs, int blockIndex, uint acBlocks, Span<byte> plane, int origin, int stride)
	{
		var block = coeffs.Slice(blockIndex * 16, 16);

		if ((acBlocks & (1u << blockIndex)) != 0)
		{
			// Vp8VectorInverseDct.CanTransform checks Sse2/AdvSimd.Arm64.IsSupported, which RyuJIT treats as a JIT-time
			// intrinsic constant -- this branch is resolved once per process, not re-evaluated per
			// block, the same way every other Sse2.IsSupported/Vector128.IsHardwareAccelerated gate in this
			// decoder is checked inline rather than cached behind a selector.
			Vp8ScalarInverseDct.TransformFullAndAdd(block, plane, origin, stride);
		}
		else
		{
			Vp8ScalarInverseDct.TransformDcOnlyAndAdd(block, plane, origin, stride);
		}
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
	private static bool DecodeMacroblockCoefficients(
		Vp8BoolDecoder tokenBr,
		byte[] probabilities,
		Vp8QuantMatrix quant,
		Vp8MacroblockModes modes,
		int mbX,
		Vp8CoefficientContext context,
		Span<short> coeffs,
		Span<short> y2Coeffs,
		Span<short> blockDc,
		Span<bool> colY,
		Span<bool> colU,
		Span<bool> colV,
		out uint nonZeroBlocks,
		out uint acBlocks)
	{
		bool anyNonZero = false;
		nonZeroBlocks = 0;
		acBlocks = 0;

		if (!modes.IsI4x4)
		{
			y2Coeffs.Clear();
			int dcCtx = (context.LeftDc ? 1 : 0) + (context.AboveDc[mbX] ? 1 : 0);
			int dcLast = Vp8CoefficientDecoder.DecodeBlock(tokenBr, probabilities, 1, dcCtx, 0, quant.Y2Dc, quant.Y2Ac, y2Coeffs);
			bool dcNonZero = dcLast > 0;
			context.LeftDc = dcNonZero;
			context.AboveDc[mbX] = dcNonZero;
			if (dcNonZero)
			{
				anyNonZero = true;
			}

			Vp8ScalarInverseWht.Transform(y2Coeffs, blockDc);
			for (int i = 0; i < 16; i++)
			{
				coeffs[(i * 16) + 0] = blockDc[i];
			}
		}

		int yPlaneType = modes.IsI4x4 ? 3 : 0;
		int yFirst = modes.IsI4x4 ? 0 : 1;

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
				Span<short> block = coeffs.Slice(idx * 16, 16);
				int last = Vp8CoefficientDecoder.DecodeBlock(tokenBr, probabilities, yPlaneType, ctx, yFirst, quant.Y1Dc, quant.Y1Ac, block);
				bool nz = last > yFirst;
				if (nz || block[0] != 0)
				{
					anyNonZero = true;
				}

				// block[0] is read after the Y2/WHT pass above has written each luma block's DC, so this sees
				// the DC the reconstruction will actually transform, not just what this block's own tokens set.
				if (last > 1)
				{
					acBlocks |= 1u << idx;
					nonZeroBlocks |= 1u << idx;
				}
				else if (block[0] != 0)
				{
					nonZeroBlocks |= 1u << idx;
				}

				rowLeft = nz;
				colY[col] = nz;
			}

			context.LeftY[row] = rowLeft;
		}

		for (int i = 0; i < 4; i++)
		{
			context.AboveY[(mbX * 4) + i] = colY[i];
		}

		anyNonZero |= DecodeChromaPlane(tokenBr, probabilities, quant, mbX, context.AboveU, context.LeftU, coeffs, colU, blockOffset: 16, ref nonZeroBlocks, ref acBlocks);
		anyNonZero |= DecodeChromaPlane(tokenBr, probabilities, quant, mbX, context.AboveV, context.LeftV, coeffs, colV, blockOffset: 20, ref nonZeroBlocks, ref acBlocks);

		return anyNonZero;
	}

	private static bool DecodeChromaPlane(
		Vp8BoolDecoder tokenBr,
		byte[] probabilities,
		Vp8QuantMatrix quant,
		int mbX,
		bool[] above,
		bool[] left,
		Span<short> coeffs,
		Span<bool> col,
		int blockOffset,
		ref uint nonZeroBlocks,
		ref uint acBlocks)
	{
		bool anyNonZero = false;

		col[0] = above[(mbX * 2) + 0];
		col[1] = above[(mbX * 2) + 1];

		for (int row = 0; row < 2; row++)
		{
			bool rowLeft = left[row];
			for (int c = 0; c < 2; c++)
			{
				int idx = blockOffset + (row * 2) + c;
				int ctx = (rowLeft ? 1 : 0) + (col[c] ? 1 : 0);
				Span<short> block = coeffs.Slice(idx * 16, 16);
				int last = Vp8CoefficientDecoder.DecodeBlock(tokenBr, probabilities, 2, ctx, 0, quant.UvDc, quant.UvAc, block);
				bool nz = last > 0;
				if (nz || block[0] != 0)
				{
					anyNonZero = true;
				}

				if (last > 1)
				{
					acBlocks |= 1u << idx;
					nonZeroBlocks |= 1u << idx;
				}
				else if (block[0] != 0)
				{
					nonZeroBlocks |= 1u << idx;
				}

				rowLeft = nz;
				col[c] = nz;
			}

			left[row] = rowLeft;
		}

		above[(mbX * 2) + 0] = col[0];
		above[(mbX * 2) + 1] = col[1];

		return anyNonZero;
	}
	private static void ComputeAboveRight(byte[] yPlane, int yStride, Func<int, int, int> yOff, int mbX, int mbY, int mbCols, bool hasAbove, Span<byte> aboveRight)
	{
		if (!hasAbove)
		{
			aboveRight.Fill(127);
			return;
		}

		if (mbX == mbCols - 1)
		{
			byte value = yPlane[yOff((mbX * 16) + 15, (mbY * 16) - 1)];
			aboveRight.Fill(value);
			return;
		}

		int baseOffset = yOff((mbX * 16) + 16, (mbY * 16) - 1);
		for (int k = 0; k < 4; k++)
		{
			aboveRight[k] = yPlane[baseOffset + k];
		}
	}

	private static void ApplyLoopFilter(
		byte[] yPlane,
		int yStride,
		byte[] uPlane,
		byte[] vPlane,
		int uvStride,
		int mbCols,
		int firstRow,
		int endRow,
		bool buffered,
		Vp8LoopFilterHeader header,
		Vp8FilterStrength[,] filterStrengths,
		int[] segmentAtMb,
		bool[] isI4x4AtMb,
		bool[] innerFilterAtMb,
		Func<int, int, int> yOff,
		Func<int, int, int> uOff)
	{
		if (!header.IsFilteringEnabled)
		{
			return;
		}

		for (int mbY = firstRow; mbY < endRow; mbY++)
		{
			for (int mbX = 0; mbX < mbCols; mbX++)
			{
				int mbIndex = buffered ? mbX : (mbY * mbCols) + mbX;
				Vp8FilterStrength strength = filterStrengths[segmentAtMb[mbIndex], isI4x4AtMb[mbIndex] ? 1 : 0];
				if (!strength.IsFilteringEnabled)
				{
					continue;
				}

				bool inner = innerFilterAtMb[mbIndex];
				int yOrigin = yOff(mbX * 16, mbY * 16);

				if (header.Simple)
				{
					if (mbX > 0)
					{
						Vp8SimpleLoopFilter.FilterLeftEdge(yPlane, yOrigin, yStride, strength.Limit + 4);
					}

					if (inner)
					{
						Vp8SimpleLoopFilter.FilterLeftEdgesInner(yPlane, yOrigin, yStride, strength.Limit);
					}

					if (mbY > 0)
					{
						Vp8SimpleLoopFilter.FilterTopEdge(yPlane, yOrigin, yStride, strength.Limit + 4);
					}

					if (inner)
					{
						Vp8SimpleLoopFilter.FilterTopEdgesInner(yPlane, yOrigin, yStride, strength.Limit);
					}
				}
				else
				{
					int uOrigin = uOff(mbX * 8, mbY * 8);

					if (mbX > 0)
					{
						Vp8NormalLoopFilter.FilterLeftEdge16(yPlane, yOrigin, yStride, strength.Limit + 4, strength.InteriorLimit, strength.HevThreshold);
						Vp8NormalLoopFilter.FilterLeftEdge8(uPlane, uOrigin, uvStride, strength.Limit + 4, strength.InteriorLimit, strength.HevThreshold);
						Vp8NormalLoopFilter.FilterLeftEdge8(vPlane, uOrigin, uvStride, strength.Limit + 4, strength.InteriorLimit, strength.HevThreshold);
					}

					if (inner)
					{
						Vp8NormalLoopFilter.FilterLeftEdgesInner16(yPlane, yOrigin, yStride, strength.Limit, strength.InteriorLimit, strength.HevThreshold);
						Vp8NormalLoopFilter.FilterLeftEdgeInner8(uPlane, uOrigin, uvStride, strength.Limit, strength.InteriorLimit, strength.HevThreshold);
						Vp8NormalLoopFilter.FilterLeftEdgeInner8(vPlane, uOrigin, uvStride, strength.Limit, strength.InteriorLimit, strength.HevThreshold);
					}

					if (mbY > 0)
					{
						Vp8NormalLoopFilter.FilterTopEdge16(yPlane, yOrigin, yStride, strength.Limit + 4, strength.InteriorLimit, strength.HevThreshold);
						Vp8NormalLoopFilter.FilterTopEdge8(uPlane, uOrigin, uvStride, strength.Limit + 4, strength.InteriorLimit, strength.HevThreshold);
						Vp8NormalLoopFilter.FilterTopEdge8(vPlane, uOrigin, uvStride, strength.Limit + 4, strength.InteriorLimit, strength.HevThreshold);
					}

					if (inner)
					{
						Vp8NormalLoopFilter.FilterTopEdgesInner16(yPlane, yOrigin, yStride, strength.Limit, strength.InteriorLimit, strength.HevThreshold);
						Vp8NormalLoopFilter.FilterTopEdgeInner8(uPlane, uOrigin, uvStride, strength.Limit, strength.InteriorLimit, strength.HevThreshold);
						Vp8NormalLoopFilter.FilterTopEdgeInner8(vPlane, uOrigin, uvStride, strength.Limit, strength.InteriorLimit, strength.HevThreshold);
					}
				}
			}
		}
	}
	private static Vp8DecodedFrame ProduceRgbFrame(
		byte[] yPlane,
		int yStride,
		byte[] uPlane,
		byte[] vPlane,
		int uvStride,
		int mbCols,
		int mbRows,
		int width,
		int height,
		Func<int, int, int> yOff,
		Func<int, int, int> uOff,
		byte[] alphaPlane)
	{
		IVp8ColorConverter converter = Vp8ColorConverterSelector.Instance;
		int bytesPerPixel = alphaPlane is null ? 3 : 4;
		// Exactly width * height * bytesPerPixel bytes -- callers (including tests) rely on Pixels.Length
		// being exact, so this is a plain heap allocation, not a pool rental.
		byte[] output = new byte[(long)width * height * bytesPerPixel];

		int chromaWidth = mbCols * 8;
		int chromaHeight = mbRows * 8;
		int uBase = uOff(0, 0);

		// Two full-width scratch rows for the upsampled chroma, rented rather than allocated: at most 16383
		// bytes each, so they stay resident in L1 across the whole frame.
		byte[] uRow = WebpBufferPool.Shared.Rent(width);
		byte[] vRow = WebpBufferPool.Shared.Rent(width);

		// Only rented when alpha is present -- see the type's remarks. Empty/unused in the common opaque case,
		// so that path allocates and touches nothing beyond what it always has.
		byte[] rgbRow = alphaPlane is null ? null : WebpBufferPool.Shared.Rent(width * 3);

		try
		{
			for (int y = 0; y < height; y++)
			{
				// Every output row interpolates between exactly two chroma rows: the one it sits closest to,
				// and its neighbour on the side the output row leans toward (clamped at the plane edges,
				// reproducing libwebp's edge replication). Resolving those once per row is what lets the
				// upsampler and converter run as row kernels instead of per-pixel calls.
				int nearRow = y >> 1;
				int farRow = Clamp((y >> 1) + (((y & 1) == 1) ? 1 : -1), chromaHeight - 1);

				var uNear = uPlane.AsSpan(uBase + (nearRow * uvStride), chromaWidth);
				var uFar = uPlane.AsSpan(uBase + (farRow * uvStride), chromaWidth);
				var vNear = vPlane.AsSpan(uBase + (nearRow * uvStride), chromaWidth);
				var vFar = vPlane.AsSpan(uBase + (farRow * uvStride), chromaWidth);

				Vp8ChromaUpsampler.UpsampleRow(uNear, uFar, uRow, width, chromaWidth);
				Vp8ChromaUpsampler.UpsampleRow(vNear, vFar, vRow, width, chromaWidth);

				var yRow = yPlane.AsSpan(yOff(0, y), width);

				if (alphaPlane is null)
				{
					converter.ConvertRow(yRow, uRow, vRow, output.AsSpan(y * width * 3, width * 3), width);
				}
				else
				{
					converter.ConvertRow(yRow, uRow, vRow, rgbRow.AsSpan(0, width * 3), width);
					SpliceAlphaRow(rgbRow, alphaPlane.AsSpan(y * width, width), output.AsSpan(y * width * 4, width * 4), width);
				}
			}
		}
		finally
		{
			WebpBufferPool.Shared.Return(uRow);
			WebpBufferPool.Shared.Return(vRow);

			if (rgbRow is not null)
			{
				WebpBufferPool.Shared.Return(rgbRow);
			}
		}

		return new Vp8DecodedFrame
		{
			Width = width,
			Height = height,
			PixelFormat = alphaPlane is not null ? PixelFormat.Rgba32 : PixelFormat.Rgb24,
			Pixels = output,
		};
	}
	private static void SpliceAlphaRow(ReadOnlySpan<byte> rgbRow, ReadOnlySpan<byte> alphaRow, Span<byte> rgbaRow, int width)
	{
		for (int x = 0; x < width; x++)
		{
			rgbaRow[(x * 4) + 0] = rgbRow[(x * 3) + 0];
			rgbaRow[(x * 4) + 1] = rgbRow[(x * 3) + 1];
			rgbaRow[(x * 4) + 2] = rgbRow[(x * 3) + 2];
			rgbaRow[(x * 4) + 3] = alphaRow[x];
		}
	}

	private static int Clamp(int value, int max) => value < 0 ? 0 : value > max ? max : value;
}
