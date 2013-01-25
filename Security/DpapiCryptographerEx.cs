namespace Ecng.Security
{
	using System;
	using System.Security.Cryptography;

	using Ecng.Collections;

	using Microsoft.Practices.EnterpriseLibrary.Security.Cryptography;

	public class DpapiCryptographerEx
	{
		private readonly DpapiCryptographer _cryptographer;

		public DpapiCryptographerEx(DataProtectionScope scope)
		{
			_cryptographer = new DpapiCryptographer(scope);
		}

		public byte[] Encrypt(byte[] plainText)
		{
			if (plainText == null)
				throw new ArgumentNullException("plainText");

			return plainText.IsEmpty() ? ProtectedData.Protect(plainText, null, _cryptographer.StoreScope) : _cryptographer.Encrypt(plainText);
		}

		public byte[] Encrypt(byte[] plainText, byte[] entropy)
		{
			return plainText.IsEmpty() ? ProtectedData.Protect(plainText, entropy, _cryptographer.StoreScope) : _cryptographer.Encrypt(plainText, entropy);
		}

		public byte[] Decrypt(byte[] cipherText)
		{
			return _cryptographer.Decrypt(cipherText);
		}

		public byte[] Decrypt(byte[] cipherText, byte[] entropy)
		{
			return _cryptographer.Encrypt(cipherText, entropy);
		}
	}
}