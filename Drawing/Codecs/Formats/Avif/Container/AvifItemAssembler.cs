namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using Ecng.Drawing.Codecs.Formats.Avif.Internal;

internal static class AvifItemAssembler
{
	public static AvifAssembledImage Assemble(byte[] data, AvifMetaBoxInfo meta)
	{
		if (meta.PrimaryItemId is not { } primaryItemId)
		{
			throw new AvifDecodingException("AVIF file is missing a primary item ('pitm').");
		}

		var color = ResolveImageItem(data, meta, primaryItemId, isColor: true);

		AvifResolvedImageItem alpha = null;
		foreach (var reference in meta.References)
		{
			if (reference.ReferenceType == "auxl" && reference.FromItemId != primaryItemId && reference.ToItemIds.Contains(primaryItemId))
			{
				uint alphaItemId = reference.FromItemId;
				if (!IsAlphaAuxiliary(data, meta, alphaItemId))
				{
					continue;
				}

				alpha = ResolveImageItem(data, meta, alphaItemId, isColor: false);
				break;
			}
		}

		if (color.Width <= 0 || color.Height <= 0
			|| color.Width > AvifDecodingLimits.MaxDimension || color.Height > AvifDecodingLimits.MaxDimension)
		{
			throw new AvifDecodingException($"AVIF image dimensions {color.Width}x{color.Height} are invalid or exceed the supported maximum of {AvifDecodingLimits.MaxDimension}.");
		}

		if ((long)color.Width * color.Height > AvifDecodingLimits.MaxPixelCount)
		{
			throw new AvifDecodingException($"AVIF image pixel count exceeds the supported maximum of {AvifDecodingLimits.MaxPixelCount}.");
		}

		var (exif, xmp) = CaptureMetadataItems(data, meta);

		return new AvifAssembledImage
		{
			Width = color.Width,
			Height = color.Height,
			BitDepth = color.BitDepth,
			Monochrome = color.Monochrome,
			ChromaSubsamplingX = color.ChromaSubsamplingX,
			ChromaSubsamplingY = color.ChromaSubsamplingY,
			HasAlpha = alpha is not null,
			ColorTiles = color.Tiles,
			AlphaTiles = alpha?.Tiles,
			GridRows = color.GridRows,
			GridColumns = color.GridColumns,
			ColorInfo = color.ColorInfo,
			Orientation = GetOrientation(data, meta, primaryItemId),
			ExifData = exif,
			XmpData = xmp,
		};
	}

	private static bool IsAlphaAuxiliary(byte[] data, AvifMetaBoxInfo meta, uint itemId)
	{
		var auxCBox = FindProperty(meta, itemId, "auxC");
		return auxCBox is { } box && AvifAuxCBox.IsAlpha(data, box);
	}

	private static AvifResolvedImageItem ResolveImageItem(byte[] data, AvifMetaBoxInfo meta, uint itemId, bool isColor)
	{
		if (!meta.Items.TryGetValue(itemId, out var itemInfo))
		{
			throw new AvifDecodingException($"AVIF item {itemId} is referenced but not described by 'iinf'.");
		}

		if (itemInfo.ItemType == "grid")
		{
			return ResolveGridItem(data, meta, itemId, isColor);
		}

		if (itemInfo.ItemType != "av01")
		{
			throw new AvifDecodingException($"AVIF item {itemId} has unsupported item type '{itemInfo.ItemType}' (only 'av01' and 'grid' are supported).");
		}

		var av1Config = GetAv1Config(data, meta, itemId)
			?? throw new AvifDecodingException($"Item {itemId} is missing an 'av1C' property.");

		// Only the color/primary item is to carry 'ispe' -- an alpha auxiliary item's plane
		// dimensions are implied to match the color item's (per MIAF's grid/derivation rules) and many
		// real encoders omit a redundant 'ispe' on it; its Width/Height here are unused by
		// AvifAssembledImage regardless (only the color item's feed into the final result).
		var (width, height) = isColor
			? ResolveDimensions(data, meta, itemId, fallbackWidth: null, fallbackHeight: null)
			: ResolveDimensions(data, meta, itemId, fallbackWidth: 0, fallbackHeight: 0);

		return new AvifResolvedImageItem
		{
			Width = width,
			Height = height,
			BitDepth = av1Config.HighBitdepth ? 10 : 8,
			Monochrome = av1Config.Monochrome,
			ChromaSubsamplingX = av1Config.ChromaSubsamplingX,
			ChromaSubsamplingY = av1Config.ChromaSubsamplingY,
			Tiles = [ResolveItemBytes(data, meta, itemId)],
			GridRows = 1,
			GridColumns = 1,
			ColorInfo = isColor ? GetColorInfo(data, meta, itemId) : null,
		};
	}

