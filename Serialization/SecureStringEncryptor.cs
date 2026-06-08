namespace Ecng.Serialization;

using System.Security.Cryptography;

using Ecng.Security;

/// <summary>
/// Default AES-based <see cref="ISecureStringEncryptor"/>. The key and entropy are always
/// supplied explicitly via the constructor — the built-in default secret lives in
/// <see cref="SecureStringHelper"/>. Implement <see cref="ISecureStringEncryptor"/> directly
/// for a completely custom scheme and assign it to <see cref="SecureStringHelper.Encryptor"/>.
/// </summary>
public class SecureStringEncryptor : ISecureStringEncryptor
{
	/// <summary>
	/// Initializes a new instance with the given key and entropy.
	/// </summary>
	/// <param name="key">Encryption key.</param>
	/// <param name="entropy">Encryption entropy (also used as the IV salt).</param>
	public SecureStringEncryptor(SecureString key, byte[] entropy)
	{
		Key = key ?? throw new ArgumentNullException(nameof(key));
		Entropy = entropy ?? throw new ArgumentNullException(nameof(entropy));
	}

	/// <summary>Encryption key.</summary>
	public SecureString Key { get; }

	/// <summary>Encryption entropy (also used as the IV salt).</summary>
	public byte[] Entropy { get; }

	/// <inheritdoc />
	public SecureString Decrypt(byte[] source)
	{
		if (source is null)
			return null;

		try
		{
			if (Scope<ContinueOnExceptionContext>.Current?.Value.DoNotEncrypt != true)
				source = source.DecryptAes(Key.UnSecure(), Entropy, Entropy);

			return source.To<string>().Secure();
		}
		catch (CryptographicException ex)
		{
			if (ContinueOnExceptionContext.TryProcess(ex))
				return null;

			throw;
		}
	}

	/// <inheritdoc />
	public byte[] Encrypt(SecureString instance)
	{
		if (instance is null)
			return null;

		var plainText = instance.UnSecure().To<byte[]>();

		if (Scope<ContinueOnExceptionContext>.Current?.Value.DoNotEncrypt == true)
			return plainText;

		return plainText.EncryptAes(Key.UnSecure(), Entropy, Entropy);
	}
}
