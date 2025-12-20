namespace Ecng.Security.Cryptographers;

using System;
using System.Security.Cryptography;

using Ecng.Common;

/// <summary>
/// Asymmetric cryptographer.
/// </summary>
public class AsymmetricCryptographer : Disposable
{
	private sealed class AsymmetricAlgorithmWrapper(AsymmetricAlgorithm value) : Wrapper<AsymmetricAlgorithm>(value)
	{
		public AsymmetricAlgorithmWrapper(AsymmetricAlgorithm algorithm, byte[] key)
			: this(CreateAlgo(algorithm, key))
		{
		}

		private static AsymmetricAlgorithm CreateAlgo(AsymmetricAlgorithm algorithm, byte[] key)
		{
			if (algorithm is RSA rsa)
				rsa.ImportParameters(key.ToRsa());

			return algorithm;
		}

		public byte[] Encrypt(byte[] plainText)
		{
			if (Value is RSA rsa)
				return rsa.Encrypt(plainText, RSAEncryptionPadding.Pkcs1);

			throw new NotSupportedException($"Encryption is not supported for algorithm type {Value.GetType().Name}");
		}

		public byte[] Decrypt(byte[] encryptedText)
		{
			if (Value is RSA rsa)
				return rsa.Decrypt(encryptedText, RSAEncryptionPadding.Pkcs1);

			throw new NotSupportedException($"Decryption is not supported for algorithm type {Value.GetType().Name}");
		}

		public byte[] CreateSignature(byte[] data, Func<HashAlgorithm> createHash)
		{
			if (createHash is null)
				throw new ArgumentNullException(nameof(createHash));

			using HashAlgorithm hash = createHash();
			var hashAlgoName = hash switch
			{
				SHA256 => HashAlgorithmName.SHA256,
				SHA384 => HashAlgorithmName.SHA384,
				SHA512 => HashAlgorithmName.SHA512,
				SHA1 => HashAlgorithmName.SHA1,
				MD5 => HashAlgorithmName.MD5,
				_ => throw new NotSupportedException($"Hash algorithm {hash.GetType().Name} is not supported")
			};

			if (Value is RSA rsa)
				return rsa.SignData(data, hashAlgoName, RSASignaturePadding.Pkcs1);
			else if (Value is DSA dsa)
			{
#if NET6_0_OR_GREATER
				return dsa.SignData(data, hashAlgoName);
#else
				var hashValue = hash.ComputeHash(data);
				return dsa.CreateSignature(hashValue);
#endif
			}
			else
				throw new NotSupportedException($"Signature creation is not supported for algorithm type {Value.GetType().Name}");
		}

