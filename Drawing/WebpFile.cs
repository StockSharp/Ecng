namespace Ecng.Drawing;

using System;
using System.Collections.Generic;

internal sealed class WebpFile
{
	public int Width;
	public int Height;
	public int Plays;
	public uint Background;
	public bool IsAnimated;
	public bool IsLossless;
	public ReadOnlyMemory<byte> Bitstream;
	public ReadOnlyMemory<byte> Alpha;
	public List<WebpFrame> Frames = [];
}
