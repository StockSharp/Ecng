namespace Ecng.Drawing.Codecs.Formats.Webp.Kernels;

using System;

internal interface IVp8LTransformKernel
{
	void SubtractGreenInverse(Span<uint> pixels);
	void PredictorTopInverse(Span<byte> row, ReadOnlySpan<byte> topRow);
	void ColorTransformInverse(Span<uint> pixels, sbyte greenToRed, sbyte greenToBlue, sbyte redToBlue);
}
