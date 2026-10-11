namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

internal static class Av1InverseTransform
{
	private static readonly int[] Cos128Lookup =
	[
		4096, 4095, 4091, 4085, 4076, 4065, 4052, 4036,
		4017, 3996, 3973, 3948, 3920, 3889, 3857, 3822,
		3784, 3745, 3703, 3659, 3612, 3564, 3513, 3461,
		3406, 3349, 3290, 3229, 3166, 3102, 3035, 2967,
		2896, 2824, 2751, 2675, 2598, 2520, 2440, 2359,
		2276, 2191, 2106, 2019, 1931, 1842, 1751, 1660,
		1567, 1474, 1380, 1285, 1189, 1092, 995, 897,
		799, 700, 601, 501, 401, 301, 201, 101, 0,
	];
	private static readonly int[] TransformRowShift = [0, 1, 2, 2, 2, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2];

	private const int Sinpi1_9 = 1321;
	private const int Sinpi2_9 = 2482;
	private const int Sinpi3_9 = 3344;
	private const int Sinpi4_9 = 3803;
	private static readonly int[] Cos128Full = BuildCos128Full();

	private static int[] BuildCos128Full()
	{
		var table = new int[256];
		for (int angle2 = 0; angle2 < 256; angle2++)
		{
			table[angle2] = angle2 <= 64
				? Cos128Lookup[angle2]
				: angle2 <= 128
					? -Cos128Lookup[128 - angle2]
					: angle2 <= 192
						? -Cos128Lookup[angle2 - 128]
						: Cos128Lookup[256 - angle2];
		}

		return table;
	}

	private static int Cos128(int angle) => Cos128Full[angle & 255];

	private static int Sin128(int angle) => Cos128Full[(angle - 64) & 255];
	private static int Round2(long x, int n) => n == 0 ? (int)x : (int)((x + (1L << (n - 1))) >> n);
	private static readonly int[][] BrevTables = BuildBrevTables();

	private static int[][] BuildBrevTables()
	{
		var tables = new int[7][];
		for (int numBits = 0; numBits <= 6; numBits++)
		{
			var table = new int[1 << numBits];
			for (int x = 0; x < table.Length; x++)
			{
				int t = 0;
				for (int i = 0; i < numBits; i++)
				{
					int bit = (x >> i) & 1;
					t += bit << (numBits - 1 - i);
				}

				table[x] = t;
			}

			tables[numBits] = table;
		}

		return tables;
	}

	private static int Brev(int numBits, int x) => BrevTables[numBits][x];

	// Reusable scratch buffer for the permutation helpers below (InverseDctPermute/AdstInputPermute/
	// AdstOutputPermute), sized to the largest possible transform length (64) and reused across every
	// row/column permutation instead of Clone()-ing a fresh array each call -- these run twice per row
	// and twice per column of every transform block, the single hottest allocation site in reconstruction.
	// ThreadStatic (not an instance field, since this class has no instance) keeps concurrent decodes on
	// different threads from stomping on each other; a single tile's own decode is always sequential.
	[ThreadStatic]
	private static int[] _permuteScratch;