	private static AvifResolvedImageItem ResolveGridItem(byte[] data, AvifMetaBoxInfo meta, uint itemId, bool isColor)
	{
		byte[] gridBytes = ResolveItemBytes(data, meta, itemId);
		var descriptor = AvifGridDescriptor.Parse(gridBytes);

		AvifItemReference? dimg = null;
		foreach (var reference in meta.References)
		{
			if (reference.ReferenceType == "dimg" && reference.FromItemId == itemId)
			{
				dimg = reference;
				break;
			}
		}

		if (dimg is not { } dimgReference)
		{
			throw new AvifDecodingException($"Grid item {itemId} has no 'dimg' tile references.");
		}

		int expectedTiles = descriptor.Rows * descriptor.Columns;
		if (expectedTiles > AvifDecodingLimits.MaxGridTiles)
		{
			throw new AvifDecodingException($"Grid item {itemId} declares {expectedTiles} tiles, exceeding the supported maximum of {AvifDecodingLimits.MaxGridTiles}.");
		}

		if (dimgReference.ToItemIds.Count != expectedTiles)
		{
			throw new AvifDecodingException($"Grid item {itemId} declares {descriptor.Rows}x{descriptor.Columns} = {expectedTiles} tiles but has {dimgReference.ToItemIds.Count} 'dimg' references.");
		}

		var tiles = new List<byte[]>(expectedTiles);
		AvifAv1Config? config = null;

		foreach (uint tileId in dimgReference.ToItemIds)
		{
			if (!meta.Items.TryGetValue(tileId, out var tileInfo) || tileInfo.ItemType != "av01")
			{
				throw new AvifDecodingException($"Grid item {itemId}'s tile {tileId} is not an AV1 image item.");
			}

			tiles.Add(ResolveItemBytes(data, meta, tileId));
			config ??= GetAv1Config(data, meta, tileId);
		}

		if (config is not { } resolvedConfig)
		{
			throw new AvifDecodingException($"Grid item {itemId}'s tiles are missing an 'av1C' property.");
		}

		var (width, height) = ResolveDimensions(data, meta, itemId, descriptor.OutputWidth, descriptor.OutputHeight);

		return new AvifResolvedImageItem
		{
			Width = width,
			Height = height,
			BitDepth = resolvedConfig.HighBitdepth ? 10 : 8,
			Monochrome = resolvedConfig.Monochrome,
			ChromaSubsamplingX = resolvedConfig.ChromaSubsamplingX,
			ChromaSubsamplingY = resolvedConfig.ChromaSubsamplingY,
			Tiles = tiles,
			GridRows = descriptor.Rows,
			GridColumns = descriptor.Columns,
			ColorInfo = isColor ? GetColorInfo(data, meta, itemId) : null,
		};
	}

	private static (int Width, int Height) ResolveDimensions(byte[] data, AvifMetaBoxInfo meta, uint itemId, int? fallbackWidth, int? fallbackHeight)
	{
		var ispeBox = FindProperty(meta, itemId, "ispe");
		if (ispeBox is { } box)
		{
			var ispe = AvifIspeBox.Parse(data, box);
			return (ispe.Width, ispe.Height);
		}

		if (fallbackWidth is { } w && fallbackHeight is { } h)
		{
			return (w, h);
		}

		throw new AvifDecodingException($"AVIF item {itemId} is missing an 'ispe' property.");
	}

	private static AvifAv1Config? GetAv1Config(byte[] data, AvifMetaBoxInfo meta, uint itemId)
	{
		var box = FindProperty(meta, itemId, "av1C");
		return box is { } b ? AvifAv1ConfigBox.Parse(data, b) : null;
	}

