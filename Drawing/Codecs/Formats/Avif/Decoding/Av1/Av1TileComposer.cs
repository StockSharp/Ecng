namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Buffers;

internal static class Av1TileComposer
{
	internal sealed class CompositedFrame
	{
		public int[][] Planes { get; init; }

		public int[] Widths { get; init; }

		public int[] Heights { get; init; }

		public Av1SequenceHeader Sequence { get; init; }
		public int NumPlanes { get; init; }
	}

	public static CompositedFrame Composite(IReadOnlyList<byte[]> tiles, int gridRows, int gridColumns, int outWidth, int outHeight, ImageProcessingLimits limits)
		=> Composite(tiles, gridRows, gridColumns, outWidth, outHeight, limits, false);

	public static CompositedFrame Composite(IReadOnlyList<byte[]> tiles, int gridRows, int gridColumns, int outWidth, int outHeight, ImageProcessingLimits limits, bool thumbnail)
	{
		long retained = 0, temporary = 0;
		foreach (var tile in tiles)
		{
			var estimate = Av1FrameDecoder.EstimateBuffers(tile);
			retained += estimate.planes;
			temporary = Math.Max(temporary, estimate.working - estimate.planes);
		}
		limits.EnsureWorkingMemory(retained + temporary + (tiles.Count == 1 ? 0 : (long)outWidth * outHeight * 24));
		var frames = new Av1DecodedFrame[tiles.Count];
		for (int i = 0; i < tiles.Count; i++)
		{
			frames[i] = Av1FrameDecoder.Decode(tiles[i], limits, thumbnail);
		}

		var seq = frames[0].Sequence;
		int numPlanes = seq.MonoChrome ? 1 : 3;
		if (tiles.Count == 1 && gridRows == 1 && gridColumns == 1 &&
			frames[0].PlaneWidths[0] >= outWidth && frames[0].PlaneHeights[0] >= outHeight)
			return new CompositedFrame
			{
				Planes = frames[0].Planes,
				Widths = frames[0].PlaneWidths,
				Heights = frames[0].PlaneHeights,
				Sequence = seq,
				NumPlanes = numPlanes,
			};
		long heldBytes = (long)outWidth * outHeight * 12;
		foreach (var frame in frames)
			foreach (var plane in frame.Planes) if (plane is not null) heldBytes += (long)plane.Length * 4;
		limits.EnsureWorkingMemory(heldBytes);

		// Rented, not `new`'d: this buffer's lifetime ends when Av1YuvToRgbConverter.Convert finishes
		// reading it (see AvifDecoder.Decode, which returns it via ReturnPlanes right after). A rented
		// array may be larger than requested, so every reader here and downstream must use the tracked
		// outWidths/outHeights, never Length -- see CopyRegion's own remarks for why that already had to
		// be true regardless of pooling. Explicitly cleared to match `new int[]`'s zero-init -- but only
		// when actually needed: a source tile smaller than the declared output (a mismatched/reused grid
		// tile item, or the last row/column of an overhanging grid) leaves copyW/copyH below
		// outWidths/outHeights, so CopyRegion below does not necessarily write every element. For the
		// common single-tile case that's cheap to rule out upfront (copyW/copyH are already known before
		// the copy runs), skipping a multi-megabyte clear entirely when the tile's own dimensions already
		// cover the whole output.
		var outPlanes = new int[3][];
		var outWidths = new int[3];
		var outHeights = new int[3];
		bool singleTile = gridRows == 1 && gridColumns == 1;
		for (int plane = 0; plane < numPlanes; plane++)
		{
			int subX = plane > 0 && seq.SubsamplingX ? 1 : 0;
			int subY = plane > 0 && seq.SubsamplingY ? 1 : 0;
			outWidths[plane] = (outWidth + subX) >> subX;
			outHeights[plane] = (outHeight + subY) >> subY;
			int length = outWidths[plane] * outHeights[plane];
			outPlanes[plane] = ArrayPool<int>.Shared.Rent(length);

			bool fullyCovered = singleTile
				&& frames[0].PlaneWidths[plane] >= outWidths[plane]
				&& frames[0].PlaneHeights[plane] >= outHeights[plane];
			if (!fullyCovered)
			{
				Array.Clear(outPlanes[plane], 0, length);
			}
		}

		if (singleTile)
		{
			var single = frames[0];
			for (int plane = 0; plane < numPlanes; plane++)
			{
				int copyW = Math.Min(outWidths[plane], single.PlaneWidths[plane]);
				int copyH = Math.Min(outHeights[plane], single.PlaneHeights[plane]);
				CopyRegion(single.Planes[plane], single.PlaneWidths[plane], single.PlaneHeights[plane], outPlanes[plane], outWidths[plane], outHeights[plane], 0, 0, copyW, copyH);
			}
		}
		else
		{
			int tileContentW = frames[0].Frame.UpscaledWidth;
			int tileContentH = frames[0].Frame.FrameHeight;

			int tileIdx = 0;
			for (int gr = 0; gr < gridRows; gr++)
			{
				for (int gc = 0; gc < gridColumns; gc++)
				{
					var tile = frames[tileIdx++];
					for (int plane = 0; plane < numPlanes; plane++)
					{
						int subX = plane > 0 && seq.SubsamplingX ? 1 : 0;
						int subY = plane > 0 && seq.SubsamplingY ? 1 : 0;

						// contentW/H is the grid's nominal per-tile cell size, used for placement stepping
						// (spec-mandated uniform across tiles); tileAvailW/H is bounded by what THIS tile
						// actually decoded to, since a shared/reused tile item (a legal HEIF construct --
						// e.g. one item referenced by both the color and alpha grids' dimg associations)
						// isn't guaranteed to match the first tile's dimensions.
						int contentW = (tileContentW + subX) >> subX;
						int contentH = (tileContentH + subY) >> subY;
						int tileAvailW = (tile.Frame.UpscaledWidth + subX) >> subX;
						int tileAvailH = (tile.Frame.FrameHeight + subY) >> subY;

						int destBaseX = gc * contentW;
						int destBaseY = gr * contentH;
						int copyW = Math.Min(Math.Min(contentW, tileAvailW), outWidths[plane] - destBaseX);
						int copyH = Math.Min(Math.Min(contentH, tileAvailH), outHeights[plane] - destBaseY);
						if (copyW <= 0 || copyH <= 0)
						{
							continue;
						}

						CopyRegion(tile.Planes[plane], tile.PlaneWidths[plane], tile.PlaneHeights[plane], outPlanes[plane], outWidths[plane], outHeights[plane], destBaseX, destBaseY, copyW, copyH);
					}
				}
			}
		}

		// Every frames[i].Planes[plane] has now been read for the last time (frames is local and never
		// escapes this method) -- return each to the pool it came from (Av1FrameDecoder.Decode's own
		// pipeline). Guarded per-plane by numPlanes since a monochrome tile never populated slots 1/2.
		foreach (var frame in frames)
		{
			for (int plane = 0; plane < numPlanes; plane++)
			{
				ArrayPool<int>.Shared.Return(frame.Planes[plane]);
			}
		}

		return new CompositedFrame
		{
			Planes = outPlanes,
			Widths = outWidths,
			Heights = outHeights,
			Sequence = seq,
			NumPlanes = numPlanes,
		};
	}
	public static void ReturnPlanes(CompositedFrame frame)
	{
		for (int plane = 0; plane < frame.NumPlanes; plane++)
		{
			ArrayPool<int>.Shared.Return(frame.Planes[plane]);
		}
	}
	private static void CopyRegion(int[] src, int srcStride, int srcHeight, int[] dst, int dstStride, int dstHeight, int destBaseX, int destBaseY, int w, int h)
	{
		if (srcStride <= 0 || dstStride <= 0)
		{
			return;
		}

		w = Math.Min(w, srcStride);
		w = Math.Min(w, dstStride - destBaseX);
		h = Math.Min(h, srcHeight);
		h = Math.Min(h, dstHeight - destBaseY);

		if (w <= 0 || h <= 0)
		{
			return;
		}

		for (int y = 0; y < h; y++)
		{
			Array.Copy(src, y * srcStride, dst, ((destBaseY + y) * dstStride) + destBaseX, w);
		}
	}
}