	private static int[] PermuteScratch() => _permuteScratch ??= new int[64];
	private static void B(int[] t, int a, int b, int angle, bool flip, int r)
	{
		_ = r; // bitstream-conformance precision bound only; not enforced here (see the class remarks)
		long x = ((long)t[a] * Cos128(angle)) - ((long)t[b] * Sin128(angle));
		long y = ((long)t[a] * Sin128(angle)) + ((long)t[b] * Cos128(angle));
		t[a] = Round2(x, 12);
		t[b] = Round2(y, 12);
		if (flip)
		{
			(t[a], t[b]) = (t[b], t[a]);
		}
	}
	private static void H(int[] t, int a, int b, bool flip, int r)
	{
		if (flip)
		{
			H(t, b, a, false, r);
			return;
		}

		int x = t[a];
		int y = t[b];
		int bound = 1 << (r - 1);
		t[a] = Math.Clamp(x + y, -bound, bound - 1);
		t[b] = Math.Clamp(x - y, -bound, bound - 1);
	}
	private static void InverseDctPermute(int[] t, int n)
	{
		var copy = PermuteScratch();
		int len = 1 << n;
		Array.Copy(t, copy, len);
		for (int i = 0; i < len; i++)
		{
			t[i] = copy[Brev(n, i)];
		}
	}
	internal static void InverseDct(int[] t, int n, int r)
	{
		InverseDctPermute(t, n);

		if (n == 6)
		{
			for (int i = 0; i <= 15; i++)
			{
				B(t, 32 + i, 63 - i, 63 - (4 * Brev(4, i)), false, r);
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 7; i++)
			{
				B(t, 16 + i, 31 - i, 6 + (Brev(3, 7 - i) << 3), false, r);
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 15; i++)
			{
				H(t, 32 + (i * 2), 33 + (i * 2), (i & 1) != 0, r);
			}
		}

		if (n >= 4)
		{
			for (int i = 0; i <= 3; i++)
			{
				B(t, 8 + i, 15 - i, 12 + (Brev(2, 3 - i) << 4), false, r);
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 7; i++)
			{
				H(t, 16 + (2 * i), 17 + (2 * i), (i & 1) != 0, r);
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 3; i++)
			{
				for (int j = 0; j <= 1; j++)
				{
					B(t, 62 - (i * 4) - j, 33 + (i * 4) + j, 60 - (16 * Brev(2, i)) + (64 * j), true, r);
				}
			}
		}

		if (n >= 3)
		{
			for (int i = 0; i <= 1; i++)
			{
				B(t, 4 + i, 7 - i, 56 - (32 * i), false, r);
			}
		}

		if (n >= 4)
		{
			for (int i = 0; i <= 3; i++)
			{
				H(t, 8 + (2 * i), 9 + (2 * i), (i & 1) != 0, r);
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 1; i++)
			{
				for (int j = 0; j <= 1; j++)
				{
					B(t, 30 - (4 * i) - j, 17 + (4 * i) + j, 24 + (j << 6) + ((1 - i) << 5), true, r);
				}
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 7; i++)
			{
				for (int j = 0; j <= 1; j++)
				{
					H(t, 32 + (i * 4) + j, 35 + (i * 4) - j, (i & 1) != 0, r);
				}
			}
		}

		for (int i = 0; i <= 1; i++)
		{
			B(t, 2 * i, (2 * i) + 1, 32 + (16 * i), (1 - i) != 0, r);
		}

		if (n >= 3)
		{
			for (int i = 0; i <= 1; i++)
			{
				H(t, 4 + (2 * i), 5 + (2 * i), i != 0, r);
			}
		}

		if (n >= 4)
		{
			for (int i = 0; i <= 1; i++)
			{
				B(t, 14 - i, 9 + i, 48 + (64 * i), true, r);
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 3; i++)
			{
				for (int j = 0; j <= 1; j++)
				{
					H(t, 16 + (4 * i) + j, 19 + (4 * i) - j, (i & 1) != 0, r);
				}
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 1; i++)
			{
				for (int j = 0; j <= 3; j++)
				{
					B(t, 61 - (i * 8) - j, 34 + (i * 8) + j, 56 - (i * 32) + ((j >> 1) * 64), true, r);
				}
			}
		}

		for (int i = 0; i <= 1; i++)
		{
			H(t, i, 3 - i, false, r);
		}

		if (n >= 3)
		{
			B(t, 6, 5, 32, true, r);
		}

		if (n >= 4)
		{
			for (int i = 0; i <= 1; i++)
			{
				for (int j = 0; j <= 1; j++)
				{
					H(t, 8 + (4 * i) + j, 11 + (4 * i) - j, i != 0, r);
				}
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 3; i++)
			{
				B(t, 29 - i, 18 + i, 48 + ((i >> 1) * 64), true, r);
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 3; i++)
			{
				for (int j = 0; j <= 3; j++)
				{
					H(t, 32 + (8 * i) + j, 39 + (8 * i) - j, (i & 1) != 0, r);
				}
			}
		}

		if (n >= 3)
		{
			for (int i = 0; i <= 3; i++)
			{
				H(t, i, 7 - i, false, r);
			}
		}

		if (n >= 4)
		{
			for (int i = 0; i <= 1; i++)
			{
				B(t, 13 - i, 10 + i, 32, true, r);
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 1; i++)
			{
				for (int j = 0; j <= 3; j++)
				{
					H(t, 16 + (i * 8) + j, 23 + (i * 8) - j, i != 0, r);
				}
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 7; i++)
			{
				B(t, 59 - i, 36 + i, i < 4 ? 48 : 112, true, r);
			}
		}

		if (n >= 4)
		{
			for (int i = 0; i <= 7; i++)
			{
				H(t, i, 15 - i, false, r);
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 3; i++)
			{
				B(t, 27 - i, 20 + i, 32, true, r);
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 7; i++)
			{
				H(t, 32 + i, 47 - i, false, r);
				H(t, 48 + i, 63 - i, true, r);
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 15; i++)
			{
				H(t, i, 31 - i, false, r);
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 7; i++)
			{
				B(t, 55 - i, 40 + i, 32, true, r);
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 31; i++)
			{
				H(t, i, 63 - i, false, r);
			}
		}
	}
	private static void InverseAdst4(int[] t, int r)
	{
		_ = r;

		long s0 = (long)Sinpi1_9 * t[0];
		long s1 = (long)Sinpi2_9 * t[0];
		long s2 = (long)Sinpi3_9 * t[1];
		long s3 = (long)Sinpi4_9 * t[2];
		long s4 = (long)Sinpi1_9 * t[2];
		long s5 = (long)Sinpi2_9 * t[3];
		long s6 = (long)Sinpi4_9 * t[3];
		long a7 = t[0] - t[2];
		long b7 = a7 + t[3];

		s0 += s3;
		s1 -= s4;
		s3 = s2;
		s2 = (long)Sinpi3_9 * b7;
		s0 += s5;
		s1 -= s6;

		long x0 = s0 + s3;
		long x1 = s1 + s3;
		long x2 = s2;
		long x3 = s0 + s1 - s3;

		t[0] = Round2(x0, 12);
		t[1] = Round2(x1, 12);
		t[2] = Round2(x2, 12);
		t[3] = Round2(x3, 12);
	}
	private static void AdstInputPermute(int[] t, int n)
	{
		int n0 = 1 << n;
		var copy = PermuteScratch();
		Array.Copy(t, copy, n0);
		for (int i = 0; i < n0; i++)
		{
			int idx = (i & 1) != 0 ? i - 1 : n0 - i - 1;
			t[i] = copy[idx];
		}
	}
	private static void AdstOutputPermute(int[] t, int n)
	{
		int n0 = 1 << n;
		var copy = PermuteScratch();
		Array.Copy(t, copy, n0);
		for (int i = 0; i < n0; i++)
		{
			int a = (i >> 3) & 1;
			int b = ((i >> 2) & 1) ^ ((i >> 3) & 1);
			int c = ((i >> 1) & 1) ^ ((i >> 2) & 1);
			int d = (i & 1) ^ ((i >> 1) & 1);
			int idx = ((d << 3) | (c << 2) | (b << 1) | a) >> (4 - n);
			t[i] = (i & 1) != 0 ? -copy[idx] : copy[idx];
		}
	}
	private static void InverseAdst8(int[] t, int r)
	{
		AdstInputPermute(t, 3);
		for (int i = 0; i <= 3; i++)
		{
			B(t, 2 * i, (2 * i) + 1, 60 - (16 * i), true, r);
		}

		for (int i = 0; i <= 3; i++)
		{
			H(t, i, 4 + i, false, r);
		}

		for (int i = 0; i <= 1; i++)
		{
			B(t, 4 + (3 * i), 5 + i, 48 - (32 * i), true, r);
		}

		for (int i = 0; i <= 1; i++)
		{
			for (int j = 0; j <= 1; j++)
			{
				H(t, (4 * j) + i, 2 + (4 * j) + i, false, r);
			}
		}

		for (int i = 0; i <= 1; i++)
		{
			B(t, 2 + (4 * i), 3 + (4 * i), 32, true, r);
		}

		AdstOutputPermute(t, 3);
	}
	private static void InverseAdst16(int[] t, int r)
	{
		AdstInputPermute(t, 4);
		for (int i = 0; i <= 7; i++)
		{
			B(t, 2 * i, (2 * i) + 1, 62 - (8 * i), true, r);
		}

		for (int i = 0; i <= 7; i++)
		{
			H(t, i, 8 + i, false, r);
		}

		for (int i = 0; i <= 1; i++)
		{
			B(t, 8 + (2 * i), 9 + (2 * i), 56 - (32 * i), true, r);
			B(t, 13 + (2 * i), 12 + (2 * i), 8 + (32 * i), true, r);
		}

		for (int i = 0; i <= 3; i++)
		{
			for (int j = 0; j <= 1; j++)
			{
				H(t, (8 * j) + i, 4 + (8 * j) + i, false, r);
			}
		}

		for (int i = 0; i <= 1; i++)
		{
			for (int j = 0; j <= 1; j++)
			{
				B(t, 4 + (8 * j) + (3 * i), 5 + (8 * j) + i, 48 - (32 * i), true, r);
			}
		}

		for (int i = 0; i <= 1; i++)
		{
			for (int j = 0; j <= 3; j++)
			{
				H(t, (4 * j) + i, 2 + (4 * j) + i, false, r);
			}
		}

		for (int i = 0; i <= 3; i++)
		{
			B(t, 2 + (4 * i), 3 + (4 * i), 32, true, r);
		}

		AdstOutputPermute(t, 4);
	}
	internal static void InverseAdst(int[] t, int n, int r)
	{
		switch (n)
		{
			case 2:
				InverseAdst4(t, r);
				break;
			case 3:
				InverseAdst8(t, r);
				break;
			default:
				InverseAdst16(t, r);
				break;
		}
	}
	private static void InverseWht(int[] t, int shift)
	{
		int a = t[0] >> shift;
		int c = t[1] >> shift;
		int d = t[2] >> shift;
		int b = t[3] >> shift;

		a += c;
		d -= b;
		int e = (a - d) >> 1;
		b = e - b;
		c = e - c;
		a -= b;
		d += c;

		t[0] = a;
		t[1] = b;
		t[2] = c;
		t[3] = d;
	}
	internal static void InverseIdentity(int[] t, int n)
	{
		switch (n)
		{
			case 2:
				for (int i = 0; i < 4; i++)
				{
					t[i] = Round2((long)t[i] * 5793, 12);
				}

				break;
			case 3:
				for (int i = 0; i < 8; i++)
				{
					t[i] *= 2;
				}

				break;
			case 4:
				for (int i = 0; i < 16; i++)
				{
					t[i] = Round2((long)t[i] * 11586, 12);
				}

				break;
			default:
				for (int i = 0; i < 32; i++)
				{
					t[i] *= 4;
				}

				break;
		}
	}

