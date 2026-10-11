namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal sealed class Av1EdgeArray
{
	private const int Offset = 2;
	private readonly int[] _data;

	public Av1EdgeArray(int capacity) => _data = new int[capacity + Offset];

	public int this[int i]
	{
		get => _data[i + Offset];
		set => _data[i + Offset] = value;
	}
}
