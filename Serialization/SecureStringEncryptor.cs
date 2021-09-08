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

		private readonly DpapiCryptographer _dpapi;

		private SecureStringEncryptor()
		{
			try
			{
				_dpapi = new DpapiCryptographer(DataProtectionScope.CurrentUser);
			}
			catch
			{
			}
		}

		public SecureString Key { get; set; }
		public byte[] Entropy { get; set; }

		public SecureString Decrypt(byte[] source)
		{
			if (source is null)
				return null;

			try
			{
				if (Scope<ContinueOnExceptionContext>.Current?.Value.DoNotEncrypt != true)
				{
					try
					{
						source = source.DecryptAes(_key.UnSecure(), _salt, _salt);
					}
					catch (CryptographicException ex)
					{
						if (_dpapi == null)
							throw;

						try
						{
							source = _dpapi.Decrypt(source, Entropy);
						}
						catch (CryptographicException)
						{
							// throws original error
							throw ex;
						}
						catch (PlatformNotSupportedException)
						{
							// throws original error
							throw ex;
						}
					}
				}

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