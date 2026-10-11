namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Ecng.Drawing.Codecs.Formats.Webp.Kernels;

internal static class Vp8LPredictorTransform
{
	public static void ApplyInverse(Span<uint> pixels, Vp8LTransform transform)
	{
		int width = transform.Xsize;
		int height = transform.Ysize;
		int bits = transform.Bits;
		var tileData = transform.Data!;
		int tilesPerRow = Vp8LMetaHuffmanImage.SubSampleSize(width, bits);
		var kernel = Vp8LTransformKernelSelector.Instance;

		// Pixel (0,0): mode 0 (fixed opaque black), unconditionally.
		pixels[0] = Vp8LPixelMath.AddWrapping(pixels[0], 0xFF000000u);

		// Rest of row 0: mode 1 (left), unconditionally -- sequential, has an intra-row dependency.
		for (int x = 1; x < width; x++)
		{
			pixels[x] = Vp8LPixelMath.AddWrapping(pixels[x], pixels[x - 1]);
		}

		for (int y = 1; y < height; y++)
		{
			int rowBase = y * width;

			// First pixel of the row: mode 2 (top), unconditionally.
			pixels[rowBase] = Vp8LPixelMath.AddWrapping(pixels[rowBase], pixels[rowBase - width]);

			int x = 1;
			while (x < width)
			{
				int tileX = x >> bits;
				int mode = (int)((tileData[((y >> bits) * tilesPerRow) + tileX] >> 8) & 0xF);
				int tileEndX = Math.Min((tileX + 1) << bits, width);
				int runLength = tileEndX - x;

				ApplyRun(pixels, kernel, width, rowBase, x, runLength, mode);
				x = tileEndX;
			}
		}
	}

	private static void ApplyRun(Span<uint> pixels, IVp8LTransformKernel kernel, int width, int rowBase, int xStart, int runLength, int mode)
	{
		if (mode == 2)
		{
			// Mode 2 (pure "top") has no intra-run dependency at all -- vectorizable, same operation as
			// Png.Filtering.VectorizedRowFilter.UnfilterUp applied to this run's raw ARGB bytes.
			var rowBytes = MemoryMarshal.Cast<uint, byte>(pixels.Slice(rowBase + xStart, runLength));
			var topBytes = MemoryMarshal.Cast<uint, byte>(pixels.Slice(rowBase - width + xStart, runLength));
			kernel.PredictorTopInverse(rowBytes, topBytes);
			return;
		}

		// `mode` is constant for the whole run, so it is resolved to a concrete predictor here, once, rather
		// than re-tested per pixel. Each Run<T> instantiation is a separate specialization whose Predict call
		// the JIT devirtualizes and inlines, leaving a loop that does only that mode's arithmetic.
		switch (mode)
		{
			case 1: Run<LeftPredictor>(pixels, width, rowBase, xStart, runLength); return;
			case 3: Run<TopRightPredictor>(pixels, width, rowBase, xStart, runLength); return;
			case 4: Run<TopLeftPredictor>(pixels, width, rowBase, xStart, runLength); return;
			case 5: Run<Average3LeftTopTopRightPredictor>(pixels, width, rowBase, xStart, runLength); return;
			case 6: Run<Average2LeftTopLeftPredictor>(pixels, width, rowBase, xStart, runLength); return;
			case 7: Run<Average2LeftTopPredictor>(pixels, width, rowBase, xStart, runLength); return;
			case 8: Run<Average2TopLeftTopPredictor>(pixels, width, rowBase, xStart, runLength); return;
			case 9: Run<Average2TopTopRightPredictor>(pixels, width, rowBase, xStart, runLength); return;
			case 10: Run<Average4Predictor>(pixels, width, rowBase, xStart, runLength); return;
			case 11: Run<SelectPredictor>(pixels, width, rowBase, xStart, runLength); return;
			case 12: Run<ClampedAddSubtractFullPredictor>(pixels, width, rowBase, xStart, runLength); return;
			case 13: Run<ClampedAddSubtractHalfPredictor>(pixels, width, rowBase, xStart, runLength); return;

			// Mode 0, plus modes 14/15, which the spec assigns no meaning -- see Predict's remarks.
			default: Run<OpaqueBlackPredictor>(pixels, width, rowBase, xStart, runLength); return;
		}
	}
	private static void Run<TPredictor>(Span<uint> pixels, int width, int rowBase, int xStart, int runLength)
		where TPredictor : struct, IPredictorMode
	{
		// The caller only ever produces runs inside a row, at or past column 1, on a row at or past 1. That is
		// what puts every index below -- from `start - width - 1` up to `start + runLength - 1` -- in bounds,
		// and so what makes the unchecked ref arithmetic safe. Checked once here rather than per pixel.
		int start = rowBase + xStart;
		if (xStart < 1 || runLength < 1 || rowBase < width || start + runLength > pixels.Length || xStart + runLength > width)
		{
			throw new WebpDecodingException("Internal error: VP8L predictor run is out of bounds.");
		}

		ref uint origin = ref MemoryMarshal.GetReference(pixels);
		uint left = Unsafe.Add(ref origin, start - 1);
		uint topLeft = Unsafe.Add(ref origin, start - width - 1);
		uint top = Unsafe.Add(ref origin, start - width);

		for (int i = 0; i < runLength; i++)
		{
			ref uint current = ref Unsafe.Add(ref origin, start + i);
			uint topRight = Unsafe.Add(ref origin, start + i - width + 1);

			left = Vp8LPixelMath.AddWrapping(current, default(TPredictor).Predict(left, top, topLeft, topRight));
			current = left;

			topLeft = top;
			top = topRight;
		}
	}
	private interface IPredictorMode
	{
		uint Predict(uint left, uint top, uint topLeft, uint topRight);
	}

