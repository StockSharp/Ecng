namespace Ecng.Tests.Serialization;

using Ecng.Serialization;

[TestClass]
[DoNotParallelize]
public class GetSecretEncryptedTests : BaseTestClass
{
	private static string EncryptAsBase64(string plain)
		=> SecureStringHelper.Encrypt(plain.Secure()).Base64();

	[TestMethod]
	public void Encrypted_True_DecryptsRoundTrip()
	{
		var key = $"ECNG_GETSECRET_ENC_RT_{Guid.NewGuid():N}";
		const string plain = "hunter2";
		var cipher = EncryptAsBase64(plain);

		Environment.SetEnvironmentVariable(key, cipher);
		try
		{
			TryGetSecret(key, encrypted: true).AssertEqual(plain);
		}
		finally
		{
			Environment.SetEnvironmentVariable(key, null);
		}
	}

	[TestMethod]
	public void Encrypted_False_ReturnsRawValue()
	{
		var key = $"ECNG_GETSECRET_ENC_RAW_{Guid.NewGuid():N}";
		const string raw = "plain-text-value";

		Environment.SetEnvironmentVariable(key, raw);
		try
		{
			TryGetSecret(key).AssertEqual(raw);
			TryGetSecret(key, encrypted: false).AssertEqual(raw);
		}
		finally
		{
			Environment.SetEnvironmentVariable(key, null);
		}
	}

	[TestMethod]
	public void Missing_ReturnsNull()
	{
		var key = $"ECNG_GETSECRET_ENC_MISSING_{Guid.NewGuid():N}";

		TryGetSecret(key).AssertNull();
		TryGetSecret(key, encrypted: true).AssertNull();
	}

	private sealed class CountingEncryptor : ISecureStringEncryptor
	{
		public int EncryptCalls;
		public int DecryptCalls;

		public byte[] Encrypt(SecureString value)
		{
			EncryptCalls++;
			return value is null ? null : ("custom:" + value.UnSecure()).To<byte[]>();
		}

		public SecureString Decrypt(byte[] cipher)
		{
			DecryptCalls++;

			if (cipher is null)
				return null;

			var s = cipher.To<string>();
			return (s.StartsWith("custom:") ? s.Substring("custom:".Length) : s).Secure();
		}
	}

	[TestMethod]
	public void Encryptor_Default_IsBuiltInAes()
		=> (SecureStringHelper.Encryptor is SecureStringEncryptor).AssertTrue();

	[TestMethod]
	public void Encryptor_SetNull_Throws()
		=> Throws<ArgumentNullException>(() => SecureStringHelper.Encryptor = null);

	[TestMethod]
	public void Encryptor_Override_IsUsedByEncryptAndDecrypt()
	{
		var original = SecureStringHelper.Encryptor;
		var custom = new CountingEncryptor();

		try
		{
			SecureStringHelper.Encryptor = custom;

			var cipher = SecureStringHelper.Encrypt("hunter2".Secure());
			custom.EncryptCalls.AssertEqual(1);

			SecureStringHelper.Decrypt(cipher).UnSecure().AssertEqual("hunter2");
			custom.DecryptCalls.AssertEqual(1);
		}
		finally
		{
			SecureStringHelper.Encryptor = original;
		}
	}
}
