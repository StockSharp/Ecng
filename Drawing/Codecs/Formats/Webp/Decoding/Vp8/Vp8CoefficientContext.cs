namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Runtime.CompilerServices;

internal sealed class Vp8CoefficientContext
{
	public readonly bool[] AboveY;
	public readonly bool[] LeftY = new bool[4];
	public readonly bool[] AboveU;
	public readonly bool[] LeftU = new bool[2];
	public readonly bool[] AboveV;
	public readonly bool[] LeftV = new bool[2];
	public readonly bool[] AboveDc;

	public bool LeftDc;

	public Vp8CoefficientContext(int macroblockCols)
	{
		AboveY = new bool[macroblockCols * 4];
		AboveU = new bool[macroblockCols * 2];
		AboveV = new bool[macroblockCols * 2];
		AboveDc = new bool[macroblockCols];
	}

	public void StartRow()
	{
		Array.Clear(LeftY);
		Array.Clear(LeftU);
		Array.Clear(LeftV);
		LeftDc = false;
	}
}
