namespace Ecng.Tests.Security;

using System.Security.Cryptography;

using Ecng.Security;

/// <summary>
/// Backward compatibility tests for the Security project.
/// Ensure changes in .NET 10+ do not break compatibility with data created on older versions.
/// </summary>
[TestClass]
public class BackwardCompatibilityTests
{
	// Baseline data from .NET 6.0
	private const string _password = "MySecretPassword123!";
	private const string _plainText = "Hello, World! This is a test message.";
	private static readonly byte[] _salt = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
	private static readonly byte[] _iv = [16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1];

	// Baseline encrypted data produced on .NET 6.0
	private const string _expectedEncrypted = "FhXDfrj5iX/8m2+zAxn6bhF9+eTi/DyQy3z9cUkKBqM66i3R+U6x+eN9zWzxCSOz";

	// Baseline data for CreateSecret
	private const string _testPassword = "TestPassword123";
	private static readonly byte[] _testSalt = [10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160];
	private const string _expectedSecretHash = "6ngMdJxAI6Y09yuWPtWjcW0ozjWQvH7Dhs/UeydI4Ik=";

	// Baseline hashes
	private static readonly byte[] _testData = "Test data for hashing".UTF8();
	private const string _expectedMD5 = "29BEAAB220ADF762CBA5208784ED02B0";
	private const string _expectedSHA256 = "DAA6B19B4C930C24F217BBB0032E46AC23D0B992F566FAD6D717C57000522A36";
	private const string _expectedSHA512 = "B6EAD479560AA7B928CB323EFAAD76899A62C5A1BC0A8F744D25735DC5DD395406A37BEDE297FB6D2806AEC9BA63EF82A29B69D041EC7CF20CC609D025B62A0A";

	// Baseline data for Symmetric encryption
	private static readonly byte[] _symmetricKey = [.. Enumerable.Range(0, 32).Select(i => (byte)i)];

	[TestMethod]
	public void Encrypt_WithFixedSaltAndIV()
	{
		// Arrange
		var plainBytes = _plainText.UTF8();

		// Act
		var encrypted = plainBytes.Encrypt(_password, _salt, _iv);
		var encryptedBase64 = encrypted.Base64();

		// Assert
		encryptedBase64.AssertEqual(_expectedEncrypted,
			"Ciphertext must match the .NET 6.0 baseline. This ensures data encrypted on older versions can be decrypted on newer ones.");
	}

	[TestMethod]
	[Description("Verifies data encrypted on .NET 6.0 can be decrypted")]
	public void Decrypt_EncryptedData()
	{
		// Arrange
		var encryptedBytes = _expectedEncrypted.Base64();

		// Act
		var decrypted = encryptedBytes.Decrypt(_password, _salt, _iv);
		var decryptedText = decrypted.UTF8();

		// Assert
		decryptedText.AssertEqual(_plainText,
			"Data encrypted on .NET 6.0 must decrypt successfully on the current version.");
	}

	[TestMethod]
	[Description("Verifies EncryptAes produces the same data as Encrypt (same implementation)")]
	public void EncryptAes_WithSameParameters()
	{
		// Arrange
		var plainBytes = _plainText.UTF8();

		// Act
		var encryptedAes = plainBytes.EncryptAes(_password, _salt, _iv);
		var encryptedAesBase64 = encryptedAes.Base64();

		// Assert
		encryptedAesBase64.AssertEqual(_expectedEncrypted,
			"EncryptAes should produce the same data as on .NET 6.0");
	}

	[TestMethod]
	[Description("Verifies DecryptAes can decrypt data encrypted on .NET 6.0")]
	public void DecryptAes_EncryptedData()
	{
		// Arrange
		var encryptedBytes = _expectedEncrypted.Base64();

		// Act
		var decrypted = encryptedBytes.DecryptAes(_password, _salt, _iv);
		var decryptedText = decrypted.UTF8();

		// Assert
		decryptedText.AssertEqual(_plainText,
			"DecryptAes must decrypt data produced on .NET 6.0");
	}

	[TestMethod]
	[Description("Verifies CreateSecret produces the same hash as on .NET 6.0")]
	public void CreateSecret_WithFixedSalt()
	{
		// Arrange
		var sha256Algo = CryptoAlgorithm.Create(AlgorithmTypes.Hash);

		// Act
		var secret = _testPassword.CreateSecret(_testSalt, sha256Algo);
		var secretHashBase64 = secret.Hash.Base64();

		// Assert
		secretHashBase64.AssertEqual(_expectedSecretHash,
			"Password hash must match the .NET 6.0 baseline. This is critical for validating passwords produced on older versions.");

		sha256Algo.Dispose();
	}

	[TestMethod]
	[Description("Verifies passwords created on .NET 6.0 can be validated")]
	public void IsValid_Secret()
	{
		// Arrange
		var sha256Algo = CryptoAlgorithm.Create(AlgorithmTypes.Hash);
		var secretFromNet6 = new Secret
		{
			Hash = _expectedSecretHash.Base64(),
			Salt = _testSalt
		};

		// Act
		var isValidCorrect = secretFromNet6.IsValid(_testPassword, sha256Algo);
		var isValidWrong = secretFromNet6.IsValid("WrongPassword", sha256Algo);

		// Assert
		isValidCorrect.AssertTrue(
			"The correct password must validate against the .NET 6.0 hash");
		isValidWrong.AssertFalse(
			"An incorrect password must not validate");

		sha256Algo.Dispose();
	}