	// ---- Batched (4-row/4-column) SIMD path ----
	//
	// Real AV1 SIMD kernels vectorize the inverse transform by batching multiple independent rows (or
	// columns) into parallel lanes, not by vectorizing *within* one row's own butterfly network: every row
	// of a 2D block undergoes the *identical* sequence of B/H steps with the *identical* angle at each step
	// (only the data differs), so batching rows means each step's trig constant broadcasts to every lane
	// instead of needing an awkward, error-prone per-lane gather of different angles. Vector256<long> (4
	// lanes) matches B's own long intermediate-precision requirement, so this batches 4 rows (or 4 columns)
	// at a time -- always exact, no remainder tail, since every real AV1 transform dimension (4/8/16/32/64)
	// is a multiple of 4.
	//
	// Every place the scalar butterfly network above stores into its `int[] t` array implicitly truncates
	// to 32-bit two's-complement precision (spec's own int32-typed T[]); TruncateToInt32Batch/RoundBatch
	// below replicate that truncation for the batched Vector256<long> lanes so this stays bit-identical to
	// the scalar path above, not just numerically close -- Round2's own call sites always immediately store
	// what it returns, so the truncation is folded into RoundBatch itself rather than repeated at each site.
#if NET7_0_OR_GREATER
	private static readonly Vector256<long> Int32Mask = Vector256.Create(0xFFFFFFFFL);
	private static readonly Vector256<long> Int32SignBit = Vector256.Create(0x80000000L);

