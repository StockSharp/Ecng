namespace Ecng.Security
{
	using System;
	using System.IO;
	using System.Security.Cryptography;

	using Ecng.Common;
	using Ecng.Serialization;

	using Microsoft.Practices.EnterpriseLibrary.Security.Cryptography;

	public static class CryptoHelper
	{
		public static byte[] ToBytes(this ProtectedKey key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			return key.DecryptedKey;
		}

		public static ProtectedKey FromBytes(this byte[] data)
		{
			return ProtectedKey.CreateFromPlaintextKey(data, CryptoAlgorithm.DefaultScope);
		}

		public static ProtectedKey FromRsa(this RSAParameters param)
		{
			var stream = new MemoryStream();
			WriteByteArray(stream, param.P);
			WriteByteArray(stream, param.Q);
			WriteByteArray(stream, param.D);
			WriteByteArray(stream, param.DP);
			WriteByteArray(stream, param.DQ);
			WriteByteArray(stream, param.InverseQ);
			WriteByteArray(stream, param.Exponent);
			WriteByteArray(stream, param.Modulus);
			return stream.To<byte[]>().FromBytes();
		}

		public static RSAParameters ToRsa(this ProtectedKey key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			var stream = key.DecryptedKey.To<Stream>();

			return new RSAParameters
			{
				P = ReadByteArray(stream),
				Q = ReadByteArray(stream),
				D = ReadByteArray(stream),
				DP = ReadByteArray(stream),
				DQ = ReadByteArray(stream),
				InverseQ = ReadByteArray(stream),
				Exponent = ReadByteArray(stream),
				Modulus = ReadByteArray(stream)
			};
		}

		#region WriteByteArray

		private static void WriteByteArray(Stream stream, byte[] array)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			stream.Write(array == null);

			if (array == null)
				return;

			stream.Write(array);
		}

		#endregion

		#region ReadByteArray

		private static byte[] ReadByteArray(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			var isNull = stream.Read<bool>();

			if (isNull)
				return null;

			return stream.Read<byte[]>();
		}

		#endregion

		#region Generate

		/// <summary>
		/// Returns a new generated <code>RSAParameters</code> class which
		/// will be used as a key for the signature.
		/// <remarks>
		/// It will generate a PRIVATE key (which includes the PUBLIC key).
		/// </remarks>
		/// </summary>
		public static RSAParameters GenerateRsa()
		{
			using (var provider = new RSACryptoServiceProvider())
				return provider.ExportParameters(true);
		}

		#endregion

		public static RSAParameters PublicPart(this RSAParameters param)
		{
			return new RSAParameters
			{
				Exponent = param.Exponent,
				Modulus = param.Modulus,
			};
		}

		public static byte[] Protect(this byte[] plainText, byte[] entropy = null, DataProtectionScope scope = CryptoAlgorithm.DefaultScope)
		{
			return ProtectedData.Protect(plainText, entropy, scope);
		}

		public static byte[] Unprotect(this byte[] cipherText, byte[] entropy = null, DataProtectionScope scope = CryptoAlgorithm.DefaultScope)
		{
			return ProtectedData.Unprotect(cipherText, entropy, scope);
		}

		// https://stackoverflow.com/a/10177020/8029915

		// This constant is used to determine the keysize of the encryption algorithm in bits.
		// We divide this by 8 within the code below to get the equivalent number of bytes.
		private const int _keySize = 256;

		// This constant determines the number of iterations for the password bytes generation function.
		private const int _derivationIterations = 1000;

		public static byte[] Encrypt(this byte[] plain, string passPhrase, byte[] salt, byte[] iv)
		{
			if (plain == null)
				throw new ArgumentNullException(nameof(plain));

			if (passPhrase.IsEmpty())
				throw new ArgumentNullException(nameof(passPhrase));

			using (var password = new Rfc2898DeriveBytes(passPhrase, salt, _derivationIterations))
			{
				var keyBytes = password.GetBytes(_keySize / 8);

				using (var symmetricKey = new RijndaelManaged())
				{
					symmetricKey.BlockSize = 256;
					symmetricKey.Mode = CipherMode.CBC;
					symmetricKey.Padding = PaddingMode.PKCS7;

					using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, iv))
					{
						using (var memoryStream = new MemoryStream())
						{
							using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
							{
								cryptoStream.Write(plain, 0, plain.Length);
								cryptoStream.FlushFinalBlock();
								// Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.

								return memoryStream.ToArray();
							}
						}
					}
				}
			}
		}

		public static byte[] Decrypt(this byte[] cipherText, string passPhrase, byte[] salt, byte[] iv)
		{
			if (cipherText == null)
				throw new ArgumentNullException(nameof(cipherText));

			if (passPhrase.IsEmpty())
				throw new ArgumentNullException(nameof(passPhrase));

			using (var password = new Rfc2898DeriveBytes(passPhrase, salt, _derivationIterations))
			{
				var keyBytes = password.GetBytes(_keySize / 8);

				using (var symmetricKey = new RijndaelManaged())
				{
					symmetricKey.BlockSize = 256;
					symmetricKey.Mode = CipherMode.CBC;
					symmetricKey.Padding = PaddingMode.PKCS7;

					using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, iv))
					{
						using (var memoryStream = new MemoryStream(cipherText))
						{
							using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
							{
								var plainTextBytes = new byte[cipherText.Length];
								var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);

								if (plainTextBytes.Length > decryptedByteCount)
									Array.Resize(ref plainTextBytes, decryptedByteCount);
								
								return plainTextBytes;
							}
						}
					}
				}
			}
		}
	}
}