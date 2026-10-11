namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.ColorConversion;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Runtime.CompilerServices;

internal sealed class Vp8ScalarColorConverter : IVp8ColorConverter
{
	internal const int YCoefficient = 19077;
	internal const int VToRed = 26149;
	internal const int UToGreen = 6419;
	internal const int VToGreen = 13320;
	internal const int UToBlue = 33050;
	internal const int RedBias = -14234;
	internal const int GreenBias = 8708;
	internal const int BlueBias = -17685;
	public void ConvertRow(ReadOnlySpan<byte> y, ReadOnlySpan<byte> u, ReadOnlySpan<byte> v, Span<byte> rgb, int width) =>
		ConvertRemainder(y, u, v, rgb, 0, width);
	internal static void ConvertRemainder(ReadOnlySpan<byte> y, ReadOnlySpan<byte> u, ReadOnlySpan<byte> v, Span<byte> rgb, int start, int width)
	{
		for (int x = start; x < width; x++)
		{
			ConvertPixel(y[x], u[x], v[x], rgb.Slice(x * 3, 3));
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ConvertPixel(byte y, byte u, byte v, Span<byte> rgb)
	{
		rgb[0] = (byte)ClipToByteAfterShift(MultHi(y, YCoefficient) + MultHi(v, VToRed) + RedBias);
		rgb[1] = (byte)ClipToByteAfterShift(MultHi(y, YCoefficient) - MultHi(u, UToGreen) - MultHi(v, VToGreen) + GreenBias);
		rgb[2] = (byte)ClipToByteAfterShift(MultHi(y, YCoefficient) + MultHi(u, UToBlue) + BlueBias);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int MultHi(int v, int coeff) => (v * coeff) >> 8;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ClipToByteAfterShift(int v)
	{
		int shifted = v >> 6;
		return shifted < 0 ? 0 : shifted > 255 ? 255 : shifted;
	}
}
