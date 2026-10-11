namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal sealed class Vp8LoopFilterHeader
{
	public const int NumRefLfDeltas = 4;
	public const int NumModeLfDeltas = 4;
	public bool Simple { get; private set; }

	public int Level { get; private set; }

	public int Sharpness { get; private set; }

	public bool UseLfDelta { get; private set; }

	public int[] RefLfDelta { get; } = new int[NumRefLfDeltas];

	public int[] ModeLfDelta { get; } = new int[NumModeLfDeltas];
	public bool IsFilteringEnabled => Level != 0;

	public static Vp8LoopFilterHeader Parse(Vp8BoolDecoder br)
	{
		var header = new Vp8LoopFilterHeader
		{
			Simple = br.GetFlag(),
			Level = (int)br.GetValue(6),
			Sharpness = (int)br.GetValue(3),
			UseLfDelta = br.GetFlag(),
		};

		if (header.UseLfDelta)
		{
			bool updateLfDelta = br.GetFlag();
			if (updateLfDelta)
			{
				// All four ref-frame deltas and all four mode deltas are always read (when present) to keep the
				// bitstream aligned, even though a lone VP8 keyframe only ever consults the intra-frame entries
				// (index 0 of each).
				for (int i = 0; i < NumRefLfDeltas; i++)
				{
					if (br.GetFlag())
					{
						header.RefLfDelta[i] = br.GetSignedValue(6);
					}
				}

				for (int i = 0; i < NumModeLfDeltas; i++)
				{
					if (br.GetFlag())
					{
						header.ModeLfDelta[i] = br.GetSignedValue(6);
					}
				}
			}
		}

		return header;
	}
}