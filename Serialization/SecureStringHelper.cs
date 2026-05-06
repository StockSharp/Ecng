namespace Ecng.Serialization;

/// <summary>
/// Public facade over the package-internal <see cref="SecureStringEncryptor"/>:
/// encrypts and decrypts <see cref="SecureString"/> payloads using the same
/// key/entropy pair the JSON / <see cref="SettingsStorage"/> primitive path
/// applies, without exposing the encryptor itself.
/// </summary>
public static class SecureStringHelper
{
	/// <summary>
	/// Encrypts <paramref name="value"/> to a byte array.
	/// </summary>
	/// <param name="value">SecureString to encrypt; <see langword="null"/> returns <see langword="null"/>.</param>
	/// <returns>Encrypted bytes, or <see langword="null"/> when <paramref name="value"/> is <see langword="null"/>.</returns>
	public static byte[] Encrypt(SecureString value)
		=> SecureStringEncryptor.Instance.Encrypt(value);

	/// <summary>
	/// Decrypts <paramref name="cipher"/> previously produced by <see cref="Encrypt"/>.
	/// </summary>
	/// <param name="cipher">Encrypted bytes; <see langword="null"/> returns <see langword="null"/>.</param>
	/// <returns>Original <see cref="SecureString"/>, or <see langword="null"/> when <paramref name="cipher"/> is <see langword="null"/>.</returns>
	public static SecureString Decrypt(byte[] cipher)
		=> SecureStringEncryptor.Instance.Decrypt(cipher);
}
