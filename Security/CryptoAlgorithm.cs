namespace Ecng.Security
{
	using System;
	using System.Security.Cryptography;

	using Ecng.Security.Cryptographers;

	public enum AlgorithmTypes
	{
		Symmetric,
		Asymmetric,
		Hash,
	}

	[Serializable]
	public class CryptoAlgorithm : IDisposable
	{
		#region Private Fields

		private readonly SymmetricCryptographer _symmetric;
		private readonly AsymmetricCryptographer _asymmetric;
		private readonly HashCryptographer _hash;

		#endregion

		#region CryptoAlgorithm.ctor()

		public CryptoAlgorithm(SymmetricCryptographer symmetric)
		{
			_symmetric = symmetric ?? throw new ArgumentNullException(nameof(symmetric));
		}

		public CryptoAlgorithm(AsymmetricCryptographer asymmetric)
		{
			_asymmetric = asymmetric ?? throw new ArgumentNullException(nameof(asymmetric));
		}

		public CryptoAlgorithm(HashCryptographer hash)
		{
			_hash = hash ?? throw new ArgumentNullException(nameof(hash));
		}

		#endregion

		#region Public Constants

		public const string DefaultSymmetricAlgoName = "AES";
		public const string DefaultAsymmetricAlgoName = "RSA";
		public const string DefaultHashAlgoName = "SHA";

		#endregion

		#region Create

		public static CryptoAlgorithm CreateAssymetricVerifier(byte[] publicKey)
			=> new(new AsymmetricCryptographer(AsymmetricAlgorithm.Create(DefaultAsymmetricAlgoName), publicKey));

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

		public byte[] Encrypt(byte[] data)
		{
			if (_symmetric is not null)
				return _symmetric.Encrypt(data);
			else if (_asymmetric is not null)
				return _asymmetric.Encrypt(data);
			else if (_hash is not null)
				return _hash.ComputeHash(data);
			else
				throw new ArgumentOutOfRangeException();
		}

		#endregion

		#region Decrypt

		public byte[] Decrypt(byte[] data)
		{
			if (_symmetric is not null)
				return _symmetric.Decrypt(data);
			else if (_asymmetric is not null)
				return _asymmetric.Decrypt(data);
			else if (_hash is not null)
				throw new NotSupportedException();
			else
				throw new ArgumentOutOfRangeException();
		}

		#endregion

		public byte[] CreateSignature(byte[] data)
		{
			if (_asymmetric is null)
				throw new NotSupportedException();

			return _asymmetric.CreateSignature(data);
		}

		public bool VerifySignature(byte[] data, byte[] signature)
		{
			if (_asymmetric is null)
				throw new NotSupportedException();

			return _asymmetric.VerifySignature(data, signature);
		}

		#region Disposable Members

		public void Dispose()
		{
			if (_symmetric != null)
				_symmetric.Dispose();

			if (_asymmetric != null)
				_asymmetric.Dispose();
		}

		#endregion
	}
}