namespace Ecng.Security.Cryptographers
{
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
                if (algorithm is RSACryptoServiceProvider rsa)
                    rsa.ImportParameters(key.ToRsa());

                return algorithm;
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
                    using var hash = SHA1.Create();
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
                    using var hash = SHA1.Create();
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
		/// <param name="algorithm"><para>The qualified assembly name of a <see cref="SymmetricAlgorithm"/>.</para></param>
		/// <param name="publicKey"><para>The public key for the algorithm.</para></param>
		/// <param name="privateKey"><para>The private key for the algorithm.</para></param>
		public AsymmetricCryptographer(AsymmetricAlgorithm algorithm, byte[] publicKey, byte[] privateKey)
            : this(publicKey is null ? null : new AsymmetricAlgorithmWrapper(algorithm, publicKey), privateKey is null ? null : new AsymmetricAlgorithmWrapper(algorithm, privateKey))
        {
        }

		/// <summary>
		/// <para>Initialize a new instance of the <see cref="AsymmetricCryptographer"/> class with an algorithm type and a key.</para>
		/// </summary>
		/// <param name="algorithm"><para>The qualified assembly name of a <see cref="SymmetricAlgorithm"/>.</para></param>
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
		/// <returns>The resulting hash.</returns>
		public byte[] CreateSignature(byte[] data)
        {
            if (_decryptor is null)
                throw new InvalidOperationException();

            return _decryptor.CreateSignature(data);
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
}