	private static Vector256<long> TruncateToInt32Batch(Vector256<long> x) => ((x & Int32Mask) ^ Int32SignBit) - Int32SignBit;

	private static Vector256<long> RoundBatch(Vector256<long> x, int n)
	{
		Vector256<long> result = n == 0 ? x : Vector256.ShiftRightArithmetic(x + Vector256.Create(1L << (n - 1)), n);
		return TruncateToInt32Batch(result);
	}
	private static void BBatch(Span<Vector256<long>> t, int a, int b, int angle, bool flip, int r)
	{
		_ = r;
		var cos = Vector256.Create((long)Cos128(angle));
		var sin = Vector256.Create((long)Sin128(angle));
		Vector256<long> x = (t[a] * cos) - (t[b] * sin);
		Vector256<long> y = (t[a] * sin) + (t[b] * cos);
		Vector256<long> ra = RoundBatch(x, 12);
		Vector256<long> rb = RoundBatch(y, 12);
		if (flip)
		{
			(ra, rb) = (rb, ra);
		}

		t[a] = ra;
		t[b] = rb;
	}
	private static void HBatch(Span<Vector256<long>> t, int a, int b, bool flip, int r)
	{
		if (flip)
		{
			HBatch(t, b, a, false, r);
			return;
		}

		Vector256<long> x = t[a];
		Vector256<long> y = t[b];
		long bound = 1L << (r - 1);
		var lo = Vector256.Create(-bound);
		var hi = Vector256.Create(bound - 1);
		t[a] = Vector256.Min(Vector256.Max(x + y, lo), hi);
		t[b] = Vector256.Min(Vector256.Max(x - y, lo), hi);
	}

	[ThreadStatic]
	private static Vector256<long>[] _permuteScratchBatch;

	private static Vector256<long>[] PermuteScratchBatch() => _permuteScratchBatch ??= new Vector256<long>[64];

