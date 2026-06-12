namespace Ecng.Tests.Security;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Ecng.Security;
using Ecng.Security.Cryptographers;

[TestClass]
public class SecretTests : BaseTestClass
{
	private const string _correctPwd = "mpoi3e/4nn3(&T(*^R";
	private const string _incorrectPwd = "mpo3e/4nn3(&T(*^R";

	private static CryptoAlgorithm CreateAlgo() => CryptoAlgorithm.Create(AlgorithmTypes.Hash);

	[TestMethod]
	public void EqualsTest()
	{
		var algo = CreateAlgo();

		var pwd1 = _correctPwd.CreateSecret(algo);
		var pwd2 = _correctPwd.CreateSecret(algo);

		pwd1.Equals(pwd1).AssertTrue();
		pwd1.Equals(pwd2).AssertFalse();

		pwd1.IsValid(_correctPwd, algo).AssertTrue();
		pwd2.IsValid(_correctPwd, algo).AssertTrue();
	}

	[TestMethod]
	public void NonEqualsTest()
	{
		var algo = CreateAlgo();

		var pwd1 = _correctPwd.CreateSecret(algo);
		var pwd2 = _incorrectPwd.CreateSecret(algo);

		pwd1.Equals(pwd2).AssertFalse();
	}

	[TestMethod]
	public void IsValidTest()
	{
		var algo = CreateAlgo();

		_correctPwd.CreateSecret(algo).IsValid(_correctPwd, algo).AssertTrue();
		_correctPwd.CreateSecret(algo).IsValid(_incorrectPwd, algo).AssertFalse();
	}

	[TestMethod]
	public void SecureStringTest()
	{
		var algo = CreateAlgo();

		var correctPwd = _correctPwd.Secure();
		var incorrectPwd = _incorrectPwd.Secure();

		correctPwd.CreateSecret(algo).IsValid(correctPwd, algo).AssertTrue();
		correctPwd.CreateSecret(algo).IsValid(incorrectPwd, algo).AssertFalse();
	}

	[TestMethod]
	public void DecryptReadStreamTest()
	{
		var initVectorBytes = "ss14fgty650h8u82".ASCII();
		var txt = File.ReadAllText("../../../Resources/encrypted_config.bin").Base64().Decrypt("qwerty", initVectorBytes, initVectorBytes).UTF8();

		txt.Length.AssertEqual(65735);
		txt.UTF8().Md5().AssertEqual("96FB3B2D0226B0A18903E929E1238557");
	}

	[TestMethod]
	public void RsaParameters_ConvertToAndFromBytes()
	{
		var rsa = CryptoHelper.GenerateRsa();
		var bytes = rsa.FromRsa();
		var restored = bytes.ToRsa();
		restored.Modulus.AssertEqual(rsa.Modulus);
		restored.Exponent.AssertEqual(rsa.Exponent);
	}

	[TestMethod]
	public void RsaParameters_PublicPart()
	{
		var rsa = CryptoHelper.GenerateRsa();
		var pub = rsa.PublicPart();
		pub.Exponent.AssertEqual(rsa.Exponent);
		pub.Modulus.AssertEqual(rsa.Modulus);
	}

	[TestMethod]
	public void EncryptDecrypt_Aes()
	{
		var plain = "test data".UTF8();
		var salt = "salt123456789012".ASCII();
		var iv = "iv12345678901234".ASCII();
		var pass = "password";
		var encrypted = plain.Encrypt(pass, salt, iv);
		var decrypted = encrypted.Decrypt(pass, salt, iv);
		decrypted.AssertEqual(plain);
	}

	[TestMethod]
	public void EncryptDecrypt_TransformAes()
	{
		var plain = "test data2".UTF8();
		var salt = "salt123456789012".ASCII();
		var iv = "iv12345678901234".ASCII();
		var pass = "password2";
		var encrypted = plain.EncryptAes(pass, salt, iv);
		var decrypted = encrypted.DecryptAes(pass, salt, iv);
		decrypted.AssertEqual(plain);
	}

	[TestMethod]
	public void Encrypt_ThrowsOnNulls()
	{
		var salt = "salt123456789012".ASCII();
		var iv = "iv12345678901234".ASCII();
		ThrowsExactly<ArgumentNullException>(() => ((byte[])null).Encrypt("pass", salt, iv));
		ThrowsExactly<ArgumentNullException>(() => "data".UTF8().Encrypt(null, salt, iv));
	}

