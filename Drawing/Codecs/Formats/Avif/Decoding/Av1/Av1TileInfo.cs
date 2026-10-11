namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal sealed class Av1TileInfo
{
	public int TileCols { get; init; }

	public int TileRows { get; init; }

	public int TileColsLog2 { get; init; }

	public int TileRowsLog2 { get; init; }
	public IReadOnlyList<int> MiColStarts { get; init; }
	public IReadOnlyList<int> MiRowStarts { get; init; }

	public int ContextUpdateTileId { get; init; }
	public int TileSizeBytes { get; init; }

	private const int MaxTileWidth = 4096;
	private const int MaxTileArea = 4096 * 2304;
	private const int MaxTileCols = 64;
	private const int MaxTileRows = 64;

	public static Av1TileInfo Parse(Av1BitReader reader, bool use128x128Superblock, int miCols, int miRows)
	{
		int sbShift = use128x128Superblock ? 5 : 4;
		int sbSize = sbShift + 2;
		int sbCols = use128x128Superblock ? (miCols + 31) >> 5 : (miCols + 15) >> 4;
		int sbRows = use128x128Superblock ? (miRows + 31) >> 5 : (miRows + 15) >> 4;

		int maxTileWidthSb = MaxTileWidth >> sbSize;
		int maxTileAreaSb = MaxTileArea >> (2 * sbSize);
		int minLog2TileCols = TileLog2(maxTileWidthSb, sbCols);
		int maxLog2TileCols = TileLog2(1, Math.Min(sbCols, MaxTileCols));
		int maxLog2TileRows = TileLog2(1, Math.Min(sbRows, MaxTileRows));
		int minLog2Tiles = Math.Max(minLog2TileCols, TileLog2(maxTileAreaSb, sbRows * sbCols));

		int tileCols, tileRows, tileColsLog2, tileRowsLog2;
		int[] miColStarts, miRowStarts;

		bool uniformTileSpacing = reader.ReadFlag();
		if (uniformTileSpacing)
		{
			tileColsLog2 = minLog2TileCols;
			while (tileColsLog2 < maxLog2TileCols)
			{
				if (!reader.ReadFlag())
				{
					break;
				}

				tileColsLog2++;
			}

			int tileWidthSb = (sbCols + (1 << tileColsLog2) - 1) >> tileColsLog2;
			var colStarts = new List<int>();
			for (int startSb = 0; startSb < sbCols; startSb += tileWidthSb)
			{
				colStarts.Add(startSb << sbShift);
			}

			colStarts.Add(miCols);
			miColStarts = [.. colStarts];
			tileCols = colStarts.Count - 1;

			int minLog2TileRows = Math.Max(minLog2Tiles - tileColsLog2, 0);
			tileRowsLog2 = minLog2TileRows;
			while (tileRowsLog2 < maxLog2TileRows)
			{
				if (!reader.ReadFlag())
				{
					break;
				}

				tileRowsLog2++;
			}

			int tileHeightSb = (sbRows + (1 << tileRowsLog2) - 1) >> tileRowsLog2;
			var rowStarts = new List<int>();
			for (int startSb = 0; startSb < sbRows; startSb += tileHeightSb)
			{
				rowStarts.Add(startSb << sbShift);
			}

			rowStarts.Add(miRows);
			miRowStarts = [.. rowStarts];
			tileRows = rowStarts.Count - 1;
		}
		else
		{
			int widestTileSb = 0;
			var colStarts = new List<int>();
			int startSbCol = 0;
			while (startSbCol < sbCols)
			{
				colStarts.Add(startSbCol << sbShift);
				int maxWidth = Math.Min(sbCols - startSbCol, maxTileWidthSb);
				int widthInSbsMinus1 = (int)reader.ReadNs((uint)maxWidth);
				int sizeSb = widthInSbsMinus1 + 1;
				widestTileSb = Math.Max(sizeSb, widestTileSb);
				startSbCol += sizeSb;
			}

			colStarts.Add(miCols);
			miColStarts = [.. colStarts];
			tileCols = colStarts.Count - 1;
			tileColsLog2 = TileLog2(1, tileCols);

			int areaSb = minLog2Tiles > 0 ? (sbRows * sbCols) >> (minLog2Tiles + 1) : sbRows * sbCols;
			int maxTileHeightSb = Math.Max(areaSb / widestTileSb, 1);

			var rowStarts = new List<int>();
			int startSbRow = 0;
			while (startSbRow < sbRows)
			{
				rowStarts.Add(startSbRow << sbShift);
				int maxHeight = Math.Min(sbRows - startSbRow, maxTileHeightSb);
				int heightInSbsMinus1 = (int)reader.ReadNs((uint)maxHeight);
				int sizeSb = heightInSbsMinus1 + 1;
				startSbRow += sizeSb;
			}

			rowStarts.Add(miRows);
			miRowStarts = [.. rowStarts];
			tileRows = rowStarts.Count - 1;
			tileRowsLog2 = TileLog2(1, tileRows);
		}

		int contextUpdateTileId = 0;
		int tileSizeBytes = 1;
		if (tileColsLog2 > 0 || tileRowsLog2 > 0)
		{
			contextUpdateTileId = (int)reader.ReadBits(tileRowsLog2 + tileColsLog2);
			tileSizeBytes = (int)reader.ReadBits(2) + 1;
		}

		return new Av1TileInfo
		{
			TileCols = tileCols,
			TileRows = tileRows,
			TileColsLog2 = tileColsLog2,
			TileRowsLog2 = tileRowsLog2,
			MiColStarts = miColStarts,
			MiRowStarts = miRowStarts,
			ContextUpdateTileId = contextUpdateTileId,
			TileSizeBytes = tileSizeBytes,
		};
	}

	private static int TileLog2(int blkSize, int target)
	{
		int k = 0;
		while ((blkSize << k) < target)
		{
			k++;
		}

		return k;
	}
}
