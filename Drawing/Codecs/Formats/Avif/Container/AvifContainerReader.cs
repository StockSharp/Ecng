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

internal static class AvifContainerReader
{
	public static AvifContainerInfo Read(Stream stream, ImageMetadata metadata)
	{
		try
		{
			return ReadCore(stream, metadata);
		}
		catch (Exception ex) when (ex is IndexOutOfRangeException or ArgumentOutOfRangeException or OverflowException or ArgumentException)
		{
			// A defensive boundary rather than exhaustive manual bounds-checking on every single field
			// read: the box-tree-walking loop in AvifBoxReader is the one place a truncated/hostile box
			// size is checked explicitly (the common, cheap-to-guard case), while a malformed field deeper
			// inside a box's own payload -- a bogus 'iinf' entry count, a corrupt 'ipma' association count,
			// etc. -- surfaces here as a converted AvifDecodingException instead of a raw framework
			// exception, satisfying the "never throw anything but AvifDecodingException/
			// AvifUnsupportedFeatureException" contract corpus tests rely on.
			throw new AvifDecodingException("Malformed AVIF container.", ex);
		}
	}

	private static AvifContainerInfo ReadCore(Stream stream, ImageMetadata metadata)
	{
		byte[] data = BufferStream(stream);

		var topLevel = AvifBoxReader.ReadBoxes(data, 0, data.Length, depth: 0);

		var ftypBox = RequireBox(topLevel, "ftyp", "AVIF file");
		var ftyp = AvifFtypBox.Parse(data, ftypBox);

		bool stillImageCompatible = ftyp.HasBrand("avif");
		bool animated = ftyp.HasBrand("avis");

		if (!stillImageCompatible && !animated)
		{
			throw new AvifDecodingException($"Unrecognized AVIF major/compatible brands (major brand '{ftyp.MajorBrand}').");
		}

		if (animated && !stillImageCompatible)
		{
			throw new AvifUnsupportedFeatureException("Animated AVIF ('avis' brand) is not supported; only still-image AVIF is supported.");
		}

		var metaBox = RequireBox(topLevel, "meta", "AVIF file");
		var metaInfo = AvifMetaBoxParser.Parse(data, metaBox, depth: 1);
		var assembled = AvifItemAssembler.Assemble(data, metaInfo);

		CaptureMetadata(assembled, metadata);

		return new AvifContainerInfo
		{
			Width = assembled.Width,
			Height = assembled.Height,
			BitDepth = assembled.BitDepth,
			Monochrome = assembled.Monochrome,
			ChromaSubsamplingX = assembled.ChromaSubsamplingX,
			ChromaSubsamplingY = assembled.ChromaSubsamplingY,
			HasAlpha = assembled.HasAlpha,
			ColorTiles = assembled.ColorTiles,
			AlphaTiles = assembled.AlphaTiles,
			GridRows = assembled.GridRows,
			GridColumns = assembled.GridColumns,
			Orientation = assembled.Orientation,
		};
	}

	private static void CaptureMetadata(AvifAssembledImage assembled, ImageMetadata metadata)
	{
		if (assembled.ExifData is { } exif)
		{
			metadata.Profiles.Add(new RawMetadataProfile { Kind = MetadataProfileKind.Exif, Data = exif });
		}

		if (assembled.XmpData is { } xmp)
		{
			metadata.Profiles.Add(new RawMetadataProfile { Kind = MetadataProfileKind.Xmp, Data = xmp });
		}

		if (assembled.ColorInfo?.IccProfile is { } icc)
		{
			metadata.Profiles.Add(new RawMetadataProfile { Kind = MetadataProfileKind.Icc, Data = icc });
		}
	}

	private static AvifBox RequireBox(List<AvifBox> boxes, string fourCc, string context)
	{
		foreach (var box in boxes)
		{
			if (box.FourCc == fourCc)
			{
				return box;
			}
		}

		throw new AvifDecodingException($"{context} is missing its '{fourCc}' box.");
	}

	private static byte[] BufferStream(Stream stream)
	{
		if (stream.CanSeek)
		{
			long declaredLength = stream.Length - stream.Position;
			if (declaredLength > AvifDecodingLimits.MaxFileSize)
			{
				throw new AvifDecodingException($"AVIF file size ({declaredLength} bytes) exceeds the supported maximum of {AvifDecodingLimits.MaxFileSize} bytes.");
			}
		}

		using var buffer = new MemoryStream();
		var chunk = new byte[81920];
		long total = 0;
		int read;

		while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
		{
			total += read;
			if (total > AvifDecodingLimits.MaxFileSize)
			{
				throw new AvifDecodingException($"AVIF file size exceeds the supported maximum of {AvifDecodingLimits.MaxFileSize} bytes.");
			}

			buffer.Write(chunk, 0, read);
		}

		return buffer.ToArray();
	}
}
