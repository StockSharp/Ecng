namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;

/// <summary>
/// Reads the JPEG segments both decoders understand the same way: tables, the frame header and the
/// application segments that say how the samples are to be shown.
/// </summary>
internal static class JpegSegments
{
	public static int FindScanEnd(ReadOnlySpan<byte> data, int position)
	{
		while (position < data.Length)
		{
			if (data[position] != 0xff) { position++; continue; }
			var marker = position;
			while (position < data.Length && data[position] == 0xff) position++;
			if (position == data.Length) break;
			var value = data[position++];
			if (value != 0 && value is not (>= 0xd0 and <= 0xd7)) return marker;
		}
		throw new InvalidDataException("JPEG scan has no closing marker.");
	}

	/// <summary>
	/// APP0: the JFIF header.
	/// </summary>
	public const int Jfif = 0xE0;

	/// <summary>
	/// APP1: Exif.
	/// </summary>
	public const int Exif = 0xE1;

	/// <summary>
	/// APP2: a part of an ICC profile.
	/// </summary>
	public const int Profile = 0xE2;

	/// <summary>
	/// APP14: the Adobe segment.
	/// </summary>
	public const int Adobe = 0xEE;

	private const int _orientationTag = 0x0112;

	/// <summary>
	/// What an APP2 segment that holds a part of an ICC profile starts with; the number of the part
	/// and the number of parts follow, a byte each.
	/// </summary>
	public static ReadOnlySpan<byte> ProfileSignature => "ICC_PROFILE\0"u8;

	/// <summary>
	/// Whether an APP0 segment is the JFIF header.
	/// </summary>
	public static bool IsJfif(ReadOnlySpan<byte> segment)
		=> segment.StartsWith("JFIF\0"u8);

	/// <summary>
	/// Reads the color transform flag of an APP14 segment.
	/// </summary>
	/// <returns><see langword="false"/> if the segment is not Adobe's.</returns>
	public static bool TryReadAdobeTransform(ReadOnlySpan<byte> segment, out int transform)
	{
		transform = -1;

		if (segment.Length < 12 || !segment.StartsWith("Adobe"u8))
			return false;

		transform = segment[11];
		return true;
	}

	/// <summary>
	/// Reads the quantization tables of a DQT segment, each into natural order.
	/// </summary>
	public static void ReadQuantizationTables(ReadOnlySpan<byte> segment, int[][] tables)
	{
		for (var at = 0; at < segment.Length;)
		{
			var precision = segment[at] >> 4;
			var index = segment[at] & 15;
			at++;

			if (index >= tables.Length || precision > 1 || segment.Length - at < 64 * (precision + 1))
				throw new InvalidDataException("Invalid JPEG quantization table.");

			var table = new int[64];

			for (var i = 0; i < 64; i++)
			{
				table[JpegTransforms.ZigZag[i]] = precision == 0 ? segment[at] : BinaryPrimitives.ReadUInt16BigEndian(segment[at..]);
				at += precision + 1;
			}

			tables[index] = table;
		}
	}

	/// <summary>
	/// Reads the Huffman tables of a DHT segment.
	/// </summary>
	public static void ReadHuffmanTables(ReadOnlySpan<byte> segment, JpegHuffmanTable[] dc, JpegHuffmanTable[] ac)
	{
		for (var at = 0; at < segment.Length;)
		{
			if (segment.Length - at < 17)
				throw new InvalidDataException("Truncated JPEG Huffman table.");

			var kind = segment[at] >> 4;
			var index = segment[at] & 15;
			at++;

			if (kind > 1 || index >= dc.Length)
				throw new InvalidDataException("Invalid JPEG Huffman table index.");

			var counts = segment.Slice(at, 16);
			at += 16;

			var symbols = 0;

			foreach (var count in counts)
				symbols += count;

			if (symbols > 256 || symbols > segment.Length - at)
				throw new InvalidDataException("Invalid JPEG Huffman symbols.");

			var table = new JpegHuffmanTable(counts, segment.Slice(at, symbols));
			at += symbols;

			if (kind == 0)
				dc[index] = table;
			else
				ac[index] = table;
		}
	}

	/// <summary>
	/// Reads a frame header: the size of the picture and its components with their block grids.
	/// </summary>
	public static JpegComponent[] ReadFrame(ReadOnlySpan<byte> segment, out int width, out int height)
		=> ReadFrame(segment, out width, out height, 1);

