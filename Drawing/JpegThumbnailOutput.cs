namespace Ecng.Drawing;

using System;
using System.Collections.Generic;
using System.IO;

internal static class JpegThumbnailOutput
{
	public static RasterImage Read(int width, int height, int outputWidth, int outputHeight,
		JpegComponent[] components, IReadOnlyList<IEnumerator<int>> scans, JpegColorModels model,
		ImageProcessingLimits limits, Action reconstruct)
	{
		var maxV = 1;
		foreach (var component in components) maxV = Math.Max(maxV, component.V);
		var downscaler = new ImageDownscaler(width, height, outputWidth, outputHeight, limits);
		var converter = new JpegColorConverter(width, components, model);
		var row = new byte[checked(width * 4)];
		try
		{
			for (var top = 0; top < height; top += maxV * 8)
			{
				foreach (var scan in scans)
					if (!scan.MoveNext()) throw new InvalidDataException("JPEG scan does not cover all image rows.");
				reconstruct();
				for (var y = 0; y < Math.Min(maxV * 8, height - top); y++)
				{
					var isRepeated = y > 0 && converter.RowsEqual(y, y - 1);
					if (!isRepeated) converter.WriteRow(y, row);
					downscaler.AddRow(row, isRepeated);
				}
			}
			foreach (var scan in scans)
				if (scan.MoveNext()) throw new InvalidDataException("JPEG scan exceeds the image height.");
			return downscaler.Image;
		}
		finally
		{
			foreach (var scan in scans) scan.Dispose();
		}
	}
}
