namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.ColorConversion;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics;

internal static class Vp8ColorConverterSelector
{
	[SuppressMessage(
		"Performance",
		"CA1859",
		Justification = "Returns IVp8ColorConverter by design: the concrete tier is chosen at runtime from hardware support, so the property cannot be typed to one implementation.")]
	public static IVp8ColorConverter Instance { get; } = Select();

	private static IVp8ColorConverter Select() =>
#if NET7_0_OR_GREATER
		Vector128.IsHardwareAccelerated ? new Vector128Vp8ColorConverter() : new Vp8ScalarColorConverter();
#else
		new Vp8ScalarColorConverter();
#endif
}