		public bool VerifySignature(byte[] data, byte[] signature)
		{
			if (Value is RSA rsa)
			{
				// Try SHA256 first (most common)
				try
				{
					if (rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
						return true;
				}
				catch
				{
				}

				// Fallback to SHA1 for backward compatibility
				try
				{
					return rsa.VerifyData(data, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
				}
				catch
				{
					return false;
				}
			}
			else if (Value is DSA dsa)
			{
#if NET6_0_OR_GREATER
				// Try SHA256 first (most common)
				try
				{
					if (dsa.VerifyData(data, signature, HashAlgorithmName.SHA256))
						return true;
				}
				catch
				{
				}

				// Fallback to SHA1 for backward compatibility
				try
				{
					return dsa.VerifyData(data, signature, HashAlgorithmName.SHA1);
				}
				catch
				{
					return false;
				}
#else
				// .NET Standard 2.0: use SHA1 hash manually
				using var sha1 = SHA1.Create();
				var hashValue = sha1.ComputeHash(data);
				return dsa.VerifySignature(hashValue, signature);
#endif
			}
			else
				throw new NotSupportedException($"Signature verification is not supported for algorithm type {Value.GetType().Name}");
		}

		public override Wrapper<AsymmetricAlgorithm> Clone()
		{
			throw new NotSupportedException();
		}

		protected override void DisposeManaged()
		{
			Value.Clear();
			base.DisposeManaged();
		}
	}

	#region Private Fields

	private readonly AsymmetricAlgorithmWrapper _encryptor;
	private readonly AsymmetricAlgorithmWrapper _decryptor;

	#endregion

	#region AsymmetricCryptographer.ctor()

	/// <summary>
	/// <para>Initialize a new instance of the <see cref="AsymmetricCryptographer"/> class with an algorithm type and a key.</para>
	/// </summary>
	/// <param name="algorithm"><para>The <see cref="AsymmetricAlgorithm"/> to use.</para></param>
	/// <param name="publicKey"><para>The public key for the algorithm.</para></param>
	/// <param name="privateKey"><para>The private key for the algorithm.</para></param>
	public AsymmetricCryptographer(AsymmetricAlgorithm algorithm, byte[] publicKey, byte[] privateKey)
		: this(publicKey is null ? null : new AsymmetricAlgorithmWrapper(algorithm, publicKey), privateKey is null ? null : new AsymmetricAlgorithmWrapper(algorithm, privateKey))
	{
	}

	/// <summary>
	/// <para>Initialize a new instance of the <see cref="AsymmetricCryptographer"/> class with an algorithm type and a key.</para>
	/// </summary>
	/// <param name="algorithm"><para>The <see cref="AsymmetricAlgorithm"/> to use.</para></param>
	/// <param name="publicKey"><para>The public key for the algorithm.</para></param>
	public AsymmetricCryptographer(AsymmetricAlgorithm algorithm, byte[] publicKey)
		: this(publicKey is null ? null : new AsymmetricAlgorithmWrapper(algorithm, publicKey), null)
	{
	}

	/// <summary>
	/// <para>Initialize a new instance of the <see cref="AsymmetricCryptographer"/> class with an algorithm type and a key.</para>
	/// </summary>
	/// <param name="encryptor">The encryptor.</param>
	/// <param name="decryptor">The decryptor.</param>
	protected AsymmetricCryptographer(AsymmetricAlgorithm encryptor, AsymmetricAlgorithm decryptor)
		: this(new AsymmetricAlgorithmWrapper(encryptor), new AsymmetricAlgorithmWrapper(decryptor))
	{
	}

	private AsymmetricCryptographer(AsymmetricAlgorithmWrapper encryptor, AsymmetricAlgorithmWrapper decryptor)
	{
		if (encryptor is null && decryptor is null)
			throw new InvalidOperationException();

		_encryptor = encryptor;
		_decryptor = decryptor;
	}

	#endregion

	/// <summary>
	/// <para>Creates a new instance of the <see cref="AsymmetricCryptographer"/> class with a public key.</para>
	/// </summary>
	/// <param name="algorithm"><see cref="AsymmetricAlgorithm"/></param>
	/// <param name="publicKey">The public key for the algorithm.</param>
	/// <returns><see cref="AsymmetricCryptographer"/></returns>
	public static AsymmetricCryptographer CreateFromPublicKey(AsymmetricAlgorithm algorithm, byte[] publicKey)
	{
		return new AsymmetricCryptographer(new AsymmetricAlgorithmWrapper(algorithm, publicKey), null);
	}

	/// <summary>
	/// <para>Creates a new instance of the <see cref="AsymmetricCryptographer"/> class with a private key.</para>
	/// </summary>
	/// <param name="algorithm"><see cref="AsymmetricAlgorithm"/></param>
	/// <param name="privateKey">The private key for the algorithm.</param>
	/// <returns><see cref="AsymmetricCryptographer"/></returns>
	public static AsymmetricCryptographer CreateFromPrivateKey(AsymmetricAlgorithm algorithm, byte[] privateKey)
	{
		return new AsymmetricCryptographer(null, new AsymmetricAlgorithmWrapper(algorithm, privateKey));
	}

	#region Encrypt

	/// <summary>
	/// <para>Encrypts bytes with the initialized algorithm and key.</para>
	/// </summary>
	/// <param name="plainText"><para>The plaintext in which you wish to encrypt.</para></param>
	/// <returns><para>The resulting cipher text.</para></returns>
	public byte[] Encrypt(byte[] plainText)
	{
		if (_encryptor is null)
			throw new InvalidOperationException();

		return _encryptor.Encrypt(plainText);
	}

	#endregion

	#region Decrypt

	/// <summary>
	/// <para>Decrypts bytes with the initialized algorithm and key.</para>
	/// </summary>
	/// <param name="encryptedText"><para>The text which you wish to decrypt.</para></param>
	/// <returns><para>The resulting plaintext.</para></returns>
	public byte[] Decrypt(byte[] encryptedText)
	{
		if (_decryptor is null)
			throw new InvalidOperationException();

		return _decryptor.Decrypt(encryptedText);
	}

	#endregion

	#region Disposable Members

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_encryptor?.Dispose();
		_decryptor?.Dispose();

		base.DisposeManaged();
	}

	#endregion

	/// <summary>
	/// <para>Computes the hash value of the plaintext.</para>
	/// </summary>
	/// <param name="data">The plaintext in which you wish to hash.</param>
	/// <param name="createHash">A function that creates a <see cref="HashAlgorithm"/> instance to use for hashing.</param>
	/// <returns>The resulting hash.</returns>
	public byte[] CreateSignature(byte[] data, Func<HashAlgorithm> createHash)
	{
		if (_decryptor is null)
			throw new InvalidOperationException();

		return _decryptor.CreateSignature(data, createHash);
	}

	/// <summary>
	/// <para>Verifies the signature.</para>
	/// </summary>
	/// <param name="data">The data.</param>
	/// <param name="signature">The signature.</param>
	/// <returns><c>true</c> if the signature is valid; otherwise, <c>false</c>.</returns>
	public bool VerifySignature(byte[] data, byte[] signature)
	{
		if (_encryptor is null)
			throw new InvalidOperationException();

		return _encryptor.VerifySignature(data, signature);
	}
}