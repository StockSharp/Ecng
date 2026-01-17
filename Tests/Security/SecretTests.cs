namespace Ecng.Tests.Security;

using System.IO;
using System.Security;

using Ecng.Security;

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
}