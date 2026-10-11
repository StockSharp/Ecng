namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal sealed class Vp8QuantIndices
{
	public int BaseQ0 { get; init; }

	public int Y1DcDelta { get; init; }

	public int Y2DcDelta { get; init; }

	public int Y2AcDelta { get; init; }

	public int UvDcDelta { get; init; }

	public int UvAcDelta { get; init; }

	public static Vp8QuantIndices Parse(Vp8BoolDecoder br)
	{
		int baseQ0 = (int)br.GetValue(7);
		int y1Dc = br.GetFlag() ? br.GetSignedValue(4) : 0;
		int y2Dc = br.GetFlag() ? br.GetSignedValue(4) : 0;
		int y2Ac = br.GetFlag() ? br.GetSignedValue(4) : 0;
		int uvDc = br.GetFlag() ? br.GetSignedValue(4) : 0;
		int uvAc = br.GetFlag() ? br.GetSignedValue(4) : 0;

		return new Vp8QuantIndices
		{
			BaseQ0 = baseQ0,
			Y1DcDelta = y1Dc,
			Y2DcDelta = y2Dc,
			Y2AcDelta = y2Ac,
			UvDcDelta = uvDc,
			UvAcDelta = uvAc,
		};
	}
}