	private static void InverseDctPermuteBatch(Span<Vector256<long>> t, int n)
	{
		var copy = PermuteScratchBatch();
		int len = 1 << n;
		t[..len].CopyTo(copy);
		for (int i = 0; i < len; i++)
		{
			t[i] = copy[Brev(n, i)];
		}
	}
	internal static void InverseDctBatch(Span<Vector256<long>> t, int n, int r)
	{
		InverseDctPermuteBatch(t, n);

		if (n == 6)
		{
			for (int i = 0; i <= 15; i++)
			{
				BBatch(t, 32 + i, 63 - i, 63 - (4 * Brev(4, i)), false, r);
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 7; i++)
			{
				BBatch(t, 16 + i, 31 - i, 6 + (Brev(3, 7 - i) << 3), false, r);
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 15; i++)
			{
				HBatch(t, 32 + (i * 2), 33 + (i * 2), (i & 1) != 0, r);
			}
		}

		if (n >= 4)
		{
			for (int i = 0; i <= 3; i++)
			{
				BBatch(t, 8 + i, 15 - i, 12 + (Brev(2, 3 - i) << 4), false, r);
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 7; i++)
			{
				HBatch(t, 16 + (2 * i), 17 + (2 * i), (i & 1) != 0, r);
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 3; i++)
			{
				for (int j = 0; j <= 1; j++)
				{
					BBatch(t, 62 - (i * 4) - j, 33 + (i * 4) + j, 60 - (16 * Brev(2, i)) + (64 * j), true, r);
				}
			}
		}

		if (n >= 3)
		{
			for (int i = 0; i <= 1; i++)
			{
				BBatch(t, 4 + i, 7 - i, 56 - (32 * i), false, r);
			}
		}

		if (n >= 4)
		{
			for (int i = 0; i <= 3; i++)
			{
				HBatch(t, 8 + (2 * i), 9 + (2 * i), (i & 1) != 0, r);
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 1; i++)
			{
				for (int j = 0; j <= 1; j++)
				{
					BBatch(t, 30 - (4 * i) - j, 17 + (4 * i) + j, 24 + (j << 6) + ((1 - i) << 5), true, r);
				}
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 7; i++)
			{
				for (int j = 0; j <= 1; j++)
				{
					HBatch(t, 32 + (i * 4) + j, 35 + (i * 4) - j, (i & 1) != 0, r);
				}
			}
		}

		for (int i = 0; i <= 1; i++)
		{
			BBatch(t, 2 * i, (2 * i) + 1, 32 + (16 * i), (1 - i) != 0, r);
		}

		if (n >= 3)
		{
			for (int i = 0; i <= 1; i++)
			{
				HBatch(t, 4 + (2 * i), 5 + (2 * i), i != 0, r);
			}
		}

		if (n >= 4)
		{
			for (int i = 0; i <= 1; i++)
			{
				BBatch(t, 14 - i, 9 + i, 48 + (64 * i), true, r);
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 3; i++)
			{
				for (int j = 0; j <= 1; j++)
				{
					HBatch(t, 16 + (4 * i) + j, 19 + (4 * i) - j, (i & 1) != 0, r);
				}
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 1; i++)
			{
				for (int j = 0; j <= 3; j++)
				{
					BBatch(t, 61 - (i * 8) - j, 34 + (i * 8) + j, 56 - (i * 32) + ((j >> 1) * 64), true, r);
				}
			}
		}

		for (int i = 0; i <= 1; i++)
		{
			HBatch(t, i, 3 - i, false, r);
		}

		if (n >= 3)
		{
			BBatch(t, 6, 5, 32, true, r);
		}

		if (n >= 4)
		{
			for (int i = 0; i <= 1; i++)
			{
				for (int j = 0; j <= 1; j++)
				{
					HBatch(t, 8 + (4 * i) + j, 11 + (4 * i) - j, i != 0, r);
				}
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 3; i++)
			{
				BBatch(t, 29 - i, 18 + i, 48 + ((i >> 1) * 64), true, r);
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 3; i++)
			{
				for (int j = 0; j <= 3; j++)
				{
					HBatch(t, 32 + (8 * i) + j, 39 + (8 * i) - j, (i & 1) != 0, r);
				}
			}
		}

		if (n >= 3)
		{
			for (int i = 0; i <= 3; i++)
			{
				HBatch(t, i, 7 - i, false, r);
			}
		}

		if (n >= 4)
		{
			for (int i = 0; i <= 1; i++)
			{
				BBatch(t, 13 - i, 10 + i, 32, true, r);
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 1; i++)
			{
				for (int j = 0; j <= 3; j++)
				{
					HBatch(t, 16 + (i * 8) + j, 23 + (i * 8) - j, i != 0, r);
				}
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 7; i++)
			{
				BBatch(t, 59 - i, 36 + i, i < 4 ? 48 : 112, true, r);
			}
		}

		if (n >= 4)
		{
			for (int i = 0; i <= 7; i++)
			{
				HBatch(t, i, 15 - i, false, r);
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 3; i++)
			{
				BBatch(t, 27 - i, 20 + i, 32, true, r);
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 7; i++)
			{
				HBatch(t, 32 + i, 47 - i, false, r);
				HBatch(t, 48 + i, 63 - i, true, r);
			}
		}

		if (n >= 5)
		{
			for (int i = 0; i <= 15; i++)
			{
				HBatch(t, i, 31 - i, false, r);
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 7; i++)
			{
				BBatch(t, 55 - i, 40 + i, 32, true, r);
			}
		}

		if (n == 6)
		{
			for (int i = 0; i <= 31; i++)
			{
				HBatch(t, i, 63 - i, false, r);
			}
		}
	}

	private static void AdstInputPermuteBatch(Span<Vector256<long>> t, int n)
	{
		int n0 = 1 << n;
		var copy = PermuteScratchBatch();
		t[..n0].CopyTo(copy);
		for (int i = 0; i < n0; i++)
		{
			int idx = (i & 1) != 0 ? i - 1 : n0 - i - 1;
			t[i] = copy[idx];
		}
	}

	private static void AdstOutputPermuteBatch(Span<Vector256<long>> t, int n)
	{
		int n0 = 1 << n;
		var copy = PermuteScratchBatch();
		t[..n0].CopyTo(copy);
		for (int i = 0; i < n0; i++)
		{
			int a = (i >> 3) & 1;
			int b = ((i >> 2) & 1) ^ ((i >> 3) & 1);
			int c = ((i >> 1) & 1) ^ ((i >> 2) & 1);
			int d = (i & 1) ^ ((i >> 1) & 1);
			int idx = ((d << 3) | (c << 2) | (b << 1) | a) >> (4 - n);
			t[i] = (i & 1) != 0 ? TruncateToInt32Batch(-copy[idx]) : copy[idx];
		}
	}

