namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal sealed class Av1RestorationUnitGrid
{
	public int UnitRows { get; init; }

	public int UnitCols { get; init; }

	public int UnitSize { get; init; }
	public int[] LrType { get; init; }
	public int[] LrWiener { get; init; }

	public int[] LrSgrSet { get; init; }
	public int[] LrSgrXqd { get; init; }
	public static int CountUnitsInFrame(int unitSize, int frameSize) => Math.Max((frameSize + (unitSize >> 1)) / unitSize, 1);

	public static Av1RestorationUnitGrid Create(int unitSize, int frameSizeForRows, int frameSizeForCols)
	{
		int unitRows = CountUnitsInFrame(unitSize, frameSizeForRows);
		int unitCols = CountUnitsInFrame(unitSize, frameSizeForCols);
		int count = unitRows * unitCols;

		return new Av1RestorationUnitGrid
		{
			UnitRows = unitRows,
			UnitCols = unitCols,
			UnitSize = unitSize,
			LrType = new int[count],
			LrWiener = new int[count * 2 * 3],
			LrSgrSet = new int[count],
			LrSgrXqd = new int[count * 2],
		};
	}
}
