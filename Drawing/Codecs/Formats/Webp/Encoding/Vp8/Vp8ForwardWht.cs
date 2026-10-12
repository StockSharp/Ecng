namespace Ecng.Drawing.Codecs.Formats.Webp.Encoding.Vp8;

using System;
using System.IO;

internal static class Vp8ForwardWht
{
	public static void Transform(ReadOnlySpan<short> input, Span<short> output)
	{
		Span<int> tmp = stackalloc int[16];

		for (int row = 0; row < 4; row++)
		{
			int c0 = input[(row * 4) + 0];
			int c1 = input[(row * 4) + 1];
			int c2 = input[(row * 4) + 2];
			int c3 = input[(row * 4) + 3];

			int a0 = c0 + c2;
			int a1 = c1 + c3;
			int a2 = c1 - c3;
			int a3 = c0 - c2;

			tmp[(row * 4) + 0] = a0 + a1;
			tmp[(row * 4) + 1] = a3 + a2;
			tmp[(row * 4) + 2] = a3 - a2;
			tmp[(row * 4) + 3] = a0 - a1;
		}

		for (int col = 0; col < 4; col++)
		{
			int t0 = tmp[(0 * 4) + col];
			int t1 = tmp[(1 * 4) + col];
			int t2 = tmp[(2 * 4) + col];
			int t3 = tmp[(3 * 4) + col];

			int a0 = t0 + t2;
			int a1 = t1 + t3;
			int a2 = t1 - t3;
			int a3 = t0 - t2;

			output[(0 * 4) + col] = (short)((a0 + a1) >> 1);
			output[(1 * 4) + col] = (short)((a3 + a2) >> 1);
			output[(2 * 4) + col] = (short)((a3 - a2) >> 1);
			output[(3 * 4) + col] = (short)((a0 - a1) >> 1);
		}
	}
}
