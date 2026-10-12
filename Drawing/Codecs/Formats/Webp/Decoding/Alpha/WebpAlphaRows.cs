namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Alpha;

using System;

using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

internal sealed class WebpAlphaRows
{
	private readonly WebpAlphaHeader _header;
	private readonly ReadOnlyMemory<byte> _payload;
	private readonly Vp8LRowDecoder _decoder;
	private readonly byte[] _previous;
	private readonly byte[] _row;
	private int _nextRow;

	public WebpAlphaRows(ReadOnlyMemory<byte> bytes, int width, int height, ImageProcessingLimits limits, long reservedBytes)
	{
		if (bytes.IsEmpty)
			throw new WebpDecodingException("ALPH chunk is empty.");
		_header = WebpAlphaHeader.Parse(bytes.Span[0]);
		_payload = bytes[1..];
		if (_header.CompressionMethod == WebpAlphaCompressionMethods.Lossless)
			_decoder = Vp8LDecoder.OpenAlphaRows(_payload, width, height, limits, reservedBytes + width * 2L);
		else if (_payload.Length < (long)width * height)
			throw new WebpDecodingException("WebP alpha plane is truncated.");
		WorkingBytes = (_decoder?.WorkingBytes ?? 0) + (long)width * 2;
		limits.EnsureWorkingMemory(WorkingBytes + reservedBytes);
		_previous = new byte[width];
		_row = new byte[width];
	}

	public long WorkingBytes { get; }

	public ReadOnlySpan<byte> ReadRow()
	{
		if (_decoder is null)
			_payload.Span.Slice(_nextRow * _row.Length, _row.Length).CopyTo(_row);
		else
		{
			var pixels = _decoder.ReadRow();
			for (var x = 0; x < _row.Length; x++)
				_row[x] = (byte)(pixels[x] >> 8);
		}
		WebpAlphaFilter.ReverseRow(_header.FilterMethod, _row, _previous, _nextRow++ == 0);
		_row.CopyTo(_previous, 0);
		return _row;
	}
}
