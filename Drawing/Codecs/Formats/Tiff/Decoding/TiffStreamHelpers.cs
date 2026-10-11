namespace Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using Ecng.Drawing.Codecs.Formats.Tiff.Internal;

internal static class TiffStreamHelpers
{
	public static byte[] BufferStream(Stream stream)
	{
		if (stream.CanSeek)
		{
			long declaredLength = stream.Length - stream.Position;
			if (declaredLength > TiffDecodingLimits.MaxFileSize)
			{
				throw new TiffDecodingException($"TIFF file size ({declaredLength} bytes) exceeds the supported maximum of {TiffDecodingLimits.MaxFileSize} bytes.");
			}
		}

		using var buffer = new MemoryStream();
		var chunk = new byte[81920];
		long total = 0;
		int read;

		while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
		{
			total += read;
			if (total > TiffDecodingLimits.MaxFileSize)
			{
				throw new TiffDecodingException($"TIFF file size exceeds the supported maximum of {TiffDecodingLimits.MaxFileSize} bytes.");
			}

			buffer.Write(chunk, 0, read);
		}

		return buffer.ToArray();
	}
}
