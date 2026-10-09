namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.Drawing;
using System.IO;

/// <summary>
/// Reads what a PNG picture states about itself in its header, without decoding the picture.
/// </summary>
public static class PngHelper
{
	// A PNG picture starts with a signature. Its first chunk follows - the length of the chunk, then its name,
	// IHDR - and opens with the width and the height of the picture, each a big-endian integer.
	private const int _signatureLength = 8;
	private const int _chunkLengthSize = sizeof(uint);
	private const int _chunkNameSize = 4;
	private const int _chunkNameOffset = _signatureLength + _chunkLengthSize;
	private const int _widthOffset = _chunkNameOffset + _chunkNameSize;
	private const int _heightOffset = _widthOffset + sizeof(int);
	private const int _headerLength = _heightOffset + sizeof(int);

	private static ReadOnlySpan<byte> Signature => [0x89, (byte)'P', (byte)'N', (byte)'G', (byte)'\r', (byte)'\n', 0x1A, (byte)'\n'];
	private static ReadOnlySpan<byte> HeaderChunkName => "IHDR"u8;

	/// <summary>
	/// Determines whether the data is a PNG picture, by the signature every PNG picture starts with.
	/// </summary>
	/// <param name="data">The data.</param>
	/// <returns><see langword="true"/> if the data starts with the PNG signature.</returns>
	public static bool IsPng(this ReadOnlySpan<byte> data)
		=> data.StartsWith(Signature);

	/// <inheritdoc cref="IsPng(ReadOnlySpan{byte})"/>
	public static bool IsPng(this byte[] data)
		=> IsPng(AsSpan(data));

	/// <summary>
	/// Reads the size of a PNG picture off its header.
	/// </summary>
	/// <param name="data">The picture, or as much of its beginning as holds the header.</param>
	/// <param name="size">The size of the picture, in pixels.</param>
	/// <returns><see langword="false"/> if the data does not begin with a PNG header.</returns>
	public static bool TryGetPngSize(this ReadOnlySpan<byte> data, out Size size)
	{
		if (data.Length < _headerLength || !IsPng(data) || !data.Slice(_chunkNameOffset, _chunkNameSize).SequenceEqual(HeaderChunkName))
		{
			size = default;
			return false;
		}

		size = new(BinaryPrimitives.ReadInt32BigEndian(data.Slice(_widthOffset)), BinaryPrimitives.ReadInt32BigEndian(data.Slice(_heightOffset)));
		return true;
	}

	/// <inheritdoc cref="TryGetPngSize(ReadOnlySpan{byte}, out Size)"/>
	public static bool TryGetPngSize(this byte[] data, out Size size)
		=> TryGetPngSize(AsSpan(data), out size);

	/// <summary>
	/// Reads the size of a PNG picture off its header.
	/// </summary>
	/// <param name="data">The picture, or as much of its beginning as holds the header.</param>
	/// <returns>The size of the picture, in pixels.</returns>
	/// <exception cref="InvalidDataException">The data does not begin with a PNG header.</exception>
	public static Size GetPngSize(this ReadOnlySpan<byte> data)
		=> TryGetPngSize(data, out var size) ? size : throw new InvalidDataException("The data does not begin with a PNG header.");

	/// <inheritdoc cref="GetPngSize(ReadOnlySpan{byte})"/>
	public static Size GetPngSize(this byte[] data)
		=> GetPngSize(AsSpan(data));

	private static ReadOnlySpan<byte> AsSpan(byte[] data)
		=> data ?? throw new ArgumentNullException(nameof(data));
}