	[TestMethod]
	public void IsValid_SecureString_ThrowsOnNull()
	{
		var algo = CreateAlgo();

		Secret secret = _correctPwd.CreateSecret(algo);
		ThrowsExactly<ArgumentNullException>(() => secret.IsValid((SecureString)null, algo));
	}

	[TestMethod]
	public void IsValid_String_ThrowsOnNull()
	{
		var algo = CreateAlgo();

		Secret secret = _correctPwd.CreateSecret(algo);
		ThrowsExactly<ArgumentNullException>(() => secret.IsValid((string)null, algo));
	}

	[TestMethod]
	public void CreateSecret_WithSecretArg()
	{
		var algo = CreateAlgo();

		var secret1 = _correctPwd.CreateSecret(algo);
		var secret2 = _correctPwd.CreateSecret(secret1, algo);
		secret2.IsValid(_correctPwd, algo).AssertTrue();
	}

	[TestMethod]
	public void CreateSecret_ThrowsOnNull()
	{
		var algo = CreateAlgo();

		SecureString s = null;
		ThrowsExactly<ArgumentNullException>(() => s.CreateSecret(algo));
	}

	[TestMethod]
	public void Md5_Sha256_Sha512_Correctness()
	{
		var data = "abc".UTF8();
		// Проверка корректности хэшей
		data.Md5().AssertEqual("900150983CD24FB0D6963F7D28E17F72");
		data.Sha256().AssertEqual("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD");
		data.Sha512().AssertEqual("DDAF35A193617ABACC417349AE20413112E6FA4E89A97EA20A9EEEE64B55D39A2192992A274FC1A836BA3C23A3FEEBBD454D4423643CE80E2A9AC94FA54CA49F");
	}

	[TestMethod]
	public void CryptoAlgorithmHashKeyAffectsDigest()
	{
		var data = "payload".UTF8();

		using var first = CryptoAlgorithm.Create(AlgorithmTypes.Hash, "first-key".UTF8());
		using var second = CryptoAlgorithm.Create(AlgorithmTypes.Hash, "second-key".UTF8());

		first.Encrypt(data).SequenceEqual(second.Encrypt(data)).AssertFalse();
	}

	[TestMethod]
	public void CryptoAlgorithmHashKeyAffectsDigestForDifferentKeyLengths()
	{
		var data = "payload".UTF8();

		using var shortKey = CryptoAlgorithm.Create(AlgorithmTypes.Hash, "k1".UTF8());
		using var longKey = CryptoAlgorithm.Create(AlgorithmTypes.Hash, "longer-key-material".UTF8());

		shortKey.Encrypt(data).SequenceEqual(longKey.Encrypt(data)).AssertFalse();
	}

	[TestMethod]
	public void SymmetricCryptographer_GeneratesFreshIvPerEncryption()
	{
		using var cryptographer = new SymmetricCryptographer(Aes.Create(), Enumerable.Range(0, 32).Select(i => (byte)i).ToArray());
		var plaintext = "same plaintext block".UTF8();

		var first = cryptographer.Encrypt(plaintext);
		var second = cryptographer.Encrypt(plaintext);

		first.Take(16).SequenceEqual(second.Take(16)).AssertFalse();
	}

	[TestMethod]
	public void SymmetricCryptographer_GeneratesFreshCiphertextForRepeatedMessages()
	{
		using var cryptographer = new SymmetricCryptographer(Aes.Create(), Enumerable.Range(0, 32).Select(i => (byte)(255 - i)).ToArray());
		var plaintext = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();

		var first = cryptographer.Encrypt(plaintext);
		var second = cryptographer.Encrypt(plaintext);

		first.SequenceEqual(second).AssertFalse();
	}

	[TestMethod]
	public void CreateSecret_CopiesSaltAndAlgo()
	{
		var salt = TypeHelper.GenerateSalt(CryptoHelper.DefaultSaltSize);
		var algo = CryptoAlgorithm.Create(AlgorithmTypes.Hash);
		var secret1 = _correctPwd.CreateSecret(salt, algo);
		var secret2 = _correctPwd.CreateSecret(secret1, algo);
		secret2.Salt.AssertEqual(secret1.Salt);
	}

