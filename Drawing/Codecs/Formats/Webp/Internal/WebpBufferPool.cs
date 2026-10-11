namespace Ecng.Drawing.Codecs.Formats.Webp.Internal;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Buffers;

internal static class WebpBufferPool
{
	private const int MaxArrayLength = 32 * 1024 * 1024;
	private const int MaxArraysPerBucket = 4;

	public static ArrayPool<byte> Shared { get; } = ArrayPool<byte>.Create(MaxArrayLength, MaxArraysPerBucket);
	public static ArrayPool<uint> SharedUInt32 { get; } = ArrayPool<uint>.Create(8 * 1024 * 1024, MaxArraysPerBucket);
	public static ArrayPool<int> SharedInt32 { get; } = ArrayPool<int>.Create(8 * 1024 * 1024, MaxArraysPerBucket);
}
