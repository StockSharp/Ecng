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

	// Baseline data for Symmetric encryption (from .NET 6.0)
	private static readonly byte[] _symmetricKey = [.. Enumerable.Range(0, 32).Select(i => (byte)i)];
	private const string _expectedSymmetricEncrypted = "J/n2QGwXtkYCI9SunYCxryBHsq/xi0CqZHGKbHoxT5v+ObNbnSWo5TIdzRl37lUB6cwItAgEAKZrpj4q3CxfSg==";

	// Baseline data for Asymmetric encryption (from .NET 6.0)
	private const string _asymmetricPublicKey = "AQEBAQEBAAMAAAABAAEAgAAAAN4wMSlXBZ2QGPptB5NrZ3qGdO5xp87Ob+AmskrC3yWYll80VnRybvLFOHF52CKtKmmv6d2Zr91woMIbLNmzmJ8ap5YcQzwdHju9iDDzYDNpkUQbFQ8v0MO2ee0bH2YBDuKYxndm15F+AjbIcy5LI7PEuE2KN0wh0jK7KFIeRkQB";
	private const string _asymmetricPrivateKey = "AEAAAAD/dkBVKPq1F0ClUg9zcIczWKqy4BGRy7iVB2mwJQ9ipgiD/+jnF/sQXPrCNdUrrbIma9pV1goZBV+vk3CjvSqrAEAAAADep//FJe6Mrx5j2ArGrcjRk/mlpj4Cxf4uJWVkLw8XlZ6vHP6wCB1Pct0fn/Aq92MqqNB3OVVi66XF0iIfLEwDAIAAAABT4q/84pz9ItU7XwSYNNmpma/2AjUP0ELOxu1TaoVjJVvIb/MgHcAjqjAoL49ZcDVuF3tdLdQG76nR/v6M5tB8tMlJDo3W5ncinXpUa/cjSfEZXBQW5R8F3anE513CEHYZm8VLxWmIsczkpX7SjFRtsE0cePvkza/ZLtha9LxxzQBAAAAAYhK7z/XTigJLRyyh0ee5f/TiU5nCJP7Y9h3KbV1M8spbKpHskAucz0Ni4XxZQlOEuUtenuV1KvXtLjKkY7gT0QBAAAAAQXmVtRvacnRss1+si5A/JaImH5grVBV1EYzdrABKp9zPtR8Jpio8tEhKzinyLjBhcQIDi4vImDZDoVCRyCj2kQBAAAAAsk5otJnYhZCpdCwb6ge6mvZAesh5UmURR8I8DshRPXY0WvGSRd+ls7OInqo9ljFQU17yD/on9OyW+zqFVta3RgADAAAAAQABAIAAAADeMDEpVwWdkBj6bQeTa2d6hnTucafOzm/gJrJKwt8lmJZfNFZ0cm7yxThxedgirSppr+ndma/dcKDCGyzZs5ifGqeWHEM8HR47vYgw82AzaZFEGxUPL9DDtnntGx9mAQ7imMZ3ZteRfgI2yHMuSyOzxLhNijdMIdIyuyhSHkZEAQ==";
	private const string _expectedAsymmetricEncrypted = "b+XSFHsxM5dzyjYNoU3faRs7FqekdaZkGB56uWt50wQ9oul4UAxn1hovfPL/OtswVc+K9zC/lsdw1gYFX7nk+UEEgyFfqqpgVSYEXXHm+1aJyEx/KKJlomUevP+uETKoLbty7WBFmhqJGq9jlZGpNJ8PTVJeiEQhoynsgwTodWc=";
	private const string _expectedAsymmetricSignature = "HRHKlzGzTr3ynoTwxRCcOiuxadnIayGbxKkN6f9JqSW4m4GxPmvoFOE5UyobF73xCJGPo3W6gmjtiqVWuxqNq19gTJ/gOM5chhjummuNmWwpSndGgUW62G8nxoD7ycYgJpiZCr8vb+mtT+6Wuiy/FY3U8gfJ1dysHpgZwZXslik=";

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
	[Description("Verifies CryptoAlgorithm.Create(Symmetric) can decrypt data from .NET 6.0")]
	public void SymmetricCrypto_DecryptBaselineData()
	{
		// IMPORTANT: This test verifies CROSS-VERSION compatibility:
		// - On .NET 6: CryptoAlgorithm.Create uses SymmetricAlgorithm.Create("AES")
		// - On .NET 10: CryptoAlgorithm.Create uses Aes.Create()
		// - Test decrypts data encrypted on .NET 6 = formats are compatible
		// NOTE: We don't test encryption matching because Symmetric encryption uses random IV each time

		// Arrange - Use encrypted data from .NET 6.0
		var encryptedBytes = _expectedSymmetricEncrypted.Base64();

		// Act
		var symmetricAlgo = CryptoAlgorithm.Create(AlgorithmTypes.Symmetric, _symmetricKey);
		var decrypted = symmetricAlgo.Decrypt(encryptedBytes);
		var decryptedText = decrypted.UTF8();

		// Assert
		decryptedText.AssertEqual(_plainText,
			"Data encrypted on .NET 6.0 with Symmetric algorithm must decrypt successfully on .NET 10+. " +
			"This verifies that Aes.Create() in .NET 10+ is compatible with SymmetricAlgorithm.Create('AES') from .NET 6.");

		// Verify encryption/decryption cycle works
		var plainBytes = _plainText.UTF8();
		var encrypted = symmetricAlgo.Encrypt(plainBytes);
		var decrypted2 = symmetricAlgo.Decrypt(encrypted);
		decrypted2.UTF8().AssertEqual(_plainText,
			"Full encryption/decryption cycle must work correctly");

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

	[TestMethod]
	[Description("Verifies CryptoAlgorithm.Create(Asymmetric) can decrypt data from .NET 6.0")]
	public void AsymmetricCrypto_DecryptBaselineData()
	{
		// IMPORTANT: This test verifies CROSS-VERSION compatibility:
		// - On .NET 6: CryptoAlgorithm.Create uses AsymmetricAlgorithm.Create("RSA") → RSACryptoServiceProvider
		// - On .NET 10: CryptoAlgorithm.Create uses RSA.Create() → RSACng or RSAOpenSsl
		// - Test decrypts data encrypted on .NET 6 = formats are compatible

		// Arrange - Use keys and encrypted data from .NET 6.0
		var publicKey = _asymmetricPublicKey.Base64();
		var privateKey = _asymmetricPrivateKey.Base64();
		var encryptedBytes = _expectedAsymmetricEncrypted.Base64();

		// Act - Decrypt using CryptoAlgorithm.Create (uses RSA.Create() on .NET 10+)
		var decryptAlgo = CryptoAlgorithm.Create(AlgorithmTypes.Asymmetric, publicKey, privateKey);
		var decrypted = decryptAlgo.Decrypt(encryptedBytes);
		var decryptedText = decrypted.UTF8();

		// Assert
		decryptedText.AssertEqual(_plainText,
			"Data encrypted on .NET 6.0 with Asymmetric algorithm must decrypt successfully on .NET 10+. " +
			"This verifies that RSA.Create() in .NET 10+ works correctly with RSACryptoServiceProvider data from .NET 6.");

		decryptAlgo.Dispose();
	}

	[TestMethod]
	[Description("Verifies CryptoAlgorithm.Create(Asymmetric) can encrypt data that matches .NET 6.0 format")]
	public void AsymmetricCrypto_EncryptWithBaselineKeys()
	{
		// Arrange - Use keys from .NET 6.0
		var plainBytes = _plainText.UTF8();
		var publicKey = _asymmetricPublicKey.Base64();
		var privateKey = _asymmetricPrivateKey.Base64();

		// Act - Encrypt and decrypt with same keys
		var encryptAlgo = CryptoAlgorithm.Create(AlgorithmTypes.Asymmetric, publicKey, default);
		var decryptAlgo = CryptoAlgorithm.Create(AlgorithmTypes.Asymmetric, publicKey, privateKey);

		var encrypted = encryptAlgo.Encrypt(plainBytes);
		var decrypted = decryptAlgo.Decrypt(encrypted);
		var decryptedText = decrypted.UTF8();

		// Assert - Full cycle works
		decryptedText.AssertEqual(_plainText,
			"Asymmetric encryption/decryption cycle with .NET 6.0 keys must work correctly");

		encryptAlgo.Dispose();
		decryptAlgo.Dispose();
	}

	[TestMethod]
	[Description("Verifies CreateAsymmetricVerifier can verify signatures from .NET 6.0")]
	public void CreateAsymmetricVerifier_VerifyBaselineSignature()
	{
		// IMPORTANT: This test verifies CROSS-VERSION compatibility:
		// - On .NET 6: CreateAsymmetricVerifier uses AsymmetricAlgorithm.Create("RSA") → RSACryptoServiceProvider
		// - On .NET 10: CreateAsymmetricVerifier uses RSA.Create() → RSACng or RSAOpenSsl
		// - Signature format (PKCS#1 with SHA256) is standardized and compatible across versions
		// - Test verifies signature created on .NET 6 can be verified on .NET 10

		// Arrange - Use public key and signature from .NET 6.0
		var publicKey = _asymmetricPublicKey.Base64();
		var signatureBytes = _expectedAsymmetricSignature.Base64();
		var dataToVerify = _plainText.UTF8();

		// Act - Verify signature using CreateAsymmetricVerifier (uses RSA.Create() on .NET 10+)
		var verifierAlgo = CryptoAlgorithm.CreateAsymmetricVerifier(publicKey);
		var isValid = verifierAlgo.VerifySignature(dataToVerify, signatureBytes);

		// Verify with wrong data should fail
		var wrongData = "Wrong data".UTF8();
		var isInvalid = verifierAlgo.VerifySignature(wrongData, signatureBytes);

		// Assert
		isValid.AssertTrue("Signature created on .NET 6.0 must be verified successfully on .NET 10+. " +
			"This verifies that CreateAsymmetricVerifier with RSA.Create() in .NET 10+ works correctly with .NET 6 signatures.");
		isInvalid.AssertFalse("Invalid signature must be rejected");

		verifierAlgo.Dispose();
	}

	[TestMethod]
	[Description("Verifies CreateAsymmetricVerifier can create and verify signatures with .NET 6.0 keys")]
	public void CreateAsymmetricVerifier_SignWithBaselineKeys()
	{
		// Arrange - Use keys from .NET 6.0
		var publicKey = _asymmetricPublicKey.Base64();
		var privateKey = _asymmetricPrivateKey.Base64();
		var dataToSign = _plainText.UTF8();

		// Act - Create signature and verify it
		var signerAlgo = CryptoAlgorithm.Create(AlgorithmTypes.Asymmetric, publicKey, privateKey);
		var signature = signerAlgo.CreateSignature(dataToSign, SHA256.Create);

		var verifierAlgo = CryptoAlgorithm.CreateAsymmetricVerifier(publicKey);
		var isValid = verifierAlgo.VerifySignature(dataToSign, signature);

		// Assert
		isValid.AssertTrue("Signature created with .NET 6.0 keys must be verified correctly");

		signerAlgo.Dispose();
		verifierAlgo.Dispose();
	}
}