	private readonly struct OpaqueBlackPredictor : IPredictorMode
	{
		public uint Predict(uint left, uint top, uint topLeft, uint topRight) => 0xFF000000u;
	}

	private readonly struct LeftPredictor : IPredictorMode
	{
		public uint Predict(uint left, uint top, uint topLeft, uint topRight) => left;
	}

	private readonly struct TopRightPredictor : IPredictorMode
	{
		public uint Predict(uint left, uint top, uint topLeft, uint topRight) => topRight;
	}

	private readonly struct TopLeftPredictor : IPredictorMode
	{
		public uint Predict(uint left, uint top, uint topLeft, uint topRight) => topLeft;
	}

	private readonly struct Average3LeftTopTopRightPredictor : IPredictorMode
	{
		public uint Predict(uint left, uint top, uint topLeft, uint topRight) => Average3(left, top, topRight);
	}

	private readonly struct Average2LeftTopLeftPredictor : IPredictorMode
	{
		public uint Predict(uint left, uint top, uint topLeft, uint topRight) => Average2(left, topLeft);
	}

	private readonly struct Average2LeftTopPredictor : IPredictorMode
	{
		public uint Predict(uint left, uint top, uint topLeft, uint topRight) => Average2(left, top);
	}

	private readonly struct Average2TopLeftTopPredictor : IPredictorMode
	{
		public uint Predict(uint left, uint top, uint topLeft, uint topRight) => Average2(topLeft, top);
	}

	private readonly struct Average2TopTopRightPredictor : IPredictorMode
	{
		public uint Predict(uint left, uint top, uint topLeft, uint topRight) => Average2(top, topRight);
	}

	private readonly struct Average4Predictor : IPredictorMode
	{
		public uint Predict(uint left, uint top, uint topLeft, uint topRight) => Average4(left, topLeft, top, topRight);
	}

	private readonly struct SelectPredictor : IPredictorMode
	{
		public uint Predict(uint left, uint top, uint topLeft, uint topRight) => Select(top, left, topLeft);
	}

	private readonly struct ClampedAddSubtractFullPredictor : IPredictorMode
	{
		public uint Predict(uint left, uint top, uint topLeft, uint topRight) => ClampedAddSubtractFull(left, top, topLeft);
	}

	private readonly struct ClampedAddSubtractHalfPredictor : IPredictorMode
	{
		public uint Predict(uint left, uint top, uint topLeft, uint topRight) => ClampedAddSubtractHalf(left, top, topLeft);
	}
	internal static uint Predict(int mode, ReadOnlySpan<uint> pixels, int index, int width)
	{
		uint left = pixels[index - 1];
		uint top = pixels[index - width];

		switch (mode)
		{
			case 0: return 0xFF000000u;
			case 1: return left;
			case 2: return top;
		}

		uint topLeft = pixels[index - width - 1];
		uint topRight = pixels[index - width + 1];

		return mode switch
		{
			3 => topRight,
			4 => topLeft,
			5 => Average3(left, top, topRight),
			6 => Average2(left, topLeft),
			7 => Average2(left, top),
			8 => Average2(topLeft, top),
			9 => Average2(top, topRight),
			10 => Average4(left, topLeft, top, topRight),
			11 => Select(top, left, topLeft),
			12 => ClampedAddSubtractFull(left, top, topLeft),
			13 => ClampedAddSubtractHalf(left, top, topLeft),

			// Modes 14/15 are not assigned any meaning by the spec; a corrupt/hostile tile image could still
			// produce them (the field is 4 raw bits). Fall back to mode 0 rather than throw mid-pixel-loop,
			// consistent with this decoder's "stop early/degrade gracefully on malformed data" convention.
			_ => 0xFF000000u,
		};
	}

