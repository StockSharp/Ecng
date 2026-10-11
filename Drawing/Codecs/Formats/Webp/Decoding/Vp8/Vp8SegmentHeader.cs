namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal sealed class Vp8SegmentHeader
{
	public const int NumSegments = 4;
	public const int NumTreeProbabilities = 3;

	public bool UseSegment { get; private set; }

	public bool UpdateMap { get; private set; }
	public bool AbsoluteDelta { get; private set; } = true;

	public int[] Quantizer { get; } = new int[NumSegments];

	public int[] FilterStrength { get; } = new int[NumSegments];
	public byte[] SegmentTreeProbabilities { get; } = new byte[NumTreeProbabilities];

	public static Vp8SegmentHeader Parse(Vp8BoolDecoder br)
	{
		var header = new Vp8SegmentHeader
		{
			UseSegment = br.GetFlag(),
		};

		if (header.UseSegment)
		{
			header.UpdateMap = br.GetFlag();
			bool updateData = br.GetFlag();
			if (updateData)
			{
				header.AbsoluteDelta = br.GetFlag();
				for (int s = 0; s < NumSegments; s++)
				{
					header.Quantizer[s] = br.GetFlag() ? br.GetSignedValue(7) : 0;
				}

				for (int s = 0; s < NumSegments; s++)
				{
					header.FilterStrength[s] = br.GetFlag() ? br.GetSignedValue(6) : 0;
				}
			}

			if (header.UpdateMap)
			{
				for (int s = 0; s < NumTreeProbabilities; s++)
				{
					header.SegmentTreeProbabilities[s] = br.GetFlag() ? (byte)br.GetValue(8) : (byte)255;
				}
			}
		}
		else
		{
			header.UpdateMap = false;
		}

		return header;
	}
}