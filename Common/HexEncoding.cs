namespace Ecng.Common;

using System;
using System.Text;

/// <summary>
/// Summary description for HexEncoding.
/// http://www.codeproject.com/KB/recipes/hexencoding.aspx
/// </summary>
public class HexEncoding : Encoding
{
	/// <summary>
	/// When overridden in a derived class, calculates the number of bytes produced by encoding the characters in the specified <see cref="T:System.String"/>.
	/// </summary>
	/// <returns>
	/// The number of bytes produced by encoding the specified characters.
	/// </returns>
	/// <param name="hexString">The <see cref="T:System.String"/> containing the set of characters to encode.</param>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="hexString"/> is null.</exception>
	/// <exception cref="T:System.Text.EncoderFallbackException">A fallback occurred (see Understanding Encodings for complete explanation)
	/// -and-
	/// <see cref="P:System.Text.Encoding.EncoderFallback"/> is set to <see cref="T:System.Text.EncoderExceptionFallback"/>.
	/// </exception>
	/// <filterpriority>1</filterpriority>
	public override int GetByteCount(string hexString)
	{
		if (hexString.IsEmpty())
			throw new ArgumentNullException(nameof(hexString));

		return GetByteCount(hexString.ToCharArray(), 0, hexString.Length);
	}

	/// <summary>
	/// When overridden in a derived class, calculates the number of bytes produced by encoding a set of characters from the specified character array.
	/// </summary>
	/// <returns>
	/// The number of bytes produced by encoding the specified characters.
	/// </returns>
	/// <param name="chars">The character array containing the set of characters to encode.</param>
	/// <param name="index">The index of the first character to encode.</param>
	/// <param name="count">The number of characters to encode.</param>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="chars"/> is null.</exception>
	/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is less than zero.
	/// -or- 
	/// <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in <paramref name="chars"/>. 
	/// </exception>
	/// <exception cref="T:System.Text.EncoderFallbackException">A fallback occurred (see Understanding Encodings for complete explanation)
	///     -and-
	/// <see cref="P:System.Text.Encoding.EncoderFallback"/> is set to <see cref="T:System.Text.EncoderExceptionFallback"/>.
	/// </exception>
	/// <filterpriority>1</filterpriority>
	public override int GetByteCount(char[] chars, int index, int count)
	{
		if (chars is null)
			throw new ArgumentNullException(nameof(chars));

		if (chars.Length < (index + count))
			throw new ArgumentOutOfRangeException(nameof(chars));

		var numHexChars = 0;

		// remove all none A-F, 0-9, characters
		for (int i = index; i < index + count; i++)
		{
			var c = chars[i];

			if (IsHexDigit(c))
				numHexChars++;
		}

		// if odd number of characters, discard last character
		if (numHexChars % 2 != 0)
		{
			numHexChars--;
		}

		return numHexChars / 2; // 2 characters per byte
	}