	private static void InverseAdst4Batch(Span<Vector256<long>> t, int r)
	{
		_ = r;
		var sinpi1 = Vector256.Create((long)Sinpi1_9);
		var sinpi2 = Vector256.Create((long)Sinpi2_9);
		var sinpi3 = Vector256.Create((long)Sinpi3_9);
		var sinpi4 = Vector256.Create((long)Sinpi4_9);

		Vector256<long> s0 = sinpi1 * t[0];
		Vector256<long> s1 = sinpi2 * t[0];
		Vector256<long> s2 = sinpi3 * t[1];
		Vector256<long> s3 = sinpi4 * t[2];
		Vector256<long> s4 = sinpi1 * t[2];
		Vector256<long> s5 = sinpi2 * t[3];
		Vector256<long> s6 = sinpi4 * t[3];
		Vector256<long> a7 = t[0] - t[2];
		Vector256<long> b7 = a7 + t[3];

		s0 += s3;
		s1 -= s4;
		s3 = s2;
		s2 = sinpi3 * b7;
		s0 += s5;
		s1 -= s6;

		Vector256<long> x0 = s0 + s3;
		Vector256<long> x1 = s1 + s3;
		Vector256<long> x2 = s2;
		Vector256<long> x3 = s0 + s1 - s3;

		t[0] = RoundBatch(x0, 12);
		t[1] = RoundBatch(x1, 12);
		t[2] = RoundBatch(x2, 12);
		t[3] = RoundBatch(x3, 12);
	}

	private static void InverseAdst8Batch(Span<Vector256<long>> t, int r)
	{
		AdstInputPermuteBatch(t, 3);
		for (int i = 0; i <= 3; i++)
		{
			BBatch(t, 2 * i, (2 * i) + 1, 60 - (16 * i), true, r);
		}

		for (int i = 0; i <= 3; i++)
		{
			HBatch(t, i, 4 + i, false, r);
		}

		for (int i = 0; i <= 1; i++)
		{
			BBatch(t, 4 + (3 * i), 5 + i, 48 - (32 * i), true, r);
		}

		for (int i = 0; i <= 1; i++)
		{
			for (int j = 0; j <= 1; j++)
			{
				HBatch(t, (4 * j) + i, 2 + (4 * j) + i, false, r);
			}
		}

		for (int i = 0; i <= 1; i++)
		{
			BBatch(t, 2 + (4 * i), 3 + (4 * i), 32, true, r);
		}

		AdstOutputPermuteBatch(t, 3);
	}

	private static void InverseAdst16Batch(Span<Vector256<long>> t, int r)
	{
		AdstInputPermuteBatch(t, 4);
		for (int i = 0; i <= 7; i++)
		{
			BBatch(t, 2 * i, (2 * i) + 1, 62 - (8 * i), true, r);
		}

		for (int i = 0; i <= 7; i++)
		{
			HBatch(t, i, 8 + i, false, r);
		}

		for (int i = 0; i <= 1; i++)
		{
			BBatch(t, 8 + (2 * i), 9 + (2 * i), 56 - (32 * i), true, r);
			BBatch(t, 13 + (2 * i), 12 + (2 * i), 8 + (32 * i), true, r);
		}

		for (int i = 0; i <= 3; i++)
		{
			for (int j = 0; j <= 1; j++)
			{
				HBatch(t, (8 * j) + i, 4 + (8 * j) + i, false, r);
			}
		}

		for (int i = 0; i <= 1; i++)
		{
			for (int j = 0; j <= 1; j++)
			{
				BBatch(t, 4 + (8 * j) + (3 * i), 5 + (8 * j) + i, 48 - (32 * i), true, r);
			}
		}

		for (int i = 0; i <= 1; i++)
		{
			for (int j = 0; j <= 3; j++)
			{
				HBatch(t, (4 * j) + i, 2 + (4 * j) + i, false, r);
			}
		}

		for (int i = 0; i <= 3; i++)
		{
			BBatch(t, 2 + (4 * i), 3 + (4 * i), 32, true, r);
		}

		AdstOutputPermuteBatch(t, 4);
	}

	internal static void InverseAdstBatch(Span<Vector256<long>> t, int n, int r)
	{
		switch (n)
		{
			case 2:
				InverseAdst4Batch(t, r);
				break;
			case 3:
				InverseAdst8Batch(t, r);
				break;
			default:
				InverseAdst16Batch(t, r);
				break;
		}
	}

	private static void InverseWhtBatch(Span<Vector256<long>> t, int shift)
	{
		Vector256<long> a = Vector256.ShiftRightArithmetic(t[0], shift);
		Vector256<long> c = Vector256.ShiftRightArithmetic(t[1], shift);
		Vector256<long> d = Vector256.ShiftRightArithmetic(t[2], shift);
		Vector256<long> b = Vector256.ShiftRightArithmetic(t[3], shift);

		a += c;
		d -= b;
		Vector256<long> e = Vector256.ShiftRightArithmetic(a - d, 1);
		b = e - b;
		c = e - c;
		a -= b;
		d += c;

		t[0] = TruncateToInt32Batch(a);
		t[1] = TruncateToInt32Batch(b);
		t[2] = TruncateToInt32Batch(c);
		t[3] = TruncateToInt32Batch(d);
	}

