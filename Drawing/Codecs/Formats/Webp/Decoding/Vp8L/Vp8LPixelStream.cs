namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

using System;

using Ecng.Drawing.Codecs.Formats.Webp.Internal;

internal sealed class Vp8LPixelStream
{
	private const int LiteralCodes = 256;
	private const int LengthLimit = LiteralCodes + 24;
	private Vp8LBitReader _reader;
	private readonly Vp8LHuffmanGroup[] _groups;
	private readonly Vp8LMetaHuffmanImage _meta;
	private readonly int _width;

	public Vp8LPixelStream(Vp8LBitReader reader, int width, int height, bool allowRecursion, ImageProcessingLimits limits)
	{
		if (width <= 0 || height <= 0 || (long)width * height > int.MaxValue)
			throw new WebpDecodingException($"Invalid VP8L stream dimensions {width}x{height}.");
		_reader = reader;
		_width = width;
		PixelCount = checked(width * height);
		if (reader.ReadBits(1) != 0)
		{
			CacheBits = (int)reader.ReadBits(4);
			if (CacheBits is < 1 or > WebpDecodingLimits.MaxColorCacheBits)
				throw new WebpDecodingException("Invalid VP8L color cache size.");
		}
		_groups = Vp8LMetaHuffmanImage.ReadGroups(reader, width, height, CacheBits, allowRecursion, limits, out _meta);
		WorkingBytes = _meta?.WorkingBytes ?? 0;
		foreach (var group in _groups)
			WorkingBytes += (long)(group.Green.Entries.Length + group.Red.Entries.Length + group.Blue.Entries.Length +
				group.Alpha.Entries.Length + group.Distance.Entries.Length) * 4;
		WorkingBytes += CacheBits == 0 ? 0 : (1 << CacheBits) * 4;
	}

	public int PixelCount { get; }
	public int CacheBits { get; }
	public long WorkingBytes { get; }

	public Vp8LPixelStream Clone()
	{
		var clone = (Vp8LPixelStream)MemberwiseClone();
		clone._reader = _reader.Clone();
		return clone;
	}

	public bool TryRead(int position, out Vp8LPixelCommand command)
	{
		command = default;
		if (position >= PixelCount || _reader.IsOverBudget)
			return false;
		var group = _groups[_meta?.GetGroupIndex(position % _width, position / _width) ?? 0];
		var code = group.Green.DecodeMain(_reader);
		if (code < LiteralCodes)
		{
			var red = group.Red.DecodeMain(_reader);
			var blue = group.Blue.DecodeMain(_reader);
			var alpha = group.Alpha.DecodeMain(_reader);
			command = new(1, 0, ((uint)alpha << 24) | ((uint)red << 16) | ((uint)code << 8) | (uint)blue, -1);
			return true;
		}
		if (code < LengthLimit)
		{
			var length = Vp8LBackwardReferenceTables.DecodePrefixCodeValue(code - LiteralCodes, _reader);
			var distanceCode = Vp8LBackwardReferenceTables.DecodePrefixCodeValue(group.Distance.DecodeMain(_reader), _reader);
			var distance = Vp8LBackwardReferenceTables.PlaneCodeToDistance(_width, distanceCode);
			if (distance > position || length > PixelCount - position)
				return false;
			command = new(length, distance, 0, -1);
			return true;
		}
		if (CacheBits != 0 && code < LengthLimit + (1 << CacheBits))
		{
			command = new(1, 0, 0, code - LengthLimit);
			return true;
		}
		return false;
	}
}
