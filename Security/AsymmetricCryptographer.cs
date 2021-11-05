namespace Ecng.Security
{
	using System;
	using System.Security.Cryptography;

	using Ecng.Common;

	using Microsoft.Practices.EnterpriseLibrary.Security.Cryptography;

	public class AsymmetricCryptographer : Disposable
	{
		private sealed class AsymmetricAlgorithmWrapper : Wrapper<AsymmetricAlgorithm>
		{
			public AsymmetricAlgorithmWrapper(Type algorithmType, ProtectedKey key)
				: this(CreateAlgo(algorithmType, key))
			{
			}

			public AsymmetricAlgorithmWrapper(Type algorithmType, byte[] key)
				: this(CreateAlgo(algorithmType, key))
			{
			}

			public AsymmetricAlgorithmWrapper(AsymmetricAlgorithm value)
				: base(value)
			{
			}

			private static AsymmetricAlgorithm CreateAlgo(Type algorithmType, byte[] key)
			{
				if (algorithmType is null)
					throw new ArgumentNullException(nameof(algorithmType));

				if (!typeof(AsymmetricAlgorithm).IsAssignableFrom(algorithmType))
					throw new ArgumentException("algorithmType");

				if (key is null)
					throw new ArgumentNullException(nameof(key));

				var retVal = algorithmType.CreateInstance<AsymmetricAlgorithm>();

				if (typeof(RSACryptoServiceProvider).IsAssignableFrom(algorithmType))
					((RSACryptoServiceProvider)retVal).ImportParameters(key.ToRsa());

				return retVal;
			}

			private static AsymmetricAlgorithm CreateAlgo(Type algorithmType, ProtectedKey key)
			{
				if (key is null)
					throw new ArgumentNullException(nameof(key));

				return CreateAlgo(algorithmType, key.DecryptedKey);
			}

			public byte[] Encrypt(byte[] plainText)
			{
				if (Value is RSACryptoServiceProvider rsa)
					return rsa.Encrypt(plainText, false);

				throw new NotImplementedException();
			}

			public byte[] Decrypt(byte[] encryptedText)
			{
				if (Value is RSACryptoServiceProvider rsa)
					return rsa.Decrypt(encryptedText, false);

				throw new NotImplementedException();
			}

			public byte[] CreateSignature(byte[] data)
			{
				if (Value is RSACryptoServiceProvider rsa)
				{
					using var hash = new SHA1CryptoServiceProvider();
					return rsa.SignData(data, hash);
				}
				else if (Value is DSACryptoServiceProvider dsa)
					return dsa.SignData(data);
				else
					throw new NotSupportedException();
			}

			public bool VerifySignature(byte[] data, byte[] signature)
			{
				if (Value is RSACryptoServiceProvider rsa)
				{
					using var hash = new SHA1CryptoServiceProvider();
					return rsa.VerifyData(data, hash, signature);
				}
				else if (Value is DSACryptoServiceProvider dsa)
					return dsa.VerifySignature(data, signature);
				else
					throw new NotSupportedException();
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
		/// <param name="algorithmType"><para>The qualified assembly name of a <see cref="SymmetricAlgorithm"/>.</para></param>
		/// <param name="publicKey"><para>The public key for the algorithm.</para></param>
		/// <param name="privateKey"><para>The private key for the algorithm.</para></param>
		public AsymmetricCryptographer(Type algorithmType, ProtectedKey publicKey, ProtectedKey privateKey)
			: this(publicKey is null ? null : new AsymmetricAlgorithmWrapper(algorithmType, publicKey), privateKey is null ? null : new AsymmetricAlgorithmWrapper(algorithmType, privateKey))
		{
		}

		/// <summary>
		/// <para>Initialize a new instance of the <see cref="AsymmetricCryptographer"/> class with an algorithm type and a key.</para>
		/// </summary>
		/// <param name="algorithmType"><para>The qualified assembly name of a <see cref="SymmetricAlgorithm"/>.</para></param>
		/// <param name="publicKey"><para>The public key for the algorithm.</para></param>
		public AsymmetricCryptographer(Type algorithmType, byte[] publicKey)
			: this(publicKey is null ? null : new AsymmetricAlgorithmWrapper(algorithmType, publicKey), null)
		{
		}

		protected AsymmetricCryptographer(AsymmetricAlgorithm encryptor, AsymmetricAlgorithm decryptor)
			: this(new AsymmetricAlgorithmWrapper(encryptor), new AsymmetricAlgorithmWrapper(decryptor))
		{
		}

		private AsymmetricCryptographer(AsymmetricAlgorithmWrapper encryptor, AsymmetricAlgorithmWrapper decryptor)
		{
			if (encryptor is null && decryptor is null)
				throw new ArgumentException();

			_encryptor = encryptor;
			_decryptor = decryptor;
		}

		#endregion

		public static AsymmetricCryptographer CreateFromPublicKey(Type algorithmType, ProtectedKey publicKey)
		{
			return new AsymmetricCryptographer(new AsymmetricAlgorithmWrapper(algorithmType, publicKey), null);
		}

		public static AsymmetricCryptographer CreateFromPrivateKey(Type algorithmType, ProtectedKey privateKey)
		{
			return new AsymmetricCryptographer(null, new AsymmetricAlgorithmWrapper(algorithmType, privateKey));
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

		protected override void DisposeManaged()
		{
			if (_encryptor != null)
				_encryptor.Dispose();

			if (_decryptor != null)
				_decryptor.Dispose();

			base.DisposeManaged();
		}

		#endregion

		public byte[] CreateSignature(byte[] data)
		{
			if (_decryptor is null)
				throw new InvalidOperationException();

			return _decryptor.CreateSignature(data);
		}

		public bool VerifySignature(byte[] data, byte[] signature)
		{
			if (_encryptor is null)
				throw new InvalidOperationException();

			return _encryptor.VerifySignature(data, signature);
		}
	}
}