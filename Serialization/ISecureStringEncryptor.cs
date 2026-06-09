namespace Ecng.Serialization;

/// <summary>
/// Encrypts and decrypts <see cref="SecureString"/> payloads. The instance used across the
/// package - by <see cref="SecureStringHelper"/> and the JSON / <see cref="SettingsStorage"/>
/// primitive path - can be replaced process-wide via <see cref="SecureStringHelper.Encryptor"/>
/// or locally via <see cref="Scope{T}"/>, letting an application plug in its own secret or
/// algorithm instead of the built-in default.
/// </summary>
public interface ISecureStringEncryptor
{
	/// <summary>
	/// Encrypts <paramref name="value"/> to a byte array.
	/// </summary>
	/// <param name="value">SecureString to encrypt; <see langword="null"/> returns <see langword="null"/>.</param>
	/// <returns>Encrypted bytes, or <see langword="null"/> when <paramref name="value"/> is <see langword="null"/>.</returns>
	byte[] Encrypt(SecureString value);

	/// <summary>
	/// Decrypts <paramref name="cipher"/> previously produced by <see cref="Encrypt"/>.
	/// </summary>
	/// <param name="cipher">Encrypted bytes; <see langword="null"/> returns <see langword="null"/>.</param>
	/// <returns>Original <see cref="SecureString"/>, or <see langword="null"/> when <paramref name="cipher"/> is <see langword="null"/>.</returns>
	SecureString Decrypt(byte[] cipher);
}