	[TestMethod]
	public void CreateSecret_EmptyString_Throws()
	{
		var algo = CreateAlgo();

		ThrowsExactly<ArgumentNullException>(() => "".CreateSecret(algo));
	}

	[TestMethod]
	public void CreateSecret_NullString_Throws()
	{
		var algo = CreateAlgo();
		
		ThrowsExactly<ArgumentNullException>(() => ((string)null).CreateSecret(algo));
	}

	[TestMethod]
	public void CreateSecret_NullSalt_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() => _correctPwd.CreateSecret(null));
	}

	[TestMethod]
	public void CreateSecret_NullSecret_Throws()
	{
		var algo = CreateAlgo();

		ThrowsExactly<ArgumentNullException>(() => _correctPwd.CreateSecret((Secret)null, algo));
	}

	[TestMethod]
	public void CreateSecret_SecureString_Correct()
	{
		var algo = CreateAlgo();

		var s = _correctPwd.Secure();
		var secret = s.CreateSecret(algo);
		secret.IsValid(_correctPwd, algo).AssertTrue();
	}

	[TestMethod]
	public void CreateSecret_SecureString_Null_Throws()
	{
		var algo = CreateAlgo();

		SecureString s = null;
		ThrowsExactly<ArgumentNullException>(() => s.CreateSecret(algo));
	}

	[TestMethod]
	public void IsValid_NullSecret_Throws()
	{
		var algo = CreateAlgo();
		
		ThrowsExactly<ArgumentNullException>(() => ((Secret)null).IsValid(_correctPwd, algo));
	}

	[TestMethod]
	public void IsValid_NullPassword_Throws()
	{
		var algo = CreateAlgo();

		var secret = _correctPwd.CreateSecret(algo);
		ThrowsExactly<ArgumentNullException>(() => secret.IsValid((string)null, algo));
	}

	[TestMethod]
	public void IsValid_NullSecureString_Throws()
	{
		var algo = CreateAlgo();

		var secret = _correctPwd.CreateSecret(algo);
		ThrowsExactly<ArgumentNullException>(() => secret.IsValid((SecureString)null, algo));
	}

	[TestMethod]
	public void EncryptDecryptAes_RandomData()
	{
		var plain = "random data".UTF8();
		var salt = "salt123456789012".ASCII();
		var iv = "iv12345678901234".ASCII();
		var pass = "password";
		var encrypted = plain.EncryptAes(pass, salt, iv);
		var decrypted = encrypted.DecryptAes(pass, salt, iv);
		decrypted.AssertEqual(plain);
	}

	[TestMethod]
	public void EncryptDecrypt_NullPlain_Throws()
	{
		var salt = "salt123456789012".ASCII();
		var iv = "iv12345678901234".ASCII();
		ThrowsExactly<ArgumentNullException>(() => ((byte[])null).Encrypt("pass", salt, iv));
	}

	[TestMethod]
	public void EncryptDecrypt_NullPass_Throws()
	{
		var salt = "salt123456789012".ASCII();
		var iv = "iv12345678901234".ASCII();
		ThrowsExactly<ArgumentNullException>(() => "data".UTF8().Encrypt(null, salt, iv));
	}

	[TestMethod]
	public void ToRsa_Null_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() => ((byte[])null).ToRsa());
	}

	[TestMethod]
	public void FromRsa_ToRsa_Correctness()
	{
		var rsa = CryptoHelper.GenerateRsa();
		var bytes = rsa.FromRsa();
		var restored = bytes.ToRsa();
		restored.Modulus.AssertEqual(rsa.Modulus);
		restored.Exponent.AssertEqual(rsa.Exponent);
	}

	/// <summary>
	/// Verifies that GetHashCode updates when Hash/Salt are changed via public setters.
	/// </summary>
	[TestMethod]
	public void HashCode_ShouldUpdateWhenDataChanges()
	{
		var secret = new Secret
		{
			Hash = [1, 2, 3],
			Salt = [4, 5, 6]
		};

		var hash1 = secret.GetHashCode();

		// Change data via public setter
		secret.Hash = [7, 8, 9];

		var hash2 = secret.GetHashCode();

		hash1.AssertNotEqual(hash2, "GetHashCode should update when Hash changes");
	}

	#region AsymmetricCryptographer Tests

	/// <summary>
	/// Verifies that AsymmetricCryptographer uses separate RSA instances for public and private keys.
	/// When both keys are provided, encryption should use public key and decryption should use private key.
	/// </summary>
	[TestMethod]
	public void AsymmetricCryptographer_SeparateKeys_EncryptDecryptWorks()
	{
		var rsa = CryptoHelper.GenerateRsa();
		var publicKeyBytes = rsa.PublicPart().FromRsa();
		var privateKeyBytes = rsa.FromRsa();

		using var algo = System.Security.Cryptography.RSA.Create();
		using var cryptographer = new AsymmetricCryptographer(algo, publicKeyBytes, privateKeyBytes);

		var plainText = "Hello, World!"u8.ToArray();

		// Encrypt with public key, decrypt with private key
		var encrypted = cryptographer.Encrypt(plainText);
		var decrypted = cryptographer.Decrypt(encrypted);

		decrypted.AssertEqual(plainText, "Decrypted text should match original");
	}

	/// <summary>
	/// Verifies that encryption uses public key (not private key).
	/// Data encrypted with public key should be decryptable with private key.
	/// </summary>
	[TestMethod]
	public void AsymmetricCryptographer_EncryptUsesPublicKey()
	{
		var rsa = CryptoHelper.GenerateRsa();
		var publicKeyBytes = rsa.PublicPart().FromRsa();
		var privateKeyBytes = rsa.FromRsa();

		using var algo1 = System.Security.Cryptography.RSA.Create();
		using var cryptographer = new AsymmetricCryptographer(algo1, publicKeyBytes, privateKeyBytes);

		var plainText = "Test message"u8.ToArray();
		var encrypted = cryptographer.Encrypt(plainText);

		// Decrypt with a separate RSA instance using only private key
		using var algo2 = System.Security.Cryptography.RSA.Create();
		algo2.ImportParameters(privateKeyBytes.ToRsa());
		var decrypted = algo2.Decrypt(encrypted, System.Security.Cryptography.RSAEncryptionPadding.Pkcs1);

		decrypted.AssertEqual(plainText, "Data encrypted with public key should be decryptable with private key");
	}

	[TestMethod]
	public void AsymmetricCryptographer_EncryptUsesConstructorPublicKey()
	{
		using var recipient = RSA.Create(2048);
		using var local = RSA.Create(2048);
		using var algo = RSA.Create();
		using var cryptographer = new AsymmetricCryptographer(
			algo,
			recipient.ExportParameters(false).FromRsa(),
			local.ExportParameters(true).FromRsa());

		var encrypted = cryptographer.Encrypt("secret".UTF8());

		recipient.Decrypt(encrypted, RSAEncryptionPadding.Pkcs1).UTF8().AssertEqual("secret");
	}

	[TestMethod]
	public void AsymmetricCryptographer_VerifiesSha384SignatureItCreated()
	{
		using var rsa = RSA.Create(2048);
		using var algo = RSA.Create();
		using var cryptographer = new AsymmetricCryptographer(
			algo,
			rsa.ExportParameters(false).FromRsa(),
			rsa.ExportParameters(true).FromRsa());
		var data = "signed".UTF8();
		var signature = cryptographer.CreateSignature(data, SHA384.Create);

		cryptographer.VerifySignature(data, signature).AssertTrue();
	}

	[TestMethod]
	public void AsymmetricCryptographer_VerifiesSha512SignatureItCreated()
	{
		using var rsa = RSA.Create(2048);
		using var algo = RSA.Create();
		using var cryptographer = new AsymmetricCryptographer(
			algo,
			rsa.ExportParameters(false).FromRsa(),
			rsa.ExportParameters(true).FromRsa());
		var data = "signed-sha512".UTF8();
		var signature = cryptographer.CreateSignature(data, SHA512.Create);

		cryptographer.VerifySignature(data, signature).AssertTrue();
	}

	/// <summary>
	/// Regression test for <see cref="X509Cryptographer"/>: ensures a public-only certificate
	/// supports Encrypt and disposes cleanly. (Was: the missing private key was wrapped into a
	/// non-null wrapper whose <c>DisposeManaged</c> dereferenced a null algorithm, throwing
	/// <see cref="NullReferenceException"/>; now the wrapper is left null and <c>Value?.Clear()</c>
	/// guards the dispose - Security\Cryptographers\AsymmetricCryptographer.cs ctor and DisposeManaged.)
	/// </summary>
	[TestMethod]
	public void X509Cryptographer_PublicOnlyCertificate_EncryptsAndDisposesWithoutNre()
	{
		using var key = RSA.Create(2048);

		var request = new CertificateRequest(
			"CN=Ecng.Tests.Security",
			key,
			HashAlgorithmName.SHA256,
			RSASignaturePadding.Pkcs1);

		var notBefore = new DateTimeOffset(DateTime.UtcNow.AddDays(-1));
		var notAfter = new DateTimeOffset(DateTime.UtcNow.AddDays(1));

		using var fullCert = request.CreateSelfSigned(notBefore, notAfter);

		// Reload as a public-only certificate: GetRSAPrivateKey() returns null.
		var publicBytes = fullCert.Export(X509ContentType.Cert);

#if NET8_0_OR_GREATER
		using var publicOnly = X509CertificateLoader.LoadCertificate(publicBytes);
#else
		using var publicOnly = new X509Certificate2(publicBytes);
#endif

		// Sanity: the reloaded certificate indeed has no private key.
		(publicOnly.GetRSAPrivateKey() is null).AssertTrue("Reloaded certificate must be public-only.");

		var plainText = "public-only".UTF8();

		// Encrypt with the public key (must work) and dispose without throwing.
		byte[] encrypted;

		using (var cryptographer = new X509Cryptographer(publicOnly))
		{
			encrypted = cryptographer.Encrypt(plainText);
		}

		(encrypted is not null && encrypted.Length > 0).AssertTrue("Encryption with public-only certificate must produce ciphertext.");

		// And confirm the ciphertext is genuinely the recipient's: it decrypts with the original private key.
		key.Decrypt(encrypted, RSAEncryptionPadding.Pkcs1).AssertEqual(plainText);
	}

	/// <summary>
	/// Regression test for <see cref="CryptoHelper.Encrypt(byte[], string, byte[], byte[])"/>: ensures
	/// the AES key derivation is hardened, so the helper output no longer matches the weak
	/// SHA1/1000-iteration derivation. (Was: PBKDF2-HMAC-SHA1 with only 1000 iterations; now
	/// PBKDF2-HMAC-SHA256 with 100000 iterations - Security\CryptoHelper.cs _derivationIterations and
	/// _derivationHash, with the old SHA1/1000 values kept only as a decryption fallback.)
	/// </summary>
	[TestMethod]
	public void CryptoHelper_Encrypt_UsesHardenedKeyDerivation()
	{
		var plain = "key derivation must be hardened".UTF8();
		var passPhrase = "correct horse battery staple";
		var salt = TypeHelper.GenerateSalt(16);
		var iv = "iv12345678901234".ASCII();

		var actual = plain.Encrypt(passPhrase, salt, iv);

		// Reproduce the WEAK derivation the finding describes: PBKDF2-HMAC-SHA1, 1000 iterations, 256-bit key.
		const int weakIterations = 1000;
		const int keySizeBytes = 256 / 8;

		var weakKey = Rfc2898DeriveBytes.Pbkdf2(passPhrase, salt, weakIterations, HashAlgorithmName.SHA1, keySizeBytes);

		using var aes = Aes.Create();
		aes.BlockSize = 128;
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;

		using var encryptor = aes.CreateEncryptor(weakKey, iv);
		using var memoryStream = new MemoryStream();
		using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
		{
			cryptoStream.Write(plain, 0, plain.Length);
			cryptoStream.FlushFinalBlock();
		}

		var weakCipher = memoryStream.ToArray();

		// With the weak derivation the helper would reproduce this ciphertext exactly; the hardened
		// derivation (different hash and iteration count) makes the two diverge.
		actual.SequenceEqual(weakCipher).AssertFalse("CryptoHelper.Encrypt must not use the weak PBKDF2-SHA1/1000 key derivation.");
	}

	#endregion
}