	private static uint Average2(uint a0, uint a1) => (((a0 ^ a1) & 0xFEFEFEFEu) >> 1) + (a0 & a1);

	private static uint Average3(uint a0, uint a1, uint a2) => Average2(Average2(a0, a2), a1);

	private static uint Average4(uint a0, uint a1, uint a2, uint a3) => Average2(Average2(a0, a1), Average2(a2, a3));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint Select(uint a, uint b, uint c)
	{
#if NET7_0_OR_GREATER
		if (!Vector128.IsHardwareAccelerated)
		{
			return SelectScalar(a, b, c);
		}

		var operands = Vector128.Create(b, a, 0u, 0u).AsByte();
		var reference = Vector128.Create(c, c, 0u, 0u).AsByte();
		ulong differences = (Vector128.Max(operands, reference) - Vector128.Min(operands, reference))
			.AsUInt64()
			.ToScalar();

		// Fold the eight |difference| bytes into four 16-bit slots -- no slot can overflow, since each holds
		// at most 255 + 255 -- then add the pairs: slots 0/1 are |b-c|'s four channels, slots 2/3 are |a-c|'s.
		const ulong ByteLanes = 0x00FF00FF00FF00FFu;
		ulong pairSums = (differences & ByteLanes) + ((differences >> 8) & ByteLanes);

		uint distanceToB = (uint)((pairSums & 0xFFFF) + ((pairSums >> 16) & 0xFFFF));
		uint distanceToA = (uint)(((pairSums >> 32) & 0xFFFF) + ((pairSums >> 48) & 0xFFFF));

		return distanceToB <= distanceToA ? a : b;
#else
		return SelectScalar(a, b, c);
#endif
	}
	internal static uint SelectScalar(uint a, uint b, uint c)
	{
		int paMinusPb =
			Sub3((int)(a >> 24), (int)(b >> 24), (int)(c >> 24)) +
			Sub3((int)((a >> 16) & 0xFF), (int)((b >> 16) & 0xFF), (int)((c >> 16) & 0xFF)) +
			Sub3((int)((a >> 8) & 0xFF), (int)((b >> 8) & 0xFF), (int)((c >> 8) & 0xFF)) +
			Sub3((int)(a & 0xFF), (int)(b & 0xFF), (int)(c & 0xFF));

		return paMinusPb <= 0 ? a : b;
	}

	private static int Sub3(int a, int b, int c) => Math.Abs(b - c) - Math.Abs(a - c);

	private static uint ClampedAddSubtractFull(uint c0, uint c1, uint c2)
	{
		int a = AddSubtractComponentFull((int)(c0 >> 24), (int)(c1 >> 24), (int)(c2 >> 24));
		int r = AddSubtractComponentFull((int)((c0 >> 16) & 0xFF), (int)((c1 >> 16) & 0xFF), (int)((c2 >> 16) & 0xFF));
		int g = AddSubtractComponentFull((int)((c0 >> 8) & 0xFF), (int)((c1 >> 8) & 0xFF), (int)((c2 >> 8) & 0xFF));
		int b = AddSubtractComponentFull((int)(c0 & 0xFF), (int)(c1 & 0xFF), (int)(c2 & 0xFF));
		return ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
	}

	private static int AddSubtractComponentFull(int a, int b, int c) => Clip255(a + b - c);

	private static uint ClampedAddSubtractHalf(uint c0, uint c1, uint c2)
	{
		uint average = Average2(c0, c1);
		int a = AddSubtractComponentHalf((int)(average >> 24), (int)(c2 >> 24));
		int r = AddSubtractComponentHalf((int)((average >> 16) & 0xFF), (int)((c2 >> 16) & 0xFF));
		int g = AddSubtractComponentHalf((int)((average >> 8) & 0xFF), (int)((c2 >> 8) & 0xFF));
		int b = AddSubtractComponentHalf((int)(average & 0xFF), (int)(c2 & 0xFF));
		return ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
	}

	private static int AddSubtractComponentHalf(int a, int b) => Clip255(a + ((a - b) / 2));

	private static int Clip255(int value) => Math.Clamp(value, 0, 255);
}
