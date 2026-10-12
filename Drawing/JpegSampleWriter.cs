namespace Ecng.Drawing;

using System;
using System.IO;
using System.Runtime.InteropServices;

using Ecng.Drawing.Codecs.Jpeg;

internal sealed class JpegSampleWriter(JpegComponent[] components, int width, int height, int precision, int pointTransform, bool isLossless)
	: JpegBlockOutputWriter
{
	public int RowCount { get; init; }
	public Action<int, int, int> BlockCompleted { get; init; }

	public override void WriteBlock(ref short blockRef, int componentIndex, int x, int y)
	{
		if ((uint)componentIndex >= components.Length || x < 0 || y < 0)
			throw new InvalidDataException("Invalid JPEG output block.");
		var component = components[componentIndex];
		var samples = MemoryMarshal.CreateReadOnlySpan(ref blockRef, 64);
		for (var row = 0; row < 8 && y + row < height; row++)
		{
			for (var col = 0; col < 8 && x + col < width; col++)
			{
				var value = isLossless ? (ushort)samples[row * 8 + col] & ((1 << (precision - pointTransform)) - 1) : samples[row * 8 + col];
				value <<= pointTransform;
				value = precision >= 8 ? value >> (precision - 8) : value * 255 / ((1 << precision) - 1);
				component.Samples[(RowCount == 0 ? y + row : (y + row) % RowCount) * component.Stride + x + col] = (byte)Math.Clamp(value, 0, 255);
			}
		}
		BlockCompleted?.Invoke(componentIndex, x, y);
	}
}
