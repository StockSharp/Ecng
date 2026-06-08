namespace Ecng.Serialization;

/// <summary>
/// Public facade over the active <see cref="ISecureStringEncryptor"/>: encrypts and decrypts
/// <see cref="SecureString"/> payloads using the same encryptor the JSON /
/// <see cref="SettingsStorage"/> primitive path applies. Assign <see cref="Encryptor"/> to
/// replace the secret or algorithm process-wide.
/// </summary>
public static class SecureStringHelper
{
	// Built-in default secret, used until a custom encryptor is assigned.
	private const string _defaultKey = "RClVEDn0O3EUsKqym1qd";
	private const string _defaultEntropy = "3hj67-!3";

	private static ISecureStringEncryptor _encryptor
		= new SecureStringEncryptor(_defaultKey.Secure(), _defaultEntropy.To<byte[]>());

	/// <summary>
	/// The encryptor used by <see cref="Encrypt"/> / <see cref="Decrypt"/> and by the JSON /
	/// <see cref="SettingsStorage"/> primitive path. Defaults to the built-in AES
	/// <see cref="SecureStringEncryptor"/>; assign your own implementation to override the
	/// secret or algorithm for the whole process. Cannot be set to <see langword="null"/>.
	/// </summary>
	public static ISecureStringEncryptor Encryptor
	{
		get => _encryptor;
		set => _encryptor = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Encrypts <paramref name="value"/> to a byte array.
	/// </summary>
	/// <param name="value">SecureString to encrypt; <see langword="null"/> returns <see langword="null"/>.</param>
	/// <returns>Encrypted bytes, or <see langword="null"/> when <paramref name="value"/> is <see langword="null"/>.</returns>
	public static byte[] Encrypt(SecureString value)
		=> Encryptor.Encrypt(value);

	/// <summary>
	/// Decrypts <paramref name="cipher"/> previously produced by <see cref="Encrypt"/>.
	/// </summary>
	/// <param name="cipher">Encrypted bytes; <see langword="null"/> returns <see langword="null"/>.</param>
	/// <returns>Original <see cref="SecureString"/>, or <see langword="null"/> when <paramref name="cipher"/> is <see langword="null"/>.</returns>
	public static SecureString Decrypt(byte[] cipher)
		=> Encryptor.Decrypt(cipher);
}