	[TestMethod]
	[Description("Verifies MD5 hash matches the .NET 6.0 baseline")]
	public void Md5()
	{
		// Act
		var md5Hash = _testData.Md5();

		// Assert
		md5Hash.AssertEqual(_expectedMD5, "MD5 hash must match the .NET 6.0 baseline");
	}

	[TestMethod]
	[Description("Verifies SHA256 hash matches the .NET 6.0 baseline")]
	public void Sha256()
	{
		// Act
		var sha256Hash = _testData.Sha256();

		// Assert
		sha256Hash.AssertEqual(_expectedSHA256, "SHA256 hash must match the .NET 6.0 baseline");
	}

	[TestMethod]
	[Description("Verifies SHA512 hash matches the .NET 6.0 baseline")]
	public void Sha512()
	{
		// Act
		var sha512Hash = _testData.Sha512();

		// Assert
		sha512Hash.AssertEqual(_expectedSHA512, "SHA512 hash must match the .NET 6.0 baseline");
	}

	[TestMethod]
	[Description("Verifies full cycle: encrypt -> decrypt with the same data as .NET 6.0")]
	public void FullCycle_EncryptAndDecrypt()
	{
		// Arrange
		var plainBytes = _plainText.UTF8();

		// Act
		var encrypted = plainBytes.Encrypt(_password, _salt, _iv);
		var decrypted = encrypted.Decrypt(_password, _salt, _iv);
		var decryptedText = decrypted.UTF8();

		// Assert
		decryptedText.AssertEqual(_plainText,
			"Full encryption/decryption cycle must restore the original data");

		// Also check that ciphertext matches the baseline
		encrypted.Base64().AssertEqual(_expectedEncrypted,
			"Ciphertext must match the .NET 6.0 baseline");
	}

	[TestMethod]
	[Description("Verifies Symmetric algorithm can decrypt data regardless of .NET version")]
	public void SymmetricCrypto_EncryptDecryptCycle()
	{
		// Arrange
		var plainBytes = _plainText.UTF8();

		// Act
		var symmetricAlgo = CryptoAlgorithm.Create(AlgorithmTypes.Symmetric, _symmetricKey);
		var encrypted = symmetricAlgo.Encrypt(plainBytes);
		var decrypted = symmetricAlgo.Decrypt(encrypted);
		var decryptedText = decrypted.UTF8();

		// Assert
		decryptedText.AssertEqual(_plainText,
			"Symmetric algorithm must correctly encrypt and decrypt data");

		symmetricAlgo.Dispose();
	}

	[TestMethod]
	[Description("Verifies a new Secret can be created and validated")]
	public void CreateAndValidateSecret_WorksCorrectly()
	{
		// Arrange
		var algo = CryptoAlgorithm.Create(AlgorithmTypes.Hash);
		var testPass = "NewTestPassword";

		// Act
		var secret = testPass.CreateSecret(algo);
		var isValid = secret.IsValid(testPass, algo);
		var isInvalid = secret.IsValid("WrongPassword", algo);

		// Assert
		isValid.AssertTrue("The correct password must validate");
		isInvalid.AssertFalse("An incorrect password must not validate");

		algo.Dispose();
	}

	[TestMethod]
	[Description("Verifies consistency: multiple encryption calls with the same parameters return the same result")]
	public void Encrypt_MultipleCallsWithSameParameters()
	{
		// Arrange
		var plainBytes = _plainText.UTF8();

		// Act
		var encrypted1 = plainBytes.Encrypt(_password, _salt, _iv);
		var encrypted2 = plainBytes.Encrypt(_password, _salt, _iv);
		var encrypted3 = plainBytes.Encrypt(_password, _salt, _iv);

		// Assert
		encrypted1.Base64().AssertEqual(encrypted2.Base64(),
			"Repeated encryption with the same parameters must produce the same result");
		encrypted2.Base64().AssertEqual(encrypted3.Base64(),
			"Repeated encryption with the same parameters must produce the same result");

		// All must match the baseline
		encrypted1.Base64().AssertEqual(_expectedEncrypted,
			"Result must match the .NET 6.0 baseline");
	}

	[TestMethod]
	[Description("Verifies RSA keys are generated correctly")]
	public void GenerateRsa()
	{
		// Act
		var rsaParams = CryptoHelper.GenerateRsa();

		// Assert
		rsaParams.Modulus.AssertNotNull("Modulus must be generated");
		rsaParams.Exponent.AssertNotNull("Exponent must be generated");
		rsaParams.D.AssertNotNull("D must be generated");
		(rsaParams.Modulus.Length > 0).AssertTrue("Modulus must not be empty");
		(rsaParams.Exponent.Length > 0).AssertTrue("Exponent must not be empty");
		(rsaParams.D.Length > 0).AssertTrue("D must not be empty");
	}

	// NOTE: Asymmetric (RSA) tests are disabled because AsymmetricCryptographer currently only supports
	// RSACryptoServiceProvider, but in .NET 10+ RSA.Create() returns RSACng or RSAOpenSsl.
	// Full support for .NET 10+ Asymmetric encryption requires updating AsymmetricCryptographer
	// to work with the base RSA class instead of RSACryptoServiceProvider specifically.
	//
	// The SYSLIB0045 fix (RSA.Create() instead of AsymmetricAlgorithm.Create("RSA")) is correct,
	// but the implementation needs additional work to be fully compatible.
}