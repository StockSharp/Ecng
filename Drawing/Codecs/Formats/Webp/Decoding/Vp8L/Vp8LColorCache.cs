namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal sealed class Vp8LColorCache
{
	// Verified against libwebp's src/utils/color_cache_utils.h (kHashMul / VP8LHashPix): hash = (argb *
	// 0x1e35a7bd) >> (32 - hash_bits). Any other multiplier would still build *a* hash table, just not one
	// that agrees with what real encoders assume, silently corrupting every cache-index reference.
	private const uint HashMultiplier = 0x1e35a7bdu;

	private readonly uint[] _entries;
	private readonly int _hashShift;

	public Vp8LColorCache(int cacheBits)
	{
		_entries = new uint[1 << cacheBits];
		_hashShift = 32 - cacheBits;
	}
	public void Insert(uint argb) => _entries[Hash(argb)] = argb;
	public uint Lookup(int index) => _entries[index];
	public bool TryGetHitIndex(uint argb, out int index)
	{
		index = Hash(argb);
		return _entries[index] == argb;
	}

	private int Hash(uint argb) => (int)((argb * HashMultiplier) >> _hashShift);
}