	public static JpegComponent[] ReadFrame(ReadOnlySpan<byte> segment, out int width, out int height, int scale, bool isLossless = false)
		=> ReadFrame(segment, out width, out height, scale, isLossless, false);

	public static JpegComponent[] ReadFrame(ReadOnlySpan<byte> segment, out int width, out int height, int scale, bool isLossless, bool isRowBuffered)
		=> ReadFrame(segment, out width, out height, scale, isLossless, isRowBuffered, ImageProcessingLimits.Default);

	public static JpegComponent[] ReadFrame(ReadOnlySpan<byte> segment, out int width, out int height, int scale, bool isLossless, bool isRowBuffered, ImageProcessingLimits limits)
	{
		if (segment.Length < 6)
			throw new InvalidDataException("Truncated JPEG frame.");
		if (isLossless ? segment[0] is < 2 or > 16 : segment[0] is not (8 or 12))
			throw new NotSupportedException("Only 8-bit and 12-bit DCT JPEG are supported.");

		height = BinaryPrimitives.ReadUInt16BigEndian(segment[1..]);
		width = BinaryPrimitives.ReadUInt16BigEndian(segment[3..]);

		if (width == 0 || height == 0)
			throw new InvalidDataException("JPEG frame has no size.");

		if (!isRowBuffered) RasterImage.EnsureDecodable((width + scale - 1) / scale, (height + scale - 1) / scale, limits);

		var count = segment[5];

		if (count is not (1 or 3 or 4))
			throw new NotSupportedException("JPEG must have 1, 3 or 4 color components.");

		if (segment.Length != 6 + count * 3)
			throw new InvalidDataException("Malformed JPEG frame components.");

		var components = new JpegComponent[count];
		var maxH = 1;
		var maxV = 1;

		for (var i = 0; i < count; i++)
		{
			var at = 6 + i * 3;
			var h = segment[at + 1] >> 4;
			var v = segment[at + 1] & 15;
			var table = segment[at + 2];

			if (h is < 1 or > 4 || v is < 1 or > 4 || table > 3)
				throw new InvalidDataException("Invalid JPEG sampling factors.");

			// A lone component is never interleaved: its blocks come in raster order whatever its factors say.
			if (count == 1)
				h = v = 1;

			for (var j = 0; j < i; j++)
				if (components[j].Id == segment[at])
					throw new InvalidDataException("Repeated JPEG frame component.");
			components[i] = new() { Id = segment[at], H = h, V = v, QuantTable = table, Precision = segment[0], Scale = scale };
			maxH = Math.Max(maxH, h);
			maxV = Math.Max(maxV, v);
		}

		var mcusWide = (width + maxH * 8 - 1) / (maxH * 8);
		var mcusHigh = (height + maxV * 8 - 1) / (maxV * 8);

		foreach (var component in components)
		{
			component.BlocksWide = mcusWide * component.H;
			component.BlocksHigh = mcusHigh * component.V;
		}

		return components;
	}

	/// <summary>
	/// Refuses a file too short to hold the picture it declares, before buffers are allocated for it.
	/// </summary>
	/// <param name="width">Width of the picture.</param>
	/// <param name="height">Height of the picture.</param>
	/// <param name="remaining">Bytes of the file that follow the header being read.</param>
	public static void EnsureEnoughData(int width, int height, int remaining)
	{
		// Every block costs at least one bit of entropy-coded data, and whatever the sampling factors
		// (1 to 4 each way), one of the components has a quarter of the blocks of a full-resolution plane.
		var blocks = (long)((width + 7) / 8) * ((height + 7) / 8) / 4;

		if (remaining * 8L < blocks)
			throw new InvalidDataException("JPEG data is too short for the size it declares.");
	}

	/// <summary>
	/// Reads the Exif orientation of a JPEG.
	/// </summary>
	/// <returns>A value from 1 to 8; 1 - the picture is stored the way it is shown - if the file says nothing readable.</returns>
	public static int ReadOrientation(ReadOnlySpan<byte> data)
	{
		var at = 2;

		while (NextBeforeScan(data, ref at, out var marker, out var segment))
		{
			if (marker != Exif)
				continue;

			var orientation = ReadExifOrientation(segment);

			if (orientation != 0)
				return orientation;
		}

		return 1;
	}

