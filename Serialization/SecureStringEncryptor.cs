namespace Ecng.Serialization;

using System;
using System.Security;
using System.Security.Cryptography;

using Ecng.Common;
using Ecng.Security;

class SecureStringEncryptor
{
	private static readonly Lazy<SecureStringEncryptor> _instance = new(() => new());
	public static SecureStringEncryptor Instance => _instance.Value;

	public SecureString Key { get; set; } = "RClVEDn0O3EUsKqym1qd".Secure();
	public byte[] Entropy { get; set; } = "3hj67-!3".To<byte[]>();

	public SecureString Decrypt(byte[] source)
	{
		if (source is null)
			return null;

		try
		{
			if (Scope<ContinueOnExceptionContext>.Current?.Value.DoNotEncrypt != true)
				source = source.DecryptAes(Key.UnSecure(), Entropy, Entropy);

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

		return plainText.EncryptAes(Key.UnSecure(), Entropy, Entropy);
	}
}