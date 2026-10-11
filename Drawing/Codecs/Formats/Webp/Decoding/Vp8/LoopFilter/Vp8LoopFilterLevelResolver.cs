namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.LoopFilter;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Vp8LoopFilterLevelResolver
{
	public static Vp8FilterStrength[,] Resolve(Vp8LoopFilterHeader header, Vp8SegmentHeader segmentHeader)
	{
		var result = new Vp8FilterStrength[Vp8SegmentHeader.NumSegments, 2];

		for (int s = 0; s < Vp8SegmentHeader.NumSegments; s++)
		{
			int baseLevel;
			if (segmentHeader.UseSegment)
			{
				baseLevel = segmentHeader.FilterStrength[s];
				if (!segmentHeader.AbsoluteDelta)
				{
					baseLevel += header.Level;
				}
			}
			else
			{
				baseLevel = header.Level;
			}

			for (int i4x4 = 0; i4x4 <= 1; i4x4++)
			{
				int level = baseLevel;
				if (header.UseLfDelta)
				{
					level += header.RefLfDelta[0];
					if (i4x4 != 0)
					{
						level += header.ModeLfDelta[0];
					}
				}

				level = level < 0 ? 0 : level > 63 ? 63 : level;

				if (level > 0)
				{
					int interiorLimit = level;
					if (header.Sharpness > 0)
					{
						interiorLimit = header.Sharpness > 4 ? interiorLimit >> 2 : interiorLimit >> 1;
						int cap = 9 - header.Sharpness;
						if (interiorLimit > cap)
						{
							interiorLimit = cap;
						}
					}

					if (interiorLimit < 1)
					{
						interiorLimit = 1;
					}

					result[s, i4x4] = new Vp8FilterStrength
					{
						Limit = (2 * level) + interiorLimit,
						InteriorLimit = interiorLimit,
						HevThreshold = level >= 40 ? 2 : level >= 15 ? 1 : 0,
					};
				}
				else
				{
					result[s, i4x4] = new Vp8FilterStrength { Limit = 0, InteriorLimit = 0, HevThreshold = 0 };
				}
			}
		}

		return result;
	}
}
