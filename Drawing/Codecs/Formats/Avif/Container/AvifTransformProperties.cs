namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class AvifTransformProperties
{
	public static AvifTransform? ParseIrot(byte[] data, AvifBox box) =>
		box.PayloadLength < 1 ? null : new AvifTransform(IsMirror: false, data[box.PayloadOffset] & 0x3);
	public static AvifTransform? ParseImir(byte[] data, AvifBox box) =>
		box.PayloadLength < 1 ? null : new AvifTransform(IsMirror: true, data[box.PayloadOffset] & 0x1);
	public static ImageOrientation ToOrientation(IEnumerable<AvifTransform> transforms)
	{
		// Track the combined transform as a 2x2 integer matrix acting on (x, y) with x right and y down:
		// x' = a*x + b*y, y' = c*x + d*y. Start at identity; each op is applied after the previous ones.
		int a = 1, b = 0, c = 0, d = 1;
		foreach (var t in transforms)
		{
			(int ma, int mb, int mc, int md) = t.IsMirror
				? (t.Value == 0 ? (-1, 0, 0, 1) : (1, 0, 0, -1))
				: t.Value switch
				{
					1 => (0, 1, -1, 0), // 90 degrees anti-clockwise: right -> up
					2 => (-1, 0, 0, -1),
					3 => (0, -1, 1, 0), // 270 anti-clockwise = 90 clockwise: right -> down
					_ => (1, 0, 0, 1),
				};

			(a, b, c, d) = ((ma * a) + (mb * c), (ma * b) + (mb * d), (mc * a) + (md * c), (mc * b) + (md * d));
		}

		// EXIF orientations expressed as the same matrices (the transform that turns the stored image into the displayed one).
		return (a, b, c, d) switch
		{
			(-1, 0, 0, 1) => ImageOrientation.MirrorHorizontal,
			(-1, 0, 0, -1) => ImageOrientation.Rotate180,
			(1, 0, 0, -1) => ImageOrientation.MirrorVertical,
			(0, 1, 1, 0) => ImageOrientation.Transpose,
			(0, -1, 1, 0) => ImageOrientation.Rotate90,
			(0, -1, -1, 0) => ImageOrientation.Transverse,
			(0, 1, -1, 0) => ImageOrientation.Rotate270,
			_ => ImageOrientation.Normal,
		};
	}
}
