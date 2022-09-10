namespace Ecng.Serialization
{
	using System;
	using System.Security;
	using System.Security.Cryptography;

	using Ecng.Common;
	using Ecng.Security;

	class SecureStringEncryptor
	{
		private static readonly Lazy<SecureStringEncryptor> _instance = new(() => new());
		public static SecureStringEncryptor Instance => _instance.Value;

		private readonly SecureString _key = "RClVEDn0O3EUsKqym1qd".Secure();
		private readonly byte[] _salt = "3hj67-!3".To<byte[]>();

		public SecureString Key { get; set; }
		public byte[] Entropy { get; set; }

		public SecureString Decrypt(byte[] source)
		{
			if (source is null)
				return null;

			try
			{
				if (Scope<ContinueOnExceptionContext>.Current?.Value.DoNotEncrypt != true)
					source = source.DecryptAes(_key.UnSecure(), _salt, _salt);

				return source.To<string>().Secure();
			}
			catch (CryptographicException ex)
			{
				if (ContinueOnExceptionContext.TryProcess(ex))
					return null;

				throw;
			}
		}

		public byte[] Encrypt(SecureString instance)
		{
			if (instance is null)
				return null;

			var plainText = instance.UnSecure().To<byte[]>();

			if (Scope<ContinueOnExceptionContext>.Current?.Value.DoNotEncrypt == true)
				return plainText;

			return plainText.EncryptAes(_key.UnSecure(), _salt, _salt);
		}
	}
}