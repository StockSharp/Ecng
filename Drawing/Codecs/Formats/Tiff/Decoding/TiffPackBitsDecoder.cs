namespace Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class TiffPackBitsDecoder
{
	public static void Decode(ReadOnlySpan<byte> compressed, Span<byte> output)
	{
		int src = 0;
		int dst = 0;

		while (src < compressed.Length && dst < output.Length)
		{
			sbyte control = unchecked((sbyte)compressed[src++]);

			if (control >= 0)
			{
				int count = control + 1;
				if (count > compressed.Length - src || count > output.Length - dst)
					throw new TiffDecodingException("Invalid TIFF PackBits literal run.");

				compressed.Slice(src, count).CopyTo(output.Slice(dst, count));
				src += count;
				dst += count;
			}
			else if (control != -128)
			{
				if (src >= compressed.Length)
				{
					throw new TiffDecodingException("Truncated TIFF PackBits run.");
				}

				byte value = compressed[src++];
				int count = 1 - control;
				if (count > output.Length - dst)
					throw new TiffDecodingException("TIFF PackBits run exceeds its strip.");
				output.Slice(dst, count).Fill(value);
				dst += count;
			}

			// control == -128: no-op, just consume the control byte.
		}
		if (dst != output.Length)
			throw new TiffDecodingException("Truncated TIFF PackBits strip.");
	}
}
