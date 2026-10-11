namespace Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal ref struct TiffLzwBitReader(ReadOnlySpan<byte> data)
{
	private readonly ReadOnlySpan<byte> _data = data;
	private int _bytePos;
	private ulong _buffer;
	private int _bitCount;
	public bool TryReadCode(int bits, out int code)
	{
		while (_bitCount < bits)
		{
			if (_bytePos >= _data.Length)
			{
				code = 0;
				return false;
			}

			_buffer |= (ulong)_data[_bytePos++] << (56 - _bitCount);
			_bitCount += 8;
		}

		code = (int)(_buffer >> (64 - bits));
		_buffer <<= bits;
		_bitCount -= bits;
		return true;
	}
}
