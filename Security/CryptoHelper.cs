namespace Ecng.Security
{
	using System;
	using System.Linq;
	using System.IO;
	using System.Security.Cryptography;
	using System.Security;

	using Ecng.Common;

	using Microsoft.Practices.EnterpriseLibrary.Security.Cryptography;

	public static class CryptoHelper
	{
		public static byte[] ToBytes(this ProtectedKey key)
		{
			if (key is null)
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

		public static RSAParameters ToRsa(this byte[] key)
		{
			if (key is null)
				throw new ArgumentNullException(nameof(key));

			var stream = key.To<Stream>();

			return new()
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

		public static RSAParameters ToRsa(this ProtectedKey key)
		{
			if (key is null)
				throw new ArgumentNullException(nameof(key));

			return key.DecryptedKey.ToRsa();
		}

		#region WriteByteArray

		private static void WriteByteArray(Stream stream, byte[] array)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			stream.WriteEx(array is null);

			if (array is null)
				return;

			stream.WriteEx(array);
		}

		#endregion

		#region ReadByteArray

		private static byte[] ReadByteArray(Stream stream)
		{
			if (stream is null)
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
			using var provider = new RSACryptoServiceProvider();
			return provider.ExportParameters(true);
		}

		#endregion

		public static RSAParameters PublicPart(this RSAParameters param)
		{
			return new()
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

		private static RijndaelManaged CreateRijndaelManaged() => new()
		{
			BlockSize = 128,
			Mode = CipherMode.CBC,
			Padding = PaddingMode.PKCS7,
		};

		public static byte[] Encrypt(this byte[] plain, string passPhrase, byte[] salt, byte[] iv)
		{
			if (plain is null)
				throw new ArgumentNullException(nameof(plain));

			if (passPhrase.IsEmpty())
				throw new ArgumentNullException(nameof(passPhrase));

			if (iv?.Length > 16)
				iv = iv.Take(16).ToArray();

			using var password = new Rfc2898DeriveBytes(passPhrase, salt, _derivationIterations);

			var keyBytes = password.GetBytes(_keySize / 8);

			using var symmetricKey = CreateRijndaelManaged();

			using var encryptor = symmetricKey.CreateEncryptor(keyBytes, iv);
			using var memoryStream = new MemoryStream();
			using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);

			cryptoStream.Write(plain, 0, plain.Length);
			cryptoStream.FlushFinalBlock();
			// Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.

			return memoryStream.ToArray();
		}

		private static byte[] ReadStream(this CryptoStream stream, int maxLen)
		{
			var buffer = new byte[maxLen];
			var offset = 0;

			while (offset < maxLen)
			{
				var numRead = stream.Read(buffer, offset, maxLen - offset);
				if (numRead == 0)
					break;

				offset += numRead;
			}

			Array.Resize(ref buffer, offset);

			return buffer;
		}

		public static byte[] Decrypt(this byte[] cipherText, string passPhrase, byte[] salt, byte[] iv)
		{
			if (cipherText is null)
				throw new ArgumentNullException(nameof(cipherText));

			if (passPhrase.IsEmpty())
				throw new ArgumentNullException(nameof(passPhrase));

			if (iv?.Length > 16)
				iv = iv.Take(16).ToArray();

			using var password = new Rfc2898DeriveBytes(passPhrase, salt, _derivationIterations);

			var keyBytes = password.GetBytes(_keySize / 8);

			using var symmetricKey = CreateRijndaelManaged();

			using var decryptor = symmetricKey.CreateDecryptor(keyBytes, iv);
			using var memoryStream = new MemoryStream(cipherText);
			using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);

			return cryptoStream.ReadStream(cipherText.Length);
		}

		private static byte[] TransformAes(bool isEncrypt, byte[] inputBytes, string passPhrase, byte[] salt, byte[] iv)
		{
			if (inputBytes is null)
				throw new ArgumentNullException(nameof(inputBytes));

			if (passPhrase.IsEmpty())
				throw new ArgumentNullException(nameof(passPhrase));

			using var password = new Rfc2898DeriveBytes(passPhrase, salt, _derivationIterations);

			var keyBytes = password.GetBytes(_keySize / 8);

			using var aes = new AesManaged { Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 };

			if (isEncrypt)
			{
				using var encryptor = aes.CreateEncryptor(keyBytes, iv);
				using var memoryStream = new MemoryStream();
				using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);

				cryptoStream.Write(inputBytes, 0, inputBytes.Length);
				cryptoStream.FlushFinalBlock();
				return memoryStream.ToArray();
			}
			else
			{
				using var decryptor = aes.CreateDecryptor(keyBytes, iv);
				using var memoryStream = new MemoryStream(inputBytes);
				using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);

				return cryptoStream.ReadStream(inputBytes.Length);
			}
		}

		public static byte[] EncryptAes(this byte[] plain, string passPhrase, byte[] salt, byte[] iv) => TransformAes(true, plain, passPhrase, salt, iv);

		public static byte[] DecryptAes(this byte[] cipherText, string passPhrase, byte[] salt, byte[] iv) => TransformAes(false, cipherText, passPhrase, salt, iv);

		private static string Hash(this byte[] value, HashAlgorithm algo)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			if (value.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(value));

			if (algo is null)
				throw new ArgumentNullException(nameof(algo));

			using (algo)
				return algo.ComputeHash(value).Digest();
		}

		public static string Md5(this byte[] value)
		{
			return value.Hash(MD5.Create());
		}

		public static string Sha256(this byte[] value)
		{
			return value.Hash(SHA256.Create());
		}

		public static string Sha512(this byte[] value)
		{
			return value.Hash(SHA512.Create());
		}

		public static bool IsValid(this Secret secret, SecureString password)
			=> secret.IsValid(password.UnSecure());

		public static bool IsValid(this Secret secret, string password)
			=> secret.Equals(password.CreateSecret(secret));

		public static Secret CreateSecret(this SecureString plainText)
			=> plainText.UnSecure().CreateSecret();

		public static Secret CreateSecret(this string plainText)
			=> plainText.CreateSecret(TypeHelper.GenerateSalt(Secret.DefaultSaltSize));

		public static Secret CreateSecret(this string plainText, Secret secret)
			=> plainText.CreateSecret(secret.CheckOnNull(nameof(secret)).Salt, secret.Algo);

		public static Secret CreateSecret(this string plainText, byte[] salt, CryptoAlgorithm algo = null)
		{
			if (plainText.IsEmpty())
				throw new ArgumentNullException(nameof(plainText));

			if (salt is null)
				throw new ArgumentNullException(nameof(salt));

			var unencodedBytes = plainText.Unicode();
			var buffer = new byte[unencodedBytes.Length + salt.Length];

			Buffer.BlockCopy(unencodedBytes, 0, buffer, 0, unencodedBytes.Length);
			Buffer.BlockCopy(salt, 0, buffer, unencodedBytes.Length - 1, salt.Length);

			return new Secret(buffer, salt, algo);
		}
	}
}