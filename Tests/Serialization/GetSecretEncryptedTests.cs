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
}
