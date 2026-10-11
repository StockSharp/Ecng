namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Vp8LBackwardCopier
{
	public static void CopyPixels(Span<uint> argb, int destPos, int distance, int length)
	{
		int sourcePos = destPos - distance;

		if (distance >= length)
		{
			argb.Slice(sourcePos, length).CopyTo(argb.Slice(destPos, length));
			return;
		}

		for (int i = 0; i < length; i++)
		{
			argb[destPos + i] = argb[sourcePos + i];
		}
	}
}