	/// <summary>
	/// Reads the ICC profile of a JPEG: the parts its APP2 segments hold, put together.
	/// </summary>
	/// <returns>
	/// The profile if it is one that can go into a re-encoded picture (see <see cref="IccProfile.IsRgb"/>);
	/// otherwise - and if the file has none, or its parts do not make a whole - empty.
	/// </returns>
	public static byte[] ReadProfile(ReadOnlySpan<byte> data)
	{
		// Where every part starts in the file and how long it is, by its number.
		Span<int> starts = stackalloc int[byte.MaxValue + 1];
		Span<int> lengths = stackalloc int[byte.MaxValue + 1];
		var count = 0;
		var found = 0;
		var total = 0;
		var at = 2;

		while (NextBeforeScan(data, ref at, out var marker, out var segment))
		{
			if (marker != Profile || !segment.StartsWith(ProfileSignature))
				continue;

			var header = ProfileSignature.Length + 2;

			if (segment.Length < header)
				return [];

			var number = segment[header - 2];

			if (count == 0)
				count = segment[header - 1];

			// Every part once: a start of zero is a part not met yet.
			if (number < 1 || number > count || segment[header - 1] != count || starts[number] != 0)
				return [];

			starts[number] = at - segment.Length + header;
			lengths[number] = segment.Length - header;
			total += lengths[number];
			found++;
		}

		if (found == 0 || found != count || total > IccProfile.MaxLength)
			return [];

		var profile = new byte[total];
		var to = 0;

		for (var number = 1; number <= count; number++)
		{
			data.Slice(starts[number], lengths[number]).CopyTo(profile.AsSpan(to));
			to += lengths[number];
		}

		return IccProfile.IsRgb(profile) ? profile : [];
	}

	// Steps to the next segment before the first scan: that is where a file says how it is to be shown.
	// False when there is no such segment left, or the file cannot be followed.
	private static bool NextBeforeScan(ReadOnlySpan<byte> data, ref int at, out int marker, out ReadOnlySpan<byte> segment)
	{
		marker = 0;
		segment = default;

		while (at + 4 <= data.Length)
		{
			if (data[at] != 0xFF)
				return false;

			marker = data[at + 1];

			// Fill bytes before a marker, and markers that stand alone.
			if (marker == 0xFF)
			{
				at++;
				continue;
			}

			if (marker is 0x01 or (>= 0xD0 and <= 0xD7))
			{
				at += 2;
				continue;
			}

			if (marker is 0xDA or 0xD9)
				return false;

			var length = BinaryPrimitives.ReadUInt16BigEndian(data[(at + 2)..]);

			if (length < 2 || length > data.Length - at - 2)
				return false;

			segment = data.Slice(at + 4, length - 2);
			at += 2 + length;
			return true;
		}

		return false;
	}

	// Returns 0 if the segment is not an Exif block or holds no usable orientation.
	private static int ReadExifOrientation(ReadOnlySpan<byte> segment)
	{
		if (!segment.StartsWith("Exif\0\0"u8))
			return 0;

		// What follows is a TIFF file: byte order, the number 42, the offset of the first directory.
		var tiff = segment[6..];

		if (tiff.Length < 8)
			return 0;

		bool bigEndian;

		if (tiff.StartsWith("MM"u8))
			bigEndian = true;
		else if (tiff.StartsWith("II"u8))
			bigEndian = false;
		else
			return 0;

		if (U16(tiff[2..], bigEndian) != 42)
			return 0;

		var directory = U32(tiff[4..], bigEndian);

		if (directory > tiff.Length - 2)
			return 0;

		var at = (int)directory;
		var entries = U16(tiff[at..], bigEndian);
		at += 2;

		// An entry is 12 bytes: tag, type, count, then the value itself when it fits four bytes.
		for (var i = 0; i < entries && at + 12 <= tiff.Length; i++, at += 12)
		{
			if (U16(tiff[at..], bigEndian) != _orientationTag)
				continue;

			const int shortType = 3;

			if (U16(tiff[(at + 2)..], bigEndian) != shortType || U32(tiff[(at + 4)..], bigEndian) != 1)
				return 0;

			var orientation = U16(tiff[(at + 8)..], bigEndian);
			return orientation is >= 1 and <= 8 ? orientation : 0;
		}

		return 0;
	}

	private static ushort U16(ReadOnlySpan<byte> data, bool bigEndian)
		=> bigEndian ? BinaryPrimitives.ReadUInt16BigEndian(data) : BinaryPrimitives.ReadUInt16LittleEndian(data);

	private static uint U32(ReadOnlySpan<byte> data, bool bigEndian)
		=> bigEndian ? BinaryPrimitives.ReadUInt32BigEndian(data) : BinaryPrimitives.ReadUInt32LittleEndian(data);
}
