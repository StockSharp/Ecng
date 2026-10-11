namespace Ecng.Drawing;

using System;

internal sealed record WebpFrame(int X, int Y, int Width, int Height, int Delay,
	bool IsNoBlend, bool IsDisposeToBackground, bool IsLossless,
	ReadOnlyMemory<byte> Bitstream, ReadOnlyMemory<byte> Alpha);
