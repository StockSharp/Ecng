namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal sealed class AvifMetaBoxInfo
{
	public uint? PrimaryItemId { get; init; }

	public IReadOnlyDictionary<uint, AvifItemInfo> Items { get; init; }

	public IReadOnlyDictionary<uint, AvifItemLocation> Locations { get; init; }

	public IReadOnlyList<AvifItemReference> References { get; init; }

	public AvifItemPropertiesBox Properties { get; init; }
	public int? IdatPayloadOffset { get; init; }
}