	private static AvifColr GetColorInfo(byte[] data, AvifMetaBoxInfo meta, uint itemId)
	{
		var box = FindProperty(meta, itemId, "colr");
		return box is { } b ? AvifColrBox.Parse(data, b) : null;
	}

	private static ImageOrientation GetOrientation(byte[] data, AvifMetaBoxInfo meta, uint itemId)
	{
		if (!meta.Properties.ItemPropertyIndices.TryGetValue(itemId, out var indices))
		{
			return ImageOrientation.Normal;
		}

		// Transformative properties apply in association (ipma) order.
		var transforms = new List<AvifTransform>();
		foreach (int index in indices)
		{
			if (index < 1 || index > meta.Properties.Properties.Count)
			{
				continue;
			}

			var box = meta.Properties.Properties[index - 1];
			var transform = box.FourCc switch
			{
				"irot" => AvifTransformProperties.ParseIrot(data, box),
				"imir" => AvifTransformProperties.ParseImir(data, box),
				_ => null,
			};

			if (transform is { } t)
			{
				transforms.Add(t);
			}
		}

		return AvifTransformProperties.ToOrientation(transforms);
	}

	private static AvifBox? FindProperty(AvifMetaBoxInfo meta, uint itemId, string fourCc)
	{
		if (!meta.Properties.ItemPropertyIndices.TryGetValue(itemId, out var indices))
		{
			return null;
		}

		foreach (int index in indices)
		{
			if (index < 1 || index > meta.Properties.Properties.Count)
			{
				continue;
			}

			var box = meta.Properties.Properties[index - 1];
			if (box.FourCc == fourCc)
			{
				return box;
			}
		}

		return null;
	}
	private static (byte[] Exif, byte[] Xmp) CaptureMetadataItems(byte[] data, AvifMetaBoxInfo meta)
	{
		byte[] exif = null;
		byte[] xmp = null;

		foreach (var (itemId, info) in meta.Items)
		{
			if (info.ItemType == "Exif" && exif is null)
			{
				byte[] raw = ResolveItemBytes(data, meta, itemId);

				// Exif items are prefixed by a 4-byte big-endian exif_tiff_header_offset (how many bytes to
				// skip before the real TIFF header begins -- usually 0, but nonzero if an "Exif\0\0" prefix
				// precedes it), per ISO/IEC 23008-12.
				if (raw.Length >= 4)
				{
					int tiffOffset = checked((int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(raw));
					int start = 4 + tiffOffset;
					if (start >= 0 && start <= raw.Length)
					{
						exif = raw[start..];
					}
				}
			}
			else if (info.ItemType == "mime" && info.ContentType == "application/rdf+xml" && xmp is null)
			{
				xmp = ResolveItemBytes(data, meta, itemId);
			}
		}

		return (exif, xmp);
	}

	private static byte[] ResolveItemBytes(byte[] data, AvifMetaBoxInfo meta, uint itemId)
	{
		if (!meta.Locations.TryGetValue(itemId, out var location))
		{
			throw new AvifDecodingException($"AVIF item {itemId} has no 'iloc' entry.");
		}

		long totalLength = 0;
		foreach (var extent in location.Extents)
		{
			totalLength += extent.Length;
		}

		if (totalLength is < 0 or > AvifDecodingLimits.MaxItemDataLength)
		{
			throw new AvifDecodingException($"AVIF item {itemId}'s data length ({totalLength}) is invalid or exceeds the supported maximum.");
		}

		var result = new byte[totalLength];
		int destOffset = 0;

		foreach (var extent in location.Extents)
		{
			long sourceOffset = extent.Offset;
			if (location.ConstructionMethod == 1)
			{
				sourceOffset += meta.IdatPayloadOffset
					?? throw new AvifDecodingException($"AVIF item {itemId} uses 'idat'-relative offsets but the file has no 'idat' box.");
			}

			if (sourceOffset < 0 || extent.Length < 0 || sourceOffset + extent.Length > data.Length)
			{
				throw new AvifDecodingException($"AVIF item {itemId} references data outside the file bounds.");
			}

			data.AsSpan(checked((int)sourceOffset), checked((int)extent.Length)).CopyTo(result.AsSpan(destOffset));
			destOffset += checked((int)extent.Length);
		}

		return result;
	}
}
