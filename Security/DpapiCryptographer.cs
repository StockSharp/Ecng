namespace Ecng.Security
{
	using System.Security.Cryptography;

	public class DpapiCryptographer
	{
		public DpapiCryptographer(DataProtectionScope scope)
		{
			Scope = scope;
		}

		public DataProtectionScope Scope { get; private set; }

		public byte[] Encrypt(byte[] plainText)
		{
			return Encrypt(plainText, null);
		}

		public byte[] Encrypt(byte[] plainText, byte[] entropy)
		{
			return plainText.Protect(entropy, Scope);
		}

		public byte[] Decrypt(byte[] cipherText)
		{
			return Decrypt(cipherText, null);
		}

		public byte[] Decrypt(byte[] cipherText, byte[] entropy)
		{
			return cipherText.Unprotect(entropy, Scope);
		}
	}
}