	/// <summary>
	/// When overridden in a derived class, encodes a set of characters from the specified character array into the specified byte array.
	/// </summary>
	/// <returns>
	/// The actual number of bytes written into <paramref name="bytes"/>.
	/// </returns>
	/// <param name="chars">The character array containing the set of characters to encode.</param>
	/// <param name="charIndex">The index of the first character to encode.</param>
	/// <param name="charCount">The number of characters to encode.</param>
	/// <param name="bytes">The byte array to contain the resulting sequence of bytes.</param>
	/// <param name="byteIndex">The index at which to start writing the resulting sequence of bytes.</param>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="chars"/> is null.
	///     -or- 
	/// <paramref name="bytes"/> is null. 
	/// </exception>
	/// <exception cref="T:System.ArgumentOutOfRangeException">
	/// <paramref name="charIndex"/> or <paramref name="charCount"/> or <paramref name="byteIndex"/> is less than zero.
	///     -or- 
	/// <paramref name="charIndex"/> and <paramref name="charCount"/> do not denote a valid range in <paramref name="chars"/>.
	///     -or- 
	/// <paramref name="byteIndex"/> is not a valid index in <paramref name="bytes"/>. 
	/// </exception>
	/// <exception cref="T:System.ArgumentException"><paramref name="bytes"/> does not have enough capacity from <paramref name="byteIndex"/> to the end of the array to accommodate the resulting bytes. 
	/// </exception>
	/// <exception cref="T:System.Text.EncoderFallbackException">A fallback occurred (see Understanding Encodings for complete explanation)
	///     -and-
	/// <see cref="P:System.Text.Encoding.EncoderFallback"/> is set to <see cref="T:System.Text.EncoderExceptionFallback"/>.
	/// </exception>
	/// <filterpriority>1</filterpriority>
	public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		var hexBytes = GetBytes(chars, charIndex, charCount, out int discarded);
		Array.Copy(hexBytes, 0, bytes, byteIndex, hexBytes.Length);
		return hexBytes.Length;
	}

	/// <summary>
	/// Creates a byte array from the hexadecimal string. Each two characters are combined
	/// to create one byte. First two hexadecimal characters become first byte in returned array.
	/// Non-hexadecimal characters are ignored. 
	/// </summary>
	/// <param name="hexString">string to convert to byte array</param>
	/// <returns>byte array, in the same left-to-right order as the hexString</returns>
	public override byte[] GetBytes(string hexString)
	{
		if (hexString.IsEmpty())
			throw new ArgumentNullException(nameof(hexString));

		return GetBytes(hexString.ToCharArray(), 0, hexString.Length, out int discarded);
	}

	/// <summary>
	/// When overridden in a derived class, calculates the number of characters produced by decoding a sequence of bytes from the specified byte array.
	/// </summary>
	/// <returns>
	/// The number of characters produced by decoding the specified sequence of bytes.
	/// </returns>
	/// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
	/// <param name="index">The index of the first byte to decode.</param>
	/// <param name="count">The number of bytes to decode.</param>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="bytes"/> is null.</exception>
	/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is less than zero.
	///     -or- 
	/// <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in <paramref name="bytes"/>. 
	/// </exception><exception cref="T:System.Text.DecoderFallbackException">A fallback occurred (see Understanding Encodings for complete explanation)
	///     -and-
	/// <see cref="P:System.Text.Encoding.DecoderFallback"/> is set to <see cref="T:System.Text.DecoderExceptionFallback"/>.
	/// </exception>
	/// <filterpriority>1</filterpriority>
	public override int GetCharCount(byte[] bytes, int index, int count)
	{
		var charCount = 0;

		for (var i = index; i < (index + count); i++)
			charCount += bytes[i].ToString("X2").Length;

		return charCount;
	}

	/// <summary>
	/// When overridden in a derived class, decodes a sequence of bytes from the specified byte array into the specified character array.
	/// </summary>
	/// <returns>
	/// The actual number of characters written into <paramref name="chars"/>.
	/// </returns>
	/// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
	/// <param name="byteIndex">The index of the first byte to decode.</param>
	/// <param name="byteCount">The number of bytes to decode.</param>
	/// <param name="chars">The character array to contain the resulting set of characters.</param>
	/// <param name="charIndex">The index at which to start writing the resulting set of characters.</param>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="bytes"/> is null.
	///     -or- 
	/// <paramref name="chars"/> is null. 
	/// </exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="byteIndex"/> or <paramref name="byteCount"/> or <paramref name="charIndex"/> is less than zero.
	///     -or- 
	/// <paramref name="byteIndex"/> and <paramref name="byteCount"/> do not denote a valid range in <paramref name="bytes"/>.
	///     -or- 
	/// <paramref name="charIndex"/> is not a valid index in <paramref name="chars"/>. 
	/// </exception>
	/// <exception cref="T:System.ArgumentException"><paramref name="chars"/> does not have enough capacity from <paramref name="charIndex"/> to the end of the array to accommodate the resulting characters. 
	/// </exception>
	/// <exception cref="T:System.Text.DecoderFallbackException">A fallback occurred (see Understanding Encodings for complete explanation)
	///     -and-
	/// <see cref="P:System.Text.Encoding.DecoderFallback"/> is set to <see cref="T:System.Text.DecoderExceptionFallback"/>.
	/// </exception>
	/// <filterpriority>1</filterpriority>
	public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		var result = new StringBuilder();

		for (var i = byteIndex; i < (byteIndex + byteCount); i++)
			result.AppendFormat(bytes[i].ToString("X2"));

		Array.Copy(result.ToString().ToCharArray(), 0, chars, charIndex, result.Length);
		return result.Length;
	}

	/// <summary>
	/// When overridden in a derived class, calculates the maximum number of bytes produced by encoding the specified number of characters.
	/// </summary>
	/// <returns>
	/// The maximum number of bytes produced by encoding the specified number of characters.
	/// </returns>
	/// <param name="charCount">The number of characters to encode.</param>
	/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="charCount"/> is less than zero. 
	/// </exception><exception cref="T:System.Text.EncoderFallbackException">A fallback occurred (see Understanding Encodings for complete explanation)
	///     -and-
	/// <see cref="P:System.Text.Encoding.EncoderFallback"/> is set to <see cref="T:System.Text.EncoderExceptionFallback"/>.
	/// </exception>
	/// <filterpriority>1</filterpriority>
	public override int GetMaxByteCount(int charCount)
	{
		return charCount * 2;
	}

	/// <summary>
	/// When overridden in a derived class, calculates the maximum number of characters produced by decoding the specified number of bytes.
	/// </summary>
	/// <returns>
	/// The maximum number of characters produced by decoding the specified number of bytes.
	/// </returns>
	/// <param name="byteCount">The number of bytes to decode.</param>
	/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="byteCount"/> is less than zero. 
	/// </exception><exception cref="T:System.Text.DecoderFallbackException">A fallback occurred (see Understanding Encodings for complete explanation)
	///     -and-
	/// <see cref="P:System.Text.Encoding.DecoderFallback"/> is set to <see cref="T:System.Text.DecoderExceptionFallback"/>.
	/// </exception>
	/// <filterpriority>1</filterpriority>
	public override int GetMaxCharCount(int byteCount)
	{
		if (byteCount % 2 != 0)
		{
			byteCount--;
		}

		return byteCount / 2;
	}

	/// <summary>
	/// Creates a byte array from the hexadecimal string. Each two characters are combined
	/// to create one byte. First two hexadecimal characters become first byte in returned array.
	/// Non-hexadecimal characters are ignored. 
	/// </summary>
	/// <param name="chars">The character array containing the set of characters to encode.</param>
	/// <param name="charIndex">The index of the first character to encode.</param>
	/// <param name="charCount">The number of characters to encode.</param>
	/// <param name="discarded">number of characters in string ignored.</param>
	/// <returns>byte array, in the same left-to-right order as the hexString.</returns>
	public static byte[] GetBytes(char[] chars, int charIndex, int charCount, out int discarded)
	{
		if (chars is null)
			throw new ArgumentNullException(nameof(chars));

		if (chars.Length < (charIndex + charCount))
			throw new ArgumentOutOfRangeException(nameof(chars));


		discarded = 0;
		var newString = string.Empty;

		// remove all none A-F, 0-9, characters
		for (var i = charIndex; i < (charIndex + charCount); i++)
		{
			var c = chars[i];

			if (IsHexDigit(c))
				newString += c;
			else
				discarded++;
		}

		// if odd number of characters, discard last character
		if (newString.Length % 2 != 0)
		{
			discarded++;
			newString = newString.Substring(0, newString.Length - 1);
		}

		var byteLength = newString.Length / 2;
		var bytes = new byte[byteLength];
		var j = 0;
		for (var i = 0; i < bytes.Length; i++)
		{
			var hex = new string([newString[j], newString[j + 1]]);
			bytes[i] = HexToByte(hex);
			j = j + 2;
		}

		return bytes;
	}

	/// <summary>
	/// Returns true is c is a hexadecimal digit (A-F, a-f, 0-9)
	/// </summary>
	/// <param name="c">Character to test.</param>
	/// <returns>true if hex digit, false if not.</returns>
	public static bool IsHexDigit(char c)
	{
		var numA = 'A'.To<int>();
		var num1 = '0'.To<int>();

		c = c.ToUpper(false);

		var numChar = c.To<int>();
		if (numChar >= numA && numChar < (numA + 6))
			return true;
		if (numChar >= num1 && numChar < (num1 + 10))
			return true;

		return false;
	}

	/// <summary>
	/// Converts 1 or 2 character string into equivalant byte value
	/// </summary>
	/// <param name="hexString">1 or 2 character string.</param>
	/// <returns>byte</returns>
	private static byte HexToByte(string hexString)
	{
		if (hexString.IsEmpty())
			throw new ArgumentNullException(nameof(hexString));

		if (hexString.Length > 2 || hexString.Length <= 0)
			throw new ArgumentException("hex must be 1 or 2 characters in length");

		return byte.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
	}
}