namespace Ecng.Security;

using System;
using System.Security.Cryptography;

using Ecng.Common;
using Ecng.Security.Cryptographers;

/// <summary>
/// Algorithm types.
/// </summary>
public enum AlgorithmTypes
{
	/// <summary>
	/// Symmetric.
	/// </summary>
	Symmetric,

	/// <summary>
	/// Asymmetric.
	/// </summary>
	Asymmetric,

	/// <summary>
	/// Hash.
	/// </summary>
	Hash,
}

/// <summary>
/// Crypto algorithm.
/// </summary>
[Serializable]
public class CryptoAlgorithm : Disposable
{
	#region Private Fields

	private readonly SymmetricCryptographer _symmetric;
	private readonly AsymmetricCryptographer _asymmetric;
	private readonly HashCryptographer _hash;

	#endregion

	#region CryptoAlgorithm.ctor()

	/// <summary>
	/// Initializes a new instance of the <see cref="CryptoAlgorithm"/> class.
	/// </summary>
	/// <param name="symmetric"><see cref="SymmetricCryptographer"/></param>
	public CryptoAlgorithm(SymmetricCryptographer symmetric)
	{
		_symmetric = symmetric ?? throw new ArgumentNullException(nameof(symmetric));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CryptoAlgorithm"/> class.
	/// </summary>
	/// <param name="asymmetric"><see cref="AsymmetricCryptographer"/></param>
	public CryptoAlgorithm(AsymmetricCryptographer asymmetric)
	{
		_asymmetric = asymmetric ?? throw new ArgumentNullException(nameof(asymmetric));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CryptoAlgorithm"/> class.
	/// </summary>
	/// <param name="hash"><see cref="HashCryptographer"/></param>
	public CryptoAlgorithm(HashCryptographer hash)
	{
		_hash = hash ?? throw new ArgumentNullException(nameof(hash));
	}

	#endregion

	#region Public Constants

	/// <summary>
	/// The default symmetric algorithm name.
	/// </summary>
	public const string DefaultSymmetricAlgoName = "AES";

	/// <summary>
	/// The default asymmetric algorithm name.
	/// </summary>
	public const string DefaultAsymmetricAlgoName = "RSA";

	/// <summary>
	/// The default hash algorithm name.
	/// </summary>
	public const string DefaultHashAlgoName = "SHA256";

	#endregion

	#region Create

	/// <summary>
	/// Creates an asymmetric cryptographer for signature verification.
	/// </summary>
	/// <param name="publicKey">The public key.</param>
	/// <returns>The asymmetric cryptographer.</returns>
	public static CryptoAlgorithm CreateAsymmetricVerifier(byte[] publicKey)
		=> new(new AsymmetricCryptographer(AsymmetricAlgorithm.Create(DefaultAsymmetricAlgoName), publicKey));

	/// <summary>
	/// Creates a symmetric cryptographer.
	/// </summary>
	/// <param name="type"><see cref="AlgorithmTypes"/></param>
	/// <param name="keys">The keys.</param>
	/// <returns><see cref="CryptoAlgorithm"/></returns>
	public static CryptoAlgorithm Create(AlgorithmTypes type, params byte[][] keys)
	{
		return type switch
		{
			AlgorithmTypes.Symmetric => new(new SymmetricCryptographer(SymmetricAlgorithm.Create(DefaultSymmetricAlgoName), keys[0])),
			AlgorithmTypes.Asymmetric => new(new AsymmetricCryptographer(AsymmetricAlgorithm.Create(DefaultAsymmetricAlgoName), keys[0], keys[1])),
			AlgorithmTypes.Hash => new(keys.Length == 0 ? new HashCryptographer(HashAlgorithm.Create(DefaultHashAlgoName)) : new HashCryptographer(HashAlgorithm.Create(DefaultHashAlgoName), keys[0])),
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown type."),
		};
	}

	#endregion

	#region Encrypt

	/// <summary>
	/// Encrypts the specified data.
	/// </summary>
	/// <param name="data">The decrypted data.</param>
	/// <returns>The encrypted data.</returns>
	public byte[] Encrypt(byte[] data)
	{
		if (_symmetric is not null)
			return _symmetric.Encrypt(data);
		else if (_asymmetric is not null)
			return _asymmetric.Encrypt(data);
		else if (_hash is not null)
			return _hash.ComputeHash(data);
		else
			throw new InvalidOperationException();
	}

	#endregion

	#region Decrypt

	/// <summary>
	/// Decrypts the specified data.
	/// </summary>
	/// <param name="data">The encrypted data.</param>
	/// <returns>The decrypted data.</returns>
	public byte[] Decrypt(byte[] data)
	{
		if (_symmetric is not null)
			return _symmetric.Decrypt(data);
		else if (_asymmetric is not null)
			return _asymmetric.Decrypt(data);
		else if (_hash is not null)
			throw new NotSupportedException();
		else
			throw new InvalidOperationException();
	}

	#endregion

	/// <summary>
	/// Computes the hash value of the plaintext.
	/// </summary>
	/// <param name="data">The plaintext in which you wish to hash.</param>
	/// <param name="createHash">A function that creates a <see cref="HashAlgorithm"/> instance to use for hashing.</param>
	/// <returns>The resulting hash.</returns>
	public byte[] CreateSignature(byte[] data, Func<HashAlgorithm> createHash)
	{
		if (_asymmetric is null)
			throw new NotSupportedException();

		return _asymmetric.CreateSignature(data, createHash);
	}

	/// <summary>
	/// Verifies the signature.
	/// </summary>
	/// <param name="data">The data.</param>
	/// <param name="signature">The signature.</param>
	/// <returns>
	/// <c>true</c> if the signature is valid; otherwise, <c>false</c>.
	/// </returns>
	public bool VerifySignature(byte[] data, byte[] signature)
	{
		if (_asymmetric is null)
			throw new NotSupportedException();

		return _asymmetric.VerifySignature(data, signature);
	}

	#region Disposable Members

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_symmetric?.Dispose();
		_asymmetric?.Dispose();
		_hash?.Dispose();
	}

	#endregion
}
