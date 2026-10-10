namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;

/// <summary>
/// What is looked at in an ICC profile before it goes from a picture into the one made of it.
/// </summary>
internal static class IccProfile
{
	/// <summary>
	/// The largest profile that is carried over.
	/// </summary>
	public const int MaxLength = 4 * 1024 * 1024;

	// A profile starts with a header of this length: its own length first, the color space of the
	// samples it describes at 16, its signature at 36.
	private const int _header = 128;

	/// <summary>
	/// Whether the bytes are a profile for RGB samples no larger than <see cref="MaxLength"/>. The pictures
	/// that are written hold RGB whatever they were read from, so a profile for anything else - CMYK,
	/// gray - no longer describes them.
	/// </summary>
	public static bool IsRgb(ReadOnlySpan<byte> profile)
		=> profile.Length is >= _header and <= MaxLength
			&& BinaryPrimitives.ReadUInt32BigEndian(profile) == profile.Length
			&& profile.Slice(16, 4).SequenceEqual("RGB "u8)
			&& profile.Slice(36, 4).SequenceEqual("acsp"u8);
}