	internal static void InverseIdentityBatch(Span<Vector256<long>> t, int n)
	{
		switch (n)
		{
			case 2:
				var scale2 = Vector256.Create(5793L);
				for (int i = 0; i < 4; i++)
				{
					t[i] = RoundBatch(t[i] * scale2, 12);
				}

				break;
			case 3:
				for (int i = 0; i < 8; i++)
				{
					t[i] = TruncateToInt32Batch(t[i] * 2);
				}

				break;
			case 4:
				var scale4 = Vector256.Create(11586L);
				for (int i = 0; i < 16; i++)
				{
					t[i] = RoundBatch(t[i] * scale4, 12);
				}

				break;
			default:
				for (int i = 0; i < 32; i++)
				{
					t[i] = TruncateToInt32Batch(t[i] * 4);
				}

				break;
		}
	}

	[ThreadStatic]
	private static Vector256<long>[] _rowScratchBatch;

	[ThreadStatic]
	private static Vector256<long>[] _colScratchBatch;

	private static Vector256<long>[] RowScratchBatch() => _rowScratchBatch ??= new Vector256<long>[64];

	private static Vector256<long>[] ColScratchBatch() => _colScratchBatch ??= new Vector256<long>[64];
	// `t` (the only stackalloc in this method) is fully overwritten, index by index, for j in [0, w) at the
	// top of every row iteration below, before any read -- same reasoning as Av1ForwardTransform.Forward2D's
	// own [SkipLocalsInit] (see that method's remarks), and this is the single hottest per-block call in the
	// entire decoder (called via both real decode and every encoder-side RDO reconstruction trial).
	public static void Inverse2D(int[] dequant, int[] residual, int txSz, int planeTxType, bool lossless, int bitDepth)
	{
		int log2W = Av1TxDimensions.WidthLog2[txSz];
		int log2H = Av1TxDimensions.HeightLog2[txSz];
		int w = 1 << log2W;
		int h = 1 << log2H;

		int rowShift = lossless ? 0 : TransformRowShift[txSz];
		int colShift = lossless ? 0 : 4;
		int rowClampRange = bitDepth + 8;
		int colClampRange = Math.Max(bitDepth + 6, 16);

		Span<Vector256<long>> t = stackalloc Vector256<long>[64];
		var tRow = RowScratchBatch();

		bool rescale = Math.Abs(log2W - log2H) == 1;
		var rescaleFactor = Vector256.Create(2896L);

		// Row pass: 4 rows at a time -- h is always a multiple of 4 (every real AV1 transform dimension is
		// a power of 2 >= 4), so this is an exact partition with no scalar remainder tail. Every row
		// undergoes the identical sequence of butterfly steps (same tx type, same angles at each step), so
		// batching 4 rows into Vector256<long> lanes is a direct, faithful parallelization of what was
		// previously h separate calls to the scalar row transform above -- see this class's own remarks
		// above InverseDctBatch for why this axis (batching independent rows), not vectorizing within one
		// row's own butterfly network, is what real AV1 SIMD kernels do.
		for (int i = 0; i < h; i += 4)
		{
			for (int j = 0; j < w; j++)
			{
				long v0 = i < 32 && j < 32 ? dequant[(i * 64) + j] : 0;
				long v1 = i + 1 < 32 && j < 32 ? dequant[((i + 1) * 64) + j] : 0;
				long v2 = i + 2 < 32 && j < 32 ? dequant[((i + 2) * 64) + j] : 0;
				long v3 = i + 3 < 32 && j < 32 ? dequant[((i + 3) * 64) + j] : 0;
				t[j] = Vector256.Create(v0, v1, v2, v3);
			}

			if (rescale)
			{
				for (int j = 0; j < w; j++)
				{
					t[j] = RoundBatch(t[j] * rescaleFactor, 12);
				}
			}

			for (int j = 0; j < w; j++)
			{
				tRow[j] = t[j];
			}

			if (lossless)
			{
				InverseWhtBatch(tRow, 2);
			}
			else if (planeTxType is Av1TxType.DctDct or Av1TxType.AdstDct or Av1TxType.FlipadstDct or Av1TxType.HDct)
			{
				InverseDctBatch(tRow, log2W, rowClampRange);
			}
			else if (planeTxType is Av1TxType.DctAdst or Av1TxType.AdstAdst or Av1TxType.DctFlipadst or Av1TxType.FlipadstFlipadst or Av1TxType.AdstFlipadst or Av1TxType.FlipadstAdst or Av1TxType.HAdst or Av1TxType.HFlipadst)
			{
				InverseAdstBatch(tRow, log2W, rowClampRange);
			}
			else
			{
				InverseIdentityBatch(tRow, log2W);
			}

			for (int j = 0; j < w; j++)
			{
				Vector256<long> rounded = RoundBatch(tRow[j], rowShift);
				residual[(i * w) + j] = (int)Vector256.GetElement(rounded, 0);
				residual[((i + 1) * w) + j] = (int)Vector256.GetElement(rounded, 1);
				residual[((i + 2) * w) + j] = (int)Vector256.GetElement(rounded, 2);
				residual[((i + 3) * w) + j] = (int)Vector256.GetElement(rounded, 3);
			}
		}

		// residual[(i*w)+j] for i in [0,h), j in [0,w) is exactly the flat range [0, h*w) in row-major
		// order, so this is a single unconditional elementwise clamp over the whole buffer -- no per-row
		// structure to preserve -- vectorizable as one flat pass regardless of w/h individually.
		int colBound = 1 << (colClampRange - 1);
		int total = h * w;
		int k = 0;
		if (Vector256.IsHardwareAccelerated)
		{
			var lo = Vector256.Create(-colBound);
			var hi = Vector256.Create(colBound - 1);
			for (; k + 8 <= total; k += 8)
			{
				var v = Vector256.LoadUnsafe(ref residual[k]);
				var clamped = Vector256.Min(Vector256.Max(v, lo), hi);
				clamped.StoreUnsafe(ref residual[k]);
			}
		}

		for (; k < total; k++)
		{
			residual[k] = Math.Clamp(residual[k], -colBound, colBound - 1);
		}

		// Column pass: 4 columns at a time, same reasoning (w is always a multiple of 4 too).
		var tCol = ColScratchBatch();
		for (int j = 0; j < w; j += 4)
		{
			for (int i = 0; i < h; i++)
			{
				tCol[i] = Vector256.Create(
					(long)residual[(i * w) + j],
					(long)residual[(i * w) + j + 1],
					(long)residual[(i * w) + j + 2],
					(long)residual[(i * w) + j + 3]);
			}

			if (lossless)
			{
				InverseWhtBatch(tCol, 0);
			}
			else if (planeTxType is Av1TxType.DctDct or Av1TxType.DctAdst or Av1TxType.DctFlipadst or Av1TxType.VDct)
			{
				InverseDctBatch(tCol, log2H, colClampRange);
			}
			else if (planeTxType is Av1TxType.AdstDct or Av1TxType.AdstAdst or Av1TxType.FlipadstDct or Av1TxType.FlipadstFlipadst or Av1TxType.AdstFlipadst or Av1TxType.FlipadstAdst or Av1TxType.VAdst or Av1TxType.VFlipadst)
			{
				InverseAdstBatch(tCol, log2H, colClampRange);
			}
			else
			{
				InverseIdentityBatch(tCol, log2H);
			}

			for (int i = 0; i < h; i++)
			{
				Vector256<long> rounded = RoundBatch(tCol[i], colShift);
				residual[(i * w) + j] = (int)Vector256.GetElement(rounded, 0);
				residual[(i * w) + j + 1] = (int)Vector256.GetElement(rounded, 1);
				residual[(i * w) + j + 2] = (int)Vector256.GetElement(rounded, 2);
				residual[(i * w) + j + 3] = (int)Vector256.GetElement(rounded, 3);
			}
		}
	}
#else
	public static void Inverse2D(int[] dequant, int[] residual, int txSz, int planeTxType, bool lossless, int bitDepth)
	{
		var log2W = Av1TxDimensions.WidthLog2[txSz];
		var log2H = Av1TxDimensions.HeightLog2[txSz];
		var width = 1 << log2W;
		var height = 1 << log2H;
		var rowShift = lossless ? 0 : TransformRowShift[txSz];
		var colShift = lossless ? 0 : 4;
		var rowRange = bitDepth + 8;
		var colRange = Math.Max(bitDepth + 6, 16);
		var bound = 1 << (colRange - 1);
		var samples = new int[64];

		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < width; x++)
			{
				var value = y < 32 && x < 32 ? dequant[y * 64 + x] : 0;
				samples[x] = Math.Abs(log2W - log2H) == 1 ? Round2((long)value * 2896, 12) : value;
			}

