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

		public static byte[] GenerateSalt(int saltSize)
		{
			using (var saltGen = new RNGCryptoServiceProvider())
			{
				var salt = new byte[saltSize];
				saltGen.GetBytes(salt);
				return salt;	
			}
		}

		public static byte[] Protect(this byte[] plainText, byte[] entropy = null, DataProtectionScope scope = CryptoAlgorithm.DefaultScope)
		{
			return ProtectedData.Protect(plainText, entropy, scope);
		}

		public static byte[] Unprotect(this byte[] cipherText, byte[] entropy = null, DataProtectionScope scope = CryptoAlgorithm.DefaultScope)
		{
			return ProtectedData.Unprotect(cipherText, entropy, scope);
		}
	}
}