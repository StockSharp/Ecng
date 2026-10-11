namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Av1TileGroupObu
{
	public static List<Av1TileRange> Parse(byte[] data, int start, int length, Av1TileInfo tileInfo)
	{
		int numTiles = tileInfo.TileCols * tileInfo.TileRows;
		var reader = new Av1BitReader(data, start, length);

		bool tileStartAndEndPresent = false;
		if (numTiles > 1)
		{
			tileStartAndEndPresent = reader.ReadFlag();
		}

		int tgStart, tgEnd;
		if (numTiles == 1 || !tileStartAndEndPresent)
		{
			tgStart = 0;
			tgEnd = numTiles - 1;
		}
		else
		{
			int tileBits = tileInfo.TileColsLog2 + tileInfo.TileRowsLog2;
			tgStart = (int)reader.ReadBits(tileBits);
			tgEnd = (int)reader.ReadBits(tileBits);
		}

		reader.ByteAlign();
		int cursor = reader.BytePosition;
		int end = start + length;

		var result = new List<Av1TileRange>();
		for (int tileNum = tgStart; tileNum <= tgEnd; tileNum++)
		{
			int tileRow = tileNum / tileInfo.TileCols;
			int tileCol = tileNum % tileInfo.TileCols;
			bool lastTile = tileNum == tgEnd;

			int tileSize;
			if (lastTile)
			{
				tileSize = end - cursor;
			}
			else
			{
				if (cursor + tileInfo.TileSizeBytes > end)
				{
					throw new AvifDecodingException("Truncated AV1 tile group: not enough bytes for a tile size field.");
				}

				long tileSizeMinus1 = (long)ReadLittleEndian(data, cursor, tileInfo.TileSizeBytes);
				tileSize = checked((int)(tileSizeMinus1 + 1));
				cursor += tileInfo.TileSizeBytes;
			}

			if (tileSize < 0 || cursor + tileSize > end)
			{
				throw new AvifDecodingException("AV1 tile size extends past the tile group's data.");
			}

			result.Add(new Av1TileRange(tileRow, tileCol, cursor, tileSize));
			cursor += tileSize;
		}

		return result;
	}
	private static ulong ReadLittleEndian(byte[] data, int offset, int byteCount)
	{
		ulong value = 0;
		for (int i = 0; i < byteCount; i++)
		{
			value |= (ulong)data[offset + i] << (8 * i);
		}

		return value;
	}
}