			if (lossless)
				InverseWht(samples, 2);
			else if (planeTxType is Av1TxType.DctDct or Av1TxType.AdstDct or Av1TxType.FlipadstDct or Av1TxType.HDct)
				InverseDct(samples, log2W, rowRange);
			else if (planeTxType is Av1TxType.DctAdst or Av1TxType.AdstAdst or Av1TxType.DctFlipadst or Av1TxType.FlipadstFlipadst or Av1TxType.AdstFlipadst or Av1TxType.FlipadstAdst or Av1TxType.HAdst or Av1TxType.HFlipadst)
				InverseAdst(samples, log2W, rowRange);
			else
				InverseIdentity(samples, log2W);

			for (var x = 0; x < width; x++)
				residual[y * width + x] = Math.Clamp(Round2(samples[x], rowShift), -bound, bound - 1);
		}

		for (var x = 0; x < width; x++)
		{
			for (var y = 0; y < height; y++)
				samples[y] = residual[y * width + x];

			if (lossless)
				InverseWht(samples, 0);
			else if (planeTxType is Av1TxType.DctDct or Av1TxType.DctAdst or Av1TxType.DctFlipadst or Av1TxType.VDct)
				InverseDct(samples, log2H, colRange);
			else if (planeTxType is Av1TxType.AdstDct or Av1TxType.AdstAdst or Av1TxType.FlipadstDct or Av1TxType.FlipadstFlipadst or Av1TxType.AdstFlipadst or Av1TxType.FlipadstAdst or Av1TxType.VAdst or Av1TxType.VFlipadst)
				InverseAdst(samples, log2H, colRange);
			else
				InverseIdentity(samples, log2H);

			for (var y = 0; y < height; y++)
				residual[y * width + x] = Round2(samples[y], colShift);
		}
	}
#endif
}
