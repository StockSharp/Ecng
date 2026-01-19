#if NETSTANDARD2_0
namespace System.Text;

/// <summary>
/// Compatibility extensions for <see cref="Encoding"/>.
/// </summary>
public static class EncodingExtensions
{
	/// <summary>
	/// Decodes a span of bytes into a string using the specified encoding.
	/// </summary>
	/// <param name="encoding">The encoding to use.</param>
	/// <param name="bytes">The span of bytes to decode.</param>
	/// <returns>The decoded string.</returns>
	public static string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
		=> encoding.GetString(bytes.ToArray());

	/// <summary>
	/// Encodes a span of characters into a span of bytes using the specified encoding.
	/// </summary>
	/// <param name="encoding">The encoding to use.</param>
	/// <param name="chars">The span of characters to encode.</param>
	/// <param name="bytes">The destination span for the encoded bytes.</param>
	/// <returns>The number of bytes written.</returns>
	public static int GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes)
	{
		var arr = encoding.GetBytes(chars.ToArray());
		arr.CopyTo(bytes);
		return arr.Length;
	}
}
#endif
