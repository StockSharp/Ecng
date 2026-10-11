namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Av1Dequantizer
{
	public static int DcQ(int b, int bitDepth) => Av1QuantLookup.DcQLookup[(bitDepth - 8) >> 1][Math.Clamp(b, 0, 255)];
	public static int AcQ(int b, int bitDepth) => Av1QuantLookup.AcQLookup[(bitDepth - 8) >> 1][Math.Clamp(b, 0, 255)];
	public static void Dequantize(int[] quant, int[] dequant, int txSz, int dcQuant, int acQuant, int bitDepth)
	{
		int dqDenom = txSz is Av1TxSize.Tx32x32 or Av1TxSize.Tx16x32 or Av1TxSize.Tx32x16 or Av1TxSize.Tx16x64 or Av1TxSize.Tx64x16
			? 2
			: txSz is Av1TxSize.Tx64x64 or Av1TxSize.Tx32x64 or Av1TxSize.Tx64x32
				? 4
				: 1;

		int w = Av1TxDimensions.Width[txSz];
		int h = Av1TxDimensions.Height[txSz];
		int tw = Math.Min(32, w);
		int th = Math.Min(32, h);

		long clampBound = 1L << (7 + bitDepth);

		for (int i = 0; i < th; i++)
		{
			for (int j = 0; j < tw; j++)
			{
				int q = i == 0 && j == 0 ? dcQuant : acQuant;
				long dq = (long)quant[(i * tw) + j] * q;
				int sign = dq < 0 ? -1 : 1;
				long dq2 = sign * ((Math.Abs(dq) & 0xFFFFFF) / dqDenom);
				dequant[(i * 64) + j] = (int)Math.Clamp(dq2, -clampBound, clampBound - 1);
			}
		}
	